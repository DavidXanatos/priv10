using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Security.AccessControl;
using System.Security.Principal;
using System.ServiceProcess;
using TaskScheduler;
using LocalPolicy;
using Microsoft.Win32;
using System.IO;

namespace PrivateWin10
{
    public class Tweaks
    {
        public List<Category> Categorys = new List<Category>();

        public Tweaks()
        {
            TweakStore.InitTweaks(Categorys);
        }

        static public bool ApplyTweak(Tweak tweak)
        {
            switch (tweak.Type)
            {
                case TweakType.SetRegistry:     return SetRegistryTweak(tweak.Path, tweak.Name, tweak.Value, tweak.usrLevel);
                case TweakType.SetGPO:          return SetGPOTweak(tweak.Path, tweak.Name, tweak.Value, tweak.usrLevel);
                case TweakType.DisableService:  return DisableService(tweak.Name);
                case TweakType.DisableTask:     return DisableTask(tweak.Path, tweak.Name);
                case TweakType.BlockFile:       return BlockFile(tweak.Path);
                //case TweakType.UseFirewall:   return ...
            }
            return false;
        }

        static public bool TestTweak(Tweak tweak)
        {
            switch (tweak.Type)
            {
                case TweakType.SetRegistry:     return TestRegistryTweak(tweak.Path, tweak.Name, tweak.Value, tweak.usrLevel);
                case TweakType.SetGPO:          return TestGPOTweak(tweak.Path, tweak.Name, tweak.Value, tweak.usrLevel);
                case TweakType.DisableService:  return IsServiceEnabled(tweak.Name) == false;
                case TweakType.DisableTask:     return IsTaskEnabled(tweak.Path, tweak.Name) == false;
                case TweakType.BlockFile:       return IsFileBlocked(tweak.Path);
                //case TweakType.UseFirewall:   return ...
            }
            return false;
        }

        static public bool UndoTweak(Tweak tweak)
        {
            switch (tweak.Type)
            {
                case TweakType.SetRegistry:     return SetRegistryTweak(tweak.Path, tweak.Name, null, tweak.usrLevel); 
                case TweakType.SetGPO:          return SetGPOTweak(tweak.Path, tweak.Name, null, tweak.usrLevel); 
                case TweakType.DisableService:  return DisableService(tweak.Name, true); 
                case TweakType.DisableTask:     return DisableTask(tweak.Path, tweak.Name, true); 
                case TweakType.BlockFile:       return BlockFile(tweak.Path, true); 
                //case TweakType.UseFirewall:   return ...
            }
            return false;
        }

        // *** Registry ***

        static public bool TestRegistryTweak(string path, string name, object value, bool usrLevel = false)
        {
            try
            {
                var subKey = (usrLevel ? Registry.CurrentUser : Registry.LocalMachine).OpenSubKey(path, false);
                if (subKey == null)
                    return false;
                return CmpRegistryValue(subKey, name, value);
            }
            catch (Exception err)
            {
                AppLog.Line("Error in {0}: {1}", MiscFunc.GetCurrentMethod(), err.Message);
            }
            return false;
        }

        static public bool SetRegistryTweak(string path, string name, object value, bool usrLevel = false)
        {
            try
            {
                var subKey = (usrLevel ? Registry.CurrentUser : Registry.LocalMachine).CreateSubKey(path, true);
                SetRegistryValue(subKey, name, value);
                return true;
            }
            catch (Exception err)
            {
                AppLog.Line("Error in {0}: {1}", MiscFunc.GetCurrentMethod(), err.Message);
            }
            return false;
        }

        static private void SetRegistryValue(RegistryKey subKey, string name, object value)
        {
            /*String        typeof(string);
            ExpandString    typeof(string);
            DWord           typeof(Int32);
            QWord           typeof(Int64);
            Binary          typeof(byte[]);
            MultiString     typeof(string[]);*/

            if (value == null)
                subKey.DeleteValue(name, false);
            else if (value.GetType() == typeof(Int64))
                subKey.SetValue(name, value, RegistryValueKind.QWord);
            else
                subKey.SetValue(name, value);
        }

        static private bool CmpRegistryValue(RegistryKey subKey, string name, object value)
        {
            object curValue = subKey.GetValue(name, null);

            if (value == null)
                return (curValue == null);
            if (curValue == null)
                return false;

            Type curType = curValue.GetType();
            Type type = value.GetType();
            if (curType != type)
                return false;

            return value.Equals(curValue);
        }


        // *** GPO ***

        static public bool TestGPOTweak(string path, string name, object value, bool usrLevel = false)
        {
            try
            {
                var gpo = new ComputerGroupPolicyObject(new GroupPolicyObjectSettings(true, true)); // read only so it does not fail without admin rights
                var key = gpo.GetRootRegistryKey(usrLevel ? GroupPolicySection.User : GroupPolicySection.Machine);
                var subKey = key.CreateSubKey(path);
                return CmpRegistryValue(subKey, name, value);
            }
            catch (Exception err)
            {
                AppLog.Line("Error in {0}: {1}", MiscFunc.GetCurrentMethod(), err.Message);
            }
            return false;
        }

