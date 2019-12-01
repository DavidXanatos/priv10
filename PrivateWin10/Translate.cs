using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PrivateWin10
{
    static public class Translate
    {
        static SortedDictionary<string, string> mStrings = new SortedDictionary<string, string>();

        static public void Load(string lang = "")
        {
            if (lang == "")
            {
                CultureInfo ci = CultureInfo.InstalledUICulture;

                /*Console.WriteLine("Default Language Info:");
                Console.WriteLine("* Name: {0}", ci.Name);
                Console.WriteLine("* Display Name: {0}", ci.DisplayName);
                Console.WriteLine("* English Name: {0}", ci.EnglishName);
                Console.WriteLine("* 2-letter ISO Name: {0}", ci.TwoLetterISOLanguageName);
                Console.WriteLine("* 3-letter ISO Name: {0}", ci.ThreeLetterISOLanguageName);
                Console.WriteLine("* 3-letter Win32 API Name: {0}", ci.ThreeLetterWindowsLanguageName);*/

                lang = ci.TwoLetterISOLanguageName;
            }


            mStrings.Add("name_system", "Windows NT Kernel/System");
            mStrings.Add("name_service", "{0} (service: {1})");
            mStrings.Add("name_app", "{0} (app: {1})");
            mStrings.Add("name_global", "All Processes");
            mStrings.Add("prefix_service", "Service: ");
            mStrings.Add("prefix_programm", "Program: ");
            mStrings.Add("prefix_app", "App: ");
            mStrings.Add("sort_no", "Unsorted");
            mStrings.Add("sort_name", "Name");
            mStrings.Add("sort_rname", "Name (rev.)");
            mStrings.Add("sort_act", "Last Activity");
            mStrings.Add("sort_rate", "Data Rate");
            mStrings.Add("sort_socks", "Socket Count");
            mStrings.Add("sort_count", "Module Count");
            mStrings.Add("str_enabled", "Enabled");
            mStrings.Add("str_disabled", "Disabled");
            mStrings.Add("str_undefined", "undefined");
            mStrings.Add("str_all", "All");
            mStrings.Add("str_domain", "Domain");
            mStrings.Add("str_private", "Private");
            mStrings.Add("str_public", "Public");
            mStrings.Add("str_allow", "Allow");
            mStrings.Add("str_block", "Block");
            mStrings.Add("str_all_actions", "All Actions");
            mStrings.Add("str_inbound", "Inbound");
            mStrings.Add("str_outbound", "Outbound");
            mStrings.Add("str_inandout", "Bidirectional");
            mStrings.Add("str_all_rules", "All Rules");
            mStrings.Add("str_open", "open");
            mStrings.Add("str_closed", "closed");
            mStrings.Add("str_listen", "listen");
            mStrings.Add("str_syn_sent", "syn sent");
            mStrings.Add("str_syn_received", "syn received");
            mStrings.Add("str_established", "established");
            mStrings.Add("str_fin_wait_1", "fin wait 1");
            mStrings.Add("str_fin_wait_2", "fin wait 2");
            mStrings.Add("str_close_wait", "close wait");
            mStrings.Add("str_closing", "closing");
            mStrings.Add("str_last_ack", "last ack");
            mStrings.Add("str_time_wait", "time wait");
            mStrings.Add("str_delete_tcb", "delete tcb");
            mStrings.Add("str_fw_blocked", "blocked");
            mStrings.Add("msg_no_sys_merge", "System or Global based entries can not be merged with other entries!");
            mStrings.Add("msg_remove_progs", "Are you sure you want to remove selected programs? All associated firewall rules will be removed as well.");
            mStrings.Add("msg_remove_rules", "Are you sure you want to remove selected rules?");
            mStrings.Add("cat_new", "[New Category]");
            mStrings.Add("cat_none", "Uncategorized");
            mStrings.Add("cat_cats", "Known Categories:");
            mStrings.Add("cat_other", "New/None:");
            mStrings.Add("cat_gen", "Generig:");
            mStrings.Add("msg_cat_name", "Enter new Category name:");
            mStrings.Add("msg_cat_some", "Some Category");
            mStrings.Add("msg_clean_progs", "Are you sure you want clean up the program list?");
            mStrings.Add("msg_clean_progs_ex", "Are you sure you want to remove all program entries which do not have firewall rules or open sockets?");
            mStrings.Add("msg_clean_res", "Removed {0} entries");
            mStrings.Add("msg_no_split_all", "At least one ID must remain in the Program entry!");
            mStrings.Add("svc_all", "[All Services]");
            mStrings.Add("pro_browse", "[Browse for Programm Executable]");
            mStrings.Add("pro_all", "[Applyes to All Programm]");
            mStrings.Add("pro_title", "Browse for Programm Executable...");
            mStrings.Add("pro_sys", "[Windows NT Kernel/System]");
            mStrings.Add("msg_already_exist", "A entry with this Identification already exists.");
            mStrings.Add("pro_any", "[Any Protocol]");
            mStrings.Add("pro_custom", "[Custom Protocol]");
            mStrings.Add("port_any", "[Any Port]");
            mStrings.Add("icmp_all", "[All Types]");
            mStrings.Add("addr_add", "[Add Address]");
            mStrings.Add("msg_rule_failed", "Failed to apply rule properties!");
            mStrings.Add("str_lan", "Lan");
            mStrings.Add("str_ras", "RemoteAccess");
            mStrings.Add("str_wifi", "Wireless");
            mStrings.Add("msg_admin_rights", "{0} requires administrative privilegs to function properly.");
            mStrings.Add("msg_admin_prompt", "{0} requires administrative privilegs operate, restart as admin?");
            mStrings.Add("msg_clear_log", "You are about the clear the entire connection log, do you want to clear the security log too?");
            mStrings.Add("acl_none", "Unconfigured");
            mStrings.Add("acl_silence", "Stop Notify");
            mStrings.Add("acl_allow", "Full Access");
            mStrings.Add("acl_edit", "Custom Config");
            mStrings.Add("acl_lan", "Lan Only");
            mStrings.Add("acl_block", "Block Access");
            mStrings.Add("lbl_prog_info", "Blocked: {0}; Allowed: {1}; Open: {2}\r\nUpload: {3}/s; Download: {4}/s");
            mStrings.Add("lbl_permanent", "Permanent Rule (forever)");
            mStrings.Add("lbl_temp", "Temporary Rule {0}");
            mStrings.Add("msg_clone_rules", "Duplicate selected rules?");
            mStrings.Add("lbl_selec", "All or Select:");
            mStrings.Add("lbl_known", "Known / Recent:");
            mStrings.Add("custom_rule", "Custom Rule for {0}");
            mStrings.Add("lbl_run_as", "Running as: {0}");
            mStrings.Add("str_admin", "Administrator");
            mStrings.Add("str_user", "User");
            mStrings.Add("lbl_run_svc", "; service installed");
            mStrings.Add("err_empty_value", "Value can not be empty");
            mStrings.Add("err_duplicate_value", "Value can not be duplicated");
            mStrings.Add("err_invalid_range", "Invalid Range value");
            mStrings.Add("err_invallid_port", "Invalid Port value");
            mStrings.Add("err_invalid_ip", "Invalid IP Address");
            mStrings.Add("err_invalid_subnet", "Invalid IP Subnet");
            mStrings.Add("lbl_ok", "Ok");
            mStrings.Add("lbl_cancel", "Cancel");
            mStrings.Add("lbl_any_ip", "Any Address");
            mStrings.Add("lbl_selected_ip", "Selected Addresses:");
            mStrings.Add("lbl_notify", "Notify");
            mStrings.Add("lbl_add", "Add");
            mStrings.Add("lbl_split", "Split");
            mStrings.Add("lbl_remove", "Remove");
            mStrings.Add("lbl_name", "Name");
            mStrings.Add("lbl_group", "Group");
            mStrings.Add("lbl_index", "Index");
            mStrings.Add("lbl_progam", "Program");
            mStrings.Add("wnd_notify", "Connection Notification Window");
            mStrings.Add("lbl_prev", "Previous");
            mStrings.Add("lbl_next", "Next");
            mStrings.Add("lbl_remember", "Remember:");
            mStrings.Add("lbl_ignore", "Ignore");
            mStrings.Add("lbl_apply", "Apply");
            mStrings.Add("lbl_direction", "Direction");
            mStrings.Add("lbl_protocol", "Protocol");
            mStrings.Add("lbl_ip_port", "Address:Port");
            mStrings.Add("lbl_remote_host", "Remote Host");
            mStrings.Add("lbl_time_stamp", "Timestamp");
            mStrings.Add("lbl_pid", "PID");
            mStrings.Add("wnd_program", "Program parameters");
            mStrings.Add("lbl_upload", "Upload");
            mStrings.Add("lbl_download", "Download");
            mStrings.Add("lbl_program", "Program");
            mStrings.Add("lbl_exe", "Executable");
            mStrings.Add("lbl_svc", "Only Services");
            mStrings.Add("lbl_app", "Only App Package");
            mStrings.Add("wnd_rule", "Firewall rule");
            mStrings.Add("lbl_rule", "Firewall rule identity");
            mStrings.Add("grp_action", "Action & Scope");
            mStrings.Add("lbl_action", "Action");
            mStrings.Add("lbl_state", "State");
            mStrings.Add("lbl_itf_all", "All Interfaces Types");
            mStrings.Add("lbl_itf_select", "Specific Types:");
            mStrings.Add("lbl_itf_lan", "LAN");
            mStrings.Add("lbl_itf_vpn", "VPN");
            mStrings.Add("lbl_itf_wifi", "Wi-Fi");
            mStrings.Add("lbl_prof_all", "All Profiles");
            mStrings.Add("lbl_prof_sel", "Selected:");
            mStrings.Add("lbl_prof_pub", "Public");
            mStrings.Add("lbl_prof_dmn", "Domain");
            mStrings.Add("lbl_prof_priv", "Private");
            mStrings.Add("grp_network", "Network Properties");
            mStrings.Add("lbl_local_ip", "Local Address");
            mStrings.Add("lbl_remote_ip", "Remote Address");
            mStrings.Add("btn_add_prog", "Add Program");
            mStrings.Add("btn_merge_progs", "Merge Programs");
            mStrings.Add("btn_del_progs", "Remove Program(s)");
            mStrings.Add("btn_cleanup_list", "Cleanup List");
            mStrings.Add("chk_ignore_local", "Ignore localHost");
            mStrings.Add("chk_ignore_lan", "Ignore LAN");
            mStrings.Add("chk_hide_local", "Hide Localhost entries");
            mStrings.Add("chk_hide_lan", "Hide LAN entries");
            mStrings.Add("lbl_sort", "Sort By:");
            mStrings.Add("lbl_type", "Type:");
            mStrings.Add("lbl_filter", "Filter:");
            mStrings.Add("btn_reload", "Reload Rules");
            mStrings.Add("chk_all", "Show All");
            mStrings.Add("grp_firewall", "Firewall Rules");
            mStrings.Add("gtp_con_log", "Connection Log");
            mStrings.Add("grp_tools", "Tools");
            mStrings.Add("grp_view", "View Options");
            mStrings.Add("btn_mk_rule", "Create Rule");
            mStrings.Add("btn_edit_rule", "Edit Rules");
            mStrings.Add("btn_remove_rule", "Remove Rules");
            mStrings.Add("btn_disable_rule", "Disable Rules");
            mStrings.Add("btn_enable_rule", "Enable Rule");
            mStrings.Add("btn_block_rule", "Set Blocking");
            mStrings.Add("btn_allow_rule", "Set Allowing");
            mStrings.Add("btn_clone_rule", "Clone Rule");
            mStrings.Add("chk_hide_disabled", "Hide Disabled");
            mStrings.Add("lbl_filter_rules", "Filter Rules");
            mStrings.Add("btn_clear_log", "Clear Log");
            mStrings.Add("lbl_show_cons", "Show Connections");
            mStrings.Add("lbl_filter_cons", "Filter Connection");
            mStrings.Add("lbl_enabled", "Enabled");
            mStrings.Add("lbl_profiles", "Profiles");
            mStrings.Add("lbl_local_port", "Local Ports");
            mStrings.Add("lbl_remote_port", "Remote Ports");
            mStrings.Add("lbl_interfaces", "Interfaces");
            mStrings.Add("lbl_access", "Access");
            mStrings.Add("lbl_icmp", "ICMP Types");
            mStrings.Add("lbl_edge", "edge Traversal");
            mStrings.Add("lbl_startup_options", "Startup Behaviour");
            mStrings.Add("chk_show_tray", "Show Tray Icon");
            mStrings.Add("chk_autorun", "Autostart at logon");
            mStrings.Add("chk_instal_svc", "Install priv10 Service");
            mStrings.Add("chk_no_uac", "Bypass UAC prompt");
            mStrings.Add("chk_tweak_check", "Monitor Tweaks for changes");
            mStrings.Add("chk_tweak_fix", "Reapply Tweaks that got undone");
            mStrings.Add("lbl_firewall_options", "Windows Firewall Option");
            mStrings.Add("chk_manage_fw", "Manage Windows Firewall");
            mStrings.Add("chk_show_notify", "Show blocked connection attempts");
            mStrings.Add("chk_fw_guard", "Guard Firewall Rules");
            mStrings.Add("chk_fw_guard_alert", "Notify about rule changes");
            mStrings.Add("chk_fw_guard_disable", "Disable not authorized rules");
            mStrings.Add("chk_fw_guard_fix", "Undo unauthorized rule changed");
            mStrings.Add("chk_fix_rules", "Undo 3rd party rule changes");
            mStrings.Add("lbl_filter_mode", "Filtering Mode");
            mStrings.Add("chk_fw_whitelist", "Whitelisting Mode (recommended)");
            mStrings.Add("chk_fw_blacklist", "Blacklisting Mode (Windows default)");
            mStrings.Add("chk_fw_disable", "Disable Windows Firewall");
            mStrings.Add("chk_audit_policy", "Audit Policy (should be set to All)");
            mStrings.Add("lbl_audit_all", "Blocked & Allowed");
            mStrings.Add("lbl_audit_blocked", "Blocked Only");
            mStrings.Add("lbl_audit_off", "Disabled");
            mStrings.Add("wnd_setup", "{0} Initial Setup");
            mStrings.Add("mnu_exit", "E&xit");
            mStrings.Add("mnu_block", "&Block Internet");
            mStrings.Add("msg_dupliate_session", "Another priv10 instance is already running.");
            mStrings.Add("str_in", "{0} In");
            mStrings.Add("str_out", "{0} Out");
            mStrings.Add("app_reload", "[Update App List]");
            mStrings.Add("msg_pick_svc", "[Select Service]");
            mStrings.Add("lbl_host_name", "Hostname");
            mStrings.Add("lbl_last_seen", "Last seen");
            mStrings.Add("lbl_seen_count", "Seen count");
            mStrings.Add("txt_unknown", "Unknown");
            mStrings.Add("tweak_reg", "Registry Tweak");
            mStrings.Add("tweak_gpo", "GPO Tweak");
            mStrings.Add("tweak_svc", "Disable Service");
            mStrings.Add("tweak_task", "Disable Task");
            mStrings.Add("tweak_file", "Block File");
            mStrings.Add("tweak_fw", "Use Firewall");
            mStrings.Add("tweak_undone", ", undone: {0} (!)");
            mStrings.Add("lbl_programs", "Programs");
            mStrings.Add("lbl_view_options", "View Options");
            mStrings.Add("lbl_sort_and", "Sort & Highlight");
            mStrings.Add("lbl_rules_and", "Rules and Details");
            mStrings.Add("lbl_view_filter", "View Filter");
            mStrings.Add("cat_uncat", "[Uncategorized]");
            mStrings.Add("filter_presets", "Filter Presets");
            mStrings.Add("filter_program", "Program Filter");
            mStrings.Add("filter_activity", "Activity Filter");
            mStrings.Add("filter_category", "Category Filter");
            mStrings.Add("filter_all", "All");
            mStrings.Add("filter_programs", "Programs");
            mStrings.Add("filter_services", "Services");
            mStrings.Add("filter_system", "System");
            mStrings.Add("filter_apps", "Apps");
            mStrings.Add("filter_uncat", "Uncategorized");
            mStrings.Add("filer_multi", "Multiple Categories");
            mStrings.Add("filter_types", "Types:");
            mStrings.Add("filter_recent", "Activity:");
            mStrings.Add("filter_recent_not", "[No Filter]");
            mStrings.Add("filter_recent_active", "Recent");
            mStrings.Add("filter_recent_blocked", "Blocked");
            mStrings.Add("filter_recent_allowed", "Allowed");
            mStrings.Add("filter_sockets", "Sockets:");
            mStrings.Add("filter_sockets_not", "[No Filter]");
            mStrings.Add("filter_sockets_any", "Any Sockets");
            mStrings.Add("filter_sockets_all", "All Sockets");
            mStrings.Add("filter_sockets_web", "HTTP 80 and 443");
            mStrings.Add("filter_sockets_tcp", "TCP Sockets");
            mStrings.Add("filter_sockets_client", "TCP Client");
            mStrings.Add("filter_sockets_server", "TCP Server");
            mStrings.Add("filter_sockets_udp", "UDP Sockets");
            mStrings.Add("filter_sockets_raw", "Raw Sockets");
            mStrings.Add("filter_sockets_none", "Without");
            mStrings.Add("filter_rules", "Rules:");
            mStrings.Add("filter_rules_not", "[No Filter]");
            mStrings.Add("filter_rules_any", "with Any");
            mStrings.Add("filter_rules_enabled", "with Active");
            mStrings.Add("filter_rules_disabled", "with Disabled");
            mStrings.Add("filter_rules_none", "Without");
            mStrings.Add("lbl_last_preset", "Last Preset");
            mStrings.Add("msg_tweak_un_done", "Tweak {0} from {1} is not applied!");
            mStrings.Add("msg_tweak_stuck", "Failed to reapply Tweak {0} from {1}.");
            mStrings.Add("msg_tweak_fixed", "Successfully reapplied Tweak {0} from {1}.");
            mStrings.Add("msg_rule_event", "Firewall rule \"{0}\" for \"{1}\" was {2}.");
            mStrings.Add("msg_rule_disabled", ", the rule has been disabled");
            mStrings.Add("msg_rule_restored", ", the original rule was restored");
            mStrings.Add("msg_rule_added", "Added");
            mStrings.Add("msg_rule_changed", "Changed");
            mStrings.Add("msg_rule_removed", "Removed");
            mStrings.Add("msg_rules_approved", "All valid Firewall Rules have been approved.");
            mStrings.Add("lbl_log_level", "Level");
            mStrings.Add("lbl_log_type", "Category");
            mStrings.Add("lbl_log_event", "Event");
            mStrings.Add("lbl_message", "Message");
            mStrings.Add("log_error", "Error");
            mStrings.Add("log_warning", "Warning");
            mStrings.Add("log_info", "Information");
            mStrings.Add("log_firewall", "Firewall");
            mStrings.Add("log_tweaks", "Tweaks");
            mStrings.Add("log_other", "Other");
            mStrings.Add("msg_tweaks_updated", "The tweak list has been updated, the old list was backuped to {0}");
            mStrings.Add("msg_stop_svc", "Do you want to stop the priv10 service too?");
            mStrings.Add("msg_stop_svc_err", "Failed to stop priv10 service!\r\nTry running net stop priv10 from an elevated command prompt.");
            mStrings.Add("str_all_events", "All Events");
            mStrings.Add("str_allowed", "Allowed");
            mStrings.Add("str_blocked", "Blocked");
            mStrings.Add("str_no_inet", "Hide Internet (WWW) Traffic");
            mStrings.Add("str_no_lan", "Hide LAN (Ethernet/WiFi) Traffic");
            mStrings.Add("str_no_multi", "Hide Multicast Packets");
            mStrings.Add("str_no_local", "Hide Localhost Traffic");
            mStrings.Add("str_no_disabled", "Hide Disabled Rules");
            mStrings.Add("btn_approve_rule", "Approve current rules");
            mStrings.Add("btn_restore_rule", "Restore original rules");
            mStrings.Add("btn_redo_rule", "Redo changes");
            mStrings.Add("btn_approve_all", "Approve all current Rules");
            mStrings.Add("btn_restore_all", "Restore all original Rules");
            mStrings.Add("btn_redo_all", "Redo all rule changes");
            mStrings.Add("btn_cleanup_rules", "Cleanup Rules");
            mStrings.Add("msg_approve_all", "Do you really want to approve all Firewall rule(s) changes?");
            mStrings.Add("msg_restore_all", "Do you really want to restore all changed Firewall Rule?");
            mStrings.Add("msg_apply_all", "Do you really want to reapply all Firewall Rule changes?");
            mStrings.Add("filter_access", "Access granted:");
            mStrings.Add("acl_any", "[No Filter]");
            mStrings.Add("acl_warn", "[Warning State]");
            mStrings.Add("menu_setup", "Run Setup Wizard");
            mStrings.Add("menu_uninstall", "Uninstall This Application");
            mStrings.Add("msg_uninstall_this", "Do you really want to uninstall {0}?");
            mStrings.Add("msg_dns_proxy_err", "Failed to start the DNS Proxy, check if port UDP {0} is not in use by an other application and retry.");
            mStrings.Add("msg_bad_dns_filter", "The entered filter expression is not valid.");
            mStrings.Add("msg_dns_filter_dup", "The entered domain is already listed.");
            mStrings.Add("msg_remove_items", "Do you really want to remove the selected items?");
            mStrings.Add("msg_restore_std", "Do you really want to overwrite the current configuration with default values?");
            //mStrings.Add("", "");


            string langINI = App.appPath + @"\Translation.ini";

            if (!File.Exists(langINI))
            {
                foreach (string key in mStrings.Keys)
                    App.IniWriteValue("en", key, mStrings[key], langINI);
                return;
            }

            if (lang != "en")
            {
                var Langs = App.IniEnumSections(langINI);
                if (Langs.Contains(lang))
                {
                    foreach (string key in mStrings.Keys.ToList())
                    {
                        string str = App.IniReadValue(lang, key, "", langINI);
                        if (str.Length == 0)
                            continue;

                        mStrings.Remove(key);
                        mStrings.Add(key, str);
                    }
                }
            }
        }

        static public string fmt(string id, params object[] args)
        {
            try
            {
                string str = id;
                mStrings.TryGetValue(id, out str);
                return string.Format(str, args);
            }
            catch
            {
                return "err on " + id;
            }
        }
    }
}


