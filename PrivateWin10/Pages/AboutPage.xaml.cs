using QLicense;
using System;
using System.Collections.Generic;
using System.Diagnostics;
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

/*
    Mind the LICENSE and don't mess with this file!
*/

namespace PrivateWin10.Pages
{
    /// <summary>
    /// Interaction logic for AboutPage.xaml
    /// </summary>
    public partial class AboutPage : UserControl, IUserPage
    {
        public AboutPage()
        {
            InitializeComponent();
            lblTitle.Content = App.mName;
            lblVerNum.Content = App.mVersion;
            //lblHWID.Content = App.lic.GetUID();
            lblLicenseFor.Content = "Private non Commercial Use";
            if (App.lic.LicenseStatus == LicenseStatus.UNDEFINED)
            {
                lblUser.Content = TextHelpers.Split2(System.Security.Principal.WindowsIdentity.GetCurrent().Name, "\\").Item2;
                lblSupporting.Content = "why are you not supporting Private WinTen? 😢";
            }
            else if (App.lic.LicenseStatus == LicenseStatus.VALID)
            {
                if (App.lic.CommercialUse)
                {
                    lblLicenseFor.Content = "Commercial Use";
                    lblUser.Content = "Licensed To:";
                    lblSupporting.Content = App.lic.LicenseName;
                }
                else
                {
                    lblUser.Content = App.lic.LicenseName;
                    lblSupporting.Content = "is supporting Private WinTen, great! 😀";
                }
            }
            else
            {
                lblLicense.Content = "License INVALID:";
                if(App.lic.WasVoided())
                    lblLicenseFor.Content = "This licence has been Voided!";
                else if (App.lic.HasExpired())
                    lblLicenseFor.Content = "This licence has Expired!";
                else
                    lblLicenseFor.Content = "The license file is broken!";
                lblUser.Content = "Licensee Name:";
                lblSupporting.Content = App.lic.LicenseName;
            }
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

        private void lblLink_MouseDown(object sender, MouseButtonEventArgs e)
        {
            Process.Start(((Label)sender).Content.ToString());
        }
    }
}
