using PrivateAPI;
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
using System.Windows.Shapes;

namespace PrivateWin10.Windows
{
    /// <summary>
    /// Interaction logic for SetupWnd.xaml
    /// </summary>
    public partial class SetupWnd : Window
    {
        public SetupWnd()
        {
            InitializeComponent();

            this.Title = Translate.fmt("wnd_setup", App.Title);

            this.lblStartup.Text = Translate.fmt("lbl_startup_options");
            this.chkAutoStart.Content = Translate.fmt("chk_autorun");
            this.chkService.Content = Translate.fmt("chk_instal_svc");
            this.chkNoUAC.Content = Translate.fmt("chk_no_uac");

            this.lblFirewall.Text = Translate.fmt("lbl_firewall_options");
            this.chkUseFW.Content = Translate.fmt("chk_manage_fw");
            this.chkNotifyFW.Content = Translate.fmt("chk_show_notify");

            chkNoUAC.IsChecked = AdminFunc.IsSkipUac(App.Key);
            chkNotifyFW.IsChecked = App.GetConfigInt("Firewall", "NotifyBlocked", 1) != 0;
        }

        private void BtnOK_Click(object sender, RoutedEventArgs e)
        {
            bool Restart = false;

            if (chkAutoStart.IsChecked == true)
            {
                App.SetConfig("Startup", "Tray", true);
                App.TrayIcon.Visible = true;

                App.AutoStart(true);
                if (chkService.IsChecked == true)
                {
                    Priv10Service.Install();
                    App.Log.SetupEventLog(App.Key);
                    Restart = true;
                }
            }
            if(chkNoUAC.IsChecked == true)
                AdminFunc.SkipUacEnable(App.Key, true);

            if (chkUseFW.IsChecked == true)
            {
                App.SetConfig("Firewall", "Enabled", 1);

                FirewallManager.FilteringModes Mode = FirewallManager.FilteringModes.WhiteList;
                App.SetConfig("Firewall", "Mode", Mode.ToString());
                App.client.SetFilteringMode(Mode);

                FirewallMonitor.Auditing audit = FirewallMonitor.Auditing.All;
                App.SetConfig("Firewall", "AuditPol", audit.ToString());
                App.client.SetAuditPolicy(audit);

                App.SetConfig("Firewall", "NotifyBlocked", chkNotifyFW.IsChecked == true ? 1 : 0);

                if (!App.client.IsConnected())
                {
                    App.StartEngine();
                    App.client.Connect();
                }
            }

            App.SetConfig("Startup", "ShowSetup", 0);
            if(Restart)
                App.Restart();
            this.DialogResult = true;
        }

        private void ChkAutoStart_Click(object sender, RoutedEventArgs e)
        {
            chkService.IsEnabled = chkAutoStart.IsChecked == true;
        }

        private void ChkUseFW_Click(object sender, RoutedEventArgs e)
        {
            chkNotifyFW.IsEnabled = chkUseFW.IsChecked == true;
        }
    }
}
