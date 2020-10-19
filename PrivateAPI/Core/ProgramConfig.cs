using MiscHelpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace PrivateAPI
{

    [Serializable()]
    [DataContract(Name = "ProgramConfig", Namespace = "http://schemas.datacontract.org/")]
    public class ProgramConfig
    {
        [DataMember()]
        public string Name = "";
        [DataMember()]
        public string Category = "";
        [DataMember()]
        public string Icon = "";

        public enum AccessLevels
        {
            Unconfigured = 0,
            FullAccess,
            OutBoundAccess,
            InBoundAccess,
            CustomConfig,
            LocalOnly,
            BlockAccess,
            StopNotify,
            AnyValue,
            WarningState
        }

        [DataMember()]
        public bool? Notify = null;
        public bool? GetNotify() { return IsSilenced() ? (bool?)false : Notify; }
        public void SetNotify(bool? set) { SilenceUntill = 0; Notify = set; }
        [DataMember()]
        public UInt64 SilenceUntill = 0;
        public bool IsSilenced() { return SilenceUntill != 0 && SilenceUntill > MiscFunc.GetUTCTime(); }
        [DataMember()]
        public AccessLevels NetAccess = AccessLevels.Unconfigured;
        [DataMember()]
        public AccessLevels CurAccess = AccessLevels.Unconfigured;
        public AccessLevels GetAccess()
        {
            if (NetAccess == AccessLevels.Unconfigured)
                return CurAccess;
            else
                return NetAccess;
        }

        public ProgramConfig Clone()
        {
            var config = new ProgramConfig();

            config.Name = this.Name;
            config.Category = this.Category;
            config.Icon = this.Icon;

            config.Notify = this.Notify;
            config.SilenceUntill = this.SilenceUntill;
            config.NetAccess = this.NetAccess;
            config.CurAccess = this.CurAccess;

            return config;
        }

        // Custom option
        // todo
    }
}
