namespace DNS.Client.RequestResolver {
    public interface IRequestResolver {
        ClientResponse Request(ClientRequest request);
    }
}
