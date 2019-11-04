using PipeIPC;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PrivateWin10
{
    public class Priv10Host : PipeHost
    {
        public Priv10Host(string name)
        {
            Name = name;
        }

        protected override RemoteCall Process(RemoteCall call)
        {
            //try
            {
                if (call.func == "GetFilteringMode")
                {
                    call.args = App.engine.GetFilteringMode();
                }
                else if (call.func == "SetFilteringMode")
                {
                    call.args = App.engine.SetFilteringMode((FirewallManager.FilteringModes)call.args);
                }
                else if (call.func == "IsFirewallGuard")
                {
                    call.args = App.engine.IsFirewallGuard();
                }
                else if (call.func == "SetFirewallGuard")
                {
                    call.args = App.engine.SetFirewallGuard(RemoteCall.GetArg<bool>(call.args, 0), RemoteCall.GetArg<FirewallGuard.Mode>(call.args, 1));
                }
                else if (call.func == "GetAuditPolicy")
                {
                    call.args = App.engine.GetAuditPolicy();
                }
                else if (call.func == "SetAuditPolicy")
                {
                    call.args = App.engine.SetAuditPolicy((FirewallMonitor.Auditing)call.args);
                }
                else if (call.func == "GetPrograms")
                {
                    call.args = App.engine.GetPrograms(RemoteCall.GetArg<List<Guid>>(call.args, 0));
                }
                else if (call.func == "GetProgram")
                {
                    call.args = App.engine.GetProgram(RemoteCall.GetArg<ProgramID>(call.args, 0), RemoteCall.GetArg<bool>(call.args, 1));
                }
                else if (call.func == "AddProgram")
                {
                    call.args = App.engine.AddProgram(RemoteCall.GetArg<ProgramID> (call.args, 0), RemoteCall.GetArg<Guid>(call.args, 1));
                }
                else if (call.func == "UpdateProgram")
                {
                    call.args = App.engine.UpdateProgram(RemoteCall.GetArg<Guid>(call.args, 0), RemoteCall.GetArg<ProgramSet.Config>(call.args, 1), RemoteCall.GetArg<UInt64>(call.args, 2));
                }
                else if (call.func == "MergePrograms")
                {
                    call.args = App.engine.MergePrograms(RemoteCall.GetArg<Guid>(call.args, 0), RemoteCall.GetArg<Guid>(call.args, 1));
                }
                else if (call.func == "SplitPrograms")
                {
                    call.args = App.engine.SplitPrograms(RemoteCall.GetArg<Guid>(call.args, 0), RemoteCall.GetArg<ProgramID>(call.args, 1));
                }
                else if (call.func == "RemoveProgram")
                {
                    call.args = App.engine.RemoveProgram(RemoteCall.GetArg<Guid>(call.args, 0), RemoteCall.GetArg<ProgramID>(call.args, 1));
                }
                else if (call.func == "LoadRules")
                {
                    call.args = App.engine.LoadRules();
                }
                else if (call.func == "GetRules")
                {
                    call.args = App.engine.GetRules((List<Guid>)call.args);
                }
                else if (call.func == "UpdateRule")
                {
                    call.args = App.engine.UpdateRule(RemoteCall.GetArg<FirewallRule>(call.args, 0), RemoteCall.GetArg<UInt64>(call.args, 1));
                }
                else if (call.func == "RemoveRule")
                {
                    call.args = App.engine.RemoveRule((FirewallRule)call.args);
                }
                else if (call.func == "BlockInternet")
                {
                    call.args = App.engine.BlockInternet((bool)call.args);
                }
                else if (call.func == "ClearLog")
                {
                    call.args = App.engine.ClearLog((bool)call.args);
                }
                else if (call.func == "CleanUpPrograms")
                {
                    call.args = App.engine.CleanUpPrograms();
                }
                else if (call.func == "GetConnections")
                {
                    call.args = App.engine.GetConnections((List<Guid>)call.args);
                }
                else if (call.func == "GetSockets")
                {
                    call.args = App.engine.GetSockets((List<Guid>)call.args);
                }
                else if (call.func == "GetDomains")
                {
                    call.args = App.engine.GetDomains((List<Guid>)call.args);
                }

                else if (call.func == "ApplyTweak")
                {
                    call.args = App.engine.ApplyTweak((TweakManager.Tweak)call.args);
                }
                else if (call.func == "TestTweak")
                {
                    call.args = App.engine.TestTweak((TweakManager.Tweak)call.args);
                }
                else if (call.func == "UndoTweak")
                {
                    call.args = App.engine.UndoTweak((TweakManager.Tweak)call.args);
                }

                else
                {
                    call.args = new Exception("Unknon FunctionCall");
                }
            }
            /*catch (Exception err)
            {
                AppLog.Exception(err);
                call.args = err;
            }*/
            return call;
        }

        public void NotifyActivity(Guid guid, Program.LogEntry entry, ProgramID progID, List<String> services = null, bool update = false)
        {
            SendPushNotification("ActivityNotification", new object[] { guid, entry, progID, services, update });
        }

        public void NotifyChange(ProgramList.ListEvent args)
        {
            SendPushNotification("ChangeNotification", new object[] { args.guid });
        }
    }
}
