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

            this.lblStartup.Text = Translate.fmt("lbl_startup_options");
            this.chkTray.Content = Translate.fmt("chk_show_tray");
            this.chkAutoStart.Content = Translate.fmt("chk_autorun");
            this.chkService.Content = Translate.fmt("chk_instal_svc");
            this.chkNoUAC.Content = Translate.fmt("chk_no_uac");

            this.chkTweakCheck.Content = Translate.fmt("chk_tweak_check");
            this.chkTweakFix.Content = Translate.fmt("chk_tweak_fix");

            this.lblFirewall.Text = Translate.fmt("lbl_firewall_options");
            this.chkUseFW.Content = Translate.fmt("chk_manage_fw");
            this.chkNotifyFW.Content = Translate.fmt("chk_show_notify");

            this.chkGuardFW.Content = Translate.fmt("chk_fw_guard");
            //this.chkFixRules.Content = Translate.fmt("chk_fix_rules");
            this.radAlert.Content = Translate.fmt("chk_fw_guard_alert");
            this.radDisable.Content = Translate.fmt("chk_fw_guard_disable");
            this.radFix.Content = Translate.fmt("chk_fw_guard_fix");

            this.lblMode.Content = Translate.fmt("lbl_filter_mode");
            this.radWhitelist.Content = Translate.fmt("chk_fw_whitelist");
            this.radBlacklist.Content = Translate.fmt("chk_fw_blacklist");
            this.radDisabled.Content = Translate.fmt("chk_fw_disable");
            this.lblAudit.Content = Translate.fmt("chk_audit_policy");

            this.lblAuditAll.Content = Translate.fmt("lbl_audit_all");
            this.lblAuditBlock.Content = Translate.fmt("lbl_audit_blocked");
            this.lblAuditNone.Content = Translate.fmt("lbl_audit_off");


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

            chkTweakCheck.IsChecked = App.GetConfigInt("TweakGuard", "AutoCheck", 1) != 0;
            chkTweakFix.IsEnabled = chkTweakCheck.IsChecked == true;
            chkTweakFix.IsChecked = App.GetConfigInt("TweakGuard", "AutoFix", 0) != 0;

            chkUseFW.IsChecked = App.GetConfigInt("Firewall", "Enabled", 0) != 0;

            if (App.client.IsConnected())
            {
                var mode = App.client.GetFilteringMode();

                radWhitelist.IsChecked = mode == FirewallManager.FilteringModes.WhiteList;
                radBlacklist.IsChecked = mode == FirewallManager.FilteringModes.BlackList;
                radDisabled.IsChecked = mode == FirewallManager.FilteringModes.NoFiltering;

                var pol = App.client.GetAuditPolicy();
                switch (pol)
                {
                    case FirewallMonitor.Auditing.All: cmbAudit.SelectedItem = lblAuditAll; break;
                    case FirewallMonitor.Auditing.Blocked: cmbAudit.SelectedItem = lblAuditBlock; break;
                    default: cmbAudit.SelectedItem = lblAuditNone; break;
                }

                chkGuardFW.IsChecked = App.client.IsFirewallGuard();
            }
            else
            {
                chkUseFW.IsEnabled = false;

                radWhitelist.IsEnabled = false;
                radBlacklist.IsEnabled = false;
                radDisabled.IsEnabled = false;
                cmbAudit.IsEnabled = false;

                chkGuardFW.IsEnabled = false;
            }

            var fix_mode = App.GetConfigInt("Firewall", "GuardMode", 0);

            radFix.IsChecked = fix_mode == (int)FirewallGuard.Mode.Fix;
            radDisable.IsChecked = fix_mode == (int)FirewallGuard.Mode.Disable;
            radAlert.IsChecked = fix_mode == (int)FirewallGuard.Mode.Alert;

            radFix.IsEnabled = radDisable.IsEnabled = radAlert.IsEnabled = chkGuardFW.IsChecked != false;


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
            if (bHold) return;

            if (!AdminFunc.IsAdministrator())
            {
                if (MessageBox.Show(Translate.fmt("msg_admin_prompt", App.mName), App.mName, MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
                    App.Restart(true);
                return;
            }

            App.client.Close();
            if (chkService.IsChecked == true)
            {
                if (App.engine != null)
                {
                    App.engine.Stop();

                    App.engine = null;
                }

                App.svc.Install(true);
                App.Log.SetupEventLog(App.mAppName);
            }
            else
            {
                App.svc.Uninstall();

                if (App.engine == null)
                {
                    App.engine = new Engine();

                    App.engine.Start();
                }
            }
            App.client.Connect();
        }

        private void chkNoUAC_Click(object sender, RoutedEventArgs e)
        {
            if (bHold) return;

            if (!AdminFunc.IsAdministrator())
            {
                if (MessageBox.Show(Translate.fmt("msg_admin_prompt", App.mName), App.mName, MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
                    App.Restart(true);
                return;
            }

            AdminFunc.SkipUacEnable(App.mName, chkNoUAC.IsChecked == true);
        }

        private void radMode_Checked(object sender, RoutedEventArgs e)
        {
            if (bHold) return;

            FirewallManager.FilteringModes Mode;
            if (radWhitelist.IsChecked == true)
                Mode = FirewallManager.FilteringModes.WhiteList;
            else if (radBlacklist.IsChecked == true)
                Mode = FirewallManager.FilteringModes.BlackList;
            else //if (radDisabled.IsChecked == true)
                Mode = FirewallManager.FilteringModes.NoFiltering;

            App.SetConfig("Firewall", "Mode", Mode.ToString());
            App.client.SetFilteringMode(Mode);
        }

        private void cmbAudit_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (bHold) return;

            FirewallMonitor.Auditing audit;
            if (cmbAudit.SelectedItem == lblAuditAll)
                audit = FirewallMonitor.Auditing.All;
            else if (cmbAudit.SelectedItem == lblAuditBlock)
                audit = FirewallMonitor.Auditing.Blocked;
            else //if (cmbAudit.SelectedItem == lblAuditNone)
                audit = FirewallMonitor.Auditing.Off;

            App.client.SetAuditPolicy(audit);
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

        private void ChkTweakCheck_Click(object sender, RoutedEventArgs e)
        {
            if (bHold) return;

            App.SetConfig("TweakGuard", "AutoCheck", chkTweakCheck.IsChecked == true ? 1 : 0);
            chkTweakFix.IsEnabled = chkTweakCheck.IsChecked == true;
        }

        private void ChkTweakFix_Click(object sender, RoutedEventArgs e)
        {
            if (bHold) return;

            App.SetConfig("TweakGuard", "AutoFix", chkTweakFix.IsChecked == true ? 1 : 0);
        }

        private void ChkGuardFW_Click(object sender, RoutedEventArgs e)
        {
            if (bHold) return;

            radFix.IsEnabled = radDisable.IsEnabled = radAlert.IsEnabled = chkGuardFW.IsChecked != false;

            setFirewallGuard();
        }

        private void radGuard_Checked(object sender, RoutedEventArgs e)
        {
            if (bHold) return;

            setFirewallGuard();
        }

        private void setFirewallGuard()
        {
            FirewallGuard.Mode Mode;
            if (radFix.IsChecked == true)
                Mode = FirewallGuard.Mode.Fix;
            else if (radDisable.IsChecked == true)
                Mode = FirewallGuard.Mode.Disable;
            else //if (radAlert.IsChecked == true)
                Mode = FirewallGuard.Mode.Alert;

            App.client.SetFirewallGuard(chkGuardFW.IsChecked != false, Mode);
        }
    }
}
