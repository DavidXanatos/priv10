using PrivateWin10.ViewModels;
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
using System.Windows.Shapes;

namespace PrivateWin10.Windows
{
    /// <summary>
    /// Interaction logic for RuleWindow.xaml
    /// </summary>
    public partial class RuleWindow : Window
    {
        protected FirewallRule Rule;

        public RuleWindow(List<ProgramList.ID> ids, FirewallRule rule)
        {
            InitializeComponent();

            Rule = rule;
            bool bNew = Rule.guid == Guid.Empty;

            txtName.Text = Rule.Name;
            cmbGroupe.ItemsSource = GroupeModel.GetInstance().Groupes;
            WpfFunc.CmbSelect(cmbGroupe, Rule.Grouping);
            if (cmbGroupe.SelectedItem == null)
                cmbGroupe.Text = Rule.Grouping;
            txtInfo.Text = Rule.Description;

            foreach (ProgramList.ID id in ids)
            {
                ContentControl program = new ContentControl() { Content = id.GetDisplayName(), Tag = id };
                cmbProgram.Items.Add(program);
                if (Rule.mID != null && id.CompareTo(Rule.mID) == 0)
                    cmbProgram.SelectedItem = program;
            }

            cmbAction.Items.Add(new ContentControl() { Content = Translate.fmt("str_allow"), Tag = Firewall.Actions.Allow });
            cmbAction.Items.Add(new ContentControl() { Content = Translate.fmt("str_block"), Tag = Firewall.Actions.Block });
            WpfFunc.CmbSelect(cmbAction, Rule.Action.ToString());

            if (Rule.Profile == (int)Firewall.Profiles.All)
            {
                radProfileAll.IsChecked = true;
                chkPrivate.IsChecked = true;
                chkDomain.IsChecked = true;
                chkPublic.IsChecked = true;
            }
            else
            {
                radProfileCustom.IsChecked = true;
                chkPrivate.IsChecked = ((Rule.Profile & (int)Firewall.Profiles.Private) != 0);
                chkDomain.IsChecked = ((Rule.Profile & (int)Firewall.Profiles.Domain) != 0);
                chkPublic.IsChecked = ((Rule.Profile & (int)Firewall.Profiles.Public) != 0);
            }

            if (Rule.Interface == (int)Firewall.Interfaces.All)
            {
                radNicAll.IsChecked = true;
                chkLAN.IsChecked = true;
                chkVPN.IsChecked = true;
                chkWiFi.IsChecked = true;
            }
            else
            {
                radNicCustom.IsChecked = true;
                chkLAN.IsChecked = ((Rule.Interface & (int)Firewall.Interfaces.Lan) != 0);
                chkVPN.IsChecked = ((Rule.Interface & (int)Firewall.Interfaces.RemoteAccess) != 0);
                chkWiFi.IsChecked = ((Rule.Interface & (int)Firewall.Interfaces.Wireless) != 0);
            }

            if(bNew)
                cmbDirection.Items.Add(new ContentControl() { Content = Translate.fmt("str_inandout"), Tag = Firewall.Directions.Bidirectiona });
            cmbDirection.Items.Add(new ContentControl() { Content = Translate.fmt("str_outbound"), Tag = Firewall.Directions.Outboun });
            cmbDirection.Items.Add(new ContentControl() { Content = Translate.fmt("str_inbound"), Tag = Firewall.Directions.Inbound });
            WpfFunc.CmbSelect(cmbDirection, Rule.Direction.ToString());

            cmbProtocol.Items.Add(new ContentControl() { Content = Translate.fmt("pro_any"), Tag = (int)NetFunc.KnownProtocols.Any });
            for (int i = (int)NetFunc.KnownProtocols.Min; i <= (int)NetFunc.KnownProtocols.Max; i++)
            {
                string name = NetFunc.Protocol2Str(i, null);
                if (name != null)
                    cmbProtocol.Items.Add(new ContentControl() { Content = i.ToString() + " - " + name, Tag = i });
            }
            if (!WpfFunc.CmbSelect(cmbProtocol, Rule.Protocol.ToString()))
                cmbProtocol.Text = Rule.Protocol.ToString();

            cmbDestPorts.Items.Add(new ContentControl() { Content = Translate.fmt("port_any"), Tag = "*" });
            cmbSrcPorts.Items.Add(new ContentControl() { Content = Translate.fmt("port_any"), Tag = "*" });
            foreach (string specialPort in FirewallRule.SpecialPorts) {
                cmbDestPorts.Items.Add(new ContentControl() { Content = specialPort, Tag = specialPort });
                cmbSrcPorts.Items.Add(new ContentControl() { Content = specialPort, Tag = specialPort });
            }
            if (!WpfFunc.CmbSelect(cmbDestPorts, Rule.RemotePorts) && Rule.RemotePorts != null)
                cmbDestPorts.Text = Rule.RemotePorts;
            if (!WpfFunc.CmbSelect(cmbSrcPorts, Rule.LocalPorts) && Rule.LocalPorts != null)
                cmbSrcPorts.Text = Rule.LocalPorts;

            addrDest.Address = Rule.RemoteAddresses;
            addrSrc.Address = Rule.LocalAddresses;
        }

