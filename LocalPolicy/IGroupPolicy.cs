using System;
using System.Runtime.InteropServices;
using System.Text;

namespace LocalPolicy.COM
{
    [ComImport, Guid("EA502722-A23D-11d1-A7D3-0000F87571E3")]
    internal class GPClass
    {
    }

    [ComImport, Guid("EA502723-A23D-11d1-A7D3-0000F87571E3"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    public interface IGroupPolicyObject
    {
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1716:IdentifiersShouldNotMatchKeywords")]
        uint New(
            [MarshalAs(UnmanagedType.LPWStr)] string domainName,
            [MarshalAs(UnmanagedType.LPWStr)] string displayName,
            uint flags);

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly")]
        uint OpenDSGPO(
            [MarshalAs(UnmanagedType.LPWStr)] string path,
            uint flags);

        uint OpenLocalMachineGPO(
            uint flags);

        uint OpenRemoteMachineGPO(
            [MarshalAs(UnmanagedType.LPWStr)] string computerName,
            uint flags);

        uint Save(
            [MarshalAs(UnmanagedType.Bool)] bool machine,
            [MarshalAs(UnmanagedType.Bool)] bool add,
            [MarshalAs(UnmanagedType.LPStruct)] Guid extension,
            [MarshalAs(UnmanagedType.LPStruct)] Guid app);

        uint Delete();

        uint GetName(
            [MarshalAs(UnmanagedType.LPWStr)] StringBuilder name,
            int maxLength);

        uint GetDisplayName(
            [MarshalAs(UnmanagedType.LPWStr)] StringBuilder name,
            int maxLength);

        uint SetDisplayName(
            [MarshalAs(UnmanagedType.LPWStr)] string name);

        uint GetPath(
            [MarshalAs(UnmanagedType.LPWStr)] StringBuilder path,
            int maxPath);

        uint GetDSPath(
            uint section,
            [MarshalAs(UnmanagedType.LPWStr)] StringBuilder path,
            int maxPath);

        uint GetFileSysPath(
            uint section,
            [MarshalAs(UnmanagedType.LPWStr)] StringBuilder path,
            int maxPath);

        uint GetRegistryKey(
            uint section,
            out IntPtr key);

        uint GetOptions(out uint options);

        uint SetOptions(
            uint options,
            uint mask);

        uint GetType(
            out IntPtr gpoType
        );

        uint GetMachineName(
            [MarshalAs(UnmanagedType.LPWStr)] StringBuilder name,
            int maxLength);

        uint GetPropertySheetPages(
            out IntPtr pages);
    }
}
