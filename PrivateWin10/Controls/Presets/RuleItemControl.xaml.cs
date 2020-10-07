using MiscHelpers;
using PrivateWin10.Windows;
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
    /// Interaction logic for RuleItemControl.xaml
    /// </summary>
    public partial class RuleItemControl : UserControl, IControlItem<FirewallPreset.SingleRule>
    {
        public event RoutedEventHandler Click;

        public event EventHandler<EventArgs> RuleChanged;

        public FirewallPreset.SingleRule rule;
        FirewallRuleEx FwRule;

        public enum PresetMode
        { 
            DontChange = 0,
            AlwaysOn = 1,
            OnOff = 2,
            OffOn = 3,
            AlwaysOff = 4,
        }

        public static Brush GetPresetColor(PresetMode value, UserControl ctrl)
        {
            switch (value)
            {
                case PresetMode.AlwaysOn:   return new SolidColorBrush(Colors.LightGreen);
                case PresetMode.OnOff:      return ctrl.FindResource("OnOff") as DrawingBrush;
                case PresetMode.OffOn:      return ctrl.FindResource("OffOn") as DrawingBrush;
                case PresetMode.AlwaysOff:  return new SolidColorBrush(Colors.LightPink);
            }
            return null;
        }

        static PresetMode GetPresetMode(bool? OnState, bool? OffState)
        { 
            if (OnState == true && OffState == true)
                return PresetMode.AlwaysOn;
            if (OnState == true && OffState == false)
                return PresetMode.OnOff;
            if (OnState == false && OffState == true)
                return PresetMode.OffOn;
            if (OnState == false && OffState == false)
                return PresetMode.AlwaysOff;
            return PresetMode.DontChange;
        }

        static void SetPresetMode(PresetMode value, out bool? OnState, out bool? OffState)
        { 
            switch (value)
            {
                case PresetMode.AlwaysOn:   OnState = OffState = true; break;
                case PresetMode.OnOff:      OnState = true; OffState = false; break;
                case PresetMode.OffOn:      OnState = false; OffState = true; break;
                case PresetMode.AlwaysOff:  OnState = OffState = false; break;
                default:                    OnState = OffState = null; break;
            } 
        }

        public static void PrepPresetCmb(ComboBox cmbPreset, UserControl ctrl, int CustomMode)
        {
            cmbPreset.Items.Add(new ComboBoxItem() { Content = Translate.fmt("preset_keep"), Tag = PresetMode.DontChange });
            cmbPreset.Items.Add(new ComboBoxItem() { Content = Translate.fmt("preset_enable"), Tag = PresetMode.AlwaysOn });
            cmbPreset.Items.Add(new ComboBoxItem() { Content = Translate.fmt("preset_set_enable"), Tag = PresetMode.OnOff, IsEnabled = (CustomMode & 1) != 0});
            cmbPreset.Items.Add(new ComboBoxItem() { Content = Translate.fmt("preset_set_disable"), Tag = PresetMode.OffOn, IsEnabled = (CustomMode & 2) != 0});
            cmbPreset.Items.Add(new ComboBoxItem() { Content = Translate.fmt("preset_disable"), Tag = PresetMode.AlwaysOff });

            if (CustomMode == 0)
                cmbPreset.IsEnabled = false;
            else
            {
                foreach (ComboBoxItem item in cmbPreset.Items)
                    item.Background = GetPresetColor((PresetMode)item.Tag, ctrl);
            }
        }

        public RuleItemControl(FirewallPreset.SingleRule rule, FirewallRuleEx fwRule, FirewallPreset firewallPreset)
        {
            InitializeComponent();

            int CustomMode = 0;
            if (firewallPreset.OnState == ProgramSet.Config.AccessLevels.CustomConfig)
                CustomMode |= 1;
            if (firewallPreset.OffState == ProgramSet.Config.AccessLevels.CustomConfig)
                CustomMode |= 2;

            PrepPresetCmb(cmbPreset, this, CustomMode);

            FwRule = fwRule;
            DoUpdate(rule);

            rect.MouseDown += new MouseButtonEventHandler(rect_Click);
            label.MouseDown += new MouseButtonEventHandler(rect_Click);
            info.PreviewMouseDown += new MouseButtonEventHandler(rect_Click);

            //toggle.Click += new RoutedEventHandler(toggle_Click);
        }

        int SuspendChange = 0;

        public void DoUpdate(FirewallPreset.SingleRule rule)
        {
            this.rule = rule;

            SuspendChange++;

            PresetMode Value = GetPresetMode(rule.OnState, rule.OffState);
            WpfFunc.CmbSelect(cmbPreset, Value.ToString());
            cmbPreset.Background = GetPresetColor(Value, this);

            SuspendChange--;

            if (FwRule == null)
            {
                label.Content = rule.RuleId;
                return;
            }

            label.Content = App.GetResourceStr(FwRule.Name);
            info.Text = App.GetResourceStr(FwRule.GetDescription(true));
            info.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString(FwRule.Action == FirewallRule.Actions.Block ? "#ffe6e9" : "#e9fce9"));
        }

        /*private void toggle_Click(object sender, RoutedEventArgs e)
        {
            
        }*/

        private bool mHasFocus = false;

        public bool GetFocus() { return mHasFocus; }

        public void SetFocus(bool set = true)
        {
            this.rect.StrokeThickness = 2;
            this.rect.Stroke = set ? new SolidColorBrush(Color.FromArgb(255, 51, 153, 255)) : null;
            //this.rect.Fill = new SolidColorBrush(set ? Color.FromArgb(255, 153, 204, 255) : Colors.Transparent);
            //this.rect.Fill = new SolidColorBrush(set ? Color.FromArgb(255, 230, 240, 255) : Colors.Transparent);

            mHasFocus = set;
        }

        private void rect_Click(object sender, RoutedEventArgs e)
        {
            Click?.Invoke(this, e);
        }

        private void CmbPreset_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (SuspendChange != 0)
                return;

            PresetMode Value = (PresetMode)(cmbPreset.SelectedItem as ComboBoxItem).Tag;
            cmbPreset.Background = GetPresetColor(Value, this);

            SetPresetMode(Value, out rule.OnState, out rule.OffState);

            RuleChanged?.Invoke(this, new EventArgs());
        }

        private void Label_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            RuleWindow ruleWnd = new RuleWindow(null, FwRule);
            if (ruleWnd.ShowDialog() != true)
                return;

            if (!App.client.UpdateRule(FwRule))
            {
                MessageBox.Show(Translate.fmt("msg_rule_failed"), App.Title, MessageBoxButton.OK, MessageBoxImage.Exclamation);
                return;
            }
        }
    }
}
