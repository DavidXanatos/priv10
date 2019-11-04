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

namespace PrivateWin10
{
    /// <summary>
    /// Interaction logic for TweakControl.xaml
    /// </summary>
    public partial class TweakControl : UserControl
    {
        public event RoutedEventHandler Click;
        public event RoutedEventHandler Toggle;
        //public event RoutedEventHandler ReqSU;

        TweakManager.Tweak Tweak;

        public TweakControl(TweakManager.Tweak tweak)
        {
            Tweak = tweak;
            //myTweak.StatusChanged += OnStatusChanged;

            InitializeComponent();

            //OnStatusChanged(null, null);

            string infoStr = "";

            switch (tweak.Type)
            {
                case TweakManager.TweakType.SetRegistry:
                case TweakManager.TweakType.SetGPO:
                    infoStr += tweak.Path + "\r\n";
                    infoStr += tweak.Key + " = " + tweak.Value + "\r\n";
                    break;
                case TweakManager.TweakType.DisableTask:
                    infoStr += "Disable Scheduled Task: " + tweak.Path + "\\" + tweak.Key + "\r\n";
                    break;
                case TweakManager.TweakType.DisableService:
                    infoStr += "Disable Service: " + tweak.Key + "\r\n";
                    break;
                case TweakManager.TweakType.BlockFile:
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

            toggle.Click += new RoutedEventHandler(toggle_Click);

            toggle.Click += new RoutedEventHandler(rect_Click);
            rect.MouseDown += new MouseButtonEventHandler(rect_Click);
            label.MouseDown += new MouseButtonEventHandler(rect_Click);
            info.PreviewMouseDown += new MouseButtonEventHandler(rect_Click);
        }

        public void SetFocus(bool set = true)
        {
            this.rect.StrokeThickness = 2;
            this.rect.Stroke = set ? new SolidColorBrush(Color.FromArgb(255, 51, 153, 255)) : null;
            //this.rect.Fill = new SolidColorBrush(set ? Color.FromArgb(255, 153, 204, 255) : Colors.Transparent);
            this.rect.Fill = new SolidColorBrush(set ? Color.FromArgb(255, 230, 240, 255) : Colors.Transparent);
        }

        private void rect_Click(object sender, RoutedEventArgs e)
        {
            Click?.Invoke(this, e);
        }

        private void toggle_Click(object sender, RoutedEventArgs e)
        {
            Toggle?.Invoke(this, e);

            /*if (!myTweak.usrLevel && !AdminFunc.IsAdministrator())
            {
                ReqSU?.Invoke(this, e);
                OnStatusChanged(null, null);
                return;
            }

            if ((bool)toggle.IsChecked)
                myTweak.Apply((System.Windows.Forms.Control.ModifierKeys & System.Windows.Forms.Keys.Control) == 0);
            else
                myTweak.Undo((System.Windows.Forms.Control.ModifierKeys & System.Windows.Forms.Keys.Control) == 0);
            */
        }

        public bool? IsChecked { get { return toggle.IsChecked; } }

        public void Update()
        {
            // toggle.IsChecked = Tweak.Test();
            toggle.IsChecked = Tweak.Status;
        }

        /*void OnStatusChanged(object sender, EventArgs arg)
        {
            Update();
        }*/
    }
}
