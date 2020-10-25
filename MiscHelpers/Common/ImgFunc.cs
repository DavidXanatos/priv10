using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace MiscHelpers
{
    public static class ImgFunc
    {
        private static Dictionary<string, ImageSource> IconCache = new Dictionary<string, ImageSource>();
        private static ReaderWriterLockSlim IconCacheLock = new ReaderWriterLockSlim();

        public static ImageSource ExeIcon16 = GetIcon(NtUtilities.NtOsKrnlPath, 16);

        public static bool IsImageFileName(string fileName)
        {
            return fileName.EndsWith(".png", StringComparison.OrdinalIgnoreCase)
            || fileName.EndsWith(".jpg", StringComparison.OrdinalIgnoreCase)
            || fileName.EndsWith(".jpeg", StringComparison.OrdinalIgnoreCase)
            || fileName.EndsWith(".bmp", StringComparison.OrdinalIgnoreCase);
        }

        public static ImageSource GetIcon(string path, double size)
        {
            string key = path + "@" + size.ToString();

            ImageSource image = null;
            IconCacheLock.EnterReadLock();
            bool bFound = IconCache.TryGetValue(key, out image);
            IconCacheLock.ExitReadLock();
            if (bFound)
                return image;

            try
            {
                var pathIndex = TextHelpers.Split2(path, "|");

                if (IsImageFileName(pathIndex.Item1))
                {
                    try
                    {
                        image = new BitmapImage(new Uri(pathIndex.Item1));
                    }
                    catch { }
                }
                else
                {
                    IconExtractor extractor = new IconExtractor(pathIndex.Item1);
                    int index = MiscFunc.parseInt(pathIndex.Item2);
                    if (index < extractor.Count)
                        image = ToImageSource(extractor.GetIcon(index, new System.Drawing.Size((int)size, (int)size)));
                }

                if (image == null)
                {
                    if (File.Exists(NtUtilities.NtOsKrnlPath)) // if running in WOW64 this does not exist
                        image = ToImageSource(Icon.ExtractAssociatedIcon(NtUtilities.NtOsKrnlPath));
                    else // fall back to an other icon
                        image = ToImageSource(Icon.ExtractAssociatedIcon(NtUtilities.Shell32Path));
                }

                image.Freeze();
            }
            catch { }

            IconCacheLock.EnterWriteLock();
            if (!IconCache.ContainsKey(key))
                IconCache.Add(key, image);
            IconCacheLock.ExitWriteLock();
            return image;
        }

        public delegate ImageSource IconExtract(string path, double size);

        public static IAsyncResult GetIconAsync(string path, double size, Func<ImageSource, int> cb)
        {
            IconExtract iconExtract = new IconExtract(ImgFunc.GetIcon);
            return iconExtract.BeginInvoke(path, size, new AsyncCallback((IAsyncResult asyncResult) =>
            {
                ImageSource icon = (asyncResult.AsyncState as IconExtract).EndInvoke(asyncResult);
                cb(icon);
            }), iconExtract);
        }


        [DllImport("gdi32.dll", SetLastError = true)]
        private static extern bool DeleteObject(IntPtr hObject);

        public static ImageSource ToImageSource(this Icon icon)
        {
            Bitmap bitmap = icon.ToBitmap();
            IntPtr hBitmap = bitmap.GetHbitmap();
            ImageSource wpfBitmap = Imaging.CreateBitmapSourceFromHBitmap(hBitmap, IntPtr.Zero, Int32Rect.Empty, BitmapSizeOptions.FromEmptyOptions());
            if (!DeleteObject(hBitmap))
                throw new Win32Exception();
            return wpfBitmap;
        }
    }

}