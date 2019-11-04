using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace PrivateWin10
{
    public class DnsInspector
    {
        Microsoft.O365.Security.ETW.UserTrace userTrace;
        Microsoft.O365.Security.ETW.Provider dnsCaptureProvider;
        Thread userThread;

        class DnsCacheEntry
        {
            public DnsCacheEntry()
            {
                Counter = 0;
            }

            public int Counter;
            //public UInt64 LastSeen; // todo: xxx
        };

        // map<int, multi_map<IPAddress, TEntry>>
        Dictionary<int, Dictionary<IPAddress, Dictionary<string, DnsCacheEntry>>> dnsQueryCache = new Dictionary<int, Dictionary<IPAddress, Dictionary<string, DnsCacheEntry>>>();

        [Serializable()]
        public class DnsEvent : EventArgs
        {
            public int ProcessId;
            public string HostName;
        }

        public event EventHandler<DnsEvent> DnsQueryEvent;

        public DnsInspector()
        {
            userTrace = new Microsoft.O365.Security.ETW.UserTrace("priv10_UserLogger");
            dnsCaptureProvider = new Microsoft.O365.Security.ETW.Provider(Guid.Parse("{55404E71-4DB9-4DEB-A5F5-8F86E46DDE56}"));
            dnsCaptureProvider.Any = Microsoft.O365.Security.ETW.Provider.AllBitsSet;
            dnsCaptureProvider.OnEvent += OnDnsQueryEvent;
            userTrace.Enable(dnsCaptureProvider);

            userThread = new Thread(() => { userTrace.Start(); });
            userThread.Start();
        }

        public void Dispose()
        {
            userTrace.Stop();
            userThread.Join();
        }


        private void OnDnsQueryEvent(Microsoft.O365.Security.ETW.IEventRecord record)
        {
            // WARNING: this function is called from the worker thread

            if (record.Id != 1001)
                return;

            if (record.GetUInt32("Status", 0) != 0)
                return;

            int ProcessId = (int)record.ProcessId;
            int ThreadId = (int)record.ThreadId;
            var HostName = record.GetUnicodeString("NodeName", null);
            var Results = record.GetUnicodeString("Result", null);

            if (ProcessId == ProcFunc.CurID)
                return; // ignore these events as thay are the result of reverse dns querries....

            /*
            "192.168.163.1" "192.168.163.1;"
            "localhost" "[::1]:8307;127.0.0.1:8307;" <- wtf is this why is there a port?!
            "DESKTOP" "fe80::189a:f1c3:3e87:be81%12;192.168.10.12;"
            "telemetry.malwarebytes.com" "54.149.69.204;54.200.191.52;54.70.191.27;54.149.66.105;54.244.17.248;54.148.98.86;"
            "web.whatsapp.com" "31.13.84.51;"
            */

            App.engine?.RunInEngineThread(() =>
            {
                // Note: this happens in the engine thread

                DnsQueryEvent?.Invoke(this, new DnsEvent() { ProcessId = ProcessId, HostName = HostName });

                foreach (string Result in Results.Split(new char[] { ';' }, StringSplitOptions.RemoveEmptyEntries))
                {
                    IPAddress Address = null;
                    if (!IPAddress.TryParse(Result, out Address) && !IPAddress.TryParse(TextHelpers.Split2(Result, ":", true).Item1, out Address))
                        continue;

                    OnHostName(ProcessId, Address, HostName);

                    Dictionary<IPAddress, Dictionary<string, DnsCacheEntry>> dnsCache = dnsQueryCache.GetOrCreate(ProcessId);

                    Dictionary<string, DnsCacheEntry> cacheEntries = dnsCache.GetOrCreate(Address);

                    DnsCacheEntry cacheEntry = cacheEntries.GetOrCreate(HostName);

                    cacheEntry.Counter++;
                }
            });
        }

        public enum NameSources : int
        {
            None = 0,
            ReverseDNS = 1,
            FoundInCache = 2, // todo
            CapturedQuery = 3
        }

        [Serializable()]
        public class WithHost
        {
            public NameSources RemoteHostNameSource = NameSources.None;
            public string RemoteHostName;

            static public readonly Action<object, string, NameSources> HostSetter = (object obj, string name, NameSources source) => {
                WithHost This = (obj as WithHost);
                if (source > This.RemoteHostNameSource)
                {
                    This.RemoteHostName = name;
                    This.RemoteHostNameSource = source;
                }
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

        public void GetHostName(int processId, IPAddress remoteAddress, object target, Action<object, string, NameSources> setter)
        {
            LookupHostName(remoteAddress, target, setter);
            FindQueryName(processId, remoteAddress, target, setter);
        }

        public class HostLookupJob
        {
            public WeakReference target;
            public Action<object, string, NameSources> setter;
            
            public void SetHostName(string hostName, NameSources source)
            {
                object obj = target.Target;
                if (obj != null)
                    setter(obj, hostName, source);
            }
        }

        public class ReverseDnsEntry
        {
            public UInt64 TimeStamp;
            public string HostName;
            public List<HostLookupJob> PendingJobs = null;
        }

        private Dictionary<IPAddress, ReverseDnsEntry> ReverseDnsCache = new Dictionary<IPAddress, ReverseDnsEntry>();

        public bool LookupHostName(IPAddress remoteAddress, object target, Action<object, string, NameSources> setter)
        {
            if (remoteAddress.Equals(IPAddress.Any) || remoteAddress.Equals(IPAddress.IPv6Any))
                return true;

            if (remoteAddress.Equals(IPAddress.Loopback) || remoteAddress.Equals(IPAddress.IPv6Loopback))
                return true;

            ReverseDnsEntry Entry = null;
            if (ReverseDnsCache.TryGetValue(remoteAddress, out Entry))
            {
                if ((Entry.TimeStamp + (5 * 60 * 1000) >= MiscFunc.GetCurTick()))
                {
                    if(Entry.HostName != null)
                        setter(target, Entry.HostName, NameSources.ReverseDNS);
                    return true;
                }
            }

            if (Entry == null)
            {
                Entry = new ReverseDnsEntry();
                ReverseDnsCache.Add(remoteAddress, Entry);
            }

            if (Entry.PendingJobs == null)
            {
                Entry.PendingJobs = new List<HostLookupJob>();

                //AppLog.Debug("reverse looking up : {0}", remoteAddress.ToString());

                // todo: use matibe api to save oin dns queries
                Dns.BeginGetHostEntry(remoteAddress, (x) =>
                {
                    // WARNING: this function is called from the worker thread

                    //x.AsyncState
                    IPHostEntry hostInfo = null;
                    try
                    {
                        hostInfo = Dns.EndGetHostEntry(x);
                    }
                    catch { }

                    App.engine?.RunInEngineThread(() =>
                    {
                        // Note: this happens in the engine thread

                        Entry.TimeStamp = MiscFunc.GetCurTick();
                        if (hostInfo != null)
                            Entry.HostName = hostInfo.HostName;

                        if (Entry.HostName != null)
                        {
                            foreach(var job in Entry.PendingJobs)
                                job.SetHostName(Entry.HostName, NameSources.ReverseDNS);
                        }
                        Entry.PendingJobs = null;
                    });
                }, null);
            }
            Entry.PendingJobs.Add(new HostLookupJob() { target = new WeakReference(target), setter = setter });

            return false;
        }

        public void CleanupCache()
        {
            Dictionary<int, System.Diagnostics.Process> Processes = new Dictionary<int, System.Diagnostics.Process>();
            foreach (var process in System.Diagnostics.Process.GetProcesses())
                Processes.Add(process.Id, process);

            // Remove all entries for no longer existing processes
            foreach (int ProcessId in dnsQueryCache.Keys.ToList())
            {
                if (!Processes.ContainsKey(ProcessId))
                    dnsQueryCache.Remove(ProcessId);
            }
        }

        public class HostObserveJob
        {
            public WeakReference target;
            public Action<object, string, NameSources> setter;
            public int processId;
            public IPAddress remoteAddress;
            public UInt64 timeOut;

            //public UInt64 CreationTime = MiscFunc.GetCurTick();

            public bool IsValid() { return target.IsAlive && timeOut > MiscFunc.GetUTCTime(); }

            public void SetHostName(string hostName, NameSources source)
            {
                object obj = target.Target;
                if (obj != null)
                    setter(obj, hostName, source);
            }
        }

        private MultiValueDictionary<IPAddress, HostObserveJob> ObserverJobs = new MultiValueDictionary<IPAddress, HostObserveJob>();

        public bool FindQueryName(int processId, IPAddress remoteAddress, object target, Action<object, string, NameSources> setter)
        {
            Dictionary<IPAddress, Dictionary<string, DnsCacheEntry>> dnsCache;
            if (dnsQueryCache.TryGetValue(processId, out dnsCache))
            {
                Dictionary<string, DnsCacheEntry> cacheEntries;
                if (dnsCache.TryGetValue(remoteAddress, out cacheEntries) && cacheEntries.Count > 0)
                {
                    // we found an entry
                    setter(target, cacheEntries.Keys.First(), NameSources.CapturedQuery);
                    return true;
                }
            }

            HostObserveJob job = new HostObserveJob() { target = new WeakReference(target), setter = setter, processId = processId, remoteAddress = remoteAddress, timeOut = MiscFunc.GetUTCTime() + 30 };
            ObserverJobs.Add(remoteAddress, job);
            return false;
        }

        private void OnHostName(int processId, IPAddress remoteAddress, string hostName)
        {
            CloneableList<HostObserveJob> Jobs = ObserverJobs.GetValues(remoteAddress, false);
            if (Jobs == null)
                return;
            foreach (HostObserveJob Job in Jobs.Clone())
            {
                if (Job.processId == processId)
                    Job.SetHostName(hostName, NameSources.CapturedQuery);
                else if (Job.IsValid())
                    continue;
                ObserverJobs.Remove(remoteAddress, Job);
            }
        }
    }
}
