namespace DNS.Client.RequestResolver {
    public class NullRequestResolver : IRequestResolver {
        public ClientResponse Request(ClientRequest request) {
            throw new ResponseException("Request failed");
        }
    }
}
