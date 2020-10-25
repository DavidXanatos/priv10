using MiscHelpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TweakEngine;
using PrivateService;
using System.IO;
using System.Xml;
using System.Diagnostics;
using PrivateAPI;
using static PrivateAPI.Priv10Conv;
using System.Runtime.Serialization;
using WinFirewallAPI;

namespace PrivateWin10
{
    public class Priv10Host : PipeHost
    {
        public Priv10Host()
        {
            Name = App.SvcName;
        }



        /////////////////////////////////////////
        // 

        protected byte[] PutProgID(ProgramID value)
        {
            if (value == null)
                return new byte[0];
            return PutStr(value.AsString());
        }

        protected ProgramID GetProgID(byte[] value)
        {
            if (value.Length == 0)
                return null;
            return ProgramID.Parse(GetStr(value));
        }

        protected byte[] PutProgSets(List<ProgramSet> list)
        {
            return PutList(list, PutProgSet);
        }

        protected byte[] PutProgSet(ProgramSet Progs)
        {
            using (MemoryStream dataStream = new MemoryStream())
            {
                using (var dataWriter = new BinaryWriter(dataStream))
                {
                    dataWriter.Write(Progs.guid.ToByteArray());

                    byte[] data = PutConfig(Progs.config);
                    dataWriter.Write(data.Length);
                    dataWriter.Write(data);

                    dataWriter.Write(Progs.Programs.Count);
                    foreach (var item in Progs.Programs)
                    {
                        data = PutProg(item.Value);
                        dataWriter.Write(data.Length);
                        dataWriter.Write(data);
                    }
                }
                return dataStream.ToArray();
            }
        }

        protected byte[] PutProg(Program Prog)
        {
            return PutXmlObj(Prog);
        }

        protected byte[] PutConfig(ProgramConfig config)
        {
            return PutXmlObj(config);
        }

        protected ProgramConfig GetConfig(byte[] value)
        {
            return GetXmlObj<ProgramConfig>(value);
        }

        protected byte[] PutRule(FirewallRuleEx rule)
        {
            return PutObjXml(rule, (FirewallRuleEx obj, XmlWriter writer) => { obj.Store(writer); });
        }

        protected FirewallRule GetRule(byte[] value)
        {
            return GetXmlObj(value, (FirewallRule obj, XmlElement node) => { obj.Load(node); });
        }

        protected byte[] PutRules(Dictionary<Guid, List<FirewallRuleEx>> rules)
        {
            return PutGuidMMap(rules, PutRule);
        }

        protected byte[] PutLogEntry(Program.LogEntry entry)
        {
            return PutXmlObj(entry);
        }

        protected byte[] PutLogList(Dictionary<Guid, List<Program.LogEntry>> log)
        {
            return PutGuidMMap(log, PutLogEntry);
        }

        protected byte[] PutSock(NetworkSocket socket)
        {
            return PutXmlObj(socket);
        }

        protected byte[] PutSocks(Dictionary<Guid, List<NetworkSocket>> sockets)
        {
            return PutGuidMMap(sockets, PutSock);
        }

        protected byte[] PutDomain(Program.DnsEntry entry)
        {
            return PutXmlObj(entry);
        }

        protected byte[] PutDomains(Dictionary<Guid, List<Program.DnsEntry>> entries)
        {
            return PutGuidMMap(entries, PutDomain);
        }

        protected byte[] PutAppInfo(UwpFunc.AppInfo info)
        {
            return PutXmlObj(info);
        }

        protected byte[] PutAppInfos(Dictionary<string, UwpFunc.AppInfo> infos)
        {
            return PutMap(infos, PutStr, PutAppInfo);
        }

        /////////////////////////////////////////
        // Dns Proxy

        protected byte[] PutQuery(DnsCacheMonitor.DnsCacheEntry entry)
        {
            return PutXmlObj(entry);
        }

        protected byte[] PutQueries(List<DnsCacheMonitor.DnsCacheEntry> list)
        {
            return PutList(list, PutQuery);
        }

        protected byte[] PutDomainFilter(DomainFilter filter)
        {
            return PutXmlObj(filter);
        }

        protected byte[] PutFilterList(List<DomainFilter> list)
        {
            return PutList(list, PutDomainFilter);
        }

        protected DomainFilter GetDomainFilter(byte[] value)
        {
            return GetXmlObj<DomainFilter>(value);
        }

        protected byte[] PutBlockList(List<DomainBlocklist> list)
        {
            return PutList(list, PutBlockEntry);
        }

        protected byte[] PutBlockEntry(DomainBlocklist entry)
        {
            return PutXmlObj(entry);
        }

        protected DomainBlocklist GetBlockEntry(byte[] value)
        {
            return GetXmlObj<DomainBlocklist>(value);
        }

        /////////////////////////////////////////
        // Privacy tweaks

