using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.InteropServices;
using System.Net;
using MiscHelpers;
using PrivateService;
using System.Runtime.Serialization;

namespace PrivateWin10
{
    public class DnsCacheMonitor: IDisposable
    {
        [Serializable()]
        [DataContract(Name = "DnsCacheEntry", Namespace = "http://schemas.datacontract.org/")]
        public class DnsCacheEntry
        {
            [DataMember()]
            public string HostName;
            [DataMember()]
            public DnsApi.DnsRecordType RecordType = DnsApi.DnsRecordType.ANY;
            [DataMember()]
            public IPAddress Address;
            [DataMember()]
            public String ResolvedString;
            public enum States
            {
                Unknown = 0,
                Resolved,
                Blocked,
                NotFound
            }
            [DataMember()]
            public States State = States.Unknown;
            [DataMember()]
            public DateTime TimeStamp = DateTime.Now;
            [DataMember()]
            public DateTime ExpirationTime;
            public DateTime GetAge()
            {
                DateTime CurrentTime = DateTime.Now;
                if (CurrentTime <= ExpirationTime)
                    return CurrentTime;
                return ExpirationTime;
            }
            public int GetTimeLeft()
            {
                DateTime CurrentTime = DateTime.Now;
                if (CurrentTime <= ExpirationTime)
                    return (int)(ExpirationTime - CurrentTime).TotalSeconds;
                return 0;
            }
            public int GetTTL()
            {
                if (TimeStamp <= ExpirationTime)
                    return (int)(ExpirationTime - TimeStamp).TotalSeconds;
                return 0;
            }
        };
        MultiValueDictionary<string, DnsCacheEntry> dnsCache = new MultiValueDictionary<string, DnsCacheEntry>();
        // reverse maping
        MultiValueDictionary<IPAddress, DnsCacheEntry> cacheByIP = new MultiValueDictionary<IPAddress, DnsCacheEntry>(); // A, AAAA
        MultiValueDictionary<string, DnsCacheEntry> cacheByStr = new MultiValueDictionary<string, DnsCacheEntry>(); // CNAME, DNAME, SRV, MX

        public class DnsEvent : EventArgs
        {
            public DnsCacheEntry Entry;
        }
        public event EventHandler<DnsEvent> DnsCacheEvent;


        DnsApi.DnsGetCacheDataTable DnsGetCacheDataTable_I;
        IntPtr dnsapiLibHandle;

        public DnsCacheMonitor()
        {
            dnsapiLibHandle = DnsApi.LoadLibrary("dnsapi.dll");
            if (dnsapiLibHandle != IntPtr.Zero)
            {
                var procAddress = DnsApi.GetProcAddress(dnsapiLibHandle, nameof(DnsApi.DnsGetCacheDataTable));
                DnsGetCacheDataTable_I = (DnsApi.DnsGetCacheDataTable)Marshal.GetDelegateForFunctionPointer(procAddress, typeof(DnsApi.DnsGetCacheDataTable));
            }
            //else
            //    throw new Win32Exception(Marshal.GetLastWin32Error());
        }

        public void Dispose()
        {
            if (dnsapiLibHandle != IntPtr.Zero)
                DnsApi.FreeLibrary(dnsapiLibHandle);
        }

