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

            lblLicenseState.Content = "License Type: Freeware for private, non commercial use.";
            lblLicenseUser.Content = "";
            if (App.lic.LicenseStatus == LicenseStatus.UNDEFINED)
            {
                //lblUser.Content = TextHelpers.Split2(System.Security.Principal.WindowsIdentity.GetCurrent().Name, "\\").Item2;
                //lblSupporting.Content = "why are you not supporting Private WinTen? 😢";
            }
            else if (App.lic.LicenseStatus == LicenseStatus.VALID)
            {
                if (App.lic.CommercialUse)
                {
                    lblLicenseState.Content = "License Type: Business for commercial use.";
                    lblLicenseUser.Content = "Licensed To: " + App.lic.LicenseName;

                    //lblUser.Content = "Licensed To:";
                    //lblSupporting.Content = App.lic.LicenseName;
                }
                else
                {
                    lblLicenseState.Content = "License Type: Personal for private use.";
                    lblLicenseUser.Content = "Licensed To: " + App.lic.LicenseName;

                    //lblUser.Content = App.lic.LicenseName;
                    //lblSupporting.Content = "is supporting Private WinTen, great! 😀";
                }
            }
            else
            {
                if(App.lic.WasVoided())
                    lblLicenseState.Content = "License INVALID: This license has been Voided!";
                else if (App.lic.HasExpired())
                    lblLicenseState.Content = "License INVALID: This license has Expired!";
                else
                    lblLicenseState.Content = "License INVALID: The license file is broken!";
                lblLicenseUser.Content = "Licensee Name: " + App.lic.LicenseName;

                //lblUser.Content = "Licensee Name:";
                //lblSupporting.Content = App.lic.LicenseName;
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
