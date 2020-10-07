using MiscHelpers;
using PrivateAPI;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Xml;
using TweakEngine;
using static PrivateAPI.Priv10Conv;

namespace PrivateWin10
{
    public class Priv10Client: PipeClient
    {
        public Priv10Client()
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

        protected List<ProgramSet> GetProgSets(byte[] value)
        {
            return GetList(value, GetProgSet);
        }

        protected ProgramSet GetProgSet(byte[] value)
        {
            ProgramSet Progs = new ProgramSet();
            using (MemoryStream dataStream = new MemoryStream(value))
            {
                using (var dataReader = new BinaryReader(dataStream))
                {
                    Progs.guid = new Guid(dataReader.ReadBytes(16));

                    int length = dataReader.ReadInt32();
                    byte[] data = dataReader.ReadBytes(length);
                    Progs.config = GetConfig(data);

                    int count = dataReader.ReadInt32();
                    for (int i = 0; i < count; i++)
                    {
                        length = dataReader.ReadInt32();
                        Program prog = GetProg(dataReader.ReadBytes(length));
                        //Program knownProg;
                        //if (Progs.Programs.TryGetValue(prog.ID, out knownProg)) {...} else
                        prog.AssignSet(Progs);
                    }
                }
            }
            return Progs;
        }

        protected Program GetProg(byte[] value)
        {
            return GetXmlObj<Program>(value);
        }

        protected byte[] PutConfig(ProgramSet.Config config)
        {
            return PutXmlObj(config);
        }

        protected ProgramSet.Config GetConfig(byte[] value)
        {
            return GetXmlObj<ProgramSet.Config>(value);
        }

        protected byte[] PutRule(FirewallRule rule)
        {
            return PutObjXml(rule, (FirewallRule obj, XmlWriter writer) => { obj.Store(writer); });
        }

        protected FirewallRuleEx GetRule(byte[] value)
        {
            return GetXmlObj(value, (FirewallRuleEx obj, XmlElement node) => { obj.Load(node); });
        }

        protected Dictionary<Guid, List<FirewallRuleEx>> GetRules(byte[] value)
        {
            return GetGuidMMap(value, (data) => { return GetRule(data); });
        }

        protected Program.LogEntry GetLogEntry(byte[] value)
        {
            return GetXmlObj<Program.LogEntry>(value);
        }

        protected Dictionary<Guid, List<Program.LogEntry>> GetLogList(byte[] data)
        {
            return GetGuidMMap(data, GetLogEntry);
        }

        protected NetworkSocket GetSock(byte[] value)
        {
            return GetXmlObj<NetworkSocket>(value);
        }

        protected Dictionary<Guid, List<NetworkSocket>> GetSocks(byte[] data)
        {
            return GetGuidMMap(data, GetSock);
        }

        protected Program.DnsEntry GetDomain(byte[] value)
        {
            return GetXmlObj<Program.DnsEntry>(value);
        }

        protected Dictionary<Guid, List<Program.DnsEntry>> GetDomains(byte[] data)
        {
            return GetGuidMMap(data, GetDomain);
        }

        protected UwpFunc.AppInfo GetAppInfo(byte[] value)
        {
            return GetXmlObj<UwpFunc.AppInfo>(value);
        }

        protected List<UwpFunc.AppInfo> GetAppInfos(byte[] data)
        {
            return GetList(data, GetAppInfo);
        }

        /////////////////////////////////////////
        // Dns Proxy
        
        protected byte[] PutQuery(DnsCacheMonitor.DnsCacheEntry entry)
        {
            return PutXmlObj(entry);
        }

        protected DnsCacheMonitor.DnsCacheEntry GetQuery(byte[] value)
        {
            return GetXmlObj<DnsCacheMonitor.DnsCacheEntry>(value);
        }

        protected List<DnsCacheMonitor.DnsCacheEntry> GetQueries(byte[] value)
        {
            return GetList(value, GetQuery);
        }

        protected byte[] PutDomainFilter(DomainFilter filter)
        {
            return PutXmlObj(filter);
        }

        protected DomainFilter GetDomainFilter(byte[] value)
        {
            return GetXmlObj<DomainFilter>(value);
        }

