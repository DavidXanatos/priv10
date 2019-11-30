using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Linq;
using System.Text;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using DNS.Protocol;
using DNS.Protocol.ResourceRecords;
using DNS.Client;
using DNS.Client.RequestResolver;

namespace DNS.Server {
    public class DnsServer {
        private const int DEFAULT_PORT = 53;
        private const int UDP_TIMEOUT = 2000;
        private const int UDP_LIMIT = 512;

        public delegate void RequestedEventHandler(IRequest request);
        public delegate void RespondedEventHandler(IRequest request, IResponse response);

        private volatile bool run = true;

        private MasterFile masterFile;

        private UdpClient udp;
        private EventEmitter emitter;
        private DnsClient client;

        public event RequestedEventHandler Requested;
        public event RespondedEventHandler Responded;

        public DnsServer(IPEndPoint endServer) {
            this.emitter = new EventEmitter();
            this.client = new DnsClient(endServer, new UdpRequestResolver());
            this.masterFile = new MasterFile();
        }

        public DnsServer(IPAddress endServer, int port = DEFAULT_PORT) : this(new IPEndPoint(endServer, port)) {}
        public DnsServer(string endServerIp, int port = DEFAULT_PORT) : this(IPAddress.Parse(endServerIp), port) {}

        public void Listen(int port = DEFAULT_PORT) {
            udp = new UdpClient(port);

            IPEndPoint local = new IPEndPoint(IPAddress.Any, port);

            emitter.Run();
            udp.Client.SendTimeout = UDP_TIMEOUT;

            while (run) {
                byte[] clientMessage = null;

                try {
                    clientMessage = udp.Receive(ref local);
                } catch (SocketException) {
                    continue;
                }

                Thread task = new Thread(() => {
                    Request request = null;

                    try {
                        request = Request.FromArray(clientMessage);
                        emitter.Schedule(() => OnRequested(request));

                        IResponse response = ResolveLocal(request);

                        emitter.Schedule(() => OnResponded(request, response));
                        udp.Send(response.ToArray(), response.Size, local);
                    }
                    catch(SocketException) {}
                    catch(ArgumentException) {}
                    catch(ResponseException e) {
                        IResponse response = e.Response;

                        if (response == null) {
                            response = Response.FromRequest(request);
                        }

                        udp.Send(response.ToArray(), response.Size, local);
                    }
                });

                task.Start();
            }
        }

        public void Close() {
            if (udp != null) {
                run = false;

                emitter.Stop();
                udp.Close();
            }
        }

        public MasterFile MasterFile {
            get { return masterFile; }
        }

        protected virtual void OnRequested(IRequest request) {
            RequestedEventHandler handlers = Requested;
            if (handlers != null) handlers(request);
        }

        protected virtual void OnResponded(IRequest request, IResponse response) {
            RespondedEventHandler handlers = Responded;
            if (handlers != null) handlers(request, response);
        }

        protected virtual IResponse ResolveLocal(Request request) {
            Response response = Response.FromRequest(request);

            foreach (Question question in request.Questions) {
                IList<IResourceRecord> answers = masterFile.Get(question);

                if (answers.Count > 0) {
                    Merge(response.AnswerRecords, answers);
                } else {
                    return ResolveRemote(request);
                }
            }

            return response;
        }

        protected virtual IResponse ResolveRemote(Request request) {
            ClientRequest remoteRequest = client.Create(request);
            return remoteRequest.Resolve();
        }

        private static void Merge<T>(IList<T> l1, IList<T> l2) {
            foreach (T obj in l2) {
                l1.Add(obj);
            }
        }
    }

    internal class EventEmitter {
        public delegate void Emit();

        private CancellationTokenSource tokenSource;
        private BlockingCollection<Emit> queue;

        public void Schedule(Emit emit) {
            if (queue != null) {
                queue.Add(emit);
            }
        }

        public void Run() {
            tokenSource = new CancellationTokenSource();
            queue = new BlockingCollection<Emit>();

            (new Thread(() => {
                try {
                    while (true) {
                        Emit emit = queue.Take(tokenSource.Token);
                        emit();
                    }
                } catch (OperationCanceledException) { }
            })).Start();
        }

        public void Stop() {
            if (tokenSource != null) {
                tokenSource.Cancel();
            }
        }
    }
}