        protected byte[] PutTweak(TweakList.Tweak tweak)
        {
            return PutObjXml(tweak, (TweakList.Tweak obj, XmlWriter writer) => { obj.Store(writer); });
        }

        protected TweakList.Tweak GetTweak(byte[] value)
        {
            return GetXmlObj(value, (TweakList.Tweak obj, XmlElement node) => { obj.Load(node); });
        }


        protected override List<byte[]> Process(string func, List<byte[]> args)
        {
            List<byte[]> ret = new List<byte[]>();
            //try
            {
                /////////////////////////////////////////
                // Windows Firewall

                if (func == "GetFilteringMode")
                {
                    ret.Add(PutStr(App.engine.GetFilteringMode()));
                }
                else if (func == "SetFilteringMode")
                {
                    ret.Add(PutBool(App.engine.SetFilteringMode(GetEnum<FirewallManager.FilteringModes>(args[0]))));
                }
                else if (func == "IsFirewallGuard")
                {
                    ret.Add(PutBool(App.engine.IsFirewallGuard()));
                }
                else if (func == "SetFirewallGuard")
                {
                    ret.Add(PutBool(App.engine.SetFirewallGuard(BitConverter.ToBoolean(args[0], 0), GetEnum<FirewallGuard.Mode>(args[1]))));
                }
                else if (func == "GetAuditPolicy")
                {
                    ret.Add(PutStr(App.engine.GetAuditPolicy()));
                }
                else if (func == "SetAuditPolicy")
                {
                    ret.Add(PutBool(App.engine.SetAuditPolicy(GetEnum<FirewallMonitor.Auditing>(args[0]))));
                }
                else if (func == "GetPrograms")
                {
                    ret.Add(PutProgSets(App.engine.GetPrograms(GetGuids(args[0]))));
                }
                else if (func == "GetProgram")
                {
                    ret.Add(PutProgSet(App.engine.GetProgram(GetProgID(args[0]), GetBool(args[1]))));
                }
                else if (func == "AddProgram")
                {
                    ret.Add(PutBool(App.engine.AddProgram(GetProgID(args[0]), GetGuid(args[1]))));
                }
                else if (func == "UpdateProgram")
                {
                    ret.Add(PutBool(App.engine.UpdateProgram(GetGuid(args[0]), GetConfig(args[1]), GetUInt64(args[2]))));
                }
                else if (func == "MergePrograms")
                {
                    ret.Add(PutBool(App.engine.MergePrograms(GetGuid(args[0]), GetGuid(args[1]))));
                }
                else if (func == "SplitPrograms")
                {
                    ret.Add(PutBool(App.engine.SplitPrograms(GetGuid(args[0]), GetProgID(args[1]))));
                }
                else if (func == "RemoveProgram")
                {
                    ret.Add(PutBool(App.engine.RemoveProgram(GetGuid(args[0]), GetProgID(args[1]))));
                }
                else if (func == "LoadRules")
                {
                    ret.Add(PutBool(App.engine.LoadRules()));
                }
                else if (func == "GetRules")
                {
                    ret.Add(PutRules(App.engine.GetRules(GetGuids(args[0]))));
                }
                else if (func == "UpdateRule")
                {
                    ret.Add(PutBool(App.engine.UpdateRule(GetRule(args[0]), GetUInt64(args[1]))));
                }
                else if (func == "RemoveRule")
                {
                    ret.Add(PutBool(App.engine.RemoveRule(GetStr(args[0]), GetProgID(args[1]))));
                }
                else if (func == "SetRuleApproval")
                {
                    ret.Add(PutInt(App.engine.SetRuleApproval(GetEnum<Priv10Engine.ApprovalMode>(args[0]), GetStr(args[1]), GetProgID(args[2]))));
                }
                else if (func == "BlockInternet")
                {
                    ret.Add(PutBool(App.engine.BlockInternet(GetBool(args[0]))));
                }
                else if (func == "ClearLog")
                {
                    ret.Add(PutBool(App.engine.ClearLog(GetBool(args[0]))));
                }
                else if (func == "ClearDnsLog")
                {
                    ret.Add(PutBool(App.engine.ClearDnsLog()));
                }
                else if (func == "CleanUpPrograms")
                {
                    ret.Add(PutInt(App.engine.CleanUpPrograms(GetBool(args[0]))));
                }
                else if (func == "CleanUpRules")
                {
                    ret.Add(PutInt(App.engine.CleanUpRules(GetEnum<Priv10Engine.CleanupMode>(args[0]))));
                }
                else if (func == "GetConnections")
                {
                    ret.Add(PutLogList(App.engine.GetConnections(GetGuids(args[0]))));
                }
                else if (func == "GetSockets")
                {
                    ret.Add(PutSocks(App.engine.GetSockets(GetGuids(args[0]))));
                }
                else if (func == "SetupDnsInspector")
                {
                    ret.Add(PutBool(App.engine.SetupDnsInspector(GetBool(args[0]))));
                }
                else if (func == "GetDomains")
                {
                    ret.Add(PutDomains(App.engine.GetDomains(GetGuids(args[0]))));
                }
                else if (func == "GetAllAppPkgs")
                {
                    ret.Add(PutAppInfos(App.engine.GetAllAppPkgs(GetBool(args[0]))));
                }
                else if (func == "GetAppPkgRes")
                {
                    ret.Add(PutStr(App.engine.GetAppPkgRes(GetStr(args[0]))));
                }

                /////////////////////////////////////////
                // Dns Proxy

                else if (func == "ConfigureDNSProxy")
                {
                    ret.Add(PutBool(App.engine.ConfigureDNSProxy(GetBool(args[0]), GetBoolx(args[1]), GetStr(args[2]))));
                }

                // Querylog
                else if (func == "GetLoggedDnsQueries")
                {
                    ret.Add(PutQueries(App.engine.GetLoggedDnsQueries()));
                }
                else if (func == "ClearLoggedDnsQueries")
                {
                    ret.Add(PutBool(App.engine.ClearLoggedDnsQueries()));
                }

                // Whitelist/Blacklist
                else if (func == "GetDomainFilter")
                {
                    ret.Add(PutFilterList(App.engine.GetDomainFilter(GetEnum<DnsBlockList.Lists>(args[0]))));
                }
                else if (func == "UpdateDomainFilter")
                {
                    ret.Add(PutBool(App.engine.UpdateDomainFilter(GetEnum<DnsBlockList.Lists>(args[0]), GetDomainFilter(args[1]))));
                }
                else if (func == "RemoveDomainFilter")
                {
                    ret.Add(PutBool(App.engine.RemoveDomainFilter(GetEnum<DnsBlockList.Lists>(args[0]), GetStr(args[1]))));
                }

                // Blocklist
                else if (func == "GetDomainBlocklists")
                {
                    ret.Add(PutBlockList(App.engine.GetDomainBlocklists()));
                }
                else if (func == "UpdateDomainBlocklist")
                {
                    ret.Add(PutBool(App.engine.UpdateDomainBlocklist(GetBlockEntry(args[0]))));
                }
                else if (func == "RemoveDomainBlocklist")
                {
                    ret.Add(PutBool(App.engine.RemoveDomainBlocklist(GetStr(args[0]))));
                }
                else if (func == "RefreshDomainBlocklist")
                {
                    ret.Add(PutBool(App.engine.RefreshDomainBlocklist(GetStr(args[0]))));
                }

                /////////////////////////////////////////
                // Privacy tweaks

                else if (func == "ApplyTweak")
                {
                    ret.Add(PutBool(App.engine.ApplyTweak(GetTweak(args[0]))));
                }
                else if (func == "TestTweak")
                {
                    ret.Add(PutBool(App.engine.TestTweak(GetTweak(args[0]))));
                }
                else if (func == "UndoTweak")
                {
                    ret.Add(PutBool(App.engine.UndoTweak(GetTweak(args[0]))));
                }

                /////////////////////////////////////////
                // Misc

                else if (func == "Quit")
                {
                    ret.Add(PutBool(App.engine.Quit()));
                }

                else
                {
                    //call.args = new Exception("Unknown FunctionCall");
                }
            }
            /*catch (Exception err)
            {
                AppLog.Exception(err);
                call.args = err;
            }*/
            return ret;
        }

