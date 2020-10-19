using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Net;
using System.Numerics;
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
using MiscHelpers;
using WinFirewallAPI;

namespace PrivateWin10.Controls
{
    /// <summary>
    /// Interaction logic for AddressControl.xaml
    /// </summary>
    public partial class AddressControl : UserControl
    {
        public string Address
        {
            get
            {
                if (radAny.IsChecked == true || lstAddr.Items.Count == 0)
                    return "*";
                string addresses = "";
                foreach (ListBoxItem item in lstAddr.Items)
                {
                    if (item.Tag == null)
                        continue;
                    if (addresses.Length > 0)
                        addresses += ",";
                    addresses += (string)item.Tag;
                }
                return addresses;
            }
            set
            {
                lstAddr.Items.Clear();
                if (FirewallRule.IsEmptyOrStar(value))
                {
                    radAny.IsChecked = true;
                    gridCustom.IsEnabled = false;
                }
                else
                {
                    radCustom.IsChecked = true;
                    string[] addresses = value.Split(',');
                    foreach (string address in addresses)
                        lstAddr.Items.Add(new ListBoxItem() { Content = address, Tag = address });
                }
                lstAddr.Items.Add(new ListBoxItem() { Content = Translate.fmt("addr_add"), Tag = null });
            }
        }


        public bool IsRemote
        {
            get { return Remote; }
            set
            {
                Remote = value;
                cmbEdit.Items.Clear();
                if (Remote)
                {
                    WpfFunc.CmbAdd(cmbEdit, FirewallRule.AddrKeywordLocalSubnet, FirewallRule.AddrKeywordLocalSubnet);
                    WpfFunc.CmbAdd(cmbEdit, FirewallRule.AddrKeywordDNS, FirewallRule.AddrKeywordDNS);
                    WpfFunc.CmbAdd(cmbEdit, FirewallRule.AddrKeywordDHCP, FirewallRule.AddrKeywordDHCP);
                    WpfFunc.CmbAdd(cmbEdit, FirewallRule.AddrKeywordWINS, FirewallRule.AddrKeywordWINS);
                    WpfFunc.CmbAdd(cmbEdit, FirewallRule.AddrKeywordDefaultGateway, FirewallRule.AddrKeywordDefaultGateway);

                    if (!UwpFunc.IsWindows7OrLower)
                    {
                        WpfFunc.CmbAdd(cmbEdit, FirewallRule.AddrKeywordIntrAnet, FirewallRule.AddrKeywordIntrAnet);
                        //WpfFunc.CmbAdd(cmbEdit, FirewallRule.AddrKeywordRmtIntrAnet, FirewallRule.AddrKeywordRmtIntrAnet);
                        WpfFunc.CmbAdd(cmbEdit, FirewallRule.AddrKeywordIntErnet, FirewallRule.AddrKeywordIntErnet);
                        //WpfFunc.CmbAdd(cmbEdit, FirewallRule.AddrKeywordPly2Renders, FirewallRule.AddrKeywordPly2Renders);
                    }
                }
            }
        }
        private bool Remote = false;

        private readonly AddressViewModel viewModel;

        public AddressControl()
        {
            InitializeComponent();

            radAny.Content = Translate.fmt("lbl_any_ip");
            radCustom.Content = Translate.fmt("lbl_selected_ip");
            btnOk.Content = Translate.fmt("lbl_ok");

            viewModel = new AddressViewModel();
            DataContext = viewModel;
        }

        private void radAny_Checked(object sender, RoutedEventArgs e)
        {
            gridCustom.IsEnabled = radAny.IsChecked == false;
            gridEdit.IsEnabled = radAny.IsChecked == false;
        }

        private void lstAddr_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            ListBoxItem item = (lstAddr.SelectedItem as ListBoxItem);
            if (item != null && item.Tag != null)
            {
                if (!WpfFunc.CmbSelect(cmbEdit, (string)item.Tag))
                    cmbEdit.Text = (string)item.Tag;
            }
            else
                cmbEdit.Text = "";