        static public bool SetGPOTweak(string path, string name, object value, bool usrLevel = false)
        {
            try
            {
                var gpo = new ComputerGroupPolicyObject();
                var key = gpo.GetRootRegistryKey(usrLevel ? GroupPolicySection.User : GroupPolicySection.Machine);
                var subKey = key.CreateSubKey(path);
                SetRegistryValue(subKey, name, value);
                gpo.Save();
                return true;
            }
            catch (Exception err)
            {
                AppLog.Line("Error in {0}: {1}", MiscFunc.GetCurrentMethod(), err.Message);
            }
            return false;
        }


        // *** Services ***

        static public bool IsServiceEnabled(string name)
        {
            bool ret = false;
            ServiceController svc = new ServiceController(name);
            try
            {
                ret = svc.StartType != ServiceStartMode.Disabled;
            }
            catch (Exception err)
            {
                AppLog.Line("Error in {0}: {1}", MiscFunc.GetCurrentMethod(), err.Message);
            }
            svc.Close();
            return ret;
        }

        static public bool DisableService(string name, bool UnDo = false)
        {
            bool ret = false;
            ServiceController svc = new ServiceController(name); // Windows Update Service
            try
            {
                if (UnDo)
                {
                    if (svc.Status != ServiceControllerStatus.Running)
                    {
                        ServiceHelper.ChangeStartMode(name, ServiceHelper.ServiceBootFlag.DemandStart);
                        //svc.Start();
                    }
                }
                else
                {
                    if (svc.Status == ServiceControllerStatus.Running)
                        svc.Stop();
                    ServiceHelper.ChangeStartMode(name, ServiceHelper.ServiceBootFlag.Disabled);
                }
                ret = true;
            }
            catch (Exception err)
            {
                AppLog.Line("Error in {0}: {1}", MiscFunc.GetCurrentMethod(), err.Message);
            }
            svc.Close();
            return ret;
        }


        // *** Tasks ***

        static public List<string> EnumTasks(string path)
        {
            List<string> list = new List<string>();
            try
            {
                TaskScheduler.TaskScheduler service = new TaskScheduler.TaskScheduler();
                service.Connect();
                ITaskFolder folder = service.GetFolder(path);

                foreach (IRegisteredTask task in folder.GetTasks((int)_TASK_ENUM_FLAGS.TASK_ENUM_HIDDEN))
                    list.Add(task.Name);
            }
            catch (Exception err)
            {
                AppLog.Line("Error in {0}: {1}", MiscFunc.GetCurrentMethod(), err.Message);
                return null;
            }
            return list;
        }

        static public bool IsTaskEnabled(string path, string name)
        {
            if (name == "*")
            {
                List<string> names = EnumTasks(path);
                if (names == null)
                    return true; // we dont know so just in case
                foreach (string found in names)
                {
                    if (IsTaskEnabled(path, found))
                        return true;
                }
                return false;
            }

            try
            {
                TaskScheduler.TaskScheduler service = new TaskScheduler.TaskScheduler();
                service.Connect();
                ITaskFolder folder = service.GetFolder(path);
                IRegisteredTask task = folder.GetTask(name);
                return task.Enabled;
            }
            catch (FileNotFoundException)
            {
                return false;
            }
            catch (Exception err)
            {
                AppLog.Line("Error in {0}: {1}", MiscFunc.GetCurrentMethod(), err.Message);
            }
            return true; // we dont know so just in case
        }

        static public bool DisableTask(string path, string name, bool UnDo = false)
        {
            if (name == "*")
            {
                List<string> names = EnumTasks(path);
                if (names == null)
                    return false;
                foreach (string found in names)
                {
                    if (!DisableTask(path, found, UnDo))
                        return false;
                }
                return true;
            }

            try
            {
                TaskScheduler.TaskScheduler service = new TaskScheduler.TaskScheduler();
                service.Connect();
                ITaskFolder folder = service.GetFolder(path);
                IRegisteredTask task = folder.GetTask(name);
                task.Enabled = UnDo ? true : false; // todo have old state saved
                return true;
            }
            catch (Exception err)
            {
                AppLog.Line("Error in {0}: {1}", MiscFunc.GetCurrentMethod(), err.Message);
            }
            return false;
        }


        // *** Files ***

        static public bool IsFileBlocked(string path)
        {
            try
            {
                path = Environment.ExpandEnvironmentVariables(path);

                if (!File.Exists(path))
                    return true;

                FileSecurity ac = File.GetAccessControl(path);
                AuthorizationRuleCollection rules = ac.GetAccessRules(true, true, typeof(System.Security.Principal.SecurityIdentifier)); // get as SID not string
                foreach (FileSystemAccessRule rule in rules)
                {
                    if (rule.AccessControlType != AccessControlType.Deny)
                        continue;
                    if (!rule.IdentityReference.Value.Equals(FileOps.SID_Worls))
                        continue;
                    if ((rule.FileSystemRights & FileSystemRights.ExecuteFile) != 0)
                        return true;
                }
            }
            catch (Exception err)
            {
                AppLog.Line("Error in {0}: {1}", MiscFunc.GetCurrentMethod(), err.Message);
            }
            return false;
        }

