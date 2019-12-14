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

namespace PrivateWin10.Pages
{
    /// <summary>
    /// Interaction logic for DnsPage.xaml
    /// </summary>
    public partial class DnsPage : UserControl, IUserPage
    {
        private Object curTab = null;

        public DnsPage()
        {
            InitializeComponent();

            this.tabQueryLog.Header = Translate.fmt("btn_query_log");
            this.tabWhitelist.Header = Translate.fmt("btn_whitelist");
            this.tabBlacklist.Header = Translate.fmt("btn_blacklist");
            this.tabBlocklists.Header = Translate.fmt("btn_blocklists");

            this.whiteList.caption.Text = Translate.fmt("btn_whitelist");
            this.blackList.caption.Text = Translate.fmt("btn_blacklist");

            tabs.Loaded += (sender, e) => {

                var lblUpstreamDns = tabs.Template.FindName("lblUpstreamDns", tabs) as Label;
                if (lblUpstreamDns != null)
                    lblUpstreamDns.Content = Translate.fmt("lbl_dns_upstream");

                var lblNavMenu = tabs.Template.FindName("lblNavMenu", tabs) as Label;
                if (lblNavMenu != null)
                    lblNavMenu.Content = Translate.fmt("lbl_nav_menu");

                UpdateStats();
            };

            blockLists.listGridExt.Restore(App.GetConfig("GUI", "blockListsGrid_Columns", ""));
            whiteList.filterGridExt.Restore(App.GetConfig("GUI", "whiteListsGrid_Columns", ""));
            blackList.filterGridExt.Restore(App.GetConfig("GUI", "blackListsGrid_Columns", ""));

            try {
                tabs.SelectedIndex = App.GetConfigInt("GUI", "DnsPage", 0);
            } catch { }

            tabs.SelectionChanged += Tabs_SelectionChanged;
        }

        public void OnShow()
        {
            UpdateStats();

            Tabs_SelectionChanged(null, null);
        }

        public void OnHide()
        {
        }

        public void OnClose()
        {
            App.SetConfig("GUI", "DnsPage", tabs.SelectedIndex);

            App.SetConfig("GUI", "blockListsGrid_Columns", blockLists.listGridExt.Save());
            App.SetConfig("GUI", "whiteListsGrid_Columns", whiteList.filterGridExt.Save());
            App.SetConfig("GUI", "blackListsGrid_Columns", blackList.filterGridExt.Save());
        }

        public void UpdateStats()
        {
            var txtUpstreamDns = tabs.Template.FindName("txtUpstreamDns", tabs) as TextBlock;
            if (txtUpstreamDns != null)
                txtUpstreamDns.Text = App.GetConfig("DNSProxy", "UpstreamDNS", "");
        }

        //
        private void Tabs_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (curTab == tabs.SelectedItem)
                return;
            curTab = tabs.SelectedItem;

            if (curTab == tabQueryLog)
                queryLog.UpdateList();
            else if (curTab == tabWhitelist)
                whiteList.UpdateList(DnsBlockList.Lists.Whitelist);
            else if(curTab == tabBlacklist)
                blackList.UpdateList(DnsBlockList.Lists.Blacklist);
            else if (curTab == tabBlocklists)
                blockLists.UpdateList();
        }
    }
}
