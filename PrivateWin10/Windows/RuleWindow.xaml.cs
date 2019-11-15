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
using System.Windows.Shapes;

namespace PrivateWin10.Windows
{
    /// <summary>
    /// Interaction logic for RuleWindow.xaml
    /// </summary>
    public partial class RuleWindow : Window
    {
        protected FirewallRule Rule;

        private readonly RuleWindowViewModel viewModel;

        public RuleWindow(List<Program> progs, FirewallRule rule)
        {
            InitializeComponent();

            this.Title = Translate.fmt("wnd_rule");

            this.grpRule.Header = Translate.fmt("lbl_rule");
            this.lblName.Text = Translate.fmt("lbl_name");
            this.lblGroup.Text = Translate.fmt("lbl_group");

            this.grpProgram.Header = Translate.fmt("lbl_program");
            this.lblProgram.Text = Translate.fmt("lbl_program");
            this.lblExecutable.Text = Translate.fmt("lbl_exe");
            this.lblService.Text = Translate.fmt("lbl_svc");
            this.lblApp.Text = Translate.fmt("lbl_app");

            this.grpAction.Header = Translate.fmt("grp_action");
            this.lblAction.Text = Translate.fmt("lbl_action");

            this.radProfileAll.Content = Translate.fmt("lbl_prof_all");
            this.radProfileCustom.Content = Translate.fmt("lbl_prof_sel");
            this.chkPublic.Content = Translate.fmt("lbl_prof_pub");
            this.chkDomain.Content = Translate.fmt("lbl_prof_dmn");
            this.chkPrivate.Content = Translate.fmt("lbl_prof_priv");

            this.radNicAll.Content = Translate.fmt("lbl_itf_all");
            this.radNicCustom.Content = Translate.fmt("lbl_itf_select");
            this.chkLAN.Content = Translate.fmt("lbl_itf_lan");
            this.chkVPN.Content = Translate.fmt("lbl_itf_vpn");
            this.chkWiFi.Content = Translate.fmt("lbl_itf_wifi");

            this.grpNetwork.Header = Translate.fmt("grp_network");
            this.lblDirection.Text = Translate.fmt("lbl_direction");
            this.lblProtocol.Text = Translate.fmt("lbl_protocol");

            this.lblLocalPorts.Text = Translate.fmt("lbl_local_port");
            this.lblRemotePorts.Text = Translate.fmt("lbl_remote_port");

            this.lblICMP.Text = Translate.fmt("lbl_icmp");

            this.lblLocalIP.Text = Translate.fmt("lbl_local_ip");
            this.lblRemoteIP.Text = Translate.fmt("lbl_remote_ip");

            this.btnOK.Content = Translate.fmt("lbl_ok");
            this.btnCancel.Content = Translate.fmt("lbl_cancel");

            Rule = rule;
            bool bNew = Rule.guid == null;

            viewModel = new RuleWindowViewModel();
            DataContext = viewModel;

            //txtName.Text = Rule.Name;
            viewModel.RuleName = Rule.Name;
            cmbGroup.ItemsSource = GroupModel.GetInstance().GetGroups();
            WpfFunc.CmbSelect(cmbGroup, Rule.Grouping);
            if (cmbGroup.SelectedItem == null)
                cmbGroup.Text = Rule.Grouping;
            txtInfo.Text = Rule.Description;

            foreach (Program prog in progs)
            {
                ContentControl program = new ContentControl() { Content = prog.Description, Tag = prog.ID };
                cmbProgram.Items.Add(program);
                if (Rule.ProgID != null && prog.ID.CompareTo(Rule.ProgID) == 0)
                    cmbProgram.SelectedItem = program;
            }

            cmbAction.Items.Add(new ContentControl() { Content = Translate.fmt("str_allow"), Tag = FirewallRule.Actions.Allow });
            cmbAction.Items.Add(new ContentControl() { Content = Translate.fmt("str_block"), Tag = FirewallRule.Actions.Block });
            //WpfFunc.CmbSelect(cmbAction, Rule.Action.ToString());
            viewModel.RuleAction = WpfFunc.CmbPick(cmbAction, Rule.Action.ToString());

            if (Rule.Profile == (int)FirewallRule.Profiles.All)
            {
                radProfileAll.IsChecked = true;
                chkPrivate.IsChecked = true;
                chkDomain.IsChecked = true;
                chkPublic.IsChecked = true;
            }
            else
            {
                radProfileCustom.IsChecked = true;
                chkPrivate.IsChecked = ((Rule.Profile & (int)FirewallRule.Profiles.Private) != 0);
                chkDomain.IsChecked = ((Rule.Profile & (int)FirewallRule.Profiles.Domain) != 0);
                chkPublic.IsChecked = ((Rule.Profile & (int)FirewallRule.Profiles.Public) != 0);
            }

            if (Rule.Interface == (int)FirewallRule.Interfaces.All)
            {
                radNicAll.IsChecked = true;
                chkLAN.IsChecked = true;
                chkVPN.IsChecked = true;
                chkWiFi.IsChecked = true;
            }
            else
            {
                radNicCustom.IsChecked = true;
                chkLAN.IsChecked = ((Rule.Interface & (int)FirewallRule.Interfaces.Lan) != 0);
                chkVPN.IsChecked = ((Rule.Interface & (int)FirewallRule.Interfaces.RemoteAccess) != 0);
                chkWiFi.IsChecked = ((Rule.Interface & (int)FirewallRule.Interfaces.Wireless) != 0);
            }

            if (bNew)
                cmbDirection.Items.Add(new ContentControl() { Content = Translate.fmt("str_inandout"), Tag = FirewallRule.Directions.Bidirectiona });
            cmbDirection.Items.Add(new ContentControl() { Content = Translate.fmt("str_outbound"), Tag = FirewallRule.Directions.Outboun });
            cmbDirection.Items.Add(new ContentControl() { Content = Translate.fmt("str_inbound"), Tag = FirewallRule.Directions.Inbound });
            WpfFunc.CmbSelect(cmbDirection, Rule.Direction.ToString());

            cmbProtocol.Items.Add(new ContentControl() { Content = Translate.fmt("pro_any"), Tag = (int)NetFunc.KnownProtocols.Any });
            for (int i = (int)NetFunc.KnownProtocols.Min; i <= (int)NetFunc.KnownProtocols.Max; i++)
            {
                string name = NetFunc.Protocol2Str((UInt32)i);
                if (name != null)
                    cmbProtocol.Items.Add(new ContentControl() { Content = i.ToString() + " - " + name, Tag = i });
            }
            //if (!WpfFunc.CmbSelect(cmbProtocol, Rule.Protocol.ToString()))
            //    cmbProtocol.Text = Rule.Protocol.ToString();
            viewModel.Protocol = WpfFunc.CmbPick(cmbProtocol, Rule.Protocol.ToString());
            if(viewModel.Protocol == null)
                viewModel.ProtocolTxt = Rule.Protocol.ToString();

            UpdatePorts();

            addrDest.Address = Rule.RemoteAddresses;
            addrSrc.Address = Rule.LocalAddresses;

            WpfFunc.LoadWnd(this, "Rule");
        }

