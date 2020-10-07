using MiscHelpers;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using PrivateService;
using PrivateAPI;

namespace PrivateWin10
{
    public class ProcessMonitorEtw : IDisposable
    {
        Microsoft.O365.Security.ETW.KernelTrace kernelTrace;
        Microsoft.O365.Security.ETW.Kernel.ProcessProvider processProvider;
        Thread kernelThread = null;

        public ProcessMonitorEtw(Microsoft.O365.Security.ETW.IEventRecordDelegate OnProcessEvent)
        {
            kernelTrace = new Microsoft.O365.Security.ETW.KernelTrace("priv10_ProcLogger");
            processProvider = new Microsoft.O365.Security.ETW.Kernel.ProcessProvider();
            processProvider.OnEvent += OnProcessEvent;
            kernelTrace.Enable(processProvider);

            kernelThread = new Thread(() => { kernelTrace.Start(); });
            kernelThread.Start();
        }

        public void Dispose()
        {
            kernelTrace.Stop();
            kernelThread.Join();
        }
    }

    public class ProcessMonitor
    {
        class ProcInfo
        {
            public string filePath;
            public DateTime StartTime;
            public DateTime? StopTime = null;
        };

        Dictionary<int, ProcInfo> Processes = new Dictionary<int, ProcInfo>();

        ProcessMonitorEtw Etw = null;

        public ProcessMonitor()
        {
            try
            {
                InitEtw();
                //AppLog.Debug("Successfully initialized ProcessMonitorEtw");
            }
            catch
            {
                Priv10Logger.LogError("Failed to initialized ProcessMonitorEtw");
            }
        }

        private void InitEtw()
        {
            Etw = new ProcessMonitorEtw(OnProcessEvent);
        }

        public void Dispose()
        {
            if (Etw != null)
                Etw.Dispose();
        }

        private void OnProcessEvent(Microsoft.O365.Security.ETW.IEventRecord record)
        {
            // WARNING: this function is called from the worker thread

            if (record.Id != 0)
                return;

            if (record.Opcode == 1) // start
            {
                //EtwAbstractLogger.OnEtwEvent(record, "proc");
                /*
                UniqueProcessKey:  (Type 16)
                ProcessId: 27204 (Type 8)
                ParentId: 8540 (Type 8)
                SessionId: 1 (Type 8)
                ExitStatus: 259 (Type 7)
                DirectoryTableBase:  (Type 16)
                Flags: 0 (Type 8)
                UserSID:  (Type 310)
                ImageFileName: calc1.exe (Type 2)
                CommandLine: calc1 (Type 1)
                PackageFullName:  (Type 1)
                ApplicationId:  (Type 1)
                 */

                int ProcessId = (int)record.GetUInt32("ProcessId", 0);
                string CommandLine = record.GetUnicodeString("CommandLine", null); // Note: the command line may contain a realtive path (!)
                string FileName = record.GetAnsiString("ImageFileName", null);
                int ParentId = (int)record.GetUInt32("ParentId", 0);

                string filePath = GetPathFromCmd(CommandLine, ProcessId, FileName/*, record.Timestamp*/, ParentId);

                if (filePath == null)
                {
                    AppLog.Debug("Process Monitor could not resolve path for prosess ({0}) : {1}", ProcessId, CommandLine);
                    return;
                }

                //AppLog.Debug("Process Started: {0}", filePath);

                App.engine?.RunInEngineThread(() =>
                {
                    // Note: this happens in the engine thread

                    if (Processes.ContainsKey(ProcessId))
                    {
                        AppLog.Debug("Possible PID conflict (pid {0} reused): {1}", ProcessId, filePath);
                        Processes.Remove(ProcessId);
                    }

                    Processes.Add(ProcessId, new ProcInfo() { filePath = filePath, StartTime = record.Timestamp });
                });

            }
            else if (record.Opcode == 2) // stop
            {
                int ProcessId = (int)record.GetUInt32("ProcessId", 0);

                App.engine?.RunInEngineThread(() =>
                {
                    // Note: this happens in the engine thread

                    ProcInfo info;
                    if (Processes.TryGetValue(ProcessId, out info))
                        info.StopTime = record.Timestamp;
                });
            }
        }

