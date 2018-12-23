using System;

namespace LocalPolicy
{
    public class GroupPolicyException : Exception
    {
        internal GroupPolicyException(string message)
            : base(message) { }
    }
}
