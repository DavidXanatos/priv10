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

namespace PrivateWin10.Pages
{
    /// <summary>
    /// Interaction logic for SettingsPage.xaml
    /// </summary>
    public partial class SettingsPage : UserControl, IUserPage
    {
        private bool bHold = false;

        public SettingsPage()
        {
            InitializeComponent();

            Refresh();
        }

        public void OnShow()
        {
            Refresh();
        }

        public void OnHide()
        {
        }

        public void OnClose()
        {
        }

        public void Refresh()
        {
            bHold = true;

            chkTray.IsChecked = App.GetConfigInt("Startup", "Tray") != 0;
            chkAutoStart.IsChecked = App.IsAutoStart();
            chkService.IsChecked = App.svc.IsInstalled();
            chkNoUAC.IsChecked = AdminFunc.IsSkipUac(App.mName);

            if (!AdminFunc.IsAdministrator())
            {
                chkService.IsEnabled = false;
                chkNoUAC.IsEnabled = false;
            }

            chkUseFW.IsChecked = App.GetConfigInt("Firewall", "Enabled", 0) != 0;

            if (App.client.IsConnected())
            {
                var mode = App.itf.GetFilteringMode();

                radWhitelist.IsChecked = mode == Firewall.FilteringModes.WhiteList;
                radBlacklist.IsChecked = mode == Firewall.FilteringModes.BlackList;
                radDisabled.IsChecked = mode == Firewall.FilteringModes.NoFiltering;

                var pol = App.itf.GetAuditPol();
                switch (pol)
                {
                    case Firewall.Auditing.All: cmbAudit.SelectedItem = lblAuditAll; break;
                    case Firewall.Auditing.Blocked: cmbAudit.SelectedItem = lblAuditBlock; break;
                    default: cmbAudit.SelectedItem = lblAuditAll; break;
                }
            }
            else
            {
                chkUseFW.IsEnabled = false;

                radWhitelist.IsEnabled = false;
                radBlacklist.IsEnabled = false;
                radDisabled.IsEnabled = false;
                cmbAudit.IsEnabled = false;
            }

            chkNotifyFW.IsEnabled = chkUseFW.IsChecked == true;
            chkNotifyFW.IsChecked = App.GetConfigInt("Firewall", "NotifyBlocked", 1) != 0;

            bHold = false;
        }

        private void chkTray_Click(object sender, RoutedEventArgs e)
        {
            if (bHold) return;
            App.SetConfig("Startup", "Tray", chkTray.IsChecked == true);
            App.mTray.Visible = chkTray.IsChecked == true;
        }

        private void chkAutoStart_Click(object sender, RoutedEventArgs e)
        {
            if (bHold) return;
            App.AutoStart(chkAutoStart.IsChecked == true);
        }

        private void chkService_Click(object sender, RoutedEventArgs e)
        {
            if (chkService.IsChecked == true)
                App.svc.Install();
            else
                App.svc.Uninstall();
        }

        private void chkNoUAC_Click(object sender, RoutedEventArgs e)
        {
            if (bHold) return;
            AdminFunc.SkipUacEnable(App.mName, chkNoUAC.IsChecked == true);
        }

        private void radMode_Checked(object sender, RoutedEventArgs e)
        {
            if (bHold) return;

            Firewall.FilteringModes Mode;
            if (radWhitelist.IsChecked == true)
                Mode = Firewall.FilteringModes.WhiteList;
            else if (radBlacklist.IsChecked == true)
                Mode = Firewall.FilteringModes.BlackList;
            else //if (radDisabled.IsChecked == true)
                Mode = Firewall.FilteringModes.NoFiltering;

            App.SetConfig("Firewall", "Mode", Mode.ToString());
            App.itf.SetFilteringMode(Mode);
        }

        private void cmbAudit_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (bHold) return;

            Firewall.Auditing audit;
            if (cmbAudit.SelectedItem == lblAuditAll)
                audit = Firewall.Auditing.All;
            else if (cmbAudit.SelectedItem == lblAuditBlock)
                audit = Firewall.Auditing.Blocked;
            else //if (cmbAudit.SelectedItem == lblAuditNone)
                audit = Firewall.Auditing.Off;

            App.SetConfig("Firewall", "AuditPol", audit.ToString());
            App.itf.SetAuditPol(audit);
        }

        private void ChkUseFW_Click(object sender, RoutedEventArgs e)
        {
            if (bHold) return;

            if (chkUseFW.IsChecked == true)
            {
                chkNotifyFW.IsEnabled = true;
                cmbAudit.SelectedItem = lblAuditAll;
                radWhitelist.IsChecked = true;
            }
            else
            {
                chkNotifyFW.IsEnabled = false;
                cmbAudit.SelectedItem = lblAuditNone;
                radBlacklist.IsChecked = true;
            }

            App.SetConfig("Firewall", "Enabled", chkUseFW.IsChecked == true ? 1 : 0);
        }

        private void chkNotifyFW_Click(object sender, RoutedEventArgs e)
        {
            if (bHold) return;

            App.SetConfig("Firewall", "NotifyBlocked", chkNotifyFW.IsChecked == true ? 1 : 0);
        }
    }
}
