using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;

public static class ImgFunc
{
    static Dictionary<string, ImageSource> IconCache = new Dictionary<string, ImageSource>();

    public static ImageSource GetIcon(string path, double size)
    {
        ImageSource image = null;
        if (IconCache.TryGetValue(path, out image))
            return image;

        var pathIndex = TextHelpers.Split2(path, "|");

        try
        {
            IconExtractor extractor = new IconExtractor(pathIndex.Item1);
            int index = MiscFunc.parseInt(pathIndex.Item2);
            if(index < extractor.Count)
                image = ToImageSource(extractor.GetIcon(index, new System.Drawing.Size((int)size, (int)size)));
        }
        catch { }

        if (image == null)
            image = ToImageSource(Icon.ExtractAssociatedIcon(MiscFunc.mNtOsKrnlPath));

        IconCache.Add(path, image);

        return image;
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
