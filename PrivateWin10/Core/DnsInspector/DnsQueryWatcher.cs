using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace PrivateWin10
{
    public class DnsInspectorEtw : IDisposable
    {
        Microsoft.O365.Security.ETW.UserTrace userTrace;
        Microsoft.O365.Security.ETW.Provider dnsCaptureProvider;
        Thread userThread;

        public DnsInspectorEtw(Microsoft.O365.Security.ETW.IEventRecordDelegate OnDnsQueryEvent)
        {
            userTrace = new Microsoft.O365.Security.ETW.UserTrace("priv10_NameResLogger"); // Microsoft-Windows-Winsock-NameResolution
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
    }

    public class DnsQueryWatcher : IDisposable
    {
        public class DnsEvent : EventArgs
        {
            public int ProcessId;
            public string HostName;
            public List<IPAddress> RemoteAddresses;
            public DateTime TimeStamp; // Note: the timestamp is needed as we may get double events with the same timestamp and dont want to count the query twice
        }
        public event EventHandler<DnsEvent> DnsQueryEvent;

        Dictionary<int, Dictionary<IPAddress, Dictionary<string, HostNameEntry>>> dnsQueryCache = new Dictionary<int, Dictionary<IPAddress, Dictionary<string, HostNameEntry>>>();

        DnsInspectorEtw Etw = null;

        public DnsQueryWatcher()
        {
            try
            {
                InitEtw();
                //AppLog.Debug("Successfully initialized DnsInspectorETW");
            }
            catch
            {
                AppLog.Debug("Failed to initialized DnsInspectorETW");
            }
        }

        private void InitEtw()
        {
            Etw = new DnsInspectorEtw(OnDnsQueryEvent);
        }

        public void Dispose()
        {
            if (Etw != null)
                Etw.Dispose();
        }

        public bool IsActive()
        {
            return Etw != null;
        }

        private void OnDnsQueryEvent(Microsoft.O365.Security.ETW.IEventRecord record)
        {
            // WARNING: this function is called from the worker thread

            if (record.Id != 1001 && record.Id != 1004)
                return;

            DateTime TimeStamp = record.Timestamp;
            UInt32 Status = record.GetUInt32("Status", 0);
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

            AppLog.Debug("Etw dns_query {0} => {1} for {2}", HostName, Results, ProcessId);

            App.engine?.RunInEngineThread(() =>
            {
                // Note: this happens in the engine thread

                List<IPAddress> RemoteAddresses = new List<IPAddress>();

                foreach (string Result in Results.Split(new char[] { ';' }, StringSplitOptions.RemoveEmptyEntries))
                {
                    IPAddress Address = null;
                    if (!IPAddress.TryParse(Result, out Address) && !IPAddress.TryParse(TextHelpers.Split2(Result, ":", true).Item1, out Address))
                        continue;

                    RemoteAddresses.Add(Address);

                    Dictionary <IPAddress, Dictionary<string, HostNameEntry>> dnsCache = dnsQueryCache.GetOrCreate(ProcessId);

                    Dictionary<string, HostNameEntry> cacheEntries = dnsCache.GetOrCreate(Address);

                    HostNameEntry cacheEntry;
                    if (!cacheEntries.TryGetValue(HostName, out cacheEntry))
                    {
                        cacheEntry = new HostNameEntry() { HostName = HostName };
                        cacheEntries.Add(HostName, cacheEntry);
                    }

                    cacheEntry.TimeStamp = TimeStamp;
                }

                DnsQueryEvent?.Invoke(this, new DnsEvent() { ProcessId = ProcessId, HostName = HostName, RemoteAddresses = RemoteAddresses, TimeStamp = TimeStamp });
            });
        }

        public List<HostNameEntry> FindHostNames(int processId, IPAddress remoteAddress)
        {
            Dictionary<IPAddress, Dictionary<string, HostNameEntry>> dnsCache;
            if (dnsQueryCache.TryGetValue(processId, out dnsCache))
            {
                Dictionary<string, HostNameEntry> cacheEntries;
                if (dnsCache.TryGetValue(remoteAddress, out cacheEntries) && cacheEntries.Count > 0)
                    return cacheEntries.Values.ToList();
            }
            return null;
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
    } 
}
