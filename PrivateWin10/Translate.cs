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
            

            mStrings.Add("name_system", "Windows NT-Kernel/System");
            mStrings.Add("name_service", "{0} (service: {1})");
            mStrings.Add("name_app", "{0} (app: {1})");
            mStrings.Add("name_global", "All Processes");
            mStrings.Add("prefix_service", "Service: ");
            mStrings.Add("prefix_programm", "Programm: ");
            mStrings.Add("prefix_app", "App: ");
            mStrings.Add("sort_no", "Unsorted");
            mStrings.Add("sort_name", "Name");
            mStrings.Add("sort_rname", "Name (rev.)");
            mStrings.Add("sort_act", "Last Activity");
            mStrings.Add("sort_count", "Module Count");
            mStrings.Add("filter_all", "All");
            mStrings.Add("filter_programs", "Programs");
            mStrings.Add("filter_services", "Services");
            mStrings.Add("filter_system", "System");
            mStrings.Add("filter_apps", "Apps");
            mStrings.Add("filter_uncat", "Uncategorized");
            mStrings.Add("filer_multi", "Multiple Categories");
            mStrings.Add("str_enabled", "Enabled");
            mStrings.Add("str_disabled", "Disabled");
            mStrings.Add("str_undefined", "undefined");
            mStrings.Add("str_all", "All");
            mStrings.Add("str_domain", "Domain");
            mStrings.Add("str_private", "Private");
            mStrings.Add("str_public", "Public");
            mStrings.Add("str_allow", "Allow");
            mStrings.Add("str_block", "Block");
            mStrings.Add("str_inbound", "Inbound");
            mStrings.Add("str_outbound", "Outbound");
            mStrings.Add("str_inandout", "Bidirectional");
            mStrings.Add("msg_no_sys_merge", "System or Global entries can not be merged with other entries!");
            mStrings.Add("msg_remove_progs", "Are you sure you want to remove the selected programs? All associated firewal rules will be removed aswell.");
            mStrings.Add("msg_remove_rules", "Are you sure you want to remove the selected rules?");
            mStrings.Add("cat_new", "[New Category]");
            mStrings.Add("cat_none", "Uncategorized");
            mStrings.Add("cat_cats", "Known Categories:");
            mStrings.Add("cat_other", "New/None:");
            mStrings.Add("cat_gen", "Generig:");
            mStrings.Add("msg_cat_name", "Enter new Category name:");
            mStrings.Add("msg_cat_some", "Some Category");
            mStrings.Add("msg_clean_progs", "Are you sure you want clean up the program list and remove all entries without firewall rules?");
            mStrings.Add("msg_clean_res", "Removed {0} entries");
            mStrings.Add("msg_no_split_all", "At least one ID must remain in the Program entry!");
            mStrings.Add("svc_all", "[All Services]");
            mStrings.Add("pro_browse", "[Browse for Programm Executable]");
            mStrings.Add("pro_all", "[Applyes to All Programm]");
            mStrings.Add("pro_title", "Browse for Programm Executable...");
            mStrings.Add("pro_sys", "[Windows NT-Kernel/System]");
            mStrings.Add("msg_already_exist", "A entry with this Identification already exists.");
            mStrings.Add("pro_any", "[Any Protocol]");
            mStrings.Add("pro_custom", "[Custom Protocol]");
            mStrings.Add("port_any", "[Any Port]");
            mStrings.Add("icmp_all", "[All Types]");
            mStrings.Add("addr_add", "[Add Address]");
            mStrings.Add("msg_rule_failed", "Failed to Apply rule properties!");
            mStrings.Add("str_lan", "Lan");
            mStrings.Add("str_ras", "RemoteAccess");
            mStrings.Add("str_wifi", "Wireless");
            mStrings.Add("msg_admin_rights", "{0} requirers administrative privilegs to function properly.");
            mStrings.Add("msg_admin_prompt", "{0} requirers administrative privilegs operate, restart as admin?");
            mStrings.Add("msg_clear_log", "You are about the clear the entier connection log, do you also want to clear the security log?");
            mStrings.Add("acl_none", "Unconfigured");
            mStrings.Add("acl_silence", "Stop Notify");
            mStrings.Add("acl_allow", "Full Access");
            mStrings.Add("acl_edit", "Custom Config");
            mStrings.Add("acl_lan", "Lan Only");
            mStrings.Add("acl_block", "Block Access");
            mStrings.Add("lbl_info", "Blocked: {0}; Allowed: {1}");
            mStrings.Add("lbl_permanent", "Permanent Rule (forever)");
            mStrings.Add("lbl_temp", "Temporary Rule {0}");
            mStrings.Add("msg_clone_rules", "Duplicate selected rules?");
            mStrings.Add("lbl_selec", "All or Select:");
            mStrings.Add("lbl_known", "Known / Recent:");
            mStrings.Add("custom_rule", "Custom Rule for {0}");
            mStrings.Add("lbl_run_as", "Running as: {0}");
            mStrings.Add("str_admin", "Admininstrator");
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
            mStrings.Add("lbl_progam", "Program");
            mStrings.Add("wnd_notify", "Connection Notification Window");
            mStrings.Add("lbl_prev", "Previouse");
            mStrings.Add("lbl_next", "Next");
            mStrings.Add("lbl_remember", "Remember:");
            mStrings.Add("lbl_ignore", "Ignore");
            mStrings.Add("lbl_apply", "Apply");
            mStrings.Add("lbl_direction", "Direction");
            mStrings.Add("lbl_protocol", "Protocol");
            mStrings.Add("lbl_ip_port", "Address:Port");
            mStrings.Add("lbl_time_stamp", "TimeStamp");
            mStrings.Add("wnd_program", "Program parameters");
            mStrings.Add("lbl_program", "Program");
            mStrings.Add("lbl_exe", "Executable");
            mStrings.Add("lbl_svc", "Only Services");
            mStrings.Add("lbl_app", "Only App Package");
            mStrings.Add("wnd_rule", "Firewall rule");
            mStrings.Add("lbl_rule", "Firewall rule identity");
            mStrings.Add("grp_action", "Action & Scope");
            mStrings.Add("lbl_action", "Action");
            mStrings.Add("lbl_itf_all", "All Interfaces Types");
            mStrings.Add("lbl_itf_select", "Specific Types:");
            mStrings.Add("lbl_itf_lan", "LAN");
            mStrings.Add("lbl_itf_vpn", "VPN");
            mStrings.Add("lbl_itf_wifi", "WiFi");
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
            mStrings.Add("lbl_sort", "Sort:");
            mStrings.Add("lbl_type", "Type:");
            mStrings.Add("lbl_filter", "Filter:");
            mStrings.Add("btn_reload", "Reload Rules");
            mStrings.Add("chk_all", "Show All");
            mStrings.Add("grp_firewall", "Firewall Rules");
            mStrings.Add("gtp_con_log", "Connection Log");
            mStrings.Add("grp_tools", "Tools");
            mStrings.Add("grp_view", "View Options");
            mStrings.Add("btn_mk_rule", "Create Rule");
            mStrings.Add("btn_edit_rule", "Enable Rules");
            mStrings.Add("btn_disable_rule", "Disable Rules");
            mStrings.Add("btn_enable_rule", "Remove Rule");
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
            mStrings.Add("lbl_icmp", "ICMP Types");
            mStrings.Add("lbl_edge", "edge Traversal");
            mStrings.Add("lbl_startup_options", "Startup Behavioure");
            mStrings.Add("chk_show_tray", "Show Tray Icon");
            mStrings.Add("chk_autorun", "Autostart at logon");
            mStrings.Add("chk_instal_svc", "Install Service (priv10)");
            mStrings.Add("chk_no_uac", "Bypass UAC prompt");
            mStrings.Add("lbl_firewall_options", "Windows Firewall Option");
            mStrings.Add("chk_manage_fw", "Manage Windows Firewall");
            mStrings.Add("chk_show_notify", "Show blocked connection atemps");
            mStrings.Add("lbl_filter_mode", "Filtering Mode");
            mStrings.Add("chk_fw_whitelist", "White-List Mode (recommended)");
            mStrings.Add("chk_fw_blacklist", "Black-List Mode (default)");
            mStrings.Add("chk_fw_disable", "Disable Windows Firewall");
            mStrings.Add("chk_audit_policy", "Audit Policy (Should be set to All)");
            mStrings.Add("lbl_audit_all", "Blocked & Allowed");
            mStrings.Add("lbl_audit_blocked", "Blocked Only");
            mStrings.Add("lbl_audit_off", "Disabled");
            mStrings.Add("wnd_setup", "Private WinTen Initial Setup");



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
