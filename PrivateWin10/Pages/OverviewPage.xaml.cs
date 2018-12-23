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
    /// Interaction logic for OverviewPage.xaml
    /// </summary>
    public partial class OverviewPage : UserControl, IUserPage
    {
        public OverviewPage()
        {
            InitializeComponent();

            string running = Translate.fmt("lbl_run_as", Translate.fmt(AdminFunc.IsAdministrator() ? "str_admin" : "str_user"));
            if (App.svc.IsInstalled())
                running += Translate.fmt("lbl_run_svc");
            lblRunning.Content = running;

            lblFirewallInfo.Content = Translate.fmt((App.GetConfigInt("Firewall", "Enabled", 0) != 0) ? "str_enabled" : "str_disabled");
        }

        public void OnShow()
        {
        }

        public void OnHide()
        {
        }

        public void OnClose()
        {
        }

    }
}