        public void NotifyActivity(Guid guid, Program.LogEntry entry, ProgramID progID, List<String> services = null, bool update = false)
        {
            List<byte[]> args = new List<byte[]>();
            args.Add(PutGuid(guid));
            args.Add(PutLogEntry(entry));
            args.Add(PutProgID(progID));
            args.Add(PutStrList(services));
            args.Add(PutBool(update));
            SendPushNotification("ActivityNotification", args);
        }

        public void NotifyRuleChange(Program prog, FirewallRuleEx rule, Priv10Engine.RuleEventType type, Priv10Engine.RuleFixAction action)
        {
            List<byte[]> args = new List<byte[]>();
            args.Add(PutProg(prog));
            args.Add(PutRule(rule));
            args.Add(PutStr(type));
            args.Add(PutStr(action));
            SendPushNotification("RuleChangeNotification", args);
        }

        public void NotifyProgUpdate(Guid guid, Priv10Engine.UpdateTypes type)
        {
            List<byte[]> args = new List<byte[]>();
            args.Add(PutGuid(guid));
            args.Add(PutStr(type));
            SendPushNotification("ProgUpdateNotification", args);
        }

        public void NotifySettingsChanged()
        {
            List<byte[]> args = new List<byte[]>();
            SendPushNotification("SettingsChangedNotification", args);
        }
    }
}
