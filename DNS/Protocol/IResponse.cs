using System.Collections.Generic;
using DNS.Protocol.ResourceRecords;

namespace DNS.Protocol {
    public interface IResponse : IMessage {
        int Id { get; set; }
        IList<IResourceRecord> AnswerRecords { get; }
        IList<IResourceRecord> AuthorityRecords { get; }
        IList<IResourceRecord> AdditionalRecords { get; }
        bool RecursionAvailable { get; set; }
        bool AuthorativeServer { get; set; }
        bool Truncated { get; set; }
        OperationCode OperationCode { get; set; }
        ResponseCode ResponseCode { get; set; }
    }
}
