
namespace LocalPolicy
{
    public struct GroupPolicyObjectOptions
    {
        public readonly bool UserEnabled;
        public readonly bool MachineEnabled;

        public GroupPolicyObjectOptions(bool userEnabled = true, bool machineEnabled = true)
        {
            UserEnabled = userEnabled;
            MachineEnabled = machineEnabled;
        }
        public GroupPolicyObjectOptions(uint flag)
        {
            UserEnabled = (flag & disableUserFlag) == 0;
            MachineEnabled = (flag & disableMachineFlag) == 0;
        }

        private const uint disableUserFlag = 0x00000001;
        private const uint disableMachineFlag = 0x00000002;

        internal uint Flag
        {
            get
            {
                uint flag = 0x00000000;
                if (!UserEnabled)
                    flag |= disableUserFlag;
                if (!MachineEnabled)
                    flag |= disableMachineFlag;
                return flag;
            }
        }

        internal uint Mask
        {
            get
            {
                // We always change everything
                return disableUserFlag 
                    | disableMachineFlag;
            }
        }
    }
}
