using System;
using System.Collections.Generic;
using System.Diagnostics;
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
    public static string GetProcessFileNameByPID(int pid)
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

    [DllImport("advapi32", CharSet = CharSet.Unicode)]
    public static extern bool ConvertStringSidToSid([In, MarshalAs(UnmanagedType.LPWStr)] string pStringSid, ref IntPtr pSID);

    [DllImport("advapi32", CharSet = CharSet.Unicode)]
    public static extern bool ConvertSidToStringSid(IntPtr pSID, [In, Out, MarshalAs(UnmanagedType.LPWStr)] ref string pStringSid);

    [StructLayout(LayoutKind.Sequential)]
    public struct TOKEN_APPCONTAINER_INFORMATION
    {
        public IntPtr Sid;
    }

    [DllImport("ntdll.dll")]
    public static extern uint NtQueryInformationToken([In] IntPtr TokenHandle, [In] uint TokenInformationClass, [In] IntPtr TokenInformation, [In] int TokenInformationLength, [Out] [Optional] out int ReturnLength);

    static public string GetAppPackageSidByPID(int PID)
    {
        //var process = System.Diagnostics.Process.GetProcessById(PID); // throws error if pid is not found
        var processHandle = OpenProcess(0x1000/*PROCESS_QUERY_LIMITED_INFORMATION*/, false, PID);
        if (processHandle == IntPtr.Zero)
            return null;

        string strSID = null;

        IntPtr tokenHandle = IntPtr.Zero;
        if (OpenProcessToken(processHandle, 8, out tokenHandle))
        {
            int retLen;
            NtQueryInformationToken(tokenHandle, 31 /*TokenAppContainerSid*/, IntPtr.Zero, 0, out retLen);

            IntPtr buffer = Marshal.AllocHGlobal((int)retLen);
            ulong status = NtQueryInformationToken(tokenHandle, 31 /*TokenAppContainerSid*/, buffer, retLen, out retLen);
            if (status >= 0)
            {
                var appContainerInfo = (TOKEN_APPCONTAINER_INFORMATION)Marshal.PtrToStructure(buffer, typeof(TOKEN_APPCONTAINER_INFORMATION));

                ConvertSidToStringSid(appContainerInfo.Sid, ref strSID);
            }
            Marshal.FreeHGlobal(buffer);

            CloseHandle(tokenHandle);
        }

        CloseHandle(processHandle);

        return strSID;
    }

    /*
    [DllImport("Kernel32.dll")]
    private static extern bool QueryFullProcessImageName([In] IntPtr hProcess, [In] uint dwFlags, [Out] StringBuilder lpExeName, [In, Out] ref uint lpdwSize);

    // Note: we can not access a module of a otehr bitness as we are so we need a native solution
    public static string GetMainModuleFileName(this Process process)
    {
        var fileNameBuilder = new StringBuilder(1024);
        uint bufferLength = (uint)fileNameBuilder.Capacity + 1;
        try
        {
            return QueryFullProcessImageName(process.Handle, 0, fileNameBuilder, ref bufferLength) ? fileNameBuilder.ToString() : null;
        }
        catch
        {
            return null;
        }
    }
    */

    static public string GetPathFromCmdLine(string commandLine)
    {
        if (commandLine[0] == '"')
        {
            int pos = commandLine.IndexOf('"', 1);
            if (pos != -1)
                return commandLine.Substring(1, pos - 1);
        }
        else
        {
            int pos = commandLine.IndexOf(' ');
            if (pos != -1)
                return commandLine.Substring(0, pos);
        }
        return commandLine;
    }
}
