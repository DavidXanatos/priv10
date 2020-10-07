using MiscHelpers;
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
    /// Interaction logic for PresetRules.xaml
    /// </summary>
    public partial class PresetFirewall : UserControl
    {
        ControlList<RuleItemControl, FirewallPreset.SingleRule> RuleList;
        List<FirewallRuleEx> Rules;

        FirewallPreset FirewallPreset;

        int SuspendChange = 0;

        public PresetFirewall()
        {
            InitializeComponent();

            ProgramControl.PrepAccessCmb(cmbOnAccess);
            ProgramControl.PrepAccessCmb(cmbOffAccess);

            RuleList = new ControlList<RuleItemControl, FirewallPreset.SingleRule>(this.ruleScroll, (rule) => 
                {
                    FirewallRuleEx FwRule = Rules.Find(x => x.guid.Equals(rule.RuleId));
                    var ctrl = new RuleItemControl(rule, FwRule, FirewallPreset);
                    ctrl.RuleChanged += Ctrl_RuleChanged;
                    return ctrl;
                }, (rule) => rule.RuleId);
        }

        private void Ctrl_RuleChanged(object sender, EventArgs e)
        {
            RuleItemControl ctrl = (RuleItemControl)sender;
            FirewallPreset.Rules[ctrl.rule.RuleId] = ctrl.rule;
        }

        public void SetItem(FirewallPreset firewallPreset)
        {
            FirewallPreset = firewallPreset;

            SuspendChange++;

            presetName.Content = FirewallPreset.Name;

            WpfFunc.CmbSelect(cmbOnAccess, FirewallPreset.OnState.ToString());
            cmbOnAccess.Background = ProgramControl.GetAccessColor(FirewallPreset.OnState);

            WpfFunc.CmbSelect(cmbOffAccess, FirewallPreset.OffState.ToString());
            cmbOffAccess.Background = ProgramControl.GetAccessColor(FirewallPreset.OffState);

            SuspendChange--;

            LoadRules();
        }

        private void LoadRules()
        {
            var rules = App.client.GetRules(new List<Guid>() { FirewallPreset.ProgSetId });
            Rules = rules?.First().Value;

            RuleList.UpdateItems(null);
            RuleList.UpdateItems(FirewallPreset.Rules.Values.ToList());
        }

        private void CmbOnAccess_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (SuspendChange > 0)
                return;

            FirewallPreset.OnState =  (ProgramSet.Config.AccessLevels)(cmbOnAccess.SelectedItem as ComboBoxItem).Tag;
            cmbOnAccess.Background = ProgramControl.GetAccessColor(FirewallPreset.OnState);

            LoadRules();
        }

        private void CmbOffAccess_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (SuspendChange > 0)
                return;

            FirewallPreset.OffState =  (ProgramSet.Config.AccessLevels)(cmbOffAccess.SelectedItem as ComboBoxItem).Tag;
            cmbOffAccess.Background = ProgramControl.GetAccessColor(FirewallPreset.OffState);

            LoadRules();
        }
    }
}
