using DNS.Client;
using DNS.Client.RequestResolver;
using DNS.Protocol;
using DNS.Protocol.ResourceRecords;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace PrivateWin10
{
    public class DnsProxyServer : IDisposable
    {
        public  const int DEFAULT_PORT = 53;
        private const int UDP_TIMEOUT = 2000;

        private volatile bool run = false;

        private UdpClient udp;
        private DnsClient resolver;
        public DnsBlockList blockList;

        Thread thread;

        private List<DnsCacheMonitor.DnsCacheEntry> QueryLogList = new List<DnsCacheMonitor.DnsCacheEntry>();
        public int QueryLogLimit = 1000; // todo: customize

        public DnsProxyServer()
        {
            blockList = new DnsBlockList();
        }

        public bool Init()
        {
            try
            {
                string strBindIP = App.GetConfig("DNSProxy", "BindIP", "");

                IPAddress BindIP = IPAddress.IPv6Any;
                if (strBindIP.Length > 0)
                    IPAddress.TryParse(strBindIP, out BindIP);

                udp = new UdpClient(BindIP.AddressFamily);
                if (BindIP.AddressFamily == AddressFamily.InterNetworkV6)
                    udp.Client.SetSocketOption(SocketOptionLevel.IPv6, SocketOptionName.IPv6Only, false); // Also accept IPv4
                udp.Client.Bind(new IPEndPoint(BindIP, App.GetConfigInt("DNSProxy", "Port", DEFAULT_PORT)));

                udp.Client.SendTimeout = UDP_TIMEOUT;
            }
            catch
            {
                App.LogError("Failed to start DNS server");
                return false;
            }

            if (!blockList.Load())
            {
                App.LogError("Restoring default blocklist list, as loading from file failed!");
                blockList.AddDefaultLists();
            }
            blockList.LoadBlockLists();

            SetupUpstreamDNS();

            run = true;
            thread = new Thread(new ThreadStart(Run));
            thread.IsBackground = true;
            thread.Name = "DnsProxy";
            thread.Start();

            ConfigureSystemDNS();

            return true;
        }

        public void Dispose()
        {
            Store();

            run = false;

            if (udp != null)
                udp.Close();

            if (thread != null)
                thread.Join(); // Note: this waits for thread finish
        }

        public void Store()
        {
            blockList.Store();
        }

        public bool SetupUpstreamDNS(string UpstreamDNS = null)
        {
            if (UpstreamDNS == null)
                UpstreamDNS = App.GetConfig("DNSProxy", "UpstreamDNS", "");

            // todo: add support for more than one server
            List<string> Servers = TextHelpers.SplitStr(UpstreamDNS, "|");
            if (Servers.Count == 0)
                return false;

            // split & parse server/port
            var IpPort = TextHelpers.Split2(Servers[0], ":");

            IPAddress Ip;
            if(!IPAddress.TryParse(IpPort.Item1, out Ip))
                return false;
            int Port = MiscFunc.parseInt(IpPort.Item2, DEFAULT_PORT);

            // setup the resolver
            resolver = new DnsClient(new IPEndPoint(Ip, Port), new UdpRequestResolver());
            return true;
        }

        public void ConfigureSystemDNS()
        {
            if (App.GetConfigInt("DnsProxy", "SetLocal", 0) != 0)
                DnsConfigurator.SetLocalDNS();
            else if (DnsConfigurator.IsAnyLocalDNS())
                DnsConfigurator.RestoreDNS();
        }

        private void Run()
        {
            while (run)
            {
                IPEndPoint local = null;
                byte[] clientMessage = null;

                try
                {
                    clientMessage = udp.Receive(ref local);
                }
                catch (SocketException)
                {
                    continue;
                }

                Thread task = new Thread(() => {

                    Request request = null;
                    IResponse response = null;

                    try
                    {
                        request = Request.FromArray(clientMessage);

                        response = ResolveLocal(request);
                    }
                    catch (SocketException) { }
                    catch (ArgumentException) { }

                    if (request == null)
                        return;
                    
                    if (response == null)
                        response = Response.FromRequest(request);

                    try
                    {
                        udp.Send(response.ToArray(), response.Size, local);
                    }
                    catch (SocketException) { }

                });

                task.Start();
            }
        }

        protected IResponse ResolveLocal(Request request)
        {
            // WARNING: this function is called from a worker thread

            // Note: DNS can in theory support multiple questions in one request, but its practically not supported, see:
            // https://stackoverflow.com/questions/4082081/requesting-a-and-aaaa-records-in-single-dns-query/4083071#4083071
            if (request.Questions.Count == 0)
                return null; // empty request

            if (request.Questions[0].Type != RecordType.PTR) // ignore reverse dns queries
                AppLog.Debug("dns query {0} {1}", request.Questions[0].Name, request.Questions[0].Type.ToString());

            IResponse response = blockList.ResolveLocal(request);
            if (response != null)
            {
                AddLoggedDnsQuery(request.Questions[0].Name.ToString(), (int)request.Questions[0].Type, null, null, blockList.ttl);
                return response;
            }

            try
            {
                response = ResolveRemote(request);
            }
            catch (ResponseException e)
            {
                response = e.Response;
            }

            if (response != null)
            {
                if (response.AnswerRecords.Count == 0)
                {
                    AddLoggedDnsQuery(request.Questions[0].Name.ToString(), (int)request.Questions[0].Type, null, null, null);
                }
                else
                {
                    foreach (var answer in response.AnswerRecords)
                    {
                        IPAddress Address = null;
                        string ResolvedString = null;
                        switch (answer.Type)
                        {
                            case RecordType.A:
                            case RecordType.AAAA: Address = (answer as IPAddressResourceRecord).IPAddress; break;
                            case RecordType.CNAME: ResolvedString = (answer as CanonicalNameResourceRecord).CanonicalDomainName.ToString(); break;
                            //case RecordType.MX: 
                            //case RecordType.PTR:
                            default:
                                continue;
                        }

                        AddLoggedDnsQuery(response.Questions[0].Name.ToString(), (int)answer.Type, Address, ResolvedString, answer.TimeToLive);
                    }
                }
            }

            return response;
        }

        protected virtual IResponse ResolveRemote(Request request)
        {
            // WARNING: this function is called from a worker thread

            if (resolver == null)
                return null;
            ClientRequest remoteRequest = resolver.Create(request);
            return remoteRequest.Resolve();
        }

        private void AddLoggedDnsQuery(string Name, int Type, IPAddress Address, string ResolvedString, TimeSpan? ttl)
        {
            // WARNING: this function is called from a worker thread

            DnsCacheMonitor.DnsCacheEntry Entry = new DnsCacheMonitor.DnsCacheEntry() { HostName = Name, RecordType = (DnsApi.DnsRecordType)Type };
            if (ttl == null)
            {
                Entry.ExpirationTime = Entry.TimeStamp;
                Entry.State = DnsCacheMonitor.DnsCacheEntry.States.NotFound;
            }
            else
            {
                Entry.ExpirationTime = Entry.TimeStamp.Add(ttl.Value);

                if (Address == null && ResolvedString == null)
                    Entry.State = DnsCacheMonitor.DnsCacheEntry.States.Blocked;
                else
                    Entry.State = DnsCacheMonitor.DnsCacheEntry.States.Resolved;
            }
            Entry.Address = Address;
            Entry.ResolvedString = ResolvedString;

            App.engine?.RunInEngineThread(() =>
            {
                App.engine.DnsInspector?.AddDnsCacheEntry(Entry);

                QueryLogList.Add(Entry);
                while (QueryLogList.Count > QueryLogLimit)
                    QueryLogList.RemoveAt(0);
            });
        }

        public List<DnsCacheMonitor.DnsCacheEntry> GetLoggedDnsQueries()
        {
            return QueryLogList;
        }

        public bool ClearLoggedDnsQueries()
        {
            QueryLogList.Clear();
            return true;
        }
    }
}
