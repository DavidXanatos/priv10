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
            mStrings.Add("msg_remove_progs", "Are you sure you want to remove the selected programs? \r\nAll associated firewal rules will be removed aswell.");
            mStrings.Add("msg_remove_rules", "Are you sure you want to remove the selected rules?");
            mStrings.Add("cat_new", "[New Category]");
            mStrings.Add("cat_none", "Uncategorized");
            mStrings.Add("cat_cats", "Known Categories:");
            mStrings.Add("cat_other", "New/None:");
            mStrings.Add("cat_gen", "Generig:");
            mStrings.Add("msg_cat_name", "Enter new Category name:");
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
