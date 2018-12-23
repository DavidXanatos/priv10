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

            chkNoUAC.IsChecked = AdminFunc.IsSkipUac(App.mName);
            chkNotifyFW.IsChecked = App.GetConfigInt("Firewall", "NotifyBlocked", 1) != 0;
        }

        private void BtnOK_Click(object sender, RoutedEventArgs e)
        {
            bool Restart = false;

            if (chkAutoStart.IsChecked == true)
            {
                App.AutoStart(true);
                if (chkService.IsChecked == true)
                {
                    App.svc.Install();
                    Restart = true;
                }
            }
            if(chkNoUAC.IsChecked == true)
                AdminFunc.SkipUacEnable(App.mName, true);

            if (chkUseFW.IsChecked == true)
            {
                App.SetConfig("Firewall", "Enabled", 1);

                Firewall.FilteringModes Mode = Firewall.FilteringModes.WhiteList;
                App.SetConfig("Firewall", "Mode", Mode.ToString());
                App.itf.SetFilteringMode(Mode);

                Firewall.Auditing audit = Firewall.Auditing.All;
                App.SetConfig("Firewall", "AuditPol", audit.ToString());
                App.itf.SetAuditPol(audit);

                App.SetConfig("Firewall", "NotifyBlocked", chkNotifyFW.IsChecked == true ? 1 : 0);
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
