using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ServiceModel;
using System.ServiceModel.Description;

namespace PrivateWin10.IPC
{
    [Serializable()]
    public class IPCSession
    {
        public string version;
        public bool duplicate;
    }


    [ServiceContract(CallbackContract = typeof(IPCCallback), SessionMode = SessionMode.Required)]
    public interface IPCInterface
    {
        [OperationContract]
        Firewall.FilteringModes GetFilteringMode();

        [OperationContract]
        bool SetFilteringMode(Firewall.FilteringModes Mode);

        [OperationContract]
        Firewall.Auditing GetAuditPol();

        [OperationContract]
        bool SetAuditPol(Firewall.Auditing audit);

        [OperationContract]
        List<Program> GetPrograms(List<Guid> guids = null);

        [OperationContract]
        Program GetProgram(ProgramList.ID id, bool canAdd = false);

        [OperationContract]
        bool AddProgram(ProgramList.ID id, Guid guid);

        [OperationContract]
        bool UpdateProgram(Guid guid, Program.Config config);

        [OperationContract]
        bool MergePrograms(Guid to, Guid from);

        [OperationContract]
        bool SplitPrograms(Guid from, ProgramList.ID id);

        [OperationContract]
        bool RemoveProgram(Guid guid, ProgramList.ID id = null);

        [OperationContract]
        bool LoadRules();

        [OperationContract]
        List<FirewallRule> GetRules(List<Guid> guids = null);

        [OperationContract]
        bool UpdateRule(FirewallRule rule);

        [OperationContract]
        bool ClearRules(ProgramList.ID id, bool bDisable);

        [OperationContract]
        bool RemoveRule(FirewallRule rule);

        [OperationContract]
        bool BlockInternet(bool bBlock);

        [OperationContract]
        bool ClearLog(bool ClearSecLog);

        [OperationContract]
        int CleanUpPrograms();

        [OperationContract]
        List<Program.LogEntry> GetConnections(List<Guid> guids = null);

        [OperationContract]
        List<AppManager.AppInfo> GetAllApps();

        /*[OperationContract]
        bool ApplyTweak(Tweak tweak);

        [OperationContract]
        bool TestTweak(Tweak tweak);

        [OperationContract]
        bool UndoTweak(Tweak tweak);*/
    }
}
