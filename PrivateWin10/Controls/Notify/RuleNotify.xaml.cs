using PrivateWin10.Windows;
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
using MiscHelpers;

namespace PrivateWin10.Controls
{
    /// <summary>
    /// Interaction logic for RuleNotify.xaml
    /// </summary>
    public partial class RuleNotify : UserControl, INotificationTab
    {
        DataGridExt rulesGridExt;

        TextBlock Ignore;
        TextBlock Approve;
        TextBlock Reject;

        public event EventHandler<EventArgs> Emptied;

        public RuleNotify()
        {
            InitializeComponent();

            rulesGridExt = new DataGridExt(rulesGrid);
            rulesGridExt.Restore(App.GetConfig("GUI", "rulesGrid_Columns", ""));

            //this.lblGuide.Text = Translate.fmt("lbl_rule_guide");

            this.lblDetails.Header = Translate.fmt("lbl_rule_details");
            this.lblProt.Text = Translate.fmt("lbl_protocol_");
            this.lblLocal.Text = Translate.fmt("lbl_local");
            this.lblRemote.Text = Translate.fmt("lbl_remote");
            this.lblProg.Text = Translate.fmt("lbl_program_");

            this.rulesGrid.Columns[1].Header = Translate.fmt("lbl_name");
            this.rulesGrid.Columns[2].Header = Translate.fmt("lbl_enabled");
            this.rulesGrid.Columns[3].Header = Translate.fmt("lbl_action");
            this.rulesGrid.Columns[4].Header = Translate.fmt("lbl_direction");
            this.rulesGrid.Columns[5].Header = Translate.fmt("lbl_program");

            Ignore = this.IgnoreSB.Content as TextBlock;
            Approve = this.ApproveSB.Content as TextBlock;
            Reject = this.RejectSB.Content as TextBlock;

            this.Ignore.Text = Translate.fmt("lbl_ignore");
            (this.IgnoreSB.MenuItemsSource[0] as MenuItem).Header = Translate.fmt("lbl_ignore_all");
            this.Approve.Text = Translate.fmt("lbl_approve");
            (this.ApproveSB.MenuItemsSource[0] as MenuItem).Header = Translate.fmt("lbl_approve_all");
            this.Reject.Text = Translate.fmt("lbl_reject");
            (this.RejectSB.MenuItemsSource[0] as MenuItem).Header = Translate.fmt("lbl_reject_all");

            UpdateState();
        }

        public void Closing()
        {
            App.SetConfig("GUI", "rulesGrid_Columns", rulesGridExt.Save());
        }

        public void RemoveCurrent()
        {
            var item = rulesGrid.SelectedItem as FirewallRuleList.RuleItem;
            rulesGrid.SelectedIndex++;
            if (item != null)
                rulesGrid.Items.Remove(item);
            UpdateState();
        }

        public void UpdateState()
        {
            this.IgnoreSB.IsEnabled = rulesGrid.Items.IsEmpty ? false : true;
            this.ApproveSB.IsEnabled = rulesGrid.Items.IsEmpty ? false : true;
            this.RejectSB.IsEnabled = rulesGrid.Items.IsEmpty ? false : true;

            if (rulesGrid.Items.IsEmpty)
                Emptied?.Invoke(this, new EventArgs());
            else if (rulesGrid.SelectedItem == null)
                rulesGrid.SelectedItem = rulesGrid.Items[0];
        }

        public bool Add(Priv10Engine.ChangeArgs args)
        {
            foreach (FirewallRuleList.RuleItem item in rulesGrid.Items)
            {
                if (item.Rule.guid == args.rule.guid)
                {
                    item.Update(args.rule);
                    return false;
                }
            }

            rulesGrid.Items.Add(new FirewallRuleList.RuleItem(args.rule, args.prog));
            UpdateState();
            return true;
        }

        public bool IsEmpty()
        {
            return rulesGrid.Items.IsEmpty;
        }

        private void RulesGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var item = rulesGrid.SelectedItem as FirewallRuleList.RuleItem;
            if (item == null)
                return;

            this.txtProt.Text = item.Protocol;
            if (item.Rule.Protocol == (int)FirewallRule.KnownProtocols.ICMP || item.Rule.Protocol == (int)FirewallRule.KnownProtocols.ICMPv6)
                this.txtProt.Text += ";" + item.ICMPOptions;
            this.txtLocal.Text = item.SrcAddress + ":" + item.SrcPorts;
            this.txtRemote.Text = item.DestPorts + ":" + item.DestPorts;
            this.txtProg.Text = item.Program;
        }

        private void RulesGrid_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            var item = rulesGrid.SelectedItem as FirewallRuleList.RuleItem;
            if (item == null)
                return;

            RuleWindow ruleWnd = new RuleWindow(new List<Program>() { item.Prog }, item.Rule);
            if (ruleWnd.ShowDialog() != true)
                return;

            if (!App.client.UpdateRule(item.Rule))
            {
                MessageBox.Show(Translate.fmt("msg_rule_failed"), App.Title, MessageBoxButton.OK, MessageBoxImage.Exclamation);
                return;
            }

            RemoveCurrent();
        }

        private void BtnIgnoreAll_Click(object sender, RoutedEventArgs e)
        {
            rulesGrid.Items.Clear();
            UpdateState();
        }

        private void BtnIgnore_Click(object sender, MouseButtonEventArgs e)
        {
            var item = rulesGrid.SelectedItem as FirewallRuleList.RuleItem;
            if (item == null)
                return;

            RemoveCurrent();
        }

        private void BtnApproveAll_Click(object sender, RoutedEventArgs e)
        {
            foreach (FirewallRuleList.RuleItem item in rulesGrid.Items)
                App.client.SetRuleApproval(Priv10Engine.ApprovalMode.ApproveChanges, item.Rule);

            rulesGrid.Items.Clear();
            UpdateState();
        }

        private void BtnApprove_Click(object sender, MouseButtonEventArgs e)
        {
            var item = rulesGrid.SelectedItem as FirewallRuleList.RuleItem;
            if (item != null)
                App.client.SetRuleApproval(Priv10Engine.ApprovalMode.ApproveChanges, item.Rule);

            RemoveCurrent();
        }

        private void BtnRejectAll_Click(object sender, RoutedEventArgs e)
        {
            foreach (FirewallRuleList.RuleItem item in rulesGrid.Items)
                App.client.SetRuleApproval(Priv10Engine.ApprovalMode.RestoreRules, item.Rule);

            rulesGrid.Items.Clear();
            UpdateState();
        }

        private void BtnReject_Click(object sender, MouseButtonEventArgs e)
        {
            var item = rulesGrid.SelectedItem as FirewallRuleList.RuleItem;
            if (item != null)
                App.client.SetRuleApproval(Priv10Engine.ApprovalMode.RestoreRules, item.Rule);

            RemoveCurrent();
        }
    }
}