            gridEdit.Visibility = Visibility.Visible;
            //lstAddr.IsEnabled = false;
        }

        private void btnOk_Click(object sender, RoutedEventArgs e)
        {
            ListBoxItem item = (lstAddr.SelectedItem as ListBoxItem);
            bool bSelected = item != null && item.Tag != null;
            bool bRemove = cmbEdit.Text.Length == 0;
            if (!bRemove)
            {
                string reason = "";
                if (cmbEdit.SelectedItem == null && !AddressControl.ValidateAddress(cmbEdit.Text, ref reason))
                    return;
            }

            gridEdit.Visibility = Visibility.Collapsed;
            //lstAddr.IsEnabled = true;

            if (bSelected)
            {
                if (bRemove)
                    lstAddr.Items.Remove(item);
                else // update item
                {
                    item.Content = cmbEdit.Text;
                    item.Tag = cmbEdit.Text;
                }
            }
            else if (cmbEdit.Text.Length > 0)
                lstAddr.Items.Insert(lstAddr.Items.Count - 1, new ListBoxItem() { Content = cmbEdit.Text, Tag = cmbEdit.Text });
        }

        public static bool ValidateAddress(string Address, ref string reason)
        {
            string[] strTemp = Address.Split('-');
            if (strTemp.Length == 1)
            {
                int temp;
                BigInteger num;
                if (strTemp[0].Contains("/")) // ip/net
                {
                    string[] strTemp2 = strTemp[0].Split('/');
                    if (strTemp2.Length != 2)
                    {
                        reason = Translate.fmt("err_invalid_subnet");
                        return false;
                    }

                    num = NetFunc.IpStrToInt(strTemp2[0], out temp);
                    int pow = MiscFunc.parseInt(strTemp2[1]);
                    BigInteger num2 = num + BigInteger.Pow(new BigInteger(2), pow);

                    BigInteger numMax = NetFunc.MaxIPofType(temp);
                    if (num2 > numMax)
                    {
                        reason = Translate.fmt("err_invalid_subnet");
                        return false;
                    }
                }
                else
                    num = NetFunc.IpStrToInt(strTemp[0], out temp);   

                if (temp != 4 && temp != 6)
                {
                    reason = Translate.fmt("err_invalid_ip");
                    return false;
                }
            }
            else if (strTemp.Length == 2)
            {
                int tempL;
                BigInteger numL = NetFunc.IpStrToInt(strTemp[0], out tempL);
                int tempR;
                BigInteger numR = NetFunc.IpStrToInt(strTemp[1], out tempR);

                if ((tempL != 4 && tempL != 6) || tempL != tempR)
                {
                    reason = Translate.fmt("err_invalid_ip");
                    return false;
                }

                if (!(numL < numR))
                {
                    reason = Translate.fmt("err_invalid_range");
                    return false;
                }
            }
            else
            {
                reason = Translate.fmt("err_invalid_range");
                return false;
            }
            return true;
        }
    }

    public class AddressViewModel : WpfFunc.ViewModelHelper, IDataErrorInfo
    {
        public string this[string propName]
        {
            get
            {
                if (propName == "AddressTxt")
                {
                    string reason = "";
                    if (curAddress == null && !AddressControl.ValidateAddress(curAddressTxt, ref reason)) // we can only select valid items
                        return reason;
                }
                return null;
            }
        }

        public string Error { get { return string.Empty; } }


        ContentControl curAddress = null;
        public ContentControl Address
        {
            get { return curAddress; }
            set { SetPropertyCmb("Address", value, ref curAddress, ref curAddressTxt); }
        }

        string curAddressTxt = "";
        public string AddressTxt
        {
            get { return curAddressTxt; }
            set { SetProperty("AddressTxt", value, ref curAddressTxt); }
        }
    }
}
