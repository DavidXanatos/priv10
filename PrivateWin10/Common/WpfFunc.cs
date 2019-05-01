using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using PrivateWin10;

public class WpfFunc
{
    public class ViewModelHelper : INotifyPropertyChanged//, IDisposable
    {
        /*public void Dispose()
        {
        }*/

        protected void SetProperty<T>(string Name, T newValue, ref T curValue)
        {
            if (Equals(newValue, curValue))
                return;
            curValue = newValue;
            RaisePropertyChanged(Name);
        }

        protected void SetPropertyCmb(string Name, ContentControl newValue, ref ContentControl curValue, ref string curText)
        {
            SetProperty(Name, newValue, ref curValue);
            if (curValue != null)
                curText = curValue.Content.ToString();
        }


        #region INotifyPropertyChanged Members

        public event PropertyChangedEventHandler PropertyChanged;

        /// <summary>
        /// Raises the PropertyChanged event if needed.
        /// </summary>
        /// <param name="propertyName">The name of the property that changed.</param>
        protected virtual void RaisePropertyChanged(string propertyName)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }

        #endregion
    }

    static public ContentControl CmbPick(ComboBox box, string tag)
    {
        if (tag == null)
            return null;
        for (int i = 0; i < box.Items.Count; i++)
        {
            ContentControl item = (box.Items[i] as ContentControl);
            if (item.Tag != null && item.Tag.ToString().Equals(tag))
                return item;
        }
        return null;
    }

    static public bool CmbSelect(ComboBox box, string tag)
    {
        ContentControl item = CmbPick(box, tag);
        if (item != null)
        {
            box.SelectedItem = item;
            return true;
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

    public static List<string> SplitAndValidate(string Values, ref bool? duplicate)
    {
        if (Values == null)
            return null;
        List<string> ValueList = new List<string>();
        foreach (string Value in Values.Split(','))
        {
            string temp = Value.Trim();
            if (duplicate != null && ValueList.Contains(temp))
            {
                duplicate = true;
                return null;
            }
            ValueList.Add(temp);
        }
        if (ValueList.Count == 0)
            return null;
        return ValueList;
    }

}