        private void Window_Closing(object sender, CancelEventArgs e)
        {
            WpfFunc.StoreWnd(this, "Rule");
        }

        private void cmbProgram_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ContentControl program = (cmbProgram.SelectedItem as ContentControl);
            if (program == null)
                return;

            ProgramID id = (program.Tag as ProgramID);

            txtPath.Text = id.Path;
            switch (id.Type)
            {
                case ProgramID.Types.Service:
                    txtService.Text = id.GetServiceName() + " (" + id.GetServiceId() + ")";
                    break;
                case ProgramID.Types.App:
                    txtApp.Text = id.GetPackageName();
                    break;
            }
            txtService.IsEnabled = id.Type == ProgramID.Types.Service;
            txtApp.IsEnabled = id.Type == ProgramID.Types.App;
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

        int? curProtocol = null;

        private static int? ParseProtocol(string Value)
        {
            Value = TextHelpers.Split2(Value, "-").Item1;
            int prot;
            if (int.TryParse(Value, out prot) && prot >= 0 && prot <= 255)
                return prot;
            return null;
        }


        private void cmbProtocol_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ContentControl protocol = (cmbProtocol.SelectedItem as ContentControl);
            int? protType;
            if (protocol == null)
                protType = ParseProtocol(cmbProtocol.Text);
            else
                protType = (int)protocol.Tag;

            if (curProtocol == protType)
                return;
            curProtocol = protType;
            if (curProtocol == null)
                return;

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
                if (!WpfFunc.CmbSelect(cmbICMP, Rule.GetIcmpTypesAndCodes()) && Rule.IcmpTypesAndCodes != null)
                    cmbICMP.Text = Rule.GetIcmpTypesAndCodes();
            }
            else
                tabParams.SelectedItem = tabNone;