        static public bool BlockFile(string path, bool UnDo = false)
        {
            try
            {
                path = Environment.ExpandEnvironmentVariables(path);
                if (!FileOps.TakeOwn(path))
                    return false;

                FileSecurity ac = File.GetAccessControl(path);
                if (UnDo)
                {
                    AuthorizationRuleCollection rules = ac.GetAccessRules(true, true, typeof(System.Security.Principal.SecurityIdentifier)); // get as SID not string
                    foreach (FileSystemAccessRule rule in rules)
                        ac.RemoveAccessRule(rule);
                }
                else
                    ac.AddAccessRule(new FileSystemAccessRule(new SecurityIdentifier(FileOps.SID_Worls), FileSystemRights.ExecuteFile, AccessControlType.Deny));
                File.SetAccessControl(path, ac);
                return true;
            }
            catch (Exception err)
            {
                AppLog.Line("Error in {0}: {1}", MiscFunc.GetCurrentMethod(), err.Message);
            }
            return false;
        }

        // *** Firewall ***
    }

    [Serializable()]
    public class Category
    {
        public string Label = "";

        public List<Group> Groups = new List<Group>();

        [field: NonSerialized]
        public event EventHandler<EventArgs> StatusChanged;

        public Category(string label)
        {
            Label = label;
        }

        public bool IsAvailable()
        {
            foreach (Group group in Groups)
            {
                if (group.IsAvailable())
                    return true;
            }
            return false;
        }

        public void Add(Group group)
        {
            group.StatusChanged += OnStatusChanged;
            Groups.Add(group);
        }

        void OnStatusChanged(object sender, EventArgs arg)
        {
            StatusChanged?.Invoke(this, new EventArgs());
        }
    }

    [Serializable()]
    public class Group
    {
        public string Label = "";

        public List<Tweak> Tweaks = new List<Tweak>();

        [field: NonSerialized]
        public event EventHandler<EventArgs> StatusChanged;

        public Group(string label)
        {
            Label = label;
        }

        public bool IsAvailable()
        {
            foreach (Tweak tweak in Tweaks)
            {
                if (tweak.IsAvailable())
                    return true;
            }
            return false;
        }

        public void Add(Tweak tweak)
        {
            tweak.StatusChanged += OnStatusChanged;
            Tweaks.Add(tweak);
        }

        void OnStatusChanged(object sender, EventArgs arg)
        {
            StatusChanged?.Invoke(this, new EventArgs());
        }
    }
    

    public enum TweakType
    {
        None = 0,
        SetRegistry,
        SetGPO,
        DisableService,
        DisableTask,
        BlockFile,
        UseFirewall
    }

    [Serializable()]
    public class Tweak
    {
        public string Label;
        public TweakType Type;
        public string Name;
        public string Path;
        public bool usrLevel = false;
        public object Value;

        public WinVer winVer = null;

        public bool Optional = false;
        public bool? Sellected = null;

        [field: NonSerialized]
        public event EventHandler<EventArgs> StatusChanged;
        //public bool Applyed = false;

        public Tweak(string lebel, TweakType type, WinVer ver)
        {
            Label = lebel;
            Type = type;
            winVer = ver;
        }

        public bool IsAvailable()
        {
            return winVer.TestHost();
        }

        public bool IsDefault()
        {
            switch (Type)
            {
                case TweakType.BlockFile:
                case TweakType.UseFirewall:
                    return false;
            }
            return true;
        }

        public static string GetTypeStr(TweakType type)
        {
            switch (type)
            {
                case TweakType.SetRegistry:     return "Registry Tweak";
                case TweakType.SetGPO:          return "GPO Tweak";
                case TweakType.DisableService:  return "Disable Service";
                case TweakType.DisableTask:     return "Disable Task";
                case TweakType.BlockFile:       return "Block File";
                case TweakType.UseFirewall:     return "Use Firewall"; // todo
            }
            return "Unknown";
        }

        // Note: on windows 10 1803 services running under the system account can not access group policy objects

        public bool Apply(bool user = false)
        {
            if(user)
                Sellected = true;
            //Applyed = true;
            bool ret;
            //if(usrLevel) // execute user level tweaks in gui context
                ret = Tweaks.ApplyTweak(this);
            //else
            //    ret = App.itf.ApplyTweak(this);
            StatusChanged?.Invoke(this, new EventArgs());
            return ret;
        }

        public bool Test()
        {
            //return Applyed;
            bool ret;
            //if (usrLevel) // execute user level tweaks in gui context
                ret = Tweaks.TestTweak(this);
            //else
            //    ret = App.itf.TestTweak(this);
            return ret;
        }

        public bool Undo(bool user = false)
        {
            if (user)
                Sellected = false;
            //Applyed = false;
            bool ret;
            //if (usrLevel) // execute user level tweaks in gui context
                ret = Tweaks.UndoTweak(this);
            //else
            //    ret = App.itf.UndoTweak(this);
            StatusChanged?.Invoke(this, new EventArgs());
            return ret;
        }
    }
}
