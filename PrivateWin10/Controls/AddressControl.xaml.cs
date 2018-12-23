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

namespace PrivateWin10.Controls
{
    /// <summary>
    /// Interaction logic for AddressControl.xaml
    /// </summary>
    public partial class AddressControl : UserControl
    {
        public string Address { get {
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
            } set {
                lstAddr.Items.Clear();
                if (value == null || value == "*")
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
            } }


        public bool IsRemote { get { return Remote; } set {
                Remote = value;
                cmbEdit.Items.Clear();
                if (Remote)
                {
                    foreach (string specialAddress in FirewallRule.SpecialAddresses)
                        cmbEdit.Items.Add(new ContentControl() { Content = specialAddress, Tag = specialAddress });
                }
            } }
        private bool Remote = false;

        public AddressControl()
        {
            InitializeComponent();
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
            gridEdit.Visibility = Visibility.Collapsed;
            //lstAddr.IsEnabled = true;

            ListBoxItem item = (lstAddr.SelectedItem as ListBoxItem);
            if (item != null && item.Tag != null)
            {
                if (cmbEdit.Text.Length > 0)
                {
                    item.Content = cmbEdit.Text;
                    item.Tag = cmbEdit.Text;
                }
                else
                    lstAddr.Items.Remove(item);
            }
            else if(cmbEdit.Text.Length > 0)
                lstAddr.Items.Insert(lstAddr.Items.Count-1, new ListBoxItem() { Content = cmbEdit.Text, Tag = cmbEdit.Text });
        }
    }
}
