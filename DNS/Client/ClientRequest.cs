using System;
using System.Collections.Generic;
using System.Net;
using DNS.Protocol;
using DNS.Client.RequestResolver;

namespace DNS.Client {
    public class ClientRequest : IRequest {
        private const int DEFAULT_PORT = 53;

        private IPEndPoint dns;
        private IRequestResolver resolver;
        private IRequest request;

        public ClientRequest(IPEndPoint dns, IRequest request = null, IRequestResolver resolver = null) {
            this.dns = dns;
            this.request = request == null ? new Request() : new Request(request);
            this.resolver = resolver == null ? new UdpRequestResolver() : resolver;
        }

        public ClientRequest(IPAddress ip, int port = DEFAULT_PORT, IRequest request = null, IRequestResolver resolver = null) :
            this(new IPEndPoint(ip, port), request, resolver) { }

        public ClientRequest(string ip, int port = DEFAULT_PORT, IRequest request = null, IRequestResolver resolver = null) :
            this(IPAddress.Parse(ip), port, request, resolver) { }

        public int Id {
            get { return request.Id; }
            set { request.Id = value; }
        }

        public OperationCode OperationCode {
            get { return request.OperationCode; }
            set { request.OperationCode = value; }
        }

        public bool RecursionDesired {
            get { return request.RecursionDesired; }
            set { request.RecursionDesired = value; }
        }

        public IList<Question> Questions {
            get { return request.Questions; }
        }

        public int Size {
            get { return request.Size; }
        }

        public byte[] ToArray() {
            return request.ToArray();
        }

        public override string ToString() {
            return request.ToString();
        }

        public IPEndPoint Dns {
            get { return dns; }
            set { dns = value; }
        }

        /// <summary>
        /// Resolves this request into a response using the provided DNS information. The given
        /// request strategy is used to retrieve the response.
        /// </summary>
        /// <exception cref="ResponseException">Throw if a malformed response is received from the server</exception>
        /// <exception cref="IOException">Thrown if a IO error occurs</exception>
        /// <exception cref="SocketException">Thrown if a the reading or writing to the socket fails</exception>
        /// <returns>The response received from server</returns>
        public ClientResponse Resolve() {
            try {
                ClientResponse response = resolver.Request(this);

                if (response.Id != this.Id) {
                    throw new ResponseException(response, "Mismatching request/response IDs");
                }
                if (response.ResponseCode != ResponseCode.NoError) {
                    throw new ResponseException(response);
                }

                return response;
            } catch (ArgumentException e) {
                throw new ResponseException("Invalid response", e);
            }
        }
    }
}
