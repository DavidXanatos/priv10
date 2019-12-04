using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

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
                AppLog.Debug("Failed to initialized ProcessMonitorEtw");
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
            // "C:\Program Files\WindowsApps\Microsoft.WindowsCalculator_10.1908.0.0_x64__8wekyb3d8bbwe\Calculator.exe" -ServerName:App.AppXsm3pg4n7er43kdh1qp4e79f1j7am68r8.mca
            var CommandLine = record.GetUnicodeString("CommandLine", null); // Note: the command line may contain a realtive path (!)

            if (record.Opcode == 1) // start
            {
            }
            else if (record.Opcode == 2) // stop
            {
            }
        }
    }
}