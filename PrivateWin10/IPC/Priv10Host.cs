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
        public Priv10Host()
        {
            Name = App.SvcName;
        }

        protected override RemoteCall Process(RemoteCall call)
        {
            //try
            {
                /////////////////////////////////////////
                // Windows Firewall

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
                else if (call.func == "SetRuleApproval")
                {
                    call.args = App.engine.SetRuleApproval(RemoteCall.GetArg<Priv10Engine.ApprovalMode>(call.args, 0), RemoteCall.GetArg<FirewallRule>(call.args, 1));
                }
                else if (call.func == "BlockInternet")
                {
                    call.args = App.engine.BlockInternet((bool)call.args);
                }
                else if (call.func == "ClearLog")
                {
                    call.args = App.engine.ClearLog((bool)call.args);
                }
                else if (call.func == "ClearDnsLog")
                {
                    call.args = App.engine.ClearDnsLog();
                }
                else if (call.func == "CleanUpPrograms")
                {
                    call.args = App.engine.CleanUpPrograms((bool)call.args);
                }
                else if (call.func == "CleanUpRules")
                {
                    call.args = App.engine.CleanUpRules();
                }
                else if (call.func == "GetConnections")
                {
                    call.args = App.engine.GetConnections((List<Guid>)call.args);
                }
                else if (call.func == "GetSockets")
                {
                    call.args = App.engine.GetSockets((List<Guid>)call.args);
                }
                else if (call.func == "SetupDnsInspector")
                {
                    call.args = App.engine.SetupDnsInspector(RemoteCall.GetArg<bool>(call.args, 0));
                }
                else if (call.func == "GetDomains")
                {
                    call.args = App.engine.GetDomains((List<Guid>)call.args);
                }
                else if (call.func == "GetAllAppPkgs")
                {
                    call.args = App.engine.GetAllAppPkgs((bool)call.args);
                }
                else if (call.func == "GetAppPkgRes")
                {
                    call.args = App.engine.GetAppPkgRes((string)call.args);
                }

                /////////////////////////////////////////
                // Dns Proxy

                else if (call.func == "ConfigureDNSProxy")
                {
                    call.args = App.engine.ConfigureDNSProxy(RemoteCall.GetArg<bool>(call.args, 0), RemoteCall.GetArg<bool?>(call.args, 1), RemoteCall.GetArg<string>(call.args, 2));
                }

                // Querylog
                else if (call.func == "GetLoggedDnsQueries")
                {
                    call.args = App.engine.GetLoggedDnsQueries();
                }
                else if (call.func == "ClearLoggedDnsQueries")
                {
                    call.args = App.engine.ClearLoggedDnsQueries();
                }

                // Whitelist/Blacklist
                else if (call.func == "GetDomainFilter")
                {
                    call.args = App.engine.GetDomainFilter(RemoteCall.GetArg<DnsBlockList.Lists>(call.args, 0));
                }
                else if (call.func == "UpdateDomainFilter")
                {
                    call.args = App.engine.UpdateDomainFilter(RemoteCall.GetArg<DnsBlockList.Lists>(call.args, 0), RemoteCall.GetArg<DomainFilter>(call.args, 1));
                }
                else if (call.func == "RemoveDomainFilter")
                {
                    call.args = App.engine.RemoveDomainFilter(RemoteCall.GetArg<DnsBlockList.Lists>(call.args, 0), RemoteCall.GetArg<string>(call.args, 1));
                }

                // Blocklist
                else if (call.func == "GetDomainBlocklists")
                {
                    call.args = App.engine.GetDomainBlocklists();
                }
                else if (call.func == "UpdateDomainBlocklist")
                {
                    call.args = App.engine.UpdateDomainBlocklist((DomainBlocklist)call.args);
                }
                else if (call.func == "RemoveDomainBlocklist")
                {
                    call.args = App.engine.RemoveDomainBlocklist((string)call.args);
                }
                else if (call.func == "RefreshDomainBlocklist")
                {
                    call.args = App.engine.RefreshDomainBlocklist((string)call.args);
                }

                /////////////////////////////////////////
                // Privacy tweaks

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

                /////////////////////////////////////////
                // Misc

                /*else if (call.func == "Quit")
                {
                    call.args = App.engine.Quit();
                }*/

                else
                {
                    call.args = new Exception("Unknown FunctionCall");
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
            Priv10Engine.FwEventArgs args = new Priv10Engine.FwEventArgs()
            {
                guid = guid,
                entry = entry,
                progID = progID,
                services = services,
                update = update
            };
            SendPushNotification("ActivityNotification", args);
        }

        public void NotifyChange(Guid guid, Priv10Engine.ChangeArgs.Types type)
        {
            Priv10Engine.ChangeArgs args = new Priv10Engine.ChangeArgs()
            {
                guid = guid,
                type = type
            };
            SendPushNotification("ChangeNotification", args);
        }
    }
}
