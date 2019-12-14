using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;


namespace PrivateWin10
{
    [Flags]
    public enum NameSources : int
    {
        None            = 0x00,
        ReverseDns      = 0x01,
        CachedQuery     = 0x02, // good
        CapturedQuery   = 0x04 // best
    }

    public class HostNameEntry
    {
        public string HostName;
        public DateTime TimeStamp;
    }

    public class DnsInspector : IDisposable
    {
        private DnsQueryWatcher queryWatcher = null;
        private DnsCacheMonitor dnsCacheMonitor = null;
        private HostNameResolver hostNameResolver = null;

        private class HostObserveJob
        {
            public WeakReference target;
            public Action<object, string, NameSources> setter;

            public int processId;
            public IPAddress remoteAddress;

            public NameSources Await = NameSources.None;
            public DateTime timeOut;

            public bool IsValid() { return target.IsAlive && Await != NameSources.None && timeOut > DateTime.Now; }

            public void SetHostName(string hostName, NameSources source)
            {
                object obj = target.Target;
                if (obj != null)
                    setter(obj, hostName, source);
            }
        }

        private MultiValueDictionary<IPAddress, HostObserveJob> ObserverJobs = new MultiValueDictionary<IPAddress, HostObserveJob>();

        DateTime LastCleanupTime = DateTime.Now;

        public DnsInspector()
        {
            queryWatcher = new DnsQueryWatcher();
            queryWatcher.DnsQueryEvent += OnDnsQueryWatched;

            dnsCacheMonitor = new DnsCacheMonitor();
            dnsCacheMonitor.DnsCacheEvent += OnDnsCacheEvent;

            hostNameResolver = new HostNameResolver();
            hostNameResolver.HostNameResolved += OnResolvedHostName;
        }

        public void Dispose()
        {
            queryWatcher.Dispose();
            dnsCacheMonitor.Dispose();
        }

        private void OnDnsQueryWatched(object sender, DnsQueryWatcher.DnsEvent Event)
        {
            foreach (IPAddress remoteAddress in Event.RemoteAddresses)
                OnHostName(remoteAddress, Event.HostName, NameSources.CapturedQuery, Event.ProcessId);

            List<ServiceHelper.ServiceInfo> Services = ServiceHelper.GetServicesByPID(Event.ProcessId);
            ProgramID ProgID = App.engine.GetProgIDbyPID(Event.ProcessId, (Services == null || Services.Count > 1) ? null : Services[0].ServiceName);
            if (ProgID == null)
                App.LogWarning("Watched a DNS query for a terminated process with id {0}", Event.ProcessId);
            else
            {
                Program prog = App.engine.ProgramList.GetProgram(ProgID, true, ProgramList.FuzzyModes.Any);
                prog?.LogDomain(Event.HostName, Event.TimeStamp);
            }
        }

        private void OnDnsCacheEvent(object sender, DnsCacheMonitor.DnsEvent Event)
        {
            //OnHostName(Event.Entry.Address, Event.Entry.HostName, NameSources.CachedQuery); // see comment in Process
        }

        private void OnResolvedHostName(object sender, HostNameResolver.DnsEvent Event)
        {
            OnHostName(Event.RemoteAddress, Event.HostNames.FirstOrDefault(), NameSources.ReverseDns);
        }

        private void OnHostName(IPAddress remoteAddress, string hostName, NameSources source, int processId = 0)
        {
            CloneableList<HostObserveJob> Jobs = ObserverJobs.GetValues(remoteAddress, false);
            if (Jobs == null)
                return;

            foreach (HostObserveJob curJob in Jobs.Clone())
            {
                if (processId == 0 || curJob.processId == processId)
                {
                    curJob.SetHostName(hostName, NameSources.CapturedQuery);
                    curJob.Await &= ~source; // clear the await
                }

                if (!curJob.IsValid())
                    ObserverJobs.Remove(remoteAddress, curJob);
            }
        }

        public void Process()
        {
            // if we are using the DNS proxy we have first hand data and not need to monitor the system DNS cache.
            if(App.engine.DnsProxy == null || App.GetConfigInt("DnsInspector", "AlwaysMonitor", 0) != 0)
                dnsCacheMonitor.SyncCache();

            if (LastCleanupTime.AddMinutes(15) < DateTime.Now) // every 15 minutes
            {
                LastCleanupTime = DateTime.Now;

                queryWatcher.CleanupCache();
                dnsCacheMonitor.CleanupCache();
                hostNameResolver.CleanupCache();
            }

            foreach (var Address in ObserverJobs.Keys.ToList())
            {
                CloneableList<HostObserveJob> curJobs = ObserverJobs[Address];
                for (int i = 0; i < curJobs.Count; i++)
                {
                    HostObserveJob curJob = curJobs[i];

                    // Note: the cache emits events on every record found, if we have to a CNAME -> A -> IP case 
                    //          we need to wait untill all records are in the cache and than properly search it
                    if ((curJob.Await & NameSources.CachedQuery) != 0)
                    {
                        string cachedName = FindMostRecentHost(dnsCacheMonitor.FindHostNames(curJob.remoteAddress));
                        if (cachedName != null)
                            curJob.SetHostName(cachedName, NameSources.CapturedQuery);
                        curJob.Await &= ~NameSources.CachedQuery; // if after one cache update we still dont have a result we wont get it
                    }

                    if (!curJob.IsValid())
                        curJobs.RemoveAt(i--);
                }
                if (curJobs.Count == 0)
                    ObserverJobs.Remove(Address);
            }
        }