        public void SyncCache()
        {
            var dnsCacheDataTable = IntPtr.Zero;
            if (DnsGetCacheDataTable_I == null || !DnsGetCacheDataTable_I(out dnsCacheDataTable))
                return;

            DateTime CurrentTime = DateTime.Now;

            // cache domains
            //var oldCache = new HashSet<string>(dnsCache.Keys);

            foreach (DnsCacheEntry cacheEntry in dnsCache.GetAllValues())
            {
                if (cacheEntry.ExpirationTime > CurrentTime)
                    cacheEntry.ExpirationTime = CurrentTime;
                // will be reset in the loop, efectivly timeouting all flushed entries imminetly
            }

            for (var tablePtr = dnsCacheDataTable; tablePtr != IntPtr.Zero; )
            {
                var entry = (DnsApi.DnsCacheEntry)Marshal.PtrToStructure(tablePtr, typeof(DnsApi.DnsCacheEntry));
                tablePtr = entry.Next;

                // Note: DnsGetCacheDataTable_I should only return one result per domain name in cache mo mater how many entries there are
                //          DnsQuery wil retrive all entries of any type for a given domain name thanks to DNS_QUERY_UN_DOCUMENTED

                var resultPtr = IntPtr.Zero;
                uint ret = DnsApi.DnsQuery(entry.Name, entry.Type, DnsApi.DnsQueryType.DNS_QUERY_NO_WIRE_QUERY | DnsApi.DnsQueryType.DNS_QUERY_UN_DOCUMENTED, IntPtr.Zero, ref resultPtr, IntPtr.Zero);
                if (ret != DnsApi.ERROR_SUCCESS)
                    continue;

                //AppLog.Debug("DnsEntries: " + entry.Name);

                // get all entries for thisdomain name
                /*CloneableList<DnsCacheEntry> curEntries = null;
                if (!dnsCache.TryGetValue(entry.Name, out curEntries))
                {
                    curEntries = new CloneableList<DnsCacheEntry>();
                    dnsCache.Add(entry.Name, curEntries);
                }
                else
                    oldCache.Remove(entry.Name);*/
                
                for (var recordIndexPtr = resultPtr; recordIndexPtr != IntPtr.Zero;)
                {
                    var record = (DnsApi.DnsRecord)Marshal.PtrToStructure(recordIndexPtr, typeof(DnsApi.DnsRecord));
                    int offset = Marshal.OffsetOf(typeof(DnsApi.DnsRecord), "Data").ToInt32();
                    IntPtr data = recordIndexPtr + offset;
                    recordIndexPtr = record.Next;

                    string HostName = record.Name;
                    IPAddress Address = null;
                    string ResolvedString = null;

                    CloneableList<DnsCacheEntry> curEntries = GetEntriesFor(HostName);

                    DnsCacheEntry curEntry = null;

                    if (record.Type == DnsApi.DnsRecordType.A || record.Type == DnsApi.DnsRecordType.AAAA)
                    {
                        switch (record.Type)
                        {
                            case DnsApi.DnsRecordType.A:
                            {   
                                var ptr = (DnsApi.DnsARecord)Marshal.PtrToStructure(data, typeof(DnsApi.DnsARecord));
                                Address = new IPAddress((UInt32)ptr.IpAddress);
                                break;
                            }
                            case DnsApi.DnsRecordType.AAAA:
                            {
                                var ptr = (DnsApi.DnsAAAARecord)Marshal.PtrToStructure(data, typeof(DnsApi.DnsAAAARecord));
                                Address = new IPAddress(ptr.IpAddress);
                                break;
                            }
                        }

                        if (Address.Equals(IPAddress.Any) || Address.Equals(IPAddress.IPv6Any)) // thats wht we get from a pi hole dns proxy if the domain is blocked
                            Address = null;

                        curEntry = curEntries.FirstOrDefault(e => { return e.RecordType == record.Type && MiscFunc.IsEqual(e.Address, Address); });
                    }
                    else // CNAME, SRV, MX, DNAME
                    {
                        switch (record.Type)
                        {
                            //case DnsApi.DnsRecordType.PTR:
                            //    Address = RevDnsHost2Address(HostName);
                            //    goto case DnsApi.DnsRecordType.CNAME;
                            //case DnsApi.DnsRecordType.DNAME: // entire zone   
                            case DnsApi.DnsRecordType.CNAME: // one host
                            {
                                var ptr = (DnsApi.DnsPTRRecord)Marshal.PtrToStructure(data, typeof(DnsApi.DnsPTRRecord));
                                ResolvedString = ptr.NameHost;
                                break; 
                            }
                            /*case DnsApi.DnsRecordType.SRV:
                            {
                                var ptr = (DnsApi.DnsSRVRecord)Marshal.PtrToStructure(data, typeof(DnsApi.DnsSRVRecord));
                                ResolvedString = ptr.NameTarget + ":" + ptr.Port;
                                break; 
                            }
                            case DnsApi.DnsRecordType.MX:
                            {
                                var ptr = (DnsApi.DnsMXRecord)Marshal.PtrToStructure(data, typeof(DnsApi.DnsMXRecord));
                                ResolvedString = ptr.NameExchange;
                                break; 
                            }*/
                            default:
                                continue;
                        }

                        if (ResolvedString.Equals("null.arpa")) // I invented that or the DnsProxyServer so probably no one else uses it
                            ResolvedString = null;

                        curEntry = curEntries.FirstOrDefault(e => { return e.RecordType == record.Type && MiscFunc.IsEqual(e.ResolvedString, ResolvedString); });
                    }

                    if (curEntry == null)
                    {
                        curEntry = new DnsCacheEntry() { HostName = HostName, RecordType = record.Type };
                        curEntry.ExpirationTime = CurrentTime.AddSeconds(record.Ttl);
                        if (Address == null && ResolvedString == null)
                            curEntry.State = DnsCacheEntry.States.Blocked;
                        else
                            curEntry.State = DnsCacheEntry.States.Resolved;
                        curEntry.Address = Address;
                        curEntry.ResolvedString = ResolvedString;

                        AddCacheEntry(curEntries, curEntry);
                    }
                    else // just update
                    {
                        curEntry.ExpirationTime = CurrentTime.AddSeconds(record.Ttl);
                    }
                }

                if (resultPtr != IntPtr.Zero)
                    DnsApi.DnsRecordListFree(resultPtr, DnsApi.DnsFreeType.DnsFreeRecordList);
            }

            /*DateTime ExpirationLimit = CurrentTime.AddMinutes(App.GetConfigInt("DnsInspector", "CacheRetention", 15));
            // remove old entries
            foreach (var Name in oldCache)
            {
                CloneableList<DnsCacheEntry> curEntries = dnsCache[Name];
                for (int i = 0; i < curEntries.Count; i++)
                {
                    DnsCacheEntry curEntry = curEntries[i];
                    if (curEntry.ExpirationTime < ExpirationLimit)
                    {
                        if (curEntry.RecordType == DnsApi.DnsRecordType.A || curEntry.RecordType == DnsApi.DnsRecordType.AAAA)
                            cacheByIP.Remove(curEntry.Address, curEntry);
                        else // CNAME, SRV, MX, DNAME
                            cacheByStr.Remove(curEntry.ResolvedString, curEntry);

                        curEntries.RemoveAt(i--);
                    }
                }
                if (curEntries.Count == 0)
                    dnsCache.Remove(Name);
            }*/

            DnsApi.DnsRecordListFree(dnsCacheDataTable, DnsApi.DnsFreeType.DnsFreeRecordList);
        }

