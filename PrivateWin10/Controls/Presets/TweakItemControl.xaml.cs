using MiscHelpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using TweakEngine;

namespace PrivateWin10.Controls
{
    /// <summary>
    /// Interaction logic for TweakItemControl.xaml
    /// </summary>
    public partial class TweakItemControl : UserControl, IControlItem<TweakPreset.SingleTweak>
    {
        public event RoutedEventHandler Click;

        public event EventHandler<EventArgs> ItemChanged;

        public TweakPreset.SingleTweak item;

        public static SolidColorBrush GetPresetColor(int value)
        {
            if (value == 1)
                return new SolidColorBrush(Colors.LightGreen);
            if (value == 0)
                return new SolidColorBrush(Colors.LightPink);
            return null;
        }

        public TweakItemControl(TweakPreset.SingleTweak item)
        {
            InitializeComponent();

            cmbPreset.Items.Add(new ComboBoxItem() { Content = Translate.fmt("keep_tweak"), Tag = -1 });
            cmbPreset.Items.Add(new ComboBoxItem() { Content = Translate.fmt("enable_tweak"), Tag = 1, Background = GetPresetColor(1) });
            cmbPreset.Items.Add(new ComboBoxItem() { Content = Translate.fmt("disable_tweak"), Tag = 0, Background = GetPresetColor(0) });

            DoUpdate(item);

            rect.MouseDown += new MouseButtonEventHandler(rect_Click);
            label.MouseDown += new MouseButtonEventHandler(rect_Click);
            info.PreviewMouseDown += new MouseButtonEventHandler(rect_Click);

            //toggle.Click += new RoutedEventHandler(toggle_Click);
        }

        int SuspendChange = 0;

        public void DoUpdate(TweakPreset.SingleTweak item)
        {
            this.item = item;

            SuspendChange++;

            label.Content = item.TweakName; // todo: poepr name translation support xxxx <<<<<<<<<<<<

            int Value = item.OnState == null ? -1 : item.OnState == true ? 1 : 0;
            WpfFunc.CmbSelect(cmbPreset, Value.ToString());
            cmbPreset.Background = GetPresetColor(Value);

            SuspendChange--;

            TweakList.Tweak tweak = App.tweaks.GetTweak(item.TweakName);
            if (tweak == null)
                return;

            string infoStr = "";

            switch (tweak.Type)
            {
                case TweakList.TweakType.SetRegistry:
                case TweakList.TweakType.SetGPO:
                    infoStr += tweak.Path + "\r\n";
                    infoStr += tweak.Key + " = " + tweak.Value + "\r\n";
                    break;
                case TweakList.TweakType.DisableTask:
                    infoStr += "Disable Scheduled Task: " + tweak.Path + "\\" + tweak.Key + "\r\n";
                    break;
                case TweakList.TweakType.DisableService:
                    infoStr += "Disable Service: " + tweak.Key + "\r\n";
                    break;
                case TweakList.TweakType.BlockFile:
                    infoStr += "Dissable Access to: " + tweak.Path + "\r\n";
                    break;
                //case TweakType.UseFirewall:
                //    infoStr += "Set Firewal roule" + "\r\n";
                //    break;
                default:
                    infoStr = "Unknown Tweak Type";
                    break;
            }

            info.Text = infoStr;
        }

        /*private void toggle_Click(object sender, RoutedEventArgs e)
        {
            
        }*/

        private bool mHasFocus = false;

        public bool GetFocus() { return mHasFocus; }

        public void SetFocus(bool set = true)
        {
            this.rect.StrokeThickness = 2;
            this.rect.Stroke = set ? new SolidColorBrush(Color.FromArgb(255, 51, 153, 255)) : null;
            //this.rect.Fill = new SolidColorBrush(set ? Color.FromArgb(255, 153, 204, 255) : Colors.Transparent);
            //this.rect.Fill = new SolidColorBrush(set ? Color.FromArgb(255, 230, 240, 255) : Colors.Transparent);

            mHasFocus = set;
        }

        private void rect_Click(object sender, RoutedEventArgs e)
        {
            Click?.Invoke(this, e);
        }

        private void CmbPreset_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (SuspendChange != 0)
                return;

            int Value = (int)(cmbPreset.SelectedItem as ComboBoxItem).Tag;

            if (Value == -1)
                item.OnState = item.OffState = null;
            else
                item.OffState = !(item.OnState = (Value == 1)).Value;

            cmbPreset.Background = GetPresetColor(Value);

            ItemChanged?.Invoke(this, new EventArgs());
        }
    }
}
