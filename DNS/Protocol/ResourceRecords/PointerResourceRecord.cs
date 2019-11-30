using System;

namespace DNS.Protocol.ResourceRecords {
    public class PointerResourceRecord : BaseResourceRecord {
        public PointerResourceRecord(IResourceRecord record, byte[] message, int dataOffset)
            : base(record) {
            PointerDomainName = Domain.FromArray(message, dataOffset);
        }

        public PointerResourceRecord(Domain domain, Domain pointer, TimeSpan ttl = default(TimeSpan)) :
            base(new ResourceRecord(domain, pointer.ToArray(), RecordType.PTR, RecordClass.IN, ttl)) {
            PointerDomainName = pointer;
        }

        public Domain PointerDomainName {
            get;
            private set;
        }

        public override string ToString() {
            return Stringify().Add("PointerDomainName").ToString();
        }
    }
}
