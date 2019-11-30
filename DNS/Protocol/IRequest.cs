namespace DNS.Protocol {
    public interface IRequest : IMessage {
        int Id { get; set; }
        OperationCode OperationCode { get; set; }
        bool RecursionDesired { get; set; }
    }
}