        static public string FindMostRecentHost(List<HostNameEntry> cacheEntries)
        {
            if(cacheEntries == null)
                return null;

            HostNameEntry bestEntry = null;
            foreach (var curEntry in cacheEntries)
            {
                if (bestEntry == null || curEntry.TimeStamp > bestEntry.TimeStamp)
                    bestEntry = curEntry;
            }
            return bestEntry?.HostName;
        }

        public void GetHostName(int processId, IPAddress remoteAddress, object target, Action<object, string, NameSources> setter)
        {
            // sanity check
            if (remoteAddress.Equals(IPAddress.Any) || remoteAddress.Equals(IPAddress.IPv6Any))
                return;
            if (remoteAddress.Equals(IPAddress.Loopback) || remoteAddress.Equals(IPAddress.IPv6Loopback))
            {
                setter(target, "localhost", NameSources.ReverseDns);
                return;
            }
            if (NetFunc.IsMultiCast(remoteAddress))
            {
                setter(target, "multicast.arpa", NameSources.ReverseDns);
                return;
            }

            NameSources Await = NameSources.None;

            if (queryWatcher.IsActive())
            {
                string capturedName = FindMostRecentHost(queryWatcher.FindHostNames(processId, remoteAddress));
                if (capturedName == null)
                    Await |= NameSources.CapturedQuery;
                else
                    setter(target, capturedName, NameSources.CapturedQuery);
            }

            if (Await != NameSources.None)
            {
                string cachedName = FindMostRecentHost(dnsCacheMonitor.FindHostNames(remoteAddress));
                if (cachedName == null)
                    Await |= NameSources.CachedQuery;
                else
                    setter(target, cachedName, NameSources.CachedQuery);
            }

            int ReverseResolve = App.GetConfigInt("DnsInspector", "UseReverseDNS", 0);
            if (ReverseResolve == 2 || (ReverseResolve == 1 && (Await & NameSources.CachedQuery) != 0))
            {
                string resolvedName = FindMostRecentHost(hostNameResolver.ResolveHostNames(remoteAddress));
                if (resolvedName == null)
                    Await |= NameSources.ReverseDns;
                else
                    setter(target, resolvedName, NameSources.ReverseDns);
            }

            if (Await != NameSources.None)
            {
                HostObserveJob job = new HostObserveJob() { target = new WeakReference(target), setter = setter, processId = processId, remoteAddress = remoteAddress, Await = Await, timeOut = DateTime.Now.AddSeconds(30) };
                ObserverJobs.Add(remoteAddress, job);
            }
        }

        public void AddDnsCacheEntry(DnsCacheMonitor.DnsCacheEntry curEntry)
        {
            dnsCacheMonitor.AddCacheEntry(curEntry);
        }
    }


    [Serializable()]
    public class WithHost
    {
        public NameSources RemoteHostNameSource = NameSources.None;
        public string RemoteHostName;

        public class ChangeEventArgs : EventArgs
        {
            public string oldName;
            public string HostName;
        }

        public event EventHandler<ChangeEventArgs> HostNameChanged;

        public void UpdateHostName(string name, NameSources source)
        {
            if (source > RemoteHostNameSource) // the bigger the better
            {
                string oldName = RemoteHostName;

                RemoteHostName = name;
                RemoteHostNameSource = source;

                if(oldName == null || !oldName.Equals(name))
                    HostNameChanged?.Invoke(this, new ChangeEventArgs() { oldName = oldName, HostName = name });
            }
        }

        static public readonly Action<object, string, NameSources> HostSetter = (object obj, string name, NameSources source) => {
            WithHost This = (obj as WithHost);
            This.UpdateHostName(name, source);
        };

        public bool Update(WithHost other)
        {
            if (RemoteHostName != null && other.RemoteHostName != null && RemoteHostName.Equals(other.RemoteHostName))
                return false;
            RemoteHostNameSource = other.RemoteHostNameSource;
            RemoteHostName = other.RemoteHostName;
            return true;
        }

        public bool HasHostName()
        {
            return RemoteHostNameSource != NameSources.None;
        }

        public string GetHostName()
        {
            return RemoteHostName;
        }
    }
}