        private void AddCacheEntry(CloneableList<DnsCacheEntry> curEntries, DnsCacheEntry curEntry)
        {
            curEntries.Add(curEntry);

            if ((curEntry.RecordType == DnsApi.DnsRecordType.A || curEntry.RecordType == DnsApi.DnsRecordType.AAAA) && curEntry.Address != null)
            {
                if (curEntry.Address != null)
                    cacheByIP.Add(curEntry.Address, curEntry);
            }
            else if (curEntry.ResolvedString != null) // CNAME, SRV, MX, DNAME
            {
                if(curEntry.ResolvedString != null)
                    cacheByStr.Add(curEntry.ResolvedString, curEntry);
            }

            DnsCacheEvent?.Invoke(this, new DnsEvent() { Entry = curEntry });
        }

        private CloneableList<DnsCacheEntry> GetEntriesFor(string HostName)
        {
            CloneableList<DnsCacheEntry> curEntries = null; // all entries for thisdomain name
            if (!dnsCache.TryGetValue(HostName, out curEntries))
            {
                curEntries = new CloneableList<DnsCacheEntry>();
                dnsCache.Add(HostName, curEntries);
            }
            return curEntries;
        }

        public void AddCacheEntry(DnsCacheEntry curEntry)
        {
            // todo: should we check if teh entry lookes as if it was blocked?

            CloneableList<DnsCacheEntry> curEntries = GetEntriesFor(curEntry.HostName);

            DnsCacheEntry oldEntry = null;
            if (curEntry.RecordType == DnsApi.DnsRecordType.A || curEntry.RecordType == DnsApi.DnsRecordType.AAAA)
                oldEntry = curEntries.FirstOrDefault(e => { return e.RecordType == curEntry.RecordType && MiscFunc.IsEqual(e.Address, curEntry.Address); });
            else // CNAME, SRV, MX, DNAME
                oldEntry = curEntries.FirstOrDefault(e => { return e.RecordType == curEntry.RecordType && MiscFunc.IsEqual(e.ResolvedString, curEntry.ResolvedString); });

            if (oldEntry == null)
                AddCacheEntry(curEntries, curEntry);
            else // just update
                oldEntry.ExpirationTime = curEntry.ExpirationTime;
        }

