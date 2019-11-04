using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;


static class ProcFunc
{
    public static int CurID = System.Diagnostics.Process.GetCurrentProcess().Id;

    public const int SystemPID = 4; // on windows system is has always PID 4


    [DllImport("kernel32.dll")]
    public static extern IntPtr OpenProcess(uint processAccess, bool bInheritHandle, int processId);

    [DllImport("psapi.dll")]
    public static extern uint GetModuleFileNameEx(IntPtr hProcess, IntPtr hModule, [Out] StringBuilder lpBaseName, [In] [MarshalAs(UnmanagedType.U4)] int nSize);

    [DllImport("advapi32.dll", SetLastError = true)]
    public static extern bool OpenProcessToken(IntPtr ProcessHandle, uint DesiredAccess, out IntPtr TokenHandle);

    [DllImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool CloseHandle(IntPtr hObject);


    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static string GetProcessName(int pid)
    {
        if (pid == ProcFunc.SystemPID)
            return MiscFunc.NtOsKrnlPath;

        // todo add cache, may be?
        var processHandle = OpenProcess(0x1000/*PROCESS_QUERY_LIMITED_INFORMATION*/, false, pid);
        if (processHandle == IntPtr.Zero)
            return null;

        string result = null;

        const int lengthSb = 4096+1;
        var sb = new StringBuilder(lengthSb);
        if (GetModuleFileNameEx(processHandle, IntPtr.Zero, sb, lengthSb) > 0)
        {
            result = sb.ToString();
        }

        CloseHandle(processHandle);

        return result;
    }


    [DllImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    static extern bool GetProcessTimes(IntPtr hProcess, out long lpCreationTime, out long lpExitTime, out long lpKernelTime, out long lpUserTime);

    public static long GetProcessCreationTime(int pid)
    {
        var processHandle = OpenProcess(0x1000/*PROCESS_QUERY_LIMITED_INFORMATION*/, false, pid);
        if (processHandle == IntPtr.Zero)
            return 0;

        long RawCreationTime;
        long RawExitTime;
        long RawKernelTime;
        long RawUserTime;
        GetProcessTimes(processHandle, out RawCreationTime, out RawExitTime, out RawKernelTime, out RawUserTime);

        CloseHandle(processHandle);

        return RawCreationTime;
    }
}
