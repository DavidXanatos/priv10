using System;
using System.Collections.Generic;
using System.Security.AccessControl;
using System.Security.Principal;
using System.ServiceProcess;
using TaskScheduler;
using LocalPolicy;
using Microsoft.Win32;
using System.IO;
using System.Threading;
using System.Diagnostics;
using System.Windows.Threading;
using MiscHelpers;
using static TweakEngine.TweakList;

namespace TweakEngine
{
    public static class TweakTools
    {
        static public bool ApplyTweak(Tweak tweak)
        {
            switch (tweak.Type)
            {
                case TweakType.SetRegistry: return SetRegistryTweak(tweak.Path, tweak.Key, tweak.Value, tweak.usrLevel);
                case TweakType.SetGPO: return SetGPOTweak(tweak.Path, tweak.Key, tweak.Value, tweak.usrLevel);
                case TweakType.DisableService: return DisableService(tweak.Key);
                case TweakType.DisableTask: return DisableTask(tweak.Path, tweak.Key);
                case TweakType.BlockFile: return BlockFile(tweak.Path);
                    //case TweakType.UseFirewall:   return ...
            }
            return false;
        }

        static public bool TestTweak(Tweak tweak)
        {
            switch (tweak.Type)
            {
                case TweakType.SetRegistry: return TestRegistryTweak(tweak.Path, tweak.Key, tweak.Value, tweak.usrLevel);
                case TweakType.SetGPO: return TestGPOTweak(tweak.Path, tweak.Key, tweak.Value, tweak.usrLevel);
                case TweakType.DisableService: return IsServiceEnabled(tweak.Key) == false;
                case TweakType.DisableTask: return IsTaskEnabled(tweak.Path, tweak.Key) == false;
                case TweakType.BlockFile: return IsFileBlocked(tweak.Path);
                    //case TweakType.UseFirewall:   return ...
            }
            return false;
        }

        static public bool UndoTweak(Tweak tweak)
        {
            switch (tweak.Type)
            {
                case TweakType.SetRegistry: return UndoRegistryTweak(tweak.Path, tweak.Key, tweak.usrLevel);
                case TweakType.SetGPO: return UndoGPOTweak(tweak.Path, tweak.Key, tweak.usrLevel);
                case TweakType.DisableService: return EnableService(tweak.Key);
                case TweakType.DisableTask: return DisableTask(tweak.Path, tweak.Key, true);
                case TweakType.BlockFile: return UnBlockFile(tweak.Path);
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
                AppLog.Exception(err);
            }
            return false;
        }

        static public bool SetRegistryTweak(string path, string name, object value, bool usrLevel = false)
        {
            try
            {
                var subKey = (usrLevel ? Registry.CurrentUser : Registry.LocalMachine).CreateSubKey(path);

                // store old value for undo
                var old_value = subKey.GetValue(name);
                if (old_value != null)
                    subKey.SetValue("Old" + name, old_value);

                SetRegistryValue(subKey, name, value);
                return true;
            }
            catch (Exception err)
            {
                AppLog.Exception(err);
            }
            return false;
        }

