/*
 *  IconExtractor/IconUtil for .NET
 *  Copyright (C) 2014 Tsuda Kageyu. All rights reserved.
 *
 *  Redistribution and use in source and binary forms, with or without
 *  modification, are permitted provided that the following conditions
 *  are met:
 *
 *   1. Redistributions of source code must retain the above copyright
 *      notice, this list of conditions and the following disclaimer.
 *   2. Redistributions in binary form must reproduce the above copyright
 *      notice, this list of conditions and the following disclaimer in the
 *      documentation and/or other materials provided with the distribution.
 *
 *  THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS
 *  "AS IS" AND ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED
 *  TO, THE IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A
 *  PARTICULAR PURPOSE ARE DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT HOLDER
 *  OR CONTRIBUTORS BE LIABLE FOR ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL,
 *  EXEMPLARY, OR CONSEQUENTIAL DAMAGES (INCLUDING, BUT NOT LIMITED TO,
 *  PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES; LOSS OF USE, DATA, OR
 *  PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND ON ANY THEORY OF
 *  LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT (INCLUDING
 *  NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THIS
 *  SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
 */

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.IO;
using System.Runtime.InteropServices;
using System.Security;
using System.Text;
using System.Windows.Forms;

namespace MiscHelpers
{

    public class IconExtractor
    {
        ////////////////////////////////////////////////////////////////////////
        // Constants

        // Flags for LoadLibraryEx().

        private const uint LOAD_LIBRARY_AS_DATAFILE = 0x00000002;

        // Resource types for EnumResourceNames().

        private readonly static IntPtr RT_ICON = (IntPtr)3;
        private readonly static IntPtr RT_GROUP_ICON = (IntPtr)14;

        private const int MAX_PATH = 260;

        ////////////////////////////////////////////////////////////////////////
        // Fields

        private byte[][] iconData = null;   // Binary data of each icon.

        ////////////////////////////////////////////////////////////////////////
        // Public properties

        /// <summary>
        /// Gets the full path of the associated file.
        /// </summary>
        public string FileName
        {
            get;
            private set;
        }

        /// <summary>
        /// Gets the count of the icons in the associated file.
        /// </summary>
        public int Count
        {
            get { return iconData == null ? 0 : iconData.Length; }
        }

        /// <summary>
        /// Initializes a new instance of the IconExtractor class from the specified file name.
        /// </summary>
        /// <param name="fileName">The file to extract icons from.</param>
        public IconExtractor(string fileName)
        {
            Initialize(fileName);
        }

        /// <summary>
        /// Extracts an icon from the file.
        /// </summary>
        /// <param name="index">Zero based index of the icon to be extracted.</param>
        /// <returns>A System.Drawing.Icon object.</returns>
        /// <remarks>Always returns new copy of the Icon. It should be disposed by the user.</remarks>
        public Icon GetIcon(int index)
        {
            if (index < 0 || Count <= index)
                throw new ArgumentOutOfRangeException("index");

            // Create an Icon based on a .ico file in memory.

            using (var ms = new MemoryStream(iconData[index]))
            {
                return new Icon(ms);
            }
        }

        /// <summary>
        /// Extracts an icon from the file.
        /// </summary>
        /// <param name="index">Zero based index of the icon to be extracted.</param>
        /// <param name="size">Size of the icon to extract</param>
        /// <returns>A System.Drawing.Icon object.</returns>
        /// <remarks>Always returns new copy of the Icon. It should be disposed by the user.</remarks>
        public Icon GetIcon(int index, Size size)
        {
            if (index < 0 || Count <= index)
                throw new ArgumentOutOfRangeException("index");

            // Create an Icon based on a .ico file in memory.

            using (var ms = new MemoryStream(iconData[index]))
            {
                return new Icon(ms, size);
            }
        }

        /// <summary>
        /// Extracts all the icons from the file.
        /// </summary>
        /// <returns>An array of System.Drawing.Icon objects.</returns>
        /// <remarks>Always returns new copies of the Icons. They should be disposed by the user.</remarks>
        public Icon[] GetAllIcons()
        {
            var icons = new List<Icon>();
            for (int i = 0; i < Count; ++i)
                icons.Add(GetIcon(i));

            return icons.ToArray();
        }