        static public IPAddress RevDnsHost2Address(string HostName)
        {
            // 1.0.0.10.in-addr.arpa -> 10.0.0.1
            // 1.0.0.0.0.0.0.0.0.0.0.0.0.0.0.0.0.0.0.0.0.0.0.0.8.b.d.0.1.0.0.2.ip6.arpa -> 2001:db8::1

            IPAddress Address = IPAddress.None;
            var HostStr = HostName.Split('.');
            if (HostStr.Length >= 32 + 2 && HostStr[32] == "ip6" && HostStr[33] == "arpa")
            {
                List<string> AddrStr = new List<string>();
                for (int i = 32 - 1; i > 0; i -= 4)
                    AddrStr.Add(HostStr[i] + HostStr[i - 1] + HostStr[i - 2] + HostStr[i - 3]);
                IPAddress.TryParse(string.Join(":", AddrStr), out Address);
            }
            else if (HostStr.Length >= 4 + 2 && HostStr[4] == "in-addr" && HostStr[5] == "arpa")
            {
                IPAddress.TryParse(HostStr[3] + "." + HostStr[2] + "." + HostStr[1] + "." + HostStr[0], out Address);
            }
            return Address;
        }

        private List<DnsCacheEntry> ResolveRedir(DnsCacheEntry CurEntry, int Level = 0)
        {
            List<DnsCacheEntry> EntryList = new List<DnsCacheEntry>();
            CloneableList<DnsCacheEntry> NameEntries;
            if (Level >= 4 || !cacheByStr.TryGetValue(CurEntry.HostName, out NameEntries)) // dont get trapped in an endles recursion
                EntryList.Add(CurEntry);
            else
                foreach (DnsCacheEntry NameEntry in NameEntries)
                    EntryList.AddRange(ResolveRedir(NameEntry, Level + 1));
            return EntryList;
        }

        public List<HostNameEntry> FindHostNames(IPAddress remoteAddress)
        {
            List<HostNameEntry> List = new List<HostNameEntry>();
            CloneableList<DnsCacheEntry> AddrEntries;
            if (cacheByIP.TryGetValue(remoteAddress, out AddrEntries))
            {
                foreach (DnsCacheEntry AddrEntry in AddrEntries)
                {
                    /*List<DnsCacheEntry> FinalEntries = ResolveRedir(AddrEntry);
                    if (FinalEntries.Count > 1)
                    {
                        foreach (var FinalEntry in FinalEntries)
                            List.Add(new HostNameEntry() { HostName = FinalEntry.HostName, TimeStamp = FinalEntry.TimeStamp });
                    }
                    else*/
                        List.Add(new HostNameEntry() { HostName = AddrEntry.HostName, TimeStamp = AddrEntry.TimeStamp });
                }
            }
            return List.Count == 0 ? null : List;
        }

        public void CleanupCache()
        {
            DateTime ExpirationLimit = DateTime.Now.AddMinutes(App.GetConfigInt("DnsInspector", "CacheRetention", 15));
            foreach (var Name in dnsCache.Keys.ToList())
            {
                CloneableList<DnsCacheEntry> curEntries = dnsCache[Name];
                for (int i = 0; i < curEntries.Count; i++)
                {
                    DnsCacheEntry curEntry = curEntries[i];
                    if (curEntry.ExpirationTime < ExpirationLimit)
                    {
                        if (curEntry.RecordType == DnsApi.DnsRecordType.A || curEntry.RecordType == DnsApi.DnsRecordType.AAAA)
                        {
                            if (curEntry.Address != null)
                                cacheByIP.Remove(curEntry.Address, curEntry);
                        }
                        else // CNAME, SRV, MX, DNAME
                        {
                            if(curEntry.ResolvedString != null)
                                cacheByStr.Remove(curEntry.ResolvedString, curEntry);
                        }

                        curEntries.RemoveAt(i--);
                    }
                }
                if (curEntries.Count == 0)
                    dnsCache.Remove(Name);
            }
        }
    }
}
