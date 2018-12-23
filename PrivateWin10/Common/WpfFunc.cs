using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using PrivateWin10;

public class WpfFunc
{
    static public bool CmbSelect(ComboBox box, string tag)
    {
        if (tag == null)
            return false;
        for (int i = 0; i < box.Items.Count; i++)
        {
            ContentControl item = (box.Items[i] as ContentControl);
            if (item.Tag.ToString().Equals(tag))
            {
                box.SelectedIndex = i;
                return true;
            }
        }
        return false;
    }

    public static void StoreWnd(Window wnd, string name)
    {
        App.SetConfig("GUI", name + "WndPos", wnd.Left + ":" + wnd.Top);
        App.SetConfig("GUI", name + "WndSize", wnd.Width + ":" + wnd.Height);
    }

    public static void LoadWnd(Window wnd, string name)
    {
        string wndPos = App.GetConfig("GUI", name + "WndPos", null);
        if (wndPos != null)
        {
            var LT = TextHelpers.Split2(wndPos, ":");
            wnd.Left = MiscFunc.parseInt(LT.Item1);
            wnd.Top = MiscFunc.parseInt(LT.Item2);
        }
        string wndSize = App.GetConfig("GUI", name + "WndSize", null);
        if (wndSize != null)
        {
            var WH = TextHelpers.Split2(wndSize, ":");
            wnd.Width = MiscFunc.parseInt(WH.Item1);
            wnd.Height = MiscFunc.parseInt(WH.Item2);
        }
    }
}
