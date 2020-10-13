using MiscHelpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace PrivateWin10
{
    [Serializable()]
    [DataContract(Name = "ProgramSet", Namespace = "http://schemas.datacontract.org/")]
    public class ProgramSet
    {
        [DataMember()]
        public Guid guid;

        [DataMember()]
        public SortedDictionary<ProgramID, Program> Programs = new SortedDictionary<ProgramID, Program>();

        [Serializable()]
        [DataContract(Name = "ProgramConfig", Namespace = "http://schemas.datacontract.org/")]
        public class Config
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
                //OutBoundAccess,
                //InBoundAccess,
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

            public Config Clone()
            {
                var config = new Config();

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

        [DataMember()]
        public Config config = new Config();

        public ProgramSet()
        {
        }
        
        public string GetIcon()
        {
            if (config.Icon != null && config.Icon.Length > 0)
                return config.Icon;
            if (Programs.Count == 0)
                return MiscFunc.Shell32Path;
            return Programs.First().Key.Path;
        }

        public bool IsSpecial()
        {
            foreach (Program prog in Programs.Values)
            {
                if (prog.IsSpecial())
                    return true;
            }
            return false;
        }
        
        /////////////////////////////////////////////////////////////
        // merged data

        public DateTime GetLastActivity(bool Allowed = true, bool Blocked = true)
        {
            DateTime lastActivity = DateTime.MinValue;
            foreach (Program prog in Programs.Values)
            {
                if (Allowed && prog.LastAllowed > lastActivity)
                    lastActivity = prog.LastAllowed;
                if (Blocked && prog.LastBlocked > lastActivity)
                    lastActivity = prog.LastBlocked;
            }
            return lastActivity;
        }

        public UInt64 GetDataRate()
        {
            UInt64 DataRate = 0;
            foreach (Program prog in Programs.Values)
                DataRate += prog.UploadRate + prog.DownloadRate;
            return DataRate;
        }

        public int GetSocketCount()
        {
            int SocketCount = 0;
            foreach (Program prog in Programs.Values)
                SocketCount += prog.SocketCount;
            return SocketCount;
        }
    }
}
