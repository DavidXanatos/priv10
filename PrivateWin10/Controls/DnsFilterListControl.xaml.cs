using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
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
    /// Interaction logic for DnsFilterListControl.xaml
    /// </summary>
    public partial class DnsFilterListControl : UserControl//, INotifyPropertyChanged
    {
        /*public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }*/

        public string Caption
        {
            get { return GetValue(CaptionProperty).ToString(); }
            set { SetValue(CaptionProperty, value); }
        }

        public static readonly DependencyProperty CaptionProperty = DependencyProperty.Register("Caption", typeof(string), typeof(DnsFilterListControl), new PropertyMetadata("Filterlist", OnCaptionPropertyChanged));

        private static void OnCaptionPropertyChanged(DependencyObject dependencyObject,
                       DependencyPropertyChangedEventArgs e)
        {
            DnsFilterListControl This = dependencyObject as DnsFilterListControl;
            This.caption.Text = This.Caption;
            //This.OnPropertyChanged("Caption");
        }

        DnsBlockList.Lists ListType = DnsBlockList.Lists.Undefined;

        ObservableCollection<FilterListItem> FilterList;

        public DataGridExt filterGridExt;

        public DnsFilterListControl()
        {
            InitializeComponent();

            btnAdd.IsEnabled = btnAddEx.IsEnabled = false;
            btnRemove.IsEnabled = btnEnable.IsEnabled = btnDisable.IsEnabled = false;

            filterGridExt = new DataGridExt(filterGrid);

            FilterList = new ObservableCollection<FilterListItem>();
            filterGrid.ItemsSource = FilterList;
        }

        public void UpdateList(DnsBlockList.Lists ListType)
        {
            List<DomainFilter> Filters = App.client.GetDomainFilter(ListType);
            if (Filters == null)
                return;

            this.ListType = ListType;
            FilterList.Clear();
            foreach (var Filter in Filters)
                AddItem(Filter);
        }

        private void AddItem(DomainFilter Filter)
        {
            var Item = new FilterListItem(Filter);
            Item.PropertyChanged += Item_PropertyChanged;
            FilterList.Add(Item);
        }

        private void Item_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            App.client.UpdateDomainFilter(ListType, (sender as FilterListItem).Filter);
        }

        private void BtnAdd_Click(object sender, RoutedEventArgs e)
        {
            AddDomain(txtDomain.Text, false);
        }

        private void BtnAddEx_Click(object sender, RoutedEventArgs e)
        {
            AddDomain(txtDomain.Text, true);
        }

        private void AddDomain(string Domain, bool RegExp)
        {
            if (RegExp ? MiscFunc.IsValidRegex(Domain) : Uri.CheckHostName(Domain.Replace("*", "asterisk")) != UriHostNameType.Dns)
            {
                MessageBox.Show(Translate.fmt("msg_bad_dns_filter"), App.mName, MessageBoxButton.OK, MessageBoxImage.Stop);
                return;
            }

            // don't add duplicated
            foreach (var Item in FilterList)
            {
                if (Item.Filter.Domain.Equals(Domain))
                {
                    MessageBox.Show(Translate.fmt("msg_dns_filter_dup"), App.mName, MessageBoxButton.OK, MessageBoxImage.Exclamation);
                    return;
                }
            }

            DomainFilter Filter = new DomainFilter() { Domain = Domain };
            if (RegExp)
                Filter.Format = DomainFilter.Formats.RegExp;
            else if (Domain.Contains("*"))
                Filter.Format = DomainFilter.Formats.WildCard;
            
            AddItem(Filter);
            App.client.UpdateDomainFilter(ListType, Filter);

            txtDomain.Text = "";
        }

        private void BtnRemove_Click(object sender, RoutedEventArgs e)
        {
            if (MessageBox.Show(Translate.fmt("msg_remove_items"), App.mName, MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.No)
                return;

            foreach (FilterListItem Item in new List<FilterListItem>(filterGrid.SelectedItems.Cast<FilterListItem>())) // copy
            {
                FilterList.Remove(Item);

                App.client.RemoveDomainFilter(ListType, Item.Filter.Domain);
            }
            FilterGrid_SelectionChanged(null, null);
        }

        private void BtnEnable_Click(object sender, RoutedEventArgs e)
        {
            foreach (FilterListItem Item in filterGrid.SelectedItems)
                Item.Enabled = false;
            FilterGrid_SelectionChanged(null, null);
        }

        private void BtnDisable_Click(object sender, RoutedEventArgs e)
        {
            foreach (FilterListItem Item in filterGrid.SelectedItems)
                Item.Enabled = false;
            FilterGrid_SelectionChanged(null, null);
        }

        private void FilterGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            int Enabled = 0;
            int Disabled = 0;
            foreach (FilterListItem Item in filterGrid.SelectedItems)
            {
                if (Item.Enabled)
                    Enabled++;
                else
                    Disabled++;
            }
            btnRemove.IsEnabled = filterGrid.SelectedItems.Count > 0;
            btnEnable.IsEnabled = Disabled > 0;
            btnDisable.IsEnabled = Enabled > 0;
        }

        private void TxtDomain_TextChanged(object sender, TextChangedEventArgs e)
        {
            btnAdd.IsEnabled = btnAddEx.IsEnabled = txtDomain.Text.Length > 0;
        }


        /////////////////////////////////
        /// FilterListItem

        public class FilterListItem : INotifyPropertyChanged
        {
            public DomainFilter Filter;

            public FilterListItem(DomainFilter Filter)
            {
                this.Filter = Filter;
            }

            public bool Enabled { get { return Filter.Enabled; } set { Filter.Enabled = value; NotifyPropertyChanged("Enabled"); } }

            public string Domain { get { return Filter.Domain + (Filter.Format == DomainFilter.Formats.RegExp ? " (regex)" : ""); } }

            public int HitCount { get { return Filter.HitCount; } }

            public DateTime? LastHit { get { return Filter.LastHit; } }


            public event PropertyChangedEventHandler PropertyChanged;
            private void NotifyPropertyChanged(string propertyName)
            {
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
            }
        }

        private void FilterGrid_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (filterGrid.SelectedItems.Count == 1)
            {
                txtDomain.Text = (filterGrid.SelectedItems[0] as FilterListItem).Filter.Domain;
            }
        }
    }
}
