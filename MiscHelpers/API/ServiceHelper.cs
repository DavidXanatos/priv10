using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.ServiceProcess;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using MiscHelpers;

namespace MiscHelpers
{

    public static class ServiceHelper
    {
        private const int STANDARD_RIGHTS_REQUIRED = 0xF0000;
        private const int SERVICE_WIN32_OWN_PROCESS = 0x00000010;
        private const int ERROR_INSUFFICIENT_BUFFER = 0x0000007a;
        private const uint SERVICE_NO_CHANGE = 0xFFFFFFFF;
        private const int SC_STATUS_PROCESS_INFO = 0;



        #region OpenSCManager
        [DllImport("advapi32.dll", EntryPoint = "OpenSCManagerW", ExactSpelling = true, CharSet = CharSet.Unicode, SetLastError = true)]
        static extern IntPtr OpenSCManager(string machineName, string databaseName, ScmAccessRights dwDesiredAccess);
        #endregion

        #region OpenService
        [DllImport("advapi32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        static extern IntPtr OpenService(IntPtr hSCManager, string lpServiceName, ServiceAccessRights dwDesiredAccess);
        #endregion

        #region CreateService
        [DllImport("advapi32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        private static extern IntPtr CreateService(IntPtr hSCManager, string lpServiceName, string lpDisplayName, ServiceAccessRights dwDesiredAccess, int dwServiceType, ServiceBootFlag dwStartType, ServiceError dwErrorControl, string lpBinaryPathName, string lpLoadOrderGroup, IntPtr lpdwTagId, string lpDependencies, string lp, string lpPassword);
        #endregion

        #region CloseServiceHandle
        [DllImport("advapi32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        static extern bool CloseServiceHandle(IntPtr hSCObject);
        #endregion

        #region QueryServiceConfig
        [StructLayout(LayoutKind.Sequential)]
        public class ServiceConfigInfo
        {
            [MarshalAs(UnmanagedType.U4)]
            public UInt32 ServiceType;
            [MarshalAs(UnmanagedType.U4)]
            public UInt32 StartType;
            [MarshalAs(UnmanagedType.U4)]
            public UInt32 ErrorControl;
            [MarshalAs(UnmanagedType.LPWStr)]
            public String BinaryPathName;
            [MarshalAs(UnmanagedType.LPWStr)]
            public String LoadOrderGroup;
            [MarshalAs(UnmanagedType.U4)]
            public UInt32 TagID;
            [MarshalAs(UnmanagedType.LPWStr)]
            public String Dependencies;
            [MarshalAs(UnmanagedType.LPWStr)]
            public String ServiceStartName;
            [MarshalAs(UnmanagedType.LPWStr)]
            public String DisplayName;
        }

        [DllImport("advapi32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        public static extern int QueryServiceConfig(IntPtr service, IntPtr queryServiceConfig, int bufferSize, ref int bytesNeeded);
        #endregion


        #region ChangeServiceConfig
        [DllImport("advapi32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        private static extern bool ChangeServiceConfig(IntPtr hService, UInt32 dwServiceType, ServiceBootFlag dwStartType, UInt32 dwErrorControl, string lpBinaryPathName, string lpLoadOrderGroup, IntPtr lpdwTagId, string lpDependencies, string lpServiceStartName, string lpPassword, string lpDisplayName);
        #endregion

        #region QueryServiceStatus
        [StructLayout(LayoutKind.Sequential)]
        public class SERVICE_STATUS
        {
            public int dwServiceType = 0;
            public ServiceState dwCurrentState = 0;
            public int dwControlsAccepted = 0;
            public int dwWin32ExitCode = 0;
            public int dwServiceSpecificExitCode = 0;
            public int dwCheckPoint = 0;
            public int dwWaitHint = 0;
        }

        [DllImport("advapi32.dll", SetLastError = true)]
        private static extern int QueryServiceStatus(IntPtr hService, SERVICE_STATUS lpServiceStatus);
        #endregion

        #region QueryServiceStatusEx
        [StructLayout(LayoutKind.Sequential)]
        public sealed class SERVICE_STATUS_PROCESS
        {
            [MarshalAs(UnmanagedType.U4)]
            public uint dwServiceType;
            [MarshalAs(UnmanagedType.U4)]
            public uint dwCurrentState;
            [MarshalAs(UnmanagedType.U4)]
            public uint dwControlsAccepted;
            [MarshalAs(UnmanagedType.U4)]
            public uint dwWin32ExitCode;
            [MarshalAs(UnmanagedType.U4)]
            public uint dwServiceSpecificExitCode;
            [MarshalAs(UnmanagedType.U4)]
            public uint dwCheckPoint;
            [MarshalAs(UnmanagedType.U4)]
            public uint dwWaitHint;
            [MarshalAs(UnmanagedType.U4)]
            public uint dwProcessId;
            [MarshalAs(UnmanagedType.U4)]
            public uint dwServiceFlags;
        }

        [DllImport("advapi32.dll", SetLastError = true)]
        internal static extern bool QueryServiceStatusEx(IntPtr hService, int infoLevel, IntPtr lpBuffer, uint cbBufSize, out uint pcbBytesNeeded);
        #endregion

        #region DeleteService
        [DllImport("advapi32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool DeleteService(IntPtr hService);
        #endregion

        #region ControlService
        [DllImport("advapi32.dll")]
        private static extern int ControlService(IntPtr hService, ServiceControl dwControl, SERVICE_STATUS lpServiceStatus);
        #endregion

        #region StartService
        [DllImport("advapi32.dll", SetLastError = true)]
        private static extern int StartService(IntPtr hService, int dwNumServiceArgs, int lpServiceArgVectors);
        #endregion

        public static void Uninstall(string serviceName)
        {
            IntPtr scm = OpenSCManager(ScmAccessRights.AllAccess);

            try
            {
                IntPtr service = OpenService(scm, serviceName, ServiceAccessRights.AllAccess);
                if (service == IntPtr.Zero)
                    throw new ApplicationException("Service not installed.");

                try
                {
                    StopService(service);

                    if (!DeleteService(service))
                        throw new ApplicationException("Could not delete service " + Marshal.GetLastWin32Error());
                }
                finally
                {
                    CloseServiceHandle(service);
                }
            }
            finally
            {
                CloseServiceHandle(scm);
            }
        }

        public static bool ServiceIsInstalled(string serviceName)
        {
            IntPtr scm = OpenSCManager(ScmAccessRights.Connect);

            try
            {
                IntPtr service = OpenService(scm, serviceName, ServiceAccessRights.QueryStatus);

                if (service == IntPtr.Zero)
                    return false;

                CloseServiceHandle(service);
                return true;
            }
            finally
            {
                CloseServiceHandle(scm);
            }
        }

        public static void Install(string serviceName, string displayName, string fileName)
        {
            IntPtr scm = OpenSCManager(ScmAccessRights.AllAccess);

            try
            {
                IntPtr service = OpenService(scm, serviceName, ServiceAccessRights.AllAccess);

                if (service == IntPtr.Zero)
                    service = CreateService(scm, serviceName, displayName, ServiceAccessRights.AllAccess, SERVICE_WIN32_OWN_PROCESS, ServiceBootFlag.AutoStart, ServiceError.Normal, fileName, null, IntPtr.Zero, null, null, null);

                if (service == IntPtr.Zero)
                    throw new ApplicationException("Failed to install service.");

                CloseServiceHandle(service);
            }
            finally
            {
                CloseServiceHandle(scm);
            }
        }

        public static void ChangeStartMode(string serviceName, ServiceBootFlag mode)
        {
            IntPtr scm = OpenSCManager(ScmAccessRights.Connect | ScmAccessRights.EnumerateService);

            try
            {
                IntPtr service = OpenService(scm, serviceName, ServiceAccessRights.QueryConfig | ServiceAccessRights.ChangeConfig);
                if (service == IntPtr.Zero)
                    throw new ApplicationException("Could not open service.");

                try
                {
                    if (!ChangeServiceConfig(service, SERVICE_NO_CHANGE, mode, SERVICE_NO_CHANGE, null, null, IntPtr.Zero, null, null, null, null))
                        throw new ApplicationException("Could not configure service " + Marshal.GetLastWin32Error());
                }
                finally
                {
                    CloseServiceHandle(service);
                }
            }
            finally
            {
                CloseServiceHandle(scm);
            }
        }

        public static ServiceConfigInfo GetServiceInfo(string serviceName)
        {
            IntPtr scm = OpenSCManager(ScmAccessRights.Connect | ScmAccessRights.EnumerateService);

            try
            {
                IntPtr service = OpenService(scm, serviceName, ServiceAccessRights.QueryConfig | ServiceAccessRights.ChangeConfig);
                if (service == IntPtr.Zero)
                    throw new ApplicationException("Could not open service.");

                try
                {
                    int bytesNeeded = 0;
                    if (QueryServiceConfig(service, IntPtr.Zero, 0, ref bytesNeeded) == 0 && bytesNeeded == 0)
                        throw new ApplicationException("Could not query service configuration" + Marshal.GetLastWin32Error());

                    IntPtr qscPtr = Marshal.AllocCoTaskMem(bytesNeeded);
                    try
                    {
                        if (QueryServiceConfig(service, qscPtr, bytesNeeded, ref bytesNeeded) == 0)
                            throw new ApplicationException("Could not query service configuration" + Marshal.GetLastWin32Error());

                        return (ServiceConfigInfo)Marshal.PtrToStructure(qscPtr, typeof(ServiceConfigInfo));
                    }
                    finally
                    {
                        Marshal.FreeCoTaskMem(qscPtr);
                    }
                }
                finally
                {
                    CloseServiceHandle(service);
                }
            }
            finally
            {
                CloseServiceHandle(scm);
            }
        }

        public static ServiceConfigInfo GetServiceInfoSafe(string serviceName)
        {
            try
            {
                return GetServiceInfo(serviceName);
            }
            catch
            {
                return null;
            }
        }

        public static bool StartService(string serviceName)
        {
            IntPtr scm = OpenSCManager(ScmAccessRights.Connect);

            bool ret = false;
            try
            {
                IntPtr service = OpenService(scm, serviceName, ServiceAccessRights.QueryStatus | ServiceAccessRights.Start);
                if (service == IntPtr.Zero)
                    throw new ApplicationException("Could not open service.");

                try
                {
                    StartService(service);
                    ret = true;
                }
                finally
                {
                    CloseServiceHandle(service);
                }
            }
            finally
            {
                CloseServiceHandle(scm);
            }
            return ret;
        }

        public static bool StopService(string serviceName)
        {
            IntPtr scm = OpenSCManager(ScmAccessRights.Connect);

            bool ret = false;
            try
            {
                IntPtr service = OpenService(scm, serviceName, ServiceAccessRights.QueryStatus | ServiceAccessRights.Stop);
                if (service == IntPtr.Zero)
                    throw new ApplicationException("Could not open service.");

                try
                {
                    StopService(service);
                    ret = true;
                }
                finally
                {
                    CloseServiceHandle(service);
                }
            }
            finally
            {
                CloseServiceHandle(scm);
            }
            return ret;
        }

        private static void StartService(IntPtr service)
        {
            SERVICE_STATUS status = new SERVICE_STATUS();
            StartService(service, 0, 0);
            var changedStatus = WaitForServiceStatus(service, ServiceState.StartPending, ServiceState.Running);
            if (!changedStatus)
                throw new ApplicationException("Unable to start service");
        }

        private static void StopService(IntPtr service)
        {
            SERVICE_STATUS status = new SERVICE_STATUS();
            ControlService(service, ServiceControl.Stop, status);
            var changedStatus = WaitForServiceStatus(service, ServiceState.StopPending, ServiceState.Stopped);
            if (!changedStatus)
                throw new ApplicationException("Unable to stop service");
        }

        public static ServiceState GetServiceState(string serviceName)
        {
            SERVICE_STATUS_PROCESS ssp = GetServiceStatus(serviceName);
            if (ssp == null)
                return ServiceState.NotFound;
            return (ServiceState)ssp.dwCurrentState;
        }

        public static SERVICE_STATUS_PROCESS GetServiceStatus(string serviceName)
        {
            IntPtr scm = OpenSCManager(ScmAccessRights.Connect);
            IntPtr zero = IntPtr.Zero;
            IntPtr service = IntPtr.Zero;

            try
            {
                service = OpenService(scm, serviceName, ServiceAccessRights.QueryStatus);

                UInt32 dwBytesAlloc = 0;
                UInt32 dwBytesNeeded = 36;
                do
                {
                    dwBytesAlloc = dwBytesNeeded;
                    // Allocate required buffer and call again.
                    zero = Marshal.AllocHGlobal((int)dwBytesAlloc);
                    if (QueryServiceStatusEx(service, SC_STATUS_PROCESS_INFO, zero, dwBytesAlloc, out dwBytesNeeded))
                    {
                        var ssp = new SERVICE_STATUS_PROCESS();
                        Marshal.PtrToStructure(zero, ssp);
                        return ssp;
                    }
                    // retry with new size info
                } while (Marshal.GetLastWin32Error() == ERROR_INSUFFICIENT_BUFFER && dwBytesAlloc < dwBytesNeeded);
            }
            finally
            {
                if (zero != IntPtr.Zero)
                    Marshal.FreeHGlobal(zero);
                if (service != IntPtr.Zero)
                    CloseServiceHandle(service);
                CloseServiceHandle(scm);
            }
            return null;
        }

        private static bool WaitForServiceStatus(IntPtr service, ServiceState waitStatus, ServiceState desiredStatus)
        {
            SERVICE_STATUS status = new SERVICE_STATUS();

            QueryServiceStatus(service, status);
            if (status.dwCurrentState == desiredStatus) return true;

            int dwStartTickCount = Environment.TickCount;
            int dwOldCheckPoint = status.dwCheckPoint;

            while (status.dwCurrentState == waitStatus)
            {
                // Do not wait longer than the wait hint. A good interval is
                // one tenth the wait hint, but no less than 1 second and no
                // more than 10 seconds.

                int dwWaitTime = status.dwWaitHint / 10;

                if (dwWaitTime < 1000) dwWaitTime = 1000;
                else if (dwWaitTime > 10000) dwWaitTime = 10000;

                Thread.Sleep(dwWaitTime);

                // Check the status again.

                if (QueryServiceStatus(service, status) == 0) break;

                if (status.dwCheckPoint > dwOldCheckPoint)
                {
                    // The service is making progress.
                    dwStartTickCount = Environment.TickCount;
                    dwOldCheckPoint = status.dwCheckPoint;
                }
                else
                {
                    if (Environment.TickCount - dwStartTickCount > status.dwWaitHint)
                    {
                        // No progress made within the wait hint
                        break;
                    }
                }
            }
            return (status.dwCurrentState == desiredStatus);
        }

        private static IntPtr OpenSCManager(ScmAccessRights rights)
        {
            IntPtr scm = OpenSCManager(null, null, rights);
            if (scm == IntPtr.Zero)
                throw new ApplicationException("Could not connect to service control manager.");

            return scm;
        }


        public enum ServiceState
        {
            Unknown = -1, // The state cannot be (has not been) retrieved.
            NotFound = 0, // The service is not known on the host server.
            Stopped = 1,
            StartPending = 2,
            StopPending = 3,
            Running = 4,
            ContinuePending = 5,
            PausePending = 6,
            Paused = 7
        }

        [Flags]
        public enum ScmAccessRights
        {
            Connect = 0x0001,
            CreateService = 0x0002,
            EnumerateService = 0x0004,
            Lock = 0x0008,
            QueryLockStatus = 0x0010,
            ModifyBootConfig = 0x0020,
            StandardRightsRequired = 0xF0000,
            AllAccess = (StandardRightsRequired | Connect | CreateService |
                         EnumerateService | Lock | QueryLockStatus | ModifyBootConfig)
        }

        [Flags]
        public enum ServiceAccessRights
        {
            QueryConfig = 0x1,
            ChangeConfig = 0x2,
            QueryStatus = 0x4,
            EnumerateDependants = 0x8,
            Start = 0x10,
            Stop = 0x20,
            PauseContinue = 0x40,
            Interrogate = 0x80,
            UserDefinedControl = 0x100,
            Delete = 0x00010000,
            StandardRightsRequired = 0xF0000,
            AllAccess = (StandardRightsRequired | QueryConfig | ChangeConfig |
                         QueryStatus | EnumerateDependants | Start | Stop | PauseContinue |
                         Interrogate | UserDefinedControl)
        }

        public enum ServiceBootFlag
        {
            Start = 0x00000000,
            SystemStart = 0x00000001,
            AutoStart = 0x00000002,
            DemandStart = 0x00000003,
            Disabled = 0x00000004
        }

        public enum ServiceControl
        {
            Stop = 0x00000001,
            Pause = 0x00000002,
            Continue = 0x00000003,
            Interrogate = 0x00000004,
            Shutdown = 0x00000005,
            ParamChange = 0x00000006,
            NetBindAdd = 0x00000007,
            NetBindRemove = 0x00000008,
            NetBindEnable = 0x00000009,
            NetBindDisable = 0x0000000A
        }

        public enum ServiceError
        {
            Ignore = 0x00000000,
            Normal = 0x00000001,
            Severe = 0x00000002,
            Critical = 0x00000003
        }

        private static string GetServiceImagePath(string serviceName)
        {
            RegistryKey regkey = Registry.LocalMachine.OpenSubKey(@"SYSTEM\CurrentControlSet\services\" + serviceName);
            if (regkey.GetValue("ImagePath") == null)
                return null;
            return regkey.GetValue("ImagePath").ToString();
        }

        public class ServiceInfo
        {
            public ServiceInfo(ServiceController sc)
            {
                ServiceName = sc.ServiceName;
                var ImagePath = GetServiceImagePath(sc.ServiceName);
                ServicePath = ImagePath != null ? ProcFunc.GetPathFromCmdLine(ImagePath) : "";
                DisplayName = sc.DisplayName;
                if (sc.Status == ServiceControllerStatus.Stopped)
                    LastKnownPID = -1;
                else
                {
                    var ssp = GetServiceStatus(sc.ServiceName);
                    LastKnownPID = ssp != null ? (int)ssp.dwProcessId : -1;
                }
            }
            public string ServiceName;
            public string ServicePath;
            public string DisplayName;
            public int LastKnownPID;
        }

        private static DateTime ServiceCacheTime = DateTime.MinValue;
        private static MultiValueDictionary<int, ServiceInfo> ServiceCacheByPID = new MultiValueDictionary<int, ServiceInfo>();
        private static Dictionary<string, ServiceInfo> ServiceCache = new Dictionary<string, ServiceInfo>();
        private static ReaderWriterLockSlim ServiceCacheLock = new ReaderWriterLockSlim();


        private static void RefreshServices()
        {
            ServiceCacheLock.EnterWriteLock();
            ServiceCacheTime = DateTime.Now;
            ServiceCache.Clear();
            ServiceCacheByPID.Clear();
            foreach (ServiceController sc in ServiceController.GetServices())
            {
                ServiceInfo info = new ServiceInfo(sc);
                if (!ServiceCache.ContainsKey(sc.ServiceName)) // should not happen but in case
                    ServiceCache.Add(sc.ServiceName, info);
                if (info.LastKnownPID != -1)
                    ServiceCacheByPID.Add(info.LastKnownPID, info);
            }
            // this takes roughly 30 ms
            ServiceCacheLock.ExitWriteLock();
        }

        public static List<ServiceInfo> GetServicesByPID(int pid)
        {
            bool doUpdate = false;
            if (pid == -1) // -1 means get all and we always want a fresh list
                doUpdate = true;
            else
            {
                ServiceCacheLock.EnterReadLock();
                doUpdate = ServiceCacheTime <= DateTime.FromFileTimeUtc(ProcFunc.GetProcessCreationTime(pid)).ToLocalTime();
                ServiceCacheLock.ExitReadLock();
            }
            if (doUpdate)
                RefreshServices();

            ServiceCacheLock.EnterReadLock();
            CloneableList<ServiceInfo> values;
            if (pid == -1)
                values = ServiceCacheByPID.GetAllValues();
            else if (!ServiceCacheByPID.TryGetValue(pid, out values))
                values = null;
            ServiceCacheLock.ExitReadLock();
            return (values != null && values.Count == 0) ? null : values;
        }

        public static List<ServiceInfo> GetAllServices()
        {
            ServiceCacheLock.EnterReadLock();
            bool doUpdate = ServiceCacheTime < DateTime.Now.AddSeconds(-30);
            ServiceCacheLock.ExitReadLock();
            if (doUpdate)
                RefreshServices();

            ServiceCacheLock.EnterReadLock();
            List<ServiceInfo> list = ServiceCache.Values.ToList();
            ServiceCacheLock.ExitReadLock();
            return list;
        }

        public static string GetServiceName(string name)
        {
            if (name == null || name.Length == 0)
                return "";

            ServiceCacheLock.EnterReadLock();
            ServiceInfo info = null;
            bool found = ServiceCache.TryGetValue(name, out info);
            ServiceCacheLock.ExitReadLock();

            if (!found)
            {
                ServiceController sc = new ServiceController(name);
                try { info = new ServiceInfo(sc); } catch { }
                sc.Close();

                ServiceCacheLock.EnterWriteLock();
                if (info != null && !ServiceCache.ContainsKey(sc.ServiceName)) // should not happen but in case
                    ServiceCache.Add(sc.ServiceName, info);
                ServiceCacheLock.ExitWriteLock();
            }

            return info == null ? "" : info.DisplayName;
        }
    }
}