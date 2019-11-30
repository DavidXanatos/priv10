using System.Collections.Generic;
using System.Collections.ObjectModel;
using DNS.Protocol;
using DNS.Protocol.ResourceRecords;

namespace DNS.Client {
    public class ClientResponse : IResponse {
        private Response response;
        private byte[] message;

        public static ClientResponse FromArray(ClientRequest request, byte[] message) {
            Response response = Response.FromArray(message);
            return new ClientResponse(request, response, message);
        }

        internal ClientResponse(ClientRequest request, Response response, byte[] message) {
            Request = request;

            this.message = message;
            this.response = response;
        }

        internal ClientResponse(ClientRequest request, Response response) {
            Request = request;

            this.message = response.ToArray();
            this.response = response;
        }

        public ClientRequest Request {
            get;
            private set;
        }

        public int Id {
            get { return response.Id; }
            set { }
        }

        public IList<IResourceRecord> AnswerRecords {
            get { return response.AnswerRecords; }
        }

        public IList<IResourceRecord> AuthorityRecords {
            get { return new ReadOnlyCollection<IResourceRecord>(response.AuthorityRecords); }
        }

        public IList<IResourceRecord> AdditionalRecords {
            get { return new ReadOnlyCollection<IResourceRecord>(response.AdditionalRecords); }
        }

        public bool RecursionAvailable {
            get { return response.RecursionAvailable; }
            set { }
        }

        public bool AuthorativeServer {
            get { return response.AuthorativeServer; }
            set { }
        }

        public bool Truncated {
            get { return response.Truncated; }
            set { }
        }

        public OperationCode OperationCode {
            get { return response.OperationCode; }
            set { }
        }

        public ResponseCode ResponseCode {
            get { return response.ResponseCode; }
            set { }
        }

        public IList<Question> Questions {
            get { return new ReadOnlyCollection<Question>(response.Questions); }
        }

        public int Size {
            get { return message.Length; }
        }

        public byte[] ToArray() {
            return message;
        }

        public override string ToString() {
            return response.ToString();
        }
    }
}