        static public bool UndoRegistryTweak(string path, string name, bool usrLevel = false)
        {
            try
            {
                var subKey = (usrLevel ? Registry.CurrentUser : Registry.LocalMachine).CreateSubKey(path);

                object value = subKey.GetValue("Old" + name);

                SetRegistryValue(subKey, name, value);

                subKey.DeleteValue("Old" + name, false);
                return true;
            }
            catch (Exception err)
            {
                AppLog.Exception(err);
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

        static ReaderWriterLockSlim gpoLocker = new ReaderWriterLockSlim();
        static ComputerGroupPolicyObject gpoObject = null;
        static Dispatcher dispatcher = null;
        static Timer gpoSaveTimer = null;
        static int GPO_SAVE_DELAY = 250;

        static ComputerGroupPolicyObject GetGPO(bool Writeable = true)
        {
            Debug.Assert(gpoLocker.IsReadLockHeld || gpoLocker.IsWriteLockHeld);

            if (gpoObject != null)
                return gpoObject;

            if (!Writeable)    
                return new ComputerGroupPolicyObject(new GroupPolicyObjectSettings(true, true)); // read only so it does not fail without admin rights

            if (gpoObject == null)
                gpoObject = new ComputerGroupPolicyObject();
            return gpoObject;
        }

        static private bool SaveGPO()
        {
            DisposeTimer();
            dispatcher = Dispatcher.CurrentDispatcher;
            gpoSaveTimer = new System.Threading.Timer(TimerElapsed, null, GPO_SAVE_DELAY, GPO_SAVE_DELAY);
            return true;
        }

        static private void TimerElapsed(Object obj)
        {
            DisposeTimer();

            dispatcher.Invoke(new Action(() => {
                gpoLocker.EnterWriteLock();

                for (int i = 1; i <= 30; i++)
                {
                    try
                    {
                        gpoObject.Save();
                        gpoObject = null;
                        break;
                    }
                    catch (FileLoadException)
                    {
                        AppLog.Debug("Retrying gpo.Save() ({0})", i);
                        Thread.Sleep(100 * i);
                    }
                }

                gpoLocker.ExitWriteLock();
            }));
        }

        static private void DisposeTimer()
        {
            if (gpoSaveTimer != null)
            {
                gpoSaveTimer.Dispose();
                gpoSaveTimer = null;
            }
        }


        static public bool TestGPOTweak(string path, string name, object value, bool usrLevel = false)
        {
            gpoLocker.EnterReadLock();
            try
            {
                var gpo = GetGPO(false);
                var key = gpo.GetRootRegistryKey(usrLevel ? GroupPolicySection.User : GroupPolicySection.Machine);
                var subKey = key.CreateSubKey(path);
                return CmpRegistryValue(subKey, name, value);
            }
            catch (Exception err)
            {
                AppLog.Exception(err);
            }
            finally
            {
                gpoLocker.ExitReadLock();
            }
            return false;
        }

        static public bool SetGPOTweak(string path, string name, object value, bool usrLevel = false)
        {
            gpoLocker.EnterWriteLock();
            try
            {
                var gpo = GetGPO();
                var key = gpo.GetRootRegistryKey(usrLevel ? GroupPolicySection.User : GroupPolicySection.Machine);
                var subKey = key.CreateSubKey(path);
                SetRegistryValue(subKey, name, value);
                return SaveGPO();
            }
            catch (Exception err)
            {
                AppLog.Exception(err);
            }
            finally
            {
                gpoLocker.ExitWriteLock();
            }
            return false;
        }

        static public bool UndoGPOTweak(string path, string name, bool usrLevel = false)
        {
            gpoLocker.EnterWriteLock();
            try
            {
                var gpo = GetGPO();
                var key = gpo.GetRootRegistryKey(usrLevel ? GroupPolicySection.User : GroupPolicySection.Machine);
                var subKey = key.CreateSubKey(path);
                subKey.DeleteValue(name, false);
                return SaveGPO();
            }
            catch (Exception err)
            {
                AppLog.Exception(err);
            }
            finally
            {
                gpoLocker.ExitWriteLock();
            }
            return false;
        }

        // *** Services ***

        static public bool IsServiceEnabled(string name)
        {
            bool ret = false;
            /*ServiceController svc = new ServiceController(name);
            try
            {
                ret = svc.StartType != ServiceStartMode.Disabled; // only present in .NET 4.6.1 and abive
            }
            catch (Exception err)
            {
                AppLog.Exception(err);
            }
            svc.Close();*/
            try
            {
                /*var svcInfo = ServiceHelper.GetServiceInfo(name);
                ret = svcInfo.StartType != (uint)ServiceStartMode.Disabled;*/
                RegistryKey reg = Registry.LocalMachine.OpenSubKey(@"SYSTEM\CurrentControlSet\services\" + name, false);
                if (reg != null)
                {
                    ret = (int)reg.GetValue("Start") != (int)ServiceStartMode.Disabled;
                }
            }
            catch (Exception err)
            {
                AppLog.Exception(err);
            }
            return ret;
        }

        static public bool DisableService(string name)
        {
            bool ret = false;
            ServiceController svc = new ServiceController(name); // Windows Update Service
            try
            {
                if (svc.Status == ServiceControllerStatus.Running)
                    svc.Stop();

                // backup original value
                RegistryKey reg = Registry.LocalMachine.OpenSubKey(@"SYSTEM\CurrentControlSet\services\" + name, true);
                var value = reg.GetValue("Start");
                reg.SetValue("OldStart", value);

                ServiceHelper.ChangeStartMode(name, ServiceHelper.ServiceBootFlag.Disabled);

                ret = true;
            }
            catch (Exception err)
            {
                AppLog.Exception(err);
            }
            svc.Close();
            return ret;
        }

        static public bool EnableService(string name)
        {
            bool ret = false;
            ServiceController svc = new ServiceController(name); // Windows Update Service
            try
            {
                RegistryKey reg = Registry.LocalMachine.OpenSubKey(@"SYSTEM\CurrentControlSet\services\" + name, true);
                var value = reg.GetValue("OldStart");
                if (value != null)
                {
                    ServiceHelper.ChangeStartMode(name, (ServiceHelper.ServiceBootFlag)((int)value));
                    //svc.Start();
                    reg.DeleteValue("OldStart");
                }
                else // fall back
                    ServiceHelper.ChangeStartMode(name, ServiceHelper.ServiceBootFlag.DemandStart);

                ret = true;
            }
            catch (Exception err)
            {
                AppLog.Exception(err);
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
                AppLog.Exception(err);
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
                AppLog.Exception(err);
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
                AppLog.Exception(err);
            }
            return false;
        }

        static public bool IsTaskPresent(string path, string name)
        {
            if (name == "*")
            {
                List<string> names = EnumTasks(path);
                return names != null && names.Count > 0;
            }

            try
            {
                TaskScheduler.TaskScheduler service = new TaskScheduler.TaskScheduler();
                service.Connect();
                ITaskFolder folder = service.GetFolder(path);
                IRegisteredTask task = folder.GetTask(name);
                return task.State != _TASK_STATE.TASK_STATE_UNKNOWN;
            }
            catch
            {
                return false;
            }
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
                    if (!rule.IdentityReference.Value.Equals(FileOps.SID_World))
                        continue;
                    if ((rule.FileSystemRights & FileSystemRights.ExecuteFile) != 0)
                        return true;
                }
            }
            catch (Exception err)
            {
                AppLog.Exception(err);
            }
            return false;
        }

        static public bool BlockFile(string path)
        {
            try
            {
                path = Environment.ExpandEnvironmentVariables(path);
                if (!FileOps.TakeOwn(path))
                    return false;

                FileSecurity ac = File.GetAccessControl(path);
                ac.AddAccessRule(new FileSystemAccessRule(new SecurityIdentifier(FileOps.SID_World), FileSystemRights.ExecuteFile, AccessControlType.Deny));
                File.SetAccessControl(path, ac);
                return true;
            }
            catch (Exception err)
            {
                AppLog.Exception(err);
            }
            return false;
        }

        static public bool UnBlockFile(string path)
        {
            try
            {
                path = Environment.ExpandEnvironmentVariables(path);
                if (!FileOps.TakeOwn(path))
                    return false;

                FileSecurity ac = File.GetAccessControl(path);
                AuthorizationRuleCollection rules = ac.GetAccessRules(true, true, typeof(System.Security.Principal.SecurityIdentifier)); // get as SID not string
                foreach (FileSystemAccessRule rule in rules)
                {
                    if (!rule.IdentityReference.ToString().Equals(FileOps.SID_World))
                        continue;
                    if (rule.FileSystemRights != FileSystemRights.ExecuteFile)
                        continue;
                    if (rule.AccessControlType != AccessControlType.Deny)
                        continue;
                    ac.RemoveAccessRule(rule);
                }
                File.SetAccessControl(path, ac);
                return true;
            }
            catch (Exception err)
            {
                AppLog.Exception(err);
            }
            return false;
        }

        // *** Firewall ***
    }
}
