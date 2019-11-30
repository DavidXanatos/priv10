using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DNS.Protocol {
    public enum ResponseCode {
        NoError = 0,
        FormatError,
        ServerFailure,
        NameError,
        NotImplemented,
        Refused,
        YXDomain,
        YXRRSet,
        NXRRSet,
        NotAuth,
        NotZone,
    }
}
