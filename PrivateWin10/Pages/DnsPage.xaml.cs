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

            try {
                tabs.SelectedIndex = App.GetConfigInt("GUI", "DnsPage", 0);
            } catch { }

            tabs.SelectionChanged += Tabs_SelectionChanged;
        }

        public void OnShow()
        {
            Tabs_SelectionChanged(null, null);
        }

        public void OnHide()
        {
        }

        public void OnClose()
        {
            App.SetConfig("GUI", "DnsPage", tabs.SelectedIndex);
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