        public string GetProcessFileNameByPID(int processID)
        {
            ProcInfo info;
            if (Processes.TryGetValue(processID, out info) && info.StopTime == null)
                return info.filePath;

            string filePath = ProcFunc.GetProcessFileNameByPID(processID);
            if (filePath == null)
                return null;
            
            var startTime = ProcFunc.GetProcessCreationTime(processID);
            if (startTime != 0)
            {
                if (info != null)
                    Processes.Remove(processID);
                Processes.Add(processID, new ProcInfo() { filePath = filePath, StartTime = DateTime.FromFileTimeUtc(startTime) });
            }

            return filePath;
        }

        public void CleanUpProcesses()
        {
            DateTime TimeOut = DateTime.Now.AddMinutes(-1);

            // check all pids and remove all invalid entries
            foreach (var pid in Processes.Keys.ToList())
            {
                var info = Processes[pid];
                if (info.StopTime == null)
                {
                    string filePath = ProcFunc.GetProcessFileNameByPID(pid);
                    if (filePath == null)
                        info.StopTime = DateTime.Now;
                    else if (!filePath.Equals(info.filePath, StringComparison.OrdinalIgnoreCase)) // to quick pid reuse
                    {
                        AppLog.Debug("Possible PID conflict (pid {0} reused): {1}", pid, filePath);
                        Processes.Remove(pid);
                    }
                }
                else if (info.StopTime < TimeOut)
                    Processes.Remove(pid);
            }
        }

        string GetPathFromCmd(string commandLine, int processID, string imageName/*, DateTime timeStamp*/, int parentID = 0)
        {
            if (commandLine.Length == 0)
                return null;

            string filePath = ProcFunc.GetPathFromCmdLine(commandLine);

            // apparently some processes can be started without a exe name in the command line WTF, anyhow:
            if (!Path.GetFileName(filePath).Equals(imageName, StringComparison.OrdinalIgnoreCase) 
            && !(Path.GetFileName(filePath) + ".exe").Equals(imageName, StringComparison.OrdinalIgnoreCase))
                filePath = imageName;
            
            // https://reverseengineering.stackexchange.com/questions/3798/c-question-marks-in-paths
            // \?? is a "fake" prefix which refers to per-user Dos devices
            if (filePath.IndexOf(@"\??\") == 0)
                filePath = filePath.Substring(4);

            filePath = Environment.ExpandEnvironmentVariables(filePath);

            if (Path.IsPathRooted(filePath))
                return filePath;

            // The file path is not fully qualifyed

            // try to get the running processes path
            if (processID != 0)
            {
                //Process proc = null;
                //try { proc = Process.GetProcessById(processID); } catch { }
                //if (proc != null)
                //{
                //var fileName = proc.GetMainModuleFileName();
                var fileName = ProcFunc.GetProcessFileNameByPID(processID);
                if (fileName != null)
                {
                    // ensure its the right process
                    //var startTime = proc.StartTime.ToUniversalTime();
                    //if ((startTime - timeStamp).TotalSeconds < 1)
                    if (Path.GetFileName(fileName).Equals(imageName, StringComparison.OrdinalIgnoreCase))
                        return fileName;
                }
                //}
            }

            // check relative paths based on the parrent process working directory
            string workingDir = ProcessUtilities.GetCurrentDirectory(parentID);
            if (workingDir != null)
            {
                var curPath = Path.Combine(workingDir, filePath);
                if (filePath[0] == '.')
                    curPath = Path.GetFullPath(curPath);

                if (File.Exists(curPath))
                    return curPath;
                if (File.Exists(curPath + ".exe"))
                    return curPath + ".exe";
            }

            // if everythign else fails, try to find the process binary using the environment path variable
            if (FindExeInPath(filePath, ref filePath))
                return filePath;

            return null;
        }

        #region FindInPath
        [DllImport("shlwapi.dll", CharSet = CharSet.Auto, SetLastError = false)]
        static extern bool PathFindOnPath([MarshalAs(UnmanagedType.LPTStr)] StringBuilder pszFile, IntPtr unused);

        public static bool FindInPath(String pszFile, ref String fullPath)
        {
            const int MAX_PATH = 260;
            StringBuilder sb = new StringBuilder(pszFile, MAX_PATH);
            if (!PathFindOnPath(sb, IntPtr.Zero))
                return false;
            fullPath = sb.ToString();
            return true;

        }
        #endregion

        public static bool FindExeInPath(String pszFile, ref String fullPath)
        {
            if (FindInPath(pszFile, ref fullPath))
                return true;
            if (FindInPath(pszFile + ".exe", ref fullPath))
                return true;
            return false;
        }
    }
}