        private void cmbProgram_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ContentControl program = (cmbProgram.SelectedItem as ContentControl);
            if (program == null)
                return;

            ProgramList.ID id = (program.Tag as ProgramList.ID);

            txtPath.Text = id.Path;
            switch (id.Type)
            {
                case ProgramList.Types.Service:
                    txtService.Text = MiscFunc.GetServiceName(id.Name) + " (" + id.Name + ")";
                    break;
                case ProgramList.Types.App:
                    txtApp.Text = AppManager.SidToPackageID(id.Name);
                    break;
            }
            txtService.IsEnabled = id.Type == ProgramList.Types.Service;
            txtApp.IsEnabled = id.Type == ProgramList.Types.App;
        }

        private void profile_Checked(object sender, RoutedEventArgs e)
        {
            chkPrivate.IsEnabled = radProfileCustom.IsChecked == true;
            chkDomain.IsEnabled = radProfileCustom.IsChecked == true;
            chkPublic.IsEnabled = radProfileCustom.IsChecked == true;
        }

        private void interface_Checked(object sender, RoutedEventArgs e)
        {
            chkLAN.IsEnabled = radNicCustom.IsChecked == true;
            chkVPN.IsEnabled = radNicCustom.IsChecked == true;
            chkWiFi.IsEnabled = radNicCustom.IsChecked == true;
        }

        int curProtocol = -1;

        private void cmbProtocol_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ContentControl protocol = (cmbProtocol.SelectedItem as ContentControl);
            int protType = (int)NetFunc.KnownProtocols.Any;
            if (protocol == null)
                protType = MiscFunc.parseInt(TextHelpers.Split2(cmbProtocol.Text, "-").Item1, (int)NetFunc.KnownProtocols.Any);
            else
                protType = (int)protocol.Tag;

            if (curProtocol == protType)
                return;
            curProtocol = protType;