        private void Initialize(string fileName)
        {
            if (fileName == null)
                return;

            IntPtr hModule = IntPtr.Zero;
            try
            {
                hModule = LoadLibraryEx(fileName, IntPtr.Zero, LOAD_LIBRARY_AS_DATAFILE);
                if (hModule == IntPtr.Zero)
                    return;

                FileName = GetFileName(hModule);

                // Enumerate the icon resource and build .ico files in memory.

                var tmpData = new List<byte[]>();

                ENUMRESNAMEPROC callback = (h, t, name, l) =>
                {
                // Refer the following URL for the data structures used here:
                // http://msdn.microsoft.com/en-us/library/ms997538.aspx

                // RT_GROUP_ICON resource consists of a GRPICONDIR and GRPICONDIRENTRY's.

                var dir = GetDataFromResource(hModule, RT_GROUP_ICON, name);

                // Calculate the size of an entire .icon file.

                int count = BitConverter.ToUInt16(dir, 4);  // GRPICONDIR.idCount
                int len = 6 + 16 * count;                   // sizeof(ICONDIR) + sizeof(ICONDIRENTRY) * count
                for (int i = 0; i < count; ++i)
                        len += BitConverter.ToInt32(dir, 6 + 14 * i + 8);   // GRPICONDIRENTRY.dwBytesInRes

                using (var dst = new BinaryWriter(new MemoryStream(len)))
                    {
                    // Copy GRPICONDIR to ICONDIR.

                    dst.Write(dir, 0, 6);

                        int picOffset = 6 + 16 * count; // sizeof(ICONDIR) + sizeof(ICONDIRENTRY) * count

                    for (int i = 0; i < count; ++i)
                        {
                        // Load the picture.

                        ushort id = BitConverter.ToUInt16(dir, 6 + 14 * i + 12);    // GRPICONDIRENTRY.nID
                        var pic = GetDataFromResource(hModule, RT_ICON, (IntPtr)id);

                        // Copy GRPICONDIRENTRY to ICONDIRENTRY.

                        dst.Seek(6 + 16 * i, SeekOrigin.Begin);

                            dst.Write(dir, 6 + 14 * i, 8);  // First 8bytes are identical.
                        dst.Write(pic.Length);          // ICONDIRENTRY.dwBytesInRes
                        dst.Write(picOffset);           // ICONDIRENTRY.dwImageOffset

                        // Copy a picture.

                        dst.Seek(picOffset, SeekOrigin.Begin);
                            dst.Write(pic, 0, pic.Length);

                            picOffset += pic.Length;
                        }

                        tmpData.Add(((MemoryStream)dst.BaseStream).ToArray());
                    }

                    return true;
                };
                EnumResourceNames(hModule, RT_GROUP_ICON, callback, IntPtr.Zero);

                iconData = tmpData.ToArray();
            }
            finally
            {
                if (hModule != IntPtr.Zero)
                    FreeLibrary(hModule);
            }
        }

        private byte[] GetDataFromResource(IntPtr hModule, IntPtr type, IntPtr name)
        {
            // Load the binary data from the specified resource.

            IntPtr hResInfo = FindResource(hModule, name, type);
            if (hResInfo == IntPtr.Zero)
                throw new Win32Exception();

            IntPtr hResData = LoadResource(hModule, hResInfo);
            if (hResData == IntPtr.Zero)
                throw new Win32Exception();

            IntPtr pResData = LockResource(hResData);
            if (pResData == IntPtr.Zero)
                throw new Win32Exception();

            uint size = SizeofResource(hModule, hResInfo);
            if (size == 0)
                throw new Win32Exception();

            byte[] buf = new byte[size];
            Marshal.Copy(pResData, buf, 0, buf.Length);

            return buf;
        }

