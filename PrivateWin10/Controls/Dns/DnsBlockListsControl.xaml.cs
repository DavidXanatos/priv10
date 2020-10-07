using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
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
using MiscHelpers;

namespace PrivateWin10.Controls
{
    /// <summary>
    /// Interaction logic for DnsBlockListsControl.xaml
    /// </summary>
    public partial class DnsBlockListsControl : UserControl
    {
        ObservableCollection<BlocklistItem> BlocklistList;

        public DataGridExt listGridExt;

        public DnsBlockListsControl()
        {
            InitializeComponent();

            this.caption.Text = Translate.fmt("btn_blocklists");
            this.lblHint.Text = Translate.fmt("btn_blocklist_hint");
            this.btnAdd.Content = Translate.fmt("btn_add_blocklist");


            this.listGrid.Columns[0].Header = Translate.fmt("str_list");
            this.listGrid.Columns[1].Header = Translate.fmt("lbl_last_update");
            this.listGrid.Columns[2].Header = Translate.fmt("lbl_entry_count");
            this.listGrid.Columns[3].Header = Translate.fmt("lbl_status");

            this.btnDefault.Content = Translate.fmt("lbl_defaults");
            this.btnRemove.Content = Translate.fmt("lbl_remove");
            this.btnUpdate.Content = Translate.fmt("lbl_update");

            btnAdd.IsEnabled = false;
            btnRemove.IsEnabled = btnUpdate.IsEnabled = false;

            listGridExt = new DataGridExt(listGrid);

            BlocklistList = new ObservableCollection<BlocklistItem>();
            listGrid.ItemsSource = BlocklistList;
        }

        public void UpdateList()
        {
            List<DomainBlocklist> Blocklists = App.client.GetDomainBlocklists();
            if (Blocklists == null)
                return;

            BlocklistList.Clear();
            foreach (var Blocklist in Blocklists)
                AddItem(Blocklist);
        }

        private void AddItem(DomainBlocklist Blocklist)
        {
            var Item = new BlocklistItem(Blocklist);
            Item.PropertyChanged += Item_PropertyChanged;
            BlocklistList.Add(Item);
        }

        /*private void BtnUpdateAll_Click(object sender, RoutedEventArgs e)
        {

        }*/

        private void BtnAdd_Click(object sender, RoutedEventArgs e)
        {
            string Url = txtListUrl.Text;

            // don't add duplicated
            foreach (var Item in BlocklistList)
            {
                if (Item.Blocklist.Url.Equals(Url))
                {
                    MessageBox.Show(Translate.fmt("msg_dns_filter_dup"), App.Title, MessageBoxButton.OK, MessageBoxImage.Exclamation);
                    return;
                }
            }

            DomainBlocklist Blocklist = new DomainBlocklist() { Url = Url };

            AddItem(Blocklist);

            App.client.UpdateDomainBlocklist(Blocklist);

            txtListUrl.Text = "";
        }

        private void Item_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            App.client.UpdateDomainBlocklist((sender as BlocklistItem).Blocklist);
        }

        private void BtnRemove_Click(object sender, RoutedEventArgs e)
        {
            if (MessageBox.Show(Translate.fmt("msg_remove_items"), App.Title, MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.No)
                return;

            foreach (BlocklistItem Item in new List<BlocklistItem>(listGrid.SelectedItems.Cast<BlocklistItem>())) // copy
            {
                BlocklistList.Remove(Item);
                App.client.RemoveDomainBlocklist(Item.Blocklist.Url);
            }
            ListGrid_SelectionChanged(null, null);
        }

        private void BtnUpdate_Click(object sender, RoutedEventArgs e)
        {
            foreach (BlocklistItem Item in new List<BlocklistItem>(listGrid.SelectedItems.Cast<BlocklistItem>()))
            {
                App.client.RefreshDomainBlocklist(Item.Blocklist.Url);
            }
        }

        private void TxtListUrl_TextChanged(object sender, TextChangedEventArgs e)
        {
            btnAdd.IsEnabled = txtListUrl.Text.Length > 0;
        }

        private void ListGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            btnRemove.IsEnabled = btnUpdate.IsEnabled = listGrid.SelectedItems.Count > 0;
        }

        private void BtnDefault_Click(object sender, RoutedEventArgs e)
        {
            if (MessageBox.Show(Translate.fmt("msg_restore_std"), App.Title, MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.No)
                return;

            foreach (var Item in BlocklistList)
                App.client.RemoveDomainBlocklist(Item.Blocklist.Url);
            BlocklistList.Clear();

            foreach (var Url in DnsBlockList.DefaultLists)
            {
                DomainBlocklist blocklist = new DomainBlocklist() { Url = Url };
                AddItem(blocklist);
                App.client.UpdateDomainBlocklist(blocklist);
            }
        }

        /////////////////////////////////
        /// FilterListItem

        public class BlocklistItem : INotifyPropertyChanged
        {
            public DomainBlocklist Blocklist;

            public BlocklistItem(DomainBlocklist Blocklist)
            {
                this.Blocklist = Blocklist;
            }

            public bool Enabled { get { return Blocklist.Enabled; } set { Blocklist.Enabled = value; NotifyPropertyChanged("Enabled"); } }

            public string Url { get { return Blocklist.Url; } }

            public DateTime? LastUpdate { get { return Blocklist.LastUpdate; } }

            public int EntryCount { get { return Blocklist.EntryCount; } }

            public string Status { get { return Blocklist.Status; } }


            public event PropertyChangedEventHandler PropertyChanged;
            private void NotifyPropertyChanged(string propertyName)
            {
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
            }
        }
    }
}
