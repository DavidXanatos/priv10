
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security.AccessControl;
using System.Security.Principal;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using TaskScheduler;

public class AdminFunc
{
    public static bool IsAdministrator()
    {
        WindowsIdentity identity = WindowsIdentity.GetCurrent();
        WindowsPrincipal principal = new WindowsPrincipal(identity);
        return principal.IsInRole(WindowsBuiltInRole.Administrator);
    }

    [DllImport("kernel32.dll", SetLastError = true, ExactSpelling = true)]
    static extern bool CheckRemoteDebuggerPresent(IntPtr hProcess, ref bool isDebuggerPresent);

    static public bool IsDebugging()
    {
        bool isDebuggerPresent = false;
        CheckRemoteDebuggerPresent(Process.GetCurrentProcess().Handle, ref isDebuggerPresent);
        return isDebuggerPresent;
    }


    static public bool IsSkipUac(string taskName)
    {
        try
        {
            TaskScheduler.TaskScheduler service = new TaskScheduler.TaskScheduler();
            service.Connect();
            ITaskFolder folder = service.GetFolder(@"\"); // root
            IRegisteredTask task = folder.GetTask(taskName);
            return task != null;
        }
        catch { }
        return false;
    }

    static public bool SkipUacEnable(string taskName, bool is_enable)
    {
        try
        {
            TaskScheduler.TaskScheduler service = new TaskScheduler.TaskScheduler();
            service.Connect();
            ITaskFolder folder = service.GetFolder(@"\"); // root
            if (is_enable)
            {
                string exePath = System.Reflection.Assembly.GetExecutingAssembly().Location;
                string appPath = Path.GetDirectoryName(exePath);

                ITaskDefinition task = service.NewTask(0);
                task.RegistrationInfo.Author = "WuMgr";
                task.Principal.RunLevel = _TASK_RUNLEVEL.TASK_RUNLEVEL_HIGHEST;

                task.Settings.AllowHardTerminate = false;
                task.Settings.StartWhenAvailable = false;
                task.Settings.DisallowStartIfOnBatteries = false;
                task.Settings.StopIfGoingOnBatteries = false;
                task.Settings.MultipleInstances = _TASK_INSTANCES_POLICY.TASK_INSTANCES_PARALLEL;
                task.Settings.ExecutionTimeLimit = "PT0S";

                IExecAction action = (IExecAction)task.Actions.Create(_TASK_ACTION_TYPE.TASK_ACTION_EXEC);
                action.Path = exePath;
                action.WorkingDirectory = appPath;
                action.Arguments = "-NoUAC $(Arg0)";

                IRegisteredTask registered_task = folder.RegisterTaskDefinition(taskName, task, (int)_TASK_CREATION.TASK_CREATE_OR_UPDATE, null, null, _TASK_LOGON_TYPE.TASK_LOGON_INTERACTIVE_TOKEN);

                if (registered_task == null)
                    return false;

                // Note: if we run as UWP we need to adjust the file permissions for this workaround to work
                if (UwpFunc.IsRunningAsUwp())
                {
                    if (!FileOps.TakeOwn(exePath))
                        return false;

                    FileSecurity ac = File.GetAccessControl(exePath);
                    ac.AddAccessRule(new FileSystemAccessRule(new SecurityIdentifier(WellKnownSidType.WorldSid, null), FileSystemRights.ReadAndExecute, AccessControlType.Allow));
                    File.SetAccessControl(exePath, ac);
                }
            }
            else
                folder.DeleteTask(taskName, 0);
        }
        catch (Exception err)
        {
            AppLog.Exception(err);
            return false;
        }
        return true;
    }

    static public bool SkipUacRun(string taskName, string[] args = null)
    {
        bool silent = true;
        try
        {
            TaskScheduler.TaskScheduler service = new TaskScheduler.TaskScheduler();
            service.Connect();
            ITaskFolder folder = service.GetFolder(@"\"); // root
            IRegisteredTask task = folder.GetTask(taskName);

            silent = false;
            AppLog.Debug("Trying to SkipUAC ...");

            IExecAction action = (IExecAction)task.Definition.Actions[1];
            if (action.Path.Equals(System.Reflection.Assembly.GetExecutingAssembly().Location, StringComparison.OrdinalIgnoreCase))
            {
                string arguments = args == null ? "" : ("\"" + string.Join("\" \"", args) + "\"");

                IRunningTask running_Task = task.RunEx(arguments, (int)_TASK_RUN_FLAGS.TASK_RUN_NO_FLAGS, 0, null);

                for (int i = 0; i < 5; i++)
                {
                    Thread.Sleep(250);
                    running_Task.Refresh();
                    _TASK_STATE state = running_Task.State;
                    if (state == _TASK_STATE.TASK_STATE_RUNNING || state == _TASK_STATE.TASK_STATE_READY || state == _TASK_STATE.TASK_STATE_DISABLED)
                    {
                        if (state == _TASK_STATE.TASK_STATE_RUNNING || state == _TASK_STATE.TASK_STATE_READY)
                            return true;
                        break;
                    }
                }
            }
        }
        catch (Exception err)
        {
            if (!silent)
                AppLog.Exception(err);
        }
        return false;
    }
}

