using MiscHelpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using PrivateAPI;

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

        [DataMember()]
        public ProgramConfig config = new ProgramConfig();

        public ProgramSet()
        {
        }

        public static string SvcIconPath = Environment.ExpandEnvironmentVariables(@"%windir%\system32\filemgmt.dll");

        public string GetIcon()
        {
            if (config.Icon != null && config.Icon.Length > 0)
                return config.Icon;
            if (Programs.Count == 0)
                return NtUtilities.Shell32Path;
            
            return GetIcon(Programs.First().Key);
        }

        public static string GetIcon(ProgramID ProgId)
        {
            if (ProgId.Type == ProgramID.Types.App)
            {
                var AppPkg = AppModel.GetInstance().GetAppPkgBySid(ProgId.GetPackageSID());
                if (AppPkg != null && AppPkg.Logo != null)
                    return AppPkg.Logo;
            }

            if (ProgId.Type == ProgramID.Types.Service) // || (ProgId.IsSystem() && ProgId.Path.Length == 0))
                return SvcIconPath;

            return ProgId.Path;
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
