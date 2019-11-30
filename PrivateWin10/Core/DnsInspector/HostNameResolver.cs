using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Net;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace PrivateWin10
{
    public class HostNameResolver
    {
        public class DnsEvent : EventArgs
        {
            public IPAddress RemoteAddress;
            public List<string> HostNames;
            //public DateTime TimeStamp;
        }
        public event EventHandler<DnsEvent> HostNameResolved;

        public class ReverseDnsEntry
        {
            public DateTime TimeStamp;
            public List<string> HostNames = new List<string>();
            public bool Pending;
        }
        private Dictionary<IPAddress, ReverseDnsEntry> ReverseDnsCache = new Dictionary<IPAddress, ReverseDnsEntry>();

        public List<HostNameEntry> ResolveHostNames(IPAddress remoteAddress)
        {
            if (remoteAddress.Equals(IPAddress.Any) || remoteAddress.Equals(IPAddress.IPv6Any))
                return new List<HostNameEntry>();

            if (remoteAddress.Equals(IPAddress.Loopback) || remoteAddress.Equals(IPAddress.IPv6Loopback))
                return new List<HostNameEntry>() { new HostNameEntry() { HostName = "localhost", TimeStamp = DateTime.Now } };

            ReverseDnsEntry Entry = null;
            if (ReverseDnsCache.TryGetValue(remoteAddress, out Entry))
            {
                if (Entry.HostNames.Count > 0)
                {
                    List<HostNameEntry> List = new List<HostNameEntry>();
                    foreach (string HostName in Entry.HostNames)
                        List.Add(new HostNameEntry() { HostName = HostName, TimeStamp = Entry.TimeStamp });
                    return List;
                }
                else if (Entry.Pending)
                    return null;
                else if ((Entry.TimeStamp.AddMinutes(1) > DateTime.Now))
                    return new List<HostNameEntry>(); // dont re query if teh last query failed
            }

            if (Entry == null)
            {
                Entry = new ReverseDnsEntry();
                Entry.Pending = true;
                ReverseDnsCache.Add(remoteAddress, Entry);
            }

            AppLog.Debug("reverse looking up : {0}", remoteAddress.ToString());

            Thread task = new Thread(() => {

                // WARNING: this function is called from the worker thread

                string AddressReverse = Address2RevDnsHost(remoteAddress);

                List<string> HostNames = new List<string>();
                try
                {
                    var resultPtr = IntPtr.Zero;
                    if (DnsApi.DnsQuery(AddressReverse, DnsApi.DnsRecordType.PTR, DnsApi.DnsQueryType.DNS_QUERY_NO_HOSTS_FILE, IntPtr.Zero, ref resultPtr, IntPtr.Zero) == DnsApi.ERROR_SUCCESS)
                    {
                        for (var recordIndexPtr = resultPtr; recordIndexPtr != IntPtr.Zero;)
                        {
                            var record = (DnsApi.DnsRecord)Marshal.PtrToStructure(recordIndexPtr, typeof(DnsApi.DnsRecord));
                            int offset = Marshal.OffsetOf(typeof(DnsApi.DnsRecord), "Data").ToInt32();
                            IntPtr data = recordIndexPtr + offset;
                            recordIndexPtr = record.Next;

                            if (record.Type != DnsApi.DnsRecordType.PTR)
                                continue;

                            var ptr = (DnsApi.DnsPTRRecord)Marshal.PtrToStructure(data, typeof(DnsApi.DnsPTRRecord));
                            HostNames.Add(ptr.NameHost);
                        }

                        if (resultPtr != IntPtr.Zero)
                            DnsApi.DnsRecordListFree(resultPtr, DnsApi.DnsFreeType.DnsFreeRecordList);
                    }
                }
                catch (Exception err)
                {
                    AppLog.Exception(err);
                }

                App.engine?.RunInEngineThread(() =>
                {
                    // Note: this happens in the engine thread

                    Entry.TimeStamp = DateTime.Now;
                    Entry.HostNames = HostNames;
                    Entry.Pending = false;

                    HostNameResolved?.Invoke(this, new DnsEvent() { RemoteAddress = remoteAddress, HostNames = HostNames/*, TimeStamp = Entry.TimeStamp*/ });
                });

            });

            task.Start();

            return null;
        }

        static public string Address2RevDnsHost(IPAddress Address)
        {
            // 10.0.0.1 -> 1.0.0.10.in-addr.arpa
            // 2001:db8::1 -> 1.0.0.0.0.0.0.0.0.0.0.0.0.0.0.0.0.0.0.0.0.0.0.0.8.b.d.0.1.0.0.2.ip6.arpa

            string AddressReverse = "";
            byte[] AddressBytes = Address.GetAddressBytes();
            if (Address.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
            {
                for (int i = AddressBytes.Length - 1; i >= 0; i--)
                    AddressReverse += AddressBytes[i].ToString() + ".";
                AddressReverse += "in-addr.arpa";
            }
            else if (Address.AddressFamily == System.Net.Sockets.AddressFamily.InterNetworkV6)
            {
                for (int i = AddressBytes.Length - 1; i >= 0; i--)
                    AddressReverse += (AddressBytes[i] & 0x0F).ToString("x") + "." + ((AddressBytes[i] >> 4) & 0x0F).ToString("x") + ".";
                AddressReverse += "ip6.arpa";
            }
            return AddressReverse;
        }

        public void CleanupCache()
        {
            // remove everythign older than 15 minutes
            DateTime AgeLimit = DateTime.Now.AddMinutes(-15);
            foreach (IPAddress remoteAddress in ReverseDnsCache.Keys.ToList())
            {
                ReverseDnsEntry value = ReverseDnsCache[remoteAddress];
                if (value.TimeStamp < AgeLimit)
                    ReverseDnsCache.Remove(remoteAddress);
            }
        }
    }
}