        protected List<DomainFilter> GetFilterList(byte[] value)
        {
            return GetList(value, GetDomainFilter);
        }

        protected List<DomainBlocklist> GetBlockList(byte[] value)
        {
            return GetList(value, GetBlockEntry);
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



        /////////////////////////////////////////
        // Windows Firewall

        public FirewallManager.FilteringModes GetFilteringMode()
        {
            List<byte[]> args = new List<byte[]>();
            List<byte[]> ret = RemoteExec("GetFilteringMode", args);
            return ret != null ? GetEnum<FirewallManager.FilteringModes>(ret[0]) : FirewallManager.FilteringModes.Unknown;
        }

        public bool SetFilteringMode(FirewallManager.FilteringModes Mode)
        {
            List<byte[]> args = new List<byte[]>();
            args.Add(PutStr(Mode));
            List<byte[]> ret = RemoteExec("SetFilteringMode", args);
            return ret != null ? GetBool(ret[0]) : false;
        }

        public bool IsFirewallGuard()
        {
            List<byte[]> args = new List<byte[]>();
            List<byte[]> ret = RemoteExec("IsFirewallGuard", args);
            return ret != null ? GetBool(ret[0]) : false;
        }

        public bool SetFirewallGuard(bool guard, FirewallGuard.Mode mode)
        {
            List<byte[]> args = new List<byte[]>();
            args.Add(PutBool(guard));
            args.Add(PutStr(mode));
            List<byte[]> ret = RemoteExec("SetFirewallGuard", args);
            return ret != null ? GetBool(ret[0]) : false;
        }

        public FirewallMonitor.Auditing GetAuditPolicy()
        {
            List<byte[]> args = new List<byte[]>();
            List<byte[]> ret = RemoteExec("GetAuditPolicy", args);
            return ret != null ? GetEnum<FirewallMonitor.Auditing>(ret[0]) : FirewallMonitor.Auditing.Off;
        }

        public bool SetAuditPolicy(FirewallMonitor.Auditing audit)
        {
            List<byte[]> args = new List<byte[]>();
            args.Add(PutStr(audit));
            List<byte[]> ret = RemoteExec("SetAuditPolicy", args);
            return ret != null ? GetBool(ret[0]) : false;
        }

        public List<ProgramSet> GetPrograms(List<Guid> guids = null)
        {
            List<byte[]> args = new List<byte[]>();
            args.Add(PutGuids(guids));
            List<byte[]> ret = RemoteExec("GetPrograms", args);
            return ret != null ? GetProgSets(ret[0]) : null;
        }

        public ProgramSet GetProgram(ProgramID id, bool canAdd = false)
        {
            List<byte[]> args = new List<byte[]>();
            args.Add(PutProgID(id));
            args.Add(PutBool(canAdd));
            List<byte[]> ret = RemoteExec("GetProgram", args);
            return ret != null ? GetProgSet(ret[0]) : null;
        }
        
        public bool AddProgram(ProgramID id, Guid guid)
        {
            List<byte[]> args = new List<byte[]>();
            args.Add(PutProgID(id));
            args.Add(PutGuid(guid));
            List<byte[]> ret = RemoteExec("AddProgram", args);
            return ret != null ? GetBool(ret[0]) : false;
        }
        
        public bool UpdateProgram(Guid guid, ProgramSet.Config config, UInt64 expiration = 0)
        {
            List<byte[]> args = new List<byte[]>();
            args.Add(PutGuid(guid));
            args.Add(PutConfig(config));
            args.Add(PutUInt64(expiration));
            List<byte[]> ret = RemoteExec("UpdateProgram", args);
            return ret != null ? GetBool(ret[0]) : false;
        }

        public bool MergePrograms(Guid to, Guid from)
        {
            List<byte[]> args = new List<byte[]>();
            args.Add(PutGuid(to));
            args.Add(PutGuid(from));
            List<byte[]> ret = RemoteExec("MergePrograms", args);
            return ret != null ? GetBool(ret[0]) : false;
        }

        public bool SplitPrograms(Guid from, ProgramID id)
        {
            List<byte[]> args = new List<byte[]>();
            args.Add(PutGuid(from));
            args.Add(PutProgID(id));
            List<byte[]> ret = RemoteExec("SplitPrograms", args);
            return ret != null ? GetBool(ret[0]) : false;
        }

        public bool RemoveProgram(Guid guid, ProgramID id = null)
        {
            List<byte[]> args = new List<byte[]>();
            args.Add(PutGuid(guid));
            args.Add(PutProgID(id));
            List<byte[]> ret = RemoteExec("RemoveProgram", args);
            return ret != null ? GetBool(ret[0]) : false;
        }
        
        public bool LoadRules()
        {
            List<byte[]> args = new List<byte[]>();
            List<byte[]> ret = RemoteExec("LoadRules", args);
            return ret != null ? GetBool(ret[0]) : false;
        }

        public Dictionary<Guid, List<FirewallRuleEx>> GetRules(List<Guid> guids = null)
        {
            List<byte[]> args = new List<byte[]>();
            args.Add(PutGuids(guids));
            List<byte[]> ret = RemoteExec("GetRules", args);
            return ret != null ? GetRules(ret[0]) : null;
        }

        public bool UpdateRule(FirewallRule rule, UInt64 expiration = 0)
        {
            List<byte[]> args = new List<byte[]>();
            args.Add(PutRule(rule));
            args.Add(PutUInt64(expiration));
            List<byte[]> ret = RemoteExec("UpdateRule", args);
            return ret != null ? GetBool(ret[0]) : false;
        }
        
        public bool RemoveRule(FirewallRule rule)
        {
            List<byte[]> args = new List<byte[]>();
            args.Add(PutStr(rule.guid));
            args.Add(PutProgID(rule.ProgID)); // we tell the progid so that we dont need to check all programs
            List<byte[]> ret = RemoteExec("RemoveRule", args);
            return ret != null ? GetBool(ret[0]) : false;
        }

        public int SetRuleApproval(Priv10Engine.ApprovalMode Mode, FirewallRule rule)
        {
            List<byte[]> args = new List<byte[]>();
            args.Add(PutStr(Mode));
            args.Add(PutStr(rule != null ? rule.guid : null)); // null means all rules
            args.Add(PutProgID(rule != null ? rule.ProgID : null)); // we tell the progid so that we dont need to check all programs
            List<byte[]> ret = RemoteExec("SetRuleApproval", args);
            return ret != null ? GetInt(ret[0]) : 0;
        }

        public bool BlockInternet(bool bBlock)
        {
            List<byte[]> args = new List<byte[]>();
            args.Add(PutBool(bBlock));
            List<byte[]> ret = RemoteExec("BlockInternet", args);
            return ret != null ? GetBool(ret[0]) : false;
        }
        
        public bool ClearLog(bool ClearSecLog)
        {
            List<byte[]> args = new List<byte[]>();
            args.Add(PutBool(ClearSecLog));
            List<byte[]> ret = RemoteExec("ClearLog", args);
            return ret != null ? GetBool(ret[0]) : false;
        }

        public bool ClearDnsLog()
        {
            List<byte[]> args = new List<byte[]>();
            List<byte[]> ret = RemoteExec("ClearDnsLog", args);
            return ret != null ? GetBool(ret[0]) : false;
        }

        public int CleanUpPrograms(bool ExtendedCleanup = false)
        {
            List<byte[]> args = new List<byte[]>();
            args.Add(PutBool(ExtendedCleanup));
            List<byte[]> ret = RemoteExec("CleanUpPrograms", args);
            return ret != null ? GetInt(ret[0]) : 0;
        }

        public int CleanUpRules()
        {
            List<byte[]> args = new List<byte[]>();
            List<byte[]> ret = RemoteExec("CleanUpRules", args);
            return ret != null ? GetInt(ret[0]) : 0;
        }
        
        public Dictionary<Guid, List<Program.LogEntry>> GetConnections(List<Guid> guids = null)
        {
            List<byte[]> args = new List<byte[]>();
            args.Add(PutGuids(guids));
            List<byte[]> ret = RemoteExec("GetConnections", args);
            return ret != null ? GetLogList(ret[0]) : null;
        }

        public Dictionary<Guid, List<NetworkSocket>> GetSockets(List<Guid> guids = null)
        {
            List<byte[]> args = new List<byte[]>();
            args.Add(PutGuids(guids));
            List<byte[]> ret = RemoteExec("GetSockets", args);
            return ret != null ? GetSocks(ret[0]) : null;
        }

        public bool SetupDnsInspector(bool Enable)
        {
            List<byte[]> args = new List<byte[]>();
            args.Add(PutBool(Enable));
            List<byte[]> ret = RemoteExec("SetupDnsInspector", args);
            return ret != null ? GetBool(ret[0]) : false;
        }

        public Dictionary<Guid, List<Program.DnsEntry>> GetDomains(List<Guid> guids = null)
        {
            List<byte[]> args = new List<byte[]>();
            args.Add(PutGuids(guids));
            List<byte[]> ret = RemoteExec("GetDomains", args);
            return ret != null ? GetDomains(ret[0]) : null;
        }

        public List<UwpFunc.AppInfo> GetAllAppPkgs(bool bReload = false)
        {
            List<byte[]> args = new List<byte[]>();
            args.Add(PutBool(bReload));
            List<byte[]> ret = RemoteExec("GetAllAppPkgs", args);
            return ret != null ? GetAppInfos(ret[0]) : null;
        }

        public string GetAppPkgRes(string str)
        {
            List<byte[]> args = new List<byte[]>();
            args.Add(PutStr(str));
            List<byte[]> ret = RemoteExec("GetAppPkgRes", args);
            return ret != null ? GetStr(ret[0]) : str;
        }

        /////////////////////////////////////////
        // Dns Proxy

        public bool ConfigureDNSProxy(bool Enable, bool? setLocal = null, string UpstreamDNS = null)
        {
            List<byte[]> args = new List<byte[]>();
            args.Add(PutBool(Enable));
            args.Add(PutBoolx(setLocal));
            args.Add(PutStr(UpstreamDNS));
            List<byte[]> ret = RemoteExec("ConfigureDNSProxy", args);
            return ret != null ? GetBool(ret[0]) : false;
        }

        // Querylog
        public List<DnsCacheMonitor.DnsCacheEntry> GetLoggedDnsQueries()
        {
            List<byte[]> args = new List<byte[]>();
            List<byte[]> ret = RemoteExec("GetLoggedDnsQueries", args);
            return ret != null ? GetQueries(ret[0]) : null;
        }

        public bool ClearLoggedDnsQueries()
        {
            List<byte[]> args = new List<byte[]>();
            List<byte[]> ret = RemoteExec("ClearLoggedDnsQueries", args);
            return ret != null ? GetBool(ret[0]) : false;
        }

        // Whitelist/Blacklist
        public List<DomainFilter> GetDomainFilter(DnsBlockList.Lists List)
        {
            List<byte[]> args = new List<byte[]>();
            List<byte[]> ret = RemoteExec("GetDomainFilter", args);
            return ret != null ? GetFilterList(ret[0]) : null;
        }

        public bool UpdateDomainFilter(DnsBlockList.Lists List, DomainFilter Filter)
        {
            List<byte[]> args = new List<byte[]>();
            args.Add(PutStr(List));
            args.Add(PutDomainFilter(Filter));
            List<byte[]> ret = RemoteExec("UpdateDomainFilter", args);
            return ret != null ? GetBool(ret[0]) : false;
        }

        public bool RemoveDomainFilter(DnsBlockList.Lists List, string Domain)
        {
            List<byte[]> args = new List<byte[]>();
            args.Add(PutStr(List));
            args.Add(PutStr(Domain));
            List<byte[]> ret = RemoteExec("RemoveDomainFilter", args);
            return ret != null ? GetBool(ret[0]) : false;
        }

        // Blocklist
        public List<DomainBlocklist> GetDomainBlocklists()
        {
            List<byte[]> args = new List<byte[]>();
            List<byte[]> ret = RemoteExec("GetDomainBlocklists", args);
            return ret != null ? GetBlockList(ret[0]) : null;
        }

        public bool UpdateDomainBlocklist(DomainBlocklist Blocklist)
        {
            List<byte[]> args = new List<byte[]>();
            args.Add(PutBlockEntry(Blocklist));
            List<byte[]> ret = RemoteExec("UpdateDomainBlocklist", args);
            return ret != null ? GetBool(ret[0]) : false;
        }

        public bool RemoveDomainBlocklist(string Url)
        {
            List<byte[]> args = new List<byte[]>();
            args.Add(PutStr(Url));
            List<byte[]> ret = RemoteExec("RemoveDomainBlocklist", args);
            return ret != null ? GetBool(ret[0]) : false;
        }

        public bool RefreshDomainBlocklist(string Url = "") // empty means all
        {
            List<byte[]> args = new List<byte[]>();
            args.Add(PutStr(Url));
            List<byte[]> ret = RemoteExec("RefreshDomainBlocklist", args);
            return ret != null ? GetBool(ret[0]) : false;
        }

        /////////////////////////////////////////
        // Privacy tweaks

        public bool ApplyTweak(TweakList.Tweak tweak)
        {
            List<byte[]> args = new List<byte[]>();
            args.Add(PutTweak(tweak));
            List<byte[]> ret = RemoteExec("ApplyTweak", args);
            return ret != null ? GetBool(ret[0]) : false;
        }

        public bool TestTweak(TweakList.Tweak tweak)
        {
            List<byte[]> args = new List<byte[]>();
            args.Add(PutTweak(tweak));
            List<byte[]> ret = RemoteExec("TestTweak", args);
            return ret != null ? GetBool(ret[0]) : false;
        }

        public bool UndoTweak(TweakList.Tweak tweak)
        {
            List<byte[]> args = new List<byte[]>();
            args.Add(PutTweak(tweak));
            List<byte[]> ret = RemoteExec("UndoTweak", args);
            return ret != null ? GetBool(ret[0]) : false;
        }

        /////////////////////////////////////////
        // Misc

        public bool Quit()
        {
            List<byte[]> ret = RemoteExec("Quit", new List<byte[]>());
            return ret != null ? GetBool(ret[0]) : false;
        }


        public event EventHandler<Priv10Engine.FwEventArgs> ActivityNotification;
        public event EventHandler<Priv10Engine.ChangeArgs> ChangeNotification;
        public event EventHandler<Priv10Engine.UpdateArgs> UpdateNotification;
        
        
        public override void HandlePushNotification(string func, List<byte[]> args)
        {
            //try
            {
                if (Application.Current == null)
                    return; // not ready yet

                if (func == "ActivityNotification")
                {
                    Application.Current.Dispatcher.BeginInvoke(new Action(() =>
                    {
                        ActivityNotification?.Invoke(this, new Priv10Engine.FwEventArgs()
                        {
                            guid = GetGuid(args[0]),
                            entry = GetLogEntry(args[1]),
                            progID = GetProgID(args[2]),
                            services = GetStrList(args[3]),
                            update = GetBool(args[4])
                        });
                    }));
                }
                else if (func == "ChangeNotification")
                {
                    Application.Current.Dispatcher.BeginInvoke(new Action(() =>
                    {
                        ChangeNotification?.Invoke(this, new Priv10Engine.ChangeArgs()
                        {
                            prog = GetProg(args[0]),
                            rule = GetRule(args[1]),
                            type = GetEnum<Priv10Engine.RuleEventType>(args[2]),
                            action = GetEnum<Priv10Engine.RuleFixAction>(args[3])
                        });
                    }));
                }
                else if (func == "UpdateNotification")
                {
                    Application.Current.Dispatcher.BeginInvoke(new Action(() =>
                    {
                        UpdateNotification?.Invoke(this, new Priv10Engine.UpdateArgs()
                        {
                            guid = GetGuid(args[0]),
                            type = GetEnum<Priv10Engine.UpdateArgs.Types>(args[1])
                        });
                    }));
                }
                else
                {
                    throw new Exception("Unknown Notificacion");
                }
            }
            //catch (Exception err)
            //{
            //    AppLog.Exception(err);
            //}
        }
    }
}