        private string GetFileName(IntPtr hModule)
        {
            // Alternative to GetModuleFileName() for the module loaded with
            // LOAD_LIBRARY_AS_DATAFILE option.

            // Get the file name in the format like:
            // "\\Device\\HarddiskVolume2\\Windows\\System32\\shell32.dll"

            string fileName;
            {
                var buf = new StringBuilder(MAX_PATH);
                int len = GetMappedFileName(
                    GetCurrentProcess(), hModule, buf, buf.Capacity);
                if (len == 0)
                    throw new Win32Exception();

                fileName = buf.ToString();
            }

            // Convert the device name to drive name like:
            // "C:\\Windows\\System32\\shell32.dll"

            for (char c = 'A'; c <= 'Z'; ++c)
            {
                var drive = c + ":";
                var buf = new StringBuilder(MAX_PATH);
                int len = QueryDosDevice(drive, buf, buf.Capacity);
                if (len == 0)
                    continue;

                var devPath = buf.ToString();
                if (fileName.StartsWith(devPath))
                    return (drive + fileName.Substring(devPath.Length));
            }

            return fileName;
        }

        [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        [SuppressUnmanagedCodeSecurity]
        public static extern IntPtr LoadLibraryEx(string lpFileName, IntPtr hFile, uint dwFlags);

        [DllImport("kernel32.dll", SetLastError = true)]
        [SuppressUnmanagedCodeSecurity]
        public static extern bool FreeLibrary(IntPtr hModule);

        [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        [SuppressUnmanagedCodeSecurity]
        public static extern bool EnumResourceNames(IntPtr hModule, IntPtr lpszType, ENUMRESNAMEPROC lpEnumFunc, IntPtr lParam);

        [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        [SuppressUnmanagedCodeSecurity]
        public static extern IntPtr FindResource(IntPtr hModule, IntPtr lpName, IntPtr lpType);

        [DllImport("kernel32.dll", SetLastError = true)]
        [SuppressUnmanagedCodeSecurity]
        public static extern IntPtr LoadResource(IntPtr hModule, IntPtr hResInfo);

        [DllImport("kernel32.dll", SetLastError = true)]
        [SuppressUnmanagedCodeSecurity]
        public static extern IntPtr LockResource(IntPtr hResData);

        [DllImport("kernel32.dll", SetLastError = true)]
        [SuppressUnmanagedCodeSecurity]
        public static extern uint SizeofResource(IntPtr hModule, IntPtr hResInfo);

        [DllImport("kernel32.dll", SetLastError = true)]
        [SuppressUnmanagedCodeSecurity]
        public static extern IntPtr GetCurrentProcess();

        [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        [SuppressUnmanagedCodeSecurity]
        public static extern int QueryDosDevice(string lpDeviceName, StringBuilder lpTargetPath, int ucchMax);

        [DllImport("psapi.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        [SuppressUnmanagedCodeSecurity]
        public static extern int GetMappedFileName(IntPtr hProcess, IntPtr lpv, StringBuilder lpFilename, int nSize);


        [UnmanagedFunctionPointer(CallingConvention.Winapi, SetLastError = true, CharSet = CharSet.Unicode)]
        [SuppressUnmanagedCodeSecurity]
        public delegate bool ENUMRESNAMEPROC(IntPtr hModule, IntPtr lpszType, IntPtr lpszName, IntPtr lParam);




        public class PickerDialog : CommonDialog
        {

            [DllImport("shell32.dll", EntryPoint = "#62", CharSet = CharSet.Unicode, SetLastError = true)]
            [SuppressUnmanagedCodeSecurity]
            public static extern bool SHPickIconDialog(IntPtr hWnd, StringBuilder pszFilename, int cchFilenameMax, out int pnIconIndex);

            private const int MAX_PATH = 260;

            [DefaultValue(default(string))]
            public string FileName
            {
                get;
                set;
            }

            [DefaultValue(0)]
            public int IconIndex
            {
                get;
                set;
            }

            protected override bool RunDialog(IntPtr hwndOwner)
            {
                var buf = new StringBuilder(FileName, MAX_PATH);
                int index;

                bool ok = SHPickIconDialog(hwndOwner, buf, MAX_PATH, out index);
                if (ok)
                {
                    FileName = Environment.ExpandEnvironmentVariables(buf.ToString());
                    IconIndex = index;
                }

                return ok;
            }

            public override void Reset()
            {
                FileName = null;
                IconIndex = 0;
            }
        }
    }

}