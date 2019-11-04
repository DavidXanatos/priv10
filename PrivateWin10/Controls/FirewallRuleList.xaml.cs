using System;
using System.Collections.Generic;
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
using PrivateWin10.Pages;
using PrivateWin10.Windows;

namespace PrivateWin10.Controls
{
    /// <summary>
    /// Interaction logic for FirewallRuleList.xaml
    /// </summary>
    public partial class FirewallRuleList : UserControl
    {
        DataGridExt rulesGridExt;

        bool mHideDisabled = false;
        string mRuleFilter = "";

        public FirewallPage firewallPage = null;

        public FirewallRuleList()
        {
            InitializeComponent();

            this.grpRules.Header = Translate.fmt("grp_firewall");
            this.grpRuleTools.Header = Translate.fmt("grp_tools");
            this.grpRuleView.Header = Translate.fmt("grp_view");

            this.btnCreateRule.Content = Translate.fmt("btn_mk_rule");
            this.btnEnableRule.Content = Translate.fmt("btn_enable_rule");
            this.btnDisableRule.Content = Translate.fmt("btn_disable_rule");
            this.btnRemoveRule.Content = Translate.fmt("btn_remove_rule");
            this.btnBlockRule.Content = Translate.fmt("btn_block_rule");
            this.btnAllowRule.Content = Translate.fmt("btn_allow_rule");
            this.btnEditRule.Content = Translate.fmt("btn_edit_rule");
            this.btnCloneRule.Content = Translate.fmt("btn_clone_rule");

            this.chkNoDisabled.Content = Translate.fmt("chk_hide_disabled");
            this.lblFilterRules.Content = Translate.fmt("lbl_filter_rules");


            this.rulesGrid.Columns[1].Header = Translate.fmt("lbl_name");
            this.rulesGrid.Columns[2].Header = Translate.fmt("lbl_group");
            this.rulesGrid.Columns[3].Header = Translate.fmt("lbl_index");
            this.rulesGrid.Columns[4].Header = Translate.fmt("lbl_enabled");
            this.rulesGrid.Columns[5].Header = Translate.fmt("lbl_profiles");
            this.rulesGrid.Columns[6].Header = Translate.fmt("lbl_action");
            this.rulesGrid.Columns[7].Header = Translate.fmt("lbl_direction");
            this.rulesGrid.Columns[8].Header = Translate.fmt("lbl_protocol");
            this.rulesGrid.Columns[9].Header = Translate.fmt("lbl_remote_ip");
            this.rulesGrid.Columns[10].Header = Translate.fmt("lbl_local_ip");
            this.rulesGrid.Columns[11].Header = Translate.fmt("lbl_remote_port");
            this.rulesGrid.Columns[12].Header = Translate.fmt("lbl_local_port");
            this.rulesGrid.Columns[13].Header = Translate.fmt("lbl_icmp");
            this.rulesGrid.Columns[14].Header = Translate.fmt("lbl_interfaces");
            this.rulesGrid.Columns[15].Header = Translate.fmt("lbl_edge");
            this.rulesGrid.Columns[16].Header = Translate.fmt("lbl_program");


            rulesGridExt = new DataGridExt(rulesGrid);
            rulesGridExt.Restore(App.GetConfig("GUI", "rulesGrid_Columns", ""));


            mRuleFilter = App.GetConfig("GUI", "RuleFilter", "");
            txtRuleFilter.Text = mRuleFilter;

            CheckRules();
        }

        public void OnClose()
        {
            App.SetConfig("GUI", "rulesGrid_Columns", rulesGridExt.Save());
        }

        public void UpdateRules(bool clear = false)
        {
            if (firewallPage == null)
                return;

            if (clear)
                rulesGrid.Items.Clear();

            Dictionary<FirewallRule, RuleItem> oldRules = new Dictionary<FirewallRule, RuleItem>();
            foreach (RuleItem oldItem in rulesGrid.Items)
                oldRules.Add(oldItem.Rule, oldItem);

            Dictionary<Guid, List<FirewallRuleEx>> rules = App.client.GetRules(firewallPage.GetCurGuids());
            foreach (var ruleSet in rules)
            {
                foreach (FirewallRuleEx rule in ruleSet.Value)
                {
                    if (mHideDisabled && rule.Enabled == false)
                        continue;

                    if (FirewallPage.DoFilter(mRuleFilter, rule.Name, new List<ProgramID>() { rule.ProgID })) // todo: move to helper class
                        continue;

                    if (!oldRules.Remove(rule))
                        rulesGrid.Items.Add(new RuleItem(rule));
                }
            }

            foreach (RuleItem item in oldRules.Values)
                rulesGrid.Items.Remove(item);

            // update existing cels
            rulesGrid.Items.Refresh();
        }