            UpdatePorts();
            someThing_Changed(sender, e);
        }

        private void cmbProtocol_TextChanged(object sender, TextChangedEventArgs e)
        {
            cmbProtocol_SelectionChanged(null, null);
        }

        private void CmbDirection_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            UpdatePorts();
            someThing_Changed(sender, e);
        }

        private void UpdatePorts()
        {
            cmbRemotePorts.Items.Clear();
            cmbLocalPorts.Items.Clear();

            if (curProtocol == (int)FirewallRule.KnownProtocols.TCP || curProtocol == (int)FirewallRule.KnownProtocols.UDP)
            {
                cmbRemotePorts.Items.Add(new ContentControl() { Content = Translate.fmt("port_any"), Tag = "*" });
                cmbLocalPorts.Items.Add(new ContentControl() { Content = Translate.fmt("port_any"), Tag = "*" });

                if (Rule.Direction == FirewallRule.Directions.Outboun)
                {
                    if (curProtocol == (int)FirewallRule.KnownProtocols.TCP)
                    {
                        cmbRemotePorts.Items.Add(new ContentControl() { Content = FirewallRule.PortKeywordIpTlsOut, Tag = FirewallRule.PortKeywordIpTlsOut });
                    }
                }
                else if (Rule.Direction == FirewallRule.Directions.Inbound)
                {
                    if (curProtocol == (int)FirewallRule.KnownProtocols.TCP)
                    {
                        cmbLocalPorts.Items.Add(new ContentControl() { Content = FirewallRule.PortKeywordIpTlsIn, Tag = FirewallRule.PortKeywordIpTlsIn });
                        cmbLocalPorts.Items.Add(new ContentControl() { Content = FirewallRule.PortKeywordRpcEp, Tag = FirewallRule.PortKeywordRpcEp });
                        cmbLocalPorts.Items.Add(new ContentControl() { Content = FirewallRule.PortKeywordRpc, Tag = FirewallRule.PortKeywordRpc });
                    }
                    else if (curProtocol == (int)FirewallRule.KnownProtocols.UDP)
                    {
                        cmbLocalPorts.Items.Add(new ContentControl() { Content = FirewallRule.PortKeywordTeredo, Tag = FirewallRule.PortKeywordTeredo });
                        cmbLocalPorts.Items.Add(new ContentControl() { Content = FirewallRule.PortKeywordPly2Disc, Tag = FirewallRule.PortKeywordPly2Disc });
                        cmbLocalPorts.Items.Add(new ContentControl() { Content = FirewallRule.PortKeywordMDns, Tag = FirewallRule.PortKeywordMDns });
                        cmbLocalPorts.Items.Add(new ContentControl() { Content = FirewallRule.PortKeywordDhcp, Tag = FirewallRule.PortKeywordDhcp });
                    }
                }

                //if (!WpfFunc.CmbSelect(cmbRemotePorts, Rule.RemotePorts) && Rule.RemotePorts != null)
                //    cmbRemotePorts.Text = Rule.RemotePorts;
                viewModel.RemotePort = WpfFunc.CmbPick(cmbRemotePorts, FirewallRule.IsEmptyOrStar(Rule.RemotePorts) ? "*" : Rule.RemotePorts);
                if (viewModel.RemotePort == null)
                    viewModel.RemotePortTxt = Rule.RemotePorts;

                //if (!WpfFunc.CmbSelect(cmbLocalPorts, Rule.LocalPorts) && Rule.LocalPorts != null)
                //    cmbLocalPorts.Text = Rule.LocalPorts;
                viewModel.LocalPort = WpfFunc.CmbPick(cmbLocalPorts, FirewallRule.IsEmptyOrStar(Rule.LocalPorts) ? "*" : Rule.LocalPorts);
                if (viewModel.LocalPort == null)
                    viewModel.LocalPortTxt = Rule.LocalPorts;
            }
        }

        private bool checkAll()
        {
            if (txtName.Text.Length == 0)
                return false;

            if (cmbProgram.SelectedItem == null)
                return false;

            if (cmbAction.SelectedItem == null)
                return false;

            if (cmbDirection.SelectedItem == null)
                return false;

            if (curProtocol == null)
                return false;

            if (curProtocol == (int)FirewallRule.KnownProtocols.TCP || curProtocol == (int)FirewallRule.KnownProtocols.UDP)
            {
                string reason = "";
                if (cmbRemotePorts.SelectedItem == null && !RuleWindow.ValidatePorts(cmbRemotePorts.Text, ref reason))
                    return false;
                if (cmbLocalPorts.SelectedItem == null && !RuleWindow.ValidatePorts(cmbLocalPorts.Text, ref reason))
                    return false;
            }
            else if (Rule.Protocol == (int)FirewallRule.KnownProtocols.ICMP || Rule.Protocol == (int)FirewallRule.KnownProtocols.ICMPv6)
            {
                // ToDo: Validate ICMP values
            }

            return true;
        }

        private void btnOK_Click(object sender, RoutedEventArgs e)
        {
            if (!checkAll())
                return;

            Rule.Name = txtName.Text;
            Rule.Grouping = cmbGroup.Text;
            Rule.Description = txtInfo.Text;

            Rule.ProgID = ((cmbProgram.SelectedItem as ContentControl).Tag as ProgramID);

            Rule.Action = (FirewallRule.Actions)(cmbAction.SelectedItem as ContentControl).Tag;

            if (radProfileAll.IsChecked == true || (chkPrivate.IsChecked == true && chkDomain.IsChecked == true && chkPublic.IsChecked == true))
                Rule.Profile = (int)FirewallRule.Profiles.All;
            else
            {
                Rule.Profile = (int)FirewallRule.Profiles.Undefined;
                if (chkPrivate.IsChecked == true)
                    Rule.Profile |= (int)FirewallRule.Profiles.Private;
                if (chkDomain.IsChecked == true)
                    Rule.Profile |= (int)FirewallRule.Profiles.Domain;
                if (chkPublic.IsChecked == true)
                    Rule.Profile |= (int)FirewallRule.Profiles.Public;
            }

            if (radProfileAll.IsChecked == true || (chkPrivate.IsChecked == true && chkDomain.IsChecked == true && chkPublic.IsChecked == true))
                Rule.Profile = (int)FirewallRule.Profiles.All;
            else
            {
                Rule.Profile = (int)FirewallRule.Profiles.Undefined;
                if (chkPrivate.IsChecked == true)
                    Rule.Profile |= (int)FirewallRule.Profiles.Private;
                if (chkDomain.IsChecked == true)
                    Rule.Profile |= (int)FirewallRule.Profiles.Domain;
                if (chkPublic.IsChecked == true)
                    Rule.Profile |= (int)FirewallRule.Profiles.Public;
            }

            if (radProfileAll.IsChecked == true || (chkLAN.IsChecked == true && chkVPN.IsChecked == true && chkWiFi.IsChecked == true))
                Rule.Interface = (int)FirewallRule.Interfaces.All;
            else
            {
                Rule.Interface = 0;
                if (chkLAN.IsChecked == true)
                    Rule.Interface |= (int)FirewallRule.Interfaces.Lan;
                if (chkVPN.IsChecked == true)
                    Rule.Interface |= (int)FirewallRule.Interfaces.RemoteAccess;
                if (chkWiFi.IsChecked == true)
                    Rule.Interface |= (int)FirewallRule.Interfaces.Wireless;
            }

            Rule.Direction = (FirewallRule.Directions)(cmbDirection.SelectedItem as ContentControl).Tag;

            Rule.Protocol = curProtocol.Value;
            if (Rule.Protocol == (int)FirewallRule.KnownProtocols.TCP || Rule.Protocol == (int)FirewallRule.KnownProtocols.UDP)
            {
                if (cmbRemotePorts.SelectedItem != null)
                    Rule.RemotePorts = (string)((cmbRemotePorts.SelectedItem as ContentControl).Tag);
                else
                    Rule.RemotePorts = cmbRemotePorts.Text.Replace(" ", ""); // white spaces are not valid

                if (cmbLocalPorts.SelectedItem != null)
                    Rule.LocalPorts = (string)((cmbLocalPorts.SelectedItem as ContentControl).Tag);
                else
                    Rule.LocalPorts = cmbLocalPorts.Text.Replace(" ", ""); // white spaces are not valid
            }
            else if (Rule.Protocol == (int)FirewallRule.KnownProtocols.ICMP || Rule.Protocol == (int)FirewallRule.KnownProtocols.ICMPv6)
            {
                if (cmbICMP.SelectedItem != null)
                    Rule.SetIcmpTypesAndCodes((cmbICMP.SelectedItem as ContentControl).Tag as string);
                else
                    Rule.SetIcmpTypesAndCodes(cmbICMP.Text);
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

        private static int? ParsePort(string Value)
        {
            int Port;
            if (int.TryParse(Value, out Port) && Port >= 0x0000 && Port <= 0xFFFF)
                return Port;
            return null;
        }

        public static bool ValidatePorts(string Ports, ref string reason)
        {
            bool? duplicates = false;
            List<string> ValueList = WpfFunc.SplitAndValidate(Ports, ref duplicates);
            if (ValueList == null)
            {
                if (duplicates == true)
                    reason = Translate.fmt("err_duplicate_value");
                else 
                    reason = Translate.fmt("err_empty_value");
                return false;
            }

            foreach (string Value in ValueList)
            {
                string[] strTemp = Value.Split('-');
                if (strTemp.Length == 1)
                {
                    if (ParsePort(strTemp[0]) == null)
                    {
                        reason = Translate.fmt("err_invallid_port");
                        return false;
                    }
                }
                else if (strTemp.Length == 2)
                {
                    int? PortL = ParsePort(strTemp[0]);
                    int? PortR = ParsePort(strTemp[1]);
                    if (PortL == null || PortR == null)
                    {
                        reason = Translate.fmt("err_invallid_port");
                        return false;
                    }

                    if (!(PortL.GetValueOrDefault() < PortR.GetValueOrDefault()))
                    {
                        reason = Translate.fmt("err_invalid_range");
                        return false;
                    }
                }
                else
                {
                    reason = Translate.fmt("err_invalid_range");
                    return false;
                }
            }
            return true;
        }

        private void someThing_Changed(object sender, RoutedEventArgs e)
        {
            btnOK.IsEnabled = checkAll();
        }
    }

    public class RuleWindowViewModel : WpfFunc.ViewModelHelper, IDataErrorInfo
    {
        public RuleWindowViewModel()
        {
        }

        public string this[string propName]
        {
            get
            {
                if (propName == "RuleName")
                {
                    if (this.RuleName == null || this.RuleName.Length == 0)
                        return "Rule Name must be set";
                }
                else if (propName == "RuleAction")
                {
                    if (curAction == null)
                        return "Rule Action must be set";
                }
                else if (propName == "ProtocolTxt")
                {
                    if (curProtocol == null)
                    {
                        int prot;
                        if (!int.TryParse(ProtocolTxt, out prot) || prot < 0 || prot > 255)
                            return "Invalid protocol value";
                    }
                }
                else if (propName == "LocalPortTxt")
                {
                    string reason = "";
                    if (curLocalPort == null && !RuleWindow.ValidatePorts(curLocalPortTxt, ref reason)) // we can only select valid items
                        return reason;
                }
                else if (propName == "RemotePortTxt")
                {
                    string reason = "";
                    if (curRemotePort == null && !RuleWindow.ValidatePorts(curRemotePortTxt, ref reason)) // we can only select valid items
                        return reason;
                }
                return null;
            }
        }

        public string Error { get { return string.Empty; } }


        string curRuleName = "";
        public string RuleName
        {
            get { return curRuleName; }
            set { SetProperty("RuleName", value, ref curRuleName); }
        }

        ContentControl curAction = null;
        public ContentControl RuleAction
        {
            get { return curAction; }
            set { SetProperty("RuleAction", value, ref curAction); }
        }

        ContentControl curProtocol = null;
        public ContentControl Protocol
        {
            get { return curProtocol; }
            set { SetPropertyCmb("Protocol", value, ref curProtocol, ref curProtocolTxt); }
        }

        string curProtocolTxt = "";
        public string ProtocolTxt
        {
            get { return curProtocolTxt; }
            set { SetProperty("ProtocolTxt", value, ref curProtocolTxt); }
        }

        ContentControl curLocalPort = null;
        public ContentControl LocalPort
        {
            get { return curLocalPort; }
            set { SetPropertyCmb("LocalPort", value, ref curLocalPort, ref curLocalPortTxt); }
        }

        string curLocalPortTxt = "";
        public string LocalPortTxt
        {
            get { return curLocalPortTxt; }
            set { SetProperty("LocalPortTxt", value, ref curLocalPortTxt); }
        }

        ContentControl curRemotePort = null;
        public ContentControl RemotePort
        {
            get { return curRemotePort; }
            set { SetPropertyCmb("RemotePort", value, ref curRemotePort, ref curRemotePortTxt); }
        }

        string curRemotePortTxt = "";
        public string RemotePortTxt
        {
            get { return curRemotePortTxt; }
            set { SetProperty("RemotePortTxt", value, ref curRemotePortTxt); }
        }

        // ToDo: add ICMP validation

        // Note: IP-Address validation is done in the address control itself

    }
}