            if (protType == (int)FirewallRule.KnownProtocols.TCP || protType == (int)FirewallRule.KnownProtocols.UDP)
                tabParams.SelectedItem = tabPorts;
            else if (protType == (int)FirewallRule.KnownProtocols.ICMP || protType == (int)FirewallRule.KnownProtocols.ICMPv6)
            {
                tabParams.SelectedItem = tabICMP;

                cmbICMP.Items.Clear();
                cmbICMP.Items.Add(new ContentControl() { Content = Translate.fmt("icmp_all"), Tag = "*" });
                bool v6 = (protType == (int)FirewallRule.KnownProtocols.ICMPv6);
                Dictionary<int, string> types = (v6 ? NetFunc.KnownIcmp6Types : NetFunc.KnownIcmp4Types);
                foreach (int type in types.Keys)
                    cmbICMP.Items.Add(new ContentControl() { Content = type.ToString() + ":* (" + types[type] + ")", Tag = type.ToString() + ":*" });
                cmbICMP.Items.Add(new ContentControl() { Content = "3:4 (Type 3, Code 4)", Tag = "3:4" }); // why does windows firewall has this explicitly
                if (!WpfFunc.CmbSelect(cmbICMP, Rule.IcmpTypesAndCodes) && Rule.IcmpTypesAndCodes != null)
                    cmbICMP.Text = Rule.IcmpTypesAndCodes;
            }
            else
                tabParams.SelectedItem = tabNone;
        }

        private void cmbProtocol_TextChanged(object sender, TextChangedEventArgs e)
        {
            cmbProtocol_SelectionChanged(null, null);
        }

        private void btnOK_Click(object sender, RoutedEventArgs e)
        {
            if (txtName.Text.Length == 0)
                return;

            if (cmbProgram.SelectedItem == null)
                return;

            if (cmbAction.SelectedItem == null)
                return;

            if (cmbDirection.SelectedItem == null)
                return;

            // todo add error messages

            Rule.Name = txtName.Text;
            Rule.Grouping = cmbGroupe.Text;
            Rule.Description = txtInfo.Text;

            Rule.mID = ((cmbProgram.SelectedItem as ContentControl).Tag as ProgramList.ID);

            Rule.Action = (Firewall.Actions)(cmbAction.SelectedItem as ContentControl).Tag;

            if (radProfileAll.IsChecked == true || (chkPrivate.IsChecked == true && chkDomain.IsChecked == true && chkPublic.IsChecked == true))
                Rule.Profile = (int)Firewall.Profiles.All;
            else
            {
                Rule.Profile = (int)Firewall.Profiles.Undefined;
                if (chkPrivate.IsChecked == true)
                    Rule.Profile |= (int)Firewall.Profiles.Private;
                if (chkDomain.IsChecked == true)
                    Rule.Profile |= (int)Firewall.Profiles.Domain;
                if (chkPublic.IsChecked == true)
                    Rule.Profile |= (int)Firewall.Profiles.Public;
            }

            if (radProfileAll.IsChecked == true || (chkPrivate.IsChecked == true && chkDomain.IsChecked == true && chkPublic.IsChecked == true))
                Rule.Profile = (int)Firewall.Profiles.All;
            else
            {
                Rule.Profile = (int)Firewall.Profiles.Undefined;
                if (chkPrivate.IsChecked == true)
                    Rule.Profile |= (int)Firewall.Profiles.Private;
                if (chkDomain.IsChecked == true)
                    Rule.Profile |= (int)Firewall.Profiles.Domain;
                if (chkPublic.IsChecked == true)
                    Rule.Profile |= (int)Firewall.Profiles.Public;
            }

            if (radProfileAll.IsChecked == true || (chkLAN.IsChecked == true && chkVPN.IsChecked == true && chkWiFi.IsChecked == true))
                Rule.Interface = (int)Firewall.Interfaces.All;
            else
            {
                Rule.Interface = (int)Firewall.Interfaces.None;
                if (chkLAN.IsChecked == true)
                    Rule.Interface |= (int)Firewall.Interfaces.Lan;
                if (chkVPN.IsChecked == true)
                    Rule.Interface |= (int)Firewall.Interfaces.RemoteAccess;
                if (chkWiFi.IsChecked == true)
                    Rule.Interface |= (int)Firewall.Interfaces.Wireless;
            }

            Rule.Direction = (Firewall.Directions)(cmbDirection.SelectedItem as ContentControl).Tag;

            if (cmbProtocol.SelectedItem == null)
                Rule.Protocol = MiscFunc.parseInt(cmbProtocol.Text);
            else
            {
                Rule.Protocol = (int)(cmbProtocol.SelectedItem as ContentControl).Tag;
                if (Rule.Protocol == (int)FirewallRule.KnownProtocols.TCP || Rule.Protocol == (int)FirewallRule.KnownProtocols.UDP)
                {
                    if (cmbDestPorts.SelectedItem != null)
                        Rule.RemotePorts = (string)((cmbDestPorts.SelectedItem as ContentControl).Tag);
                    else
                        Rule.RemotePorts = cmbDestPorts.Text;

                    if (cmbSrcPorts.SelectedItem != null)
                        Rule.LocalPorts = (string)((cmbSrcPorts.SelectedItem as ContentControl).Tag);
                    else
                        Rule.LocalPorts = cmbSrcPorts.Text;
                }
                else if (Rule.Protocol == (int)FirewallRule.KnownProtocols.ICMP || Rule.Protocol == (int)FirewallRule.KnownProtocols.ICMPv6)
                {
                    if (cmbICMP.SelectedItem != null)
                        Rule.IcmpTypesAndCodes = (string)((cmbICMP.SelectedItem as ContentControl).Tag);
                    else
                        Rule.IcmpTypesAndCodes = cmbICMP.Text;
                }
            }

            Rule.RemoteAddresses = addrDest.Address;
            Rule.LocalAddresses = addrSrc.Address;

            this.DialogResult = true;
        }

        private void chkProfile_Unchecked(object sender, RoutedEventArgs e)
        {
            // Note: at least one profile must be checked
            if (chkPrivate.IsChecked != true && chkDomain.IsChecked != true && chkPublic.IsChecked != true)
                (sender as CheckBox).IsChecked = true;
        }
    }
}