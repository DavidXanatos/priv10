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
using System.IO.Compression;
using Microsoft.Win32;
using System.IO;
using System.Diagnostics;

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

            this.lblPrivacy.Text = Translate.fmt("lbl_tweak_guard");
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

            this.chkDnsInspector.Content = Translate.fmt("lbl_use_inspector");
            this.chkReverseDNS.Content = Translate.fmt("lbl_use_rev_dns");
            this.lblDNS.Text = Translate.fmt("lbl_dns_proxy");
            this.chkEnableDNS.Content = Translate.fmt("lbl_use_dns_proxy");
            this.chkLocalDNS.Content = Translate.fmt("lbl_setup_dns");
            this.lblRootDNS.Content = Translate.fmt("lbl_dns_root");

            WpfFunc.CmbAdd(this.cmbRootDNS, "Google", "8.8.8.8|8.8.4.4");
            WpfFunc.CmbAdd(this.cmbRootDNS, "OpenDNS", "208.67.222.222|208.67.220.220");
            WpfFunc.CmbAdd(this.cmbRootDNS, "Level3", "209.244.0.3|209.244.0.4");
            WpfFunc.CmbAdd(this.cmbRootDNS, "DNS.WATCH", "84.200.69.80|84.200.70.40");
            WpfFunc.CmbAdd(this.cmbRootDNS, "Cloudflare", "1.1.1.1|1.0.0.1");

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
            chkService.IsChecked = Priv10Service.IsInstalled();
            chkNoUAC.IsChecked = AdminFunc.IsSkipUac(App.Key);

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

                chkGuardFW.IsEnabled = chkUseFW.IsChecked == true;
                chkGuardFW.IsChecked = App.client.IsFirewallGuard();

                chkEnableDNS.IsChecked = App.GetConfigInt("DnsProxy", "Enabled", 0) != 0;
            }
            else
            {
                chkUseFW.IsEnabled = false;

                radWhitelist.IsEnabled = false;
                radBlacklist.IsEnabled = false;
                radDisabled.IsEnabled = false;
                cmbAudit.IsEnabled = false;

                chkGuardFW.IsEnabled = false;

                chkEnableDNS.IsEnabled = false;
            }

            var fix_mode = App.GetConfigInt("Firewall", "GuardMode", 0);

            radFix.IsChecked = fix_mode == (int)FirewallGuard.Mode.Fix;
            radDisable.IsChecked = fix_mode == (int)FirewallGuard.Mode.Disable;
            radAlert.IsChecked = fix_mode == (int)FirewallGuard.Mode.Alert;

            radFix.IsEnabled = radDisable.IsEnabled = radAlert.IsEnabled = (chkGuardFW.IsChecked != false && chkGuardFW.IsEnabled);


            chkNotifyFW.IsEnabled = chkUseFW.IsChecked == true;
            chkNotifyFW.IsChecked = App.GetConfigInt("Firewall", "NotifyBlocked", 1) != 0;

            chkDnsInspector.IsChecked = App.GetConfigInt("DnsInspector", "Enabled", 0) != 0;

            chkReverseDNS.IsChecked = App.GetConfigInt("DnsInspector", "UseReverseDNS", 0) != 0;

            // DNS
            chkLocalDNS.IsChecked = App.GetConfigInt("DnsProxy", "SetLocal", 0) != 0;
            string UpstreamDNS = App.GetConfig("DNSProxy", "UpstreamDNS", "8.8.8.8");
            if (!WpfFunc.CmbSelect(cmbRootDNS, UpstreamDNS))
                cmbRootDNS.Text = UpstreamDNS;
            CheckDNS();

            //this.btnBackup.IsEnabled = this.btnRestore.IsEnabled = !App.isPortable;

            bHold = false;
        }

        private void chkTray_Click(object sender, RoutedEventArgs e)
        {
            if (bHold) return;

            App.SetConfig("Startup", "Tray", chkTray.IsChecked == true);
            App.TrayIcon.Visible = chkTray.IsChecked == true;
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
                if (MessageBox.Show(Translate.fmt("msg_admin_prompt", App.Title), App.Title, MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
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

                Priv10Service.Install(true);
                App.Log.SetupEventLog(App.Key);
            }
            else
            {
                Priv10Service.Uninstall();

                if (App.engine == null)
                {
                    App.engine = new Priv10Engine();

                    App.engine.Start();
                }
            }
            App.client.Connect();

            App.MainWnd.UpdateEnabled();
        }

        private void chkNoUAC_Click(object sender, RoutedEventArgs e)
        {
            if (bHold) return;

            if (!AdminFunc.IsAdministrator())
            {
                if (MessageBox.Show(Translate.fmt("msg_admin_prompt", App.Title), App.Title, MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
                    App.Restart(true);
                return;
            }

            AdminFunc.SkipUacEnable(App.Key, chkNoUAC.IsChecked == true);
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
                cmbAudit.SelectedItem = lblAuditAll;
                radWhitelist.IsChecked = true;
            }
            else
            {
                cmbAudit.SelectedItem = lblAuditNone;
                chkGuardFW.IsChecked = false;
                radBlacklist.IsChecked = true;
            }

            chkNotifyFW.IsEnabled = chkUseFW.IsChecked == true;
            chkGuardFW.IsEnabled = chkUseFW.IsChecked == true;

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

        private void ChkDnsInspector_Click(object sender, RoutedEventArgs e)
        {
            if (bHold) return;

            App.client.SetupDnsInspector(chkDnsInspector.IsChecked == true);
        }

        private void ChkReverseDNS_Click(object sender, RoutedEventArgs e)
        {
            if (bHold) return;

            App.SetConfig("DnsInspector", "UseReverseDNS", chkReverseDNS.IsChecked == true ? 1 : 0);
        }

        
        private void ChkEnableDNS_Click(object sender, RoutedEventArgs e)
        {
            ConfigureDNS();
        }

        private void ChkLocalDNS_Click(object sender, RoutedEventArgs e)
        {
            // todo: xxx check if there are any 3rd party dns's set

            ConfigureDNS(true);
        }

        private void CmbRootDNS_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Return)
                CmbRootDNS_SelectionChanged(sender, null);
        }

        private void CmbRootDNS_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ConfigureDNS();
        }

        private void ConfigureDNS(bool setLocal = false)
        {
            CheckDNS();
            if (bHold) return;

            string UpstreamDNS = cmbRootDNS.Text;
            if (cmbRootDNS.SelectedItem != null && (cmbRootDNS.SelectedItem as ComboBoxItem).Tag != null)
                UpstreamDNS = (cmbRootDNS.SelectedItem as ComboBoxItem).Tag.ToString();
            else
            {
                // todo: xxx check if upstream dns is a valid IP
            }

            if (!App.client.ConfigureDNSProxy(chkEnableDNS.IsChecked == true, setLocal ? chkLocalDNS.IsChecked : null, UpstreamDNS))
            {
                MessageBox.Show(Translate.fmt("msg_dns_proxy_err", App.GetConfigInt("DNSProxy", "Port", DnsProxyServer.DEFAULT_PORT)), App.Title, MessageBoxButton.OK, MessageBoxImage.Stop);

                bHold = true;
                App.SetConfig("DnsProxy", "Enabled", 0);
                chkEnableDNS.IsChecked = false;
                bHold = false;
            }
            App.MainWnd.UpdateEnabled();
        }

        private void CheckDNS()
        {
            bool is_dns_standard = App.GetConfig("DNSProxy", "BindIP", "").Length == 0 && App.GetConfigInt("DNSProxy", "Port", DnsProxyServer.DEFAULT_PORT) == DnsProxyServer.DEFAULT_PORT;

            // always allow unchecking, allow checking only when the DNS Proxy is in a compatible way and enabled
            chkLocalDNS.IsEnabled = chkLocalDNS.IsChecked == true || is_dns_standard && chkEnableDNS.IsChecked == true;

            cmbRootDNS.IsEnabled = chkEnableDNS.IsChecked == true;
        }

        private void BtnBackup_Click(object sender, RoutedEventArgs e)
        {
            SaveFileDialog saveFileDialog = new SaveFileDialog();
            saveFileDialog.Filter = "Zip Archives|*.zip";
            saveFileDialog.FileName = "PrivateWin10-Data.zip";
            if (saveFileDialog.ShowDialog() != true)
                return;

            try
            {
                ZipFile.CreateFromDirectory(App.dataPath, saveFileDialog.FileName);
            }
            catch (Exception err)
            {
                MessageBox.Show(Translate.fmt("msg_backup_error", err.Message), App.Title, MessageBoxButton.OK, MessageBoxImage.Stop);
                return;
            }
            MessageBox.Show(Translate.fmt("msg_backup_ok", saveFileDialog.FileName), App.Title, MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void BtnRestore_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Filter = "Zip Archives|*.zip";
            openFileDialog.FileName = "PrivateWin10-Data.zip";
            if (openFileDialog.ShowDialog() != true)
                return;

            try
            {
                bool bFoundIni = false;
                using (ZipArchive archive = ZipFile.OpenRead(openFileDialog.FileName))
                {
                    foreach (ZipArchiveEntry entry in archive.Entries)
                    {
                        if (entry.FullName.Equals(App.Key + ".ini"))
                        {
                            bFoundIni = true;
                            break;
                        }
                    }
                }

                if (!bFoundIni)
                    throw new Exception(Translate.fmt("msg_restore_no_ini"));
            }
            catch (Exception err)
            {
                MessageBox.Show(Translate.fmt("msg_restore_error", err.Message), App.Title, MessageBoxButton.OK, MessageBoxImage.Stop);
                return;
            }

            MessageBox.Show(Translate.fmt("msg_restore_info"), App.Title, MessageBoxButton.OK, MessageBoxImage.Information);

            string arguments = "-restore " + openFileDialog.FileName;
            ProcessStartInfo startInfo = new ProcessStartInfo(App.exePath, arguments);
            startInfo.UseShellExecute = true;
            startInfo.Verb = "runas";
            Process.Start(startInfo);
            Environment.Exit(0);
        }
    }
}