        private void btnCreateRule_Click(object sender, RoutedEventArgs e)
        {
            firewallPage.ShowRuleWindow(null);
        }

        private void btnEditRule_Click(object sender, RoutedEventArgs e)
        {
            RuleItem item = (rulesGrid.SelectedItem as RuleItem);
            if (item == null)
                return;

            firewallPage.ShowRuleWindow(item.Rule);
        }

        private void ruleGrid_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            btnEditRule_Click(null, null);
        }


        private void btnEnableRule_Click(object sender, RoutedEventArgs e)
        {
            foreach (RuleItem item in rulesGrid.SelectedItems)
            {
                item.Rule.Enabled = true;
                App.client.UpdateRule(item.Rule);
            }
        }

        private void btnDisableRule_Click(object sender, RoutedEventArgs e)
        {
            foreach (RuleItem item in rulesGrid.SelectedItems)
            {
                item.Rule.Enabled = false;
                App.client.UpdateRule(item.Rule);
            }
        }

        private void btnRemoveRule_Click(object sender, RoutedEventArgs e)
        {
            if (MessageBox.Show(Translate.fmt("msg_remove_rules"), App.mName, MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.No)
                return;

            foreach (RuleItem item in rulesGrid.SelectedItems)
                App.client.RemoveRule(item.Rule);
        }

        private void btnBlockRule_Click(object sender, RoutedEventArgs e)
        {
            foreach (RuleItem item in rulesGrid.SelectedItems)
            {
                item.Rule.Action = FirewallRule.Actions.Block;
                App.client.UpdateRule(item.Rule);
            }
        }

        private void btnAllowRule_Click(object sender, RoutedEventArgs e)
        {
            foreach (RuleItem item in rulesGrid.SelectedItems)
            {
                item.Rule.Action = FirewallRule.Actions.Allow;
                App.client.UpdateRule(item.Rule);
            }
        }

        private void btnCloneRule_Click(object sender, RoutedEventArgs e)
        {
            if (MessageBox.Show(Translate.fmt("msg_clone_rules"), App.mName, MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.No)
                return;

            foreach (RuleItem item in rulesGrid.SelectedItems)
            {
                FirewallRule rule = item.Rule.Duplicate();
                rule.Name += " - Duplicate"; // todo: translate
                App.client.UpdateRule(rule);
            }
        }


        private void chkNoDisabled_Click(object sender, RoutedEventArgs e)
        {
            mHideDisabled = chkNoDisabled.IsChecked == true;
            UpdateRules(true);
        }

        private void txtRuleFilter_TextChanged(object sender, TextChangedEventArgs e)
        {
            mRuleFilter = txtRuleFilter.Text;
            App.SetConfig("GUI", "RuleFilter", mRuleFilter);
            UpdateRules(true);
        }

        private void RuleGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            CheckRules();
        }

        private void CheckRules()
        {
            int SelectedCount = 0;
            int EnabledCount = 0;
            int DisabledCount = 0;
            int AllowingCount = 0;
            int BlockingCount = 0;

            foreach (RuleItem item in rulesGrid.SelectedItems)
            {
                SelectedCount++;
                if (item.Rule.Enabled)
                    EnabledCount++;
                else
                    DisabledCount++;
                if (item.Rule.Action == FirewallRule.Actions.Allow)
                    AllowingCount++;
                if (item.Rule.Action == FirewallRule.Actions.Block)
                    BlockingCount++;
            }

            btnEnableRule.IsEnabled = DisabledCount >= 1;
            btnDisableRule.IsEnabled = EnabledCount >= 1;
            btnRemoveRule.IsEnabled = SelectedCount >= 1;
            btnBlockRule.IsEnabled = AllowingCount >= 1;
            btnAllowRule.IsEnabled = BlockingCount >= 1;
            btnEditRule.IsEnabled = SelectedCount == 1;
            btnCloneRule.IsEnabled = SelectedCount >= 1;
        }



        /////////////////////////////////
        /// RuleItem


        public class RuleItem : INotifyPropertyChanged
        {
            public FirewallRuleEx Rule;

            public RuleItem(FirewallRuleEx rule)
            {
                Rule = rule;
            }

            void DoUpdate()
            {
                NotifyPropertyChanged(null);
            }

            public ImageSource Icon { get { return ImgFunc.GetIcon(Rule.ProgID.Path, 16); } }

            public string Name
            {
                get
                {
                    if (Rule.Name.Substring(0, 2) == "@{" && App.PkgMgr != null)
                        return App.PkgMgr.GetAppResourceStr(Rule.Name);
                    else if (Rule.Name.Substring(0, 1) == "@")
                        return MiscFunc.GetResourceStr(Rule.Name);
                    return Rule.Name;
                }
            }
            public string Program { get { return Rule.ProgID.FormatString(); } }

            public string Grouping
            {
                get
                {
                    if (Rule.Grouping != null && Rule.Grouping.Substring(0, 1) == "@")
                        return MiscFunc.GetResourceStr(Rule.Grouping);
                    return Rule.Grouping;
                }
            }

            public int Index { get { return Rule.Index; } }

            public string Enabled { get { return Translate.fmt(Rule.Enabled ? "str_enabled" : "str_disabled"); } }

            public string NameColor { get { return Rule.State != FirewallRuleEx.States.Approved ? "warn" : ""; } }

            public string DisabledColor { get { return Rule.Enabled ? "" : "gray"; } }

            public string Profiles
            {
                get
                {
                    if (Rule.Profile == (int)FirewallRule.Profiles.All)
                        return Translate.fmt("str_all");
                    else
                    {
                        List<string> profiles = new List<string>();
                        if ((Rule.Profile & (int)FirewallRule.Profiles.Private) != 0)
                            profiles.Add(Translate.fmt("str_private"));
                        if ((Rule.Profile & (int)FirewallRule.Profiles.Domain) != 0)
                            profiles.Add(Translate.fmt("str_domain"));
                        if ((Rule.Profile & (int)FirewallRule.Profiles.Public) != 0)
                            profiles.Add(Translate.fmt("str_public"));
                        return string.Join(",", profiles.ToArray().Reverse());
                    }
                }
            }
            public string Action
            {
                get
                {
                    switch (Rule.Action)
                    {
                        case FirewallRule.Actions.Allow: return Translate.fmt("str_allow");
                        case FirewallRule.Actions.Block: return Translate.fmt("str_block");
                        default: return Translate.fmt("str_undefined");
                    }
                }
            }

            public string ActionColor
            {
                get
                {
                    switch (Rule.Action)
                    {
                        case FirewallRule.Actions.Allow: return "green";
                        case FirewallRule.Actions.Block: return "red";
                        default: return "";
                    }
                }
            }

            public string Direction
            {
                get
                {
                    switch (Rule.Direction)
                    {
                        case FirewallRule.Directions.Inbound: return Translate.fmt("str_inbound");
                        case FirewallRule.Directions.Outboun: return Translate.fmt("str_outbound");
                        default: return Translate.fmt("str_undefined");
                    }
                }
            }
            public string Protocol { get { return (Rule.Protocol == (int)NetFunc.KnownProtocols.Any) ? Translate.fmt("pro_any") : NetFunc.Protocol2Str((UInt32)Rule.Protocol); } }
            public string DestAddress { get { return Rule.RemoteAddresses; } }
            public string DestPorts { get { return Rule.RemotePorts; } }
            public string SrcAddress { get { return Rule.LocalAddresses; } }
            public string SrcPorts { get { return Rule.LocalPorts; } }

            public string ICMPOptions { get { return Rule.GetIcmpTypesAndCodes(); } }

            public string Interfaces
            {
                get
                {
                    if (Rule.Interface == (int)FirewallRule.Interfaces.All)
                        return Translate.fmt("str_all");
                    else
                    {
                        List<string> interfaces = new List<string>();
                        if ((Rule.Profile & (int)FirewallRule.Interfaces.Lan) != 0)
                            interfaces.Add(Translate.fmt("str_lan"));
                        if ((Rule.Profile & (int)FirewallRule.Interfaces.RemoteAccess) != 0)
                            interfaces.Add(Translate.fmt("str_ras"));
                        if ((Rule.Profile & (int)FirewallRule.Interfaces.Wireless) != 0)
                            interfaces.Add(Translate.fmt("str_wifi"));
                        return string.Join(",", interfaces.ToArray().Reverse());
                    }
                }
            }

            public string EdgeTraversal { get { return Rule.EdgeTraversal.ToString(); } }

            #region INotifyPropertyChanged Members

            public event PropertyChangedEventHandler PropertyChanged;

            #endregion

            #region Private Helpers

            private void NotifyPropertyChanged(string propertyName)
            {
                if (PropertyChanged != null)
                {
                    PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
                }
            }

            #endregion
        }
    }
}
