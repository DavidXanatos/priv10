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

public static class ImgFunc
{
    private static Dictionary<string, ImageSource> IconCache = new Dictionary<string, ImageSource>();
    private static ReaderWriterLockSlim IconCacheLock = new ReaderWriterLockSlim();

    public static ImageSource GetIcon(string path, double size)
    {
        ImageSource image = null;
        IconCacheLock.EnterReadLock();
        IconCache.TryGetValue(path, out image);
        IconCacheLock.ExitReadLock();
        if(image != null)
            return image;

        var pathIndex = TextHelpers.Split2(path, "|");

        IconExtractor extractor = new IconExtractor(pathIndex.Item1);
        int index = MiscFunc.parseInt(pathIndex.Item2);
        if(index < extractor.Count)
            image = ToImageSource(extractor.GetIcon(index, new System.Drawing.Size((int)size, (int)size)));

        if (image == null)
            image = ToImageSource(Icon.ExtractAssociatedIcon(MiscFunc.NtOsKrnlPath));

        IconCacheLock.EnterWriteLock();
        image.Freeze();
        if (!IconCache.ContainsKey(path))
            IconCache.Add(path, image);
        IconCacheLock.ExitWriteLock();

        return image;
    }

    public delegate ImageSource IconExtract(string path, double size);

    public static IAsyncResult GetIconAsync(string path, double size, Func<ImageSource, int> cb)
    {
        IconExtract iconExtract = new IconExtract(ImgFunc.GetIcon);
        return iconExtract.BeginInvoke(path, size, new AsyncCallback((IAsyncResult asyncResult) => {
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
