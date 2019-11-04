using PrivateWin10.Pages;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
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
    /// Interaction logic for FirewallLogList.xaml
    /// </summary>
    public partial class FirewallLogList : UserControl
    {
        DataGridExt consGridExt;

        public FirewallPage firewallPage = null;

        enum ConTypes
        {
            All = 0,
            Blocked,
            Allowed
        }
        ConTypes mConTypes = ConTypes.All;

        string mConFilter = "";
        int mConLimit = 1000;

        public FirewallLogList()
        {
            InitializeComponent();

            //this.grpLog.Header = Translate.fmt("gtp_con_log");
            this.grpLogTools.Header = Translate.fmt("grp_tools");
            this.grpLogView.Header = Translate.fmt("grp_view");

            this.btnMkRule.Content = Translate.fmt("btn_mk_rule");
            this.btnClearLog.Content = Translate.fmt("btn_clear_log");
            this.lblShowCons.Content = Translate.fmt("lbl_show_cons");
            this.chkNoLocal.Content = Translate.fmt("chk_hide_local");
            this.chkNoLAN.Content = Translate.fmt("chk_hide_lan");            
            this.lblFilterCons.Content = Translate.fmt("lbl_filter_cons");


            this.consGrid.Columns[1].Header = Translate.fmt("lbl_name");
            this.consGrid.Columns[2].Header = Translate.fmt("lbl_time_stamp");
            this.consGrid.Columns[3].Header = Translate.fmt("lbl_action");
            this.consGrid.Columns[4].Header = Translate.fmt("lbl_direction");
            this.consGrid.Columns[5].Header = Translate.fmt("lbl_protocol");
            this.consGrid.Columns[6].Header = Translate.fmt("lbl_remote_ip");
            this.consGrid.Columns[7].Header = Translate.fmt("lbl_remote_port");
            this.consGrid.Columns[8].Header = Translate.fmt("lbl_local_ip");
            this.consGrid.Columns[9].Header = Translate.fmt("lbl_local_port");
            this.consGrid.Columns[10].Header = Translate.fmt("lbl_program");

            consGridExt = new DataGridExt(consGrid);
            consGridExt.Restore(App.GetConfig("GUI", "consGrid_Columns", ""));


            mConFilter = App.GetConfig("GUI", "ConFilter", "");
            txtConFilter.Text = mConFilter;

            try
            {
                mConTypes = (ConTypes)App.GetConfigInt("FwLog", "ConTypes", 0);
                cmbConTypes.SelectedIndex = (int)mConTypes;
                //this.chkAllowed.IsChecked = App.GetConfigInt("FwLog", "ShowAllowed", 1) == 1;
                //this.chkBlocked.IsChecked = App.GetConfigInt("FwLog", "ShowBlocked", 1) == 1;
                this.chkNoLocal.IsChecked = App.GetConfigInt("FwLog", "ConNoLocal", 0) == 1;
                this.chkNoLAN.IsChecked = App.GetConfigInt("FwLog", "ConNoLan", 0) == 1;
            }
            catch { }

            //mConLimit = App.engine.programs.MaxLogLength * 10; // todo
            mConLimit = App.GetConfigInt("GUI", "LogLimit", 1000);

            CheckLogLines();
        }

        public void OnClose()
        {
            App.SetConfig("GUI", "consGrid_Columns", consGridExt.Save());
        }

        public void UpdateConnections(bool clear = false)
        {
            if (firewallPage == null)
                return;

            if (clear)
                consGrid.Items.Clear();

            Dictionary<Guid, LogItem> oldLog = new Dictionary<Guid, LogItem>();
            foreach (LogItem oldItem in consGrid.Items)
                oldLog.Add(oldItem.args.guid, oldItem);

            Dictionary<Guid, List<Program.LogEntry>> entries = App.client.GetConnections(firewallPage.GetCurGuids(mConFilter));
            foreach (var entrySet in entries)
            {
                ProgramControl item = null;
                ProgramSet prog = firewallPage.GetProgSet(entrySet.Key, null, out item);
                if (prog == null)
                    continue;

                foreach (Program.LogEntry entry in entrySet.Value)
                {
                    if (!TestEntry(prog, entry))
                        continue;

                    //LogItem Item;
                    //if (!oldLog.TryGetValue(entry.guid, out Item))
                        if (!oldLog.Remove(entry.guid))
                    {
                        Program program = ProgramList.GetProgramFuzzy(prog.Programs, entry.ProgID, ProgramList.FuzzyModes.Any);
                        
                        consGrid.Items.Insert(0, new LogItem(entry, program != null ? program.Description : prog.config.Name));
                    }
                    /*else
                    {
                        oldLog.Remove(entry.guid);
                        Item.Update(entry);
                    }*/
                }
            }

            foreach (LogItem item in oldLog.Values)
                consGrid.Items.Remove(item);
        }

        private bool TestEntry(ProgramSet prog, Program.LogEntry entry)
        {
            switch (mConTypes)
            {
                case ConTypes.Allowed: if (entry.FwEvent.Action != FirewallRule.Actions.Allow) return false; break;
                case ConTypes.Blocked: if (entry.FwEvent.Action != FirewallRule.Actions.Block) return false; break;
            }

            if (chkNoLocal.IsChecked == true && (NetFunc.IsLocalHost(entry.FwEvent.RemoteAddress) || NetFunc.IsMultiCast(entry.FwEvent.RemoteAddress)))
                return false;

            if (chkNoLAN.IsChecked == true && FirewallRule.MatchAddress(entry.FwEvent.RemoteAddress, "LocalSubnet"))
                return false; 

            if (FirewallPage.DoFilter(mConFilter, prog.config.Name, new List<ProgramID>() { entry.ProgID }))
                return false;

            return true;
        }

        public void AddEntry(ProgramSet prog, Program program, FirewallManager.NotifyArgs args)
        {
            if (!TestEntry(prog, args.entry))
                return;

            if (args.update)
            {
                foreach (LogItem Item in consGrid.Items)
                {
                    if (Item.args.guid.Equals(args.entry.guid))
                    {
                        Item.Update(args.entry);
                        return;
                    }
                }
            }

            consGrid.Items.Insert(0, new LogItem(args.entry, program != null ? program.Description : null));

            while (consGrid.Items.Count > mConLimit)
                consGrid.Items.RemoveAt(mConLimit);
        }

        private void cmbConTypes_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            mConTypes = (ConTypes)cmbConTypes.SelectedIndex;
            App.SetConfig("FwLog", "ConTypes", (int)mConTypes);
            UpdateConnections(true);
        }
		
        private void LogTypeFilters_Click(object sender, RoutedEventArgs e)
        {
            //App.SetConfig("FwLog", "ShowAllowed", this.chkAllowed.IsChecked == true ? 1 : 0);
            //App.SetConfig("FwLog", "ShowBlocked", this.chkBlocked.IsChecked == true ? 1 : 0);
            App.SetConfig("FwLog", "ConNoLocal", this.chkNoLocal.IsChecked == true ? 1 : 0);
            App.SetConfig("FwLog", "ConNoLan", this.chkNoLAN.IsChecked == true ? 1 : 0);
            UpdateConnections(true);
        }


        private void txtConFilter_TextChanged(object sender, TextChangedEventArgs e)
        {
            mConFilter = txtConFilter.Text;
            App.SetConfig("GUI", "ConFilter", mConFilter);
            UpdateConnections(true);
        }

        private void btnMkRule_Click(object sender, RoutedEventArgs e)
        {
            LogItem entry = (consGrid.SelectedItem as LogItem);
            if (entry == null)
                return;

            ProgramControl item = null;
            ProgramSet prog = firewallPage.GetProgSet(entry.args.guid, entry.args.ProgID, out item);
            if (prog == null)
                return;
            Program program = null;
            prog.Programs.TryGetValue(entry.args.ProgID, out program);
            if (program == null)
                return;

            FirewallRule rule = new FirewallRule() { guid = null, ProgID = entry.args.ProgID, Profile = (int)FirewallRule.Profiles.All, Interface = (int)FirewallRule.Interfaces.All, Enabled = true };

            rule.ProgID = entry.args.ProgID;
            rule.Name = Translate.fmt("custom_rule", program.Description);
            rule.Grouping = FirewallManager.RuleGroup;

            rule.Direction = entry.args.FwEvent.Direction;
            rule.Protocol = (int)entry.args.FwEvent.Protocol;
            switch (entry.args.FwEvent.Protocol)
            {
                /*case (int)FirewallRule.KnownProtocols.ICMP:
                case (int)FirewallRule.KnownProtocols.ICMPv6:

                    break;*/
                case (int)FirewallRule.KnownProtocols.TCP:
                case (int)FirewallRule.KnownProtocols.UDP:
                    rule.RemotePorts = entry.args.FwEvent.RemotePort.ToString();
                    break;
            }
            rule.RemoteAddresses = entry.args.FwEvent.RemoteAddress.ToString();

            firewallPage.ShowRuleWindow(rule);
        }

        private void btnClearLog_Click(object sender, RoutedEventArgs e)
        {
            MessageBoxResult res = MessageBox.Show(Translate.fmt("msg_clear_log"), App.mName, MessageBoxButton.YesNoCancel, MessageBoxImage.Question);

            if (res == MessageBoxResult.Cancel)
                return;

            if (App.client.ClearLog(res == MessageBoxResult.Yes))
                consGrid.Items.Clear();
        }

        private void CheckLogLines()
        {
            btnMkRule.IsEnabled = consGrid.SelectedItems.Count == 1;
        }


        private void ConsGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            CheckLogLines();
        }


        /////////////////////////////////
        /// LogItem

        public class LogItem : INotifyPropertyChanged
        {
            public Program.LogEntry args;
            public string name;

            public LogItem(Program.LogEntry args, string name)
            {
                this.args = args;
                this.name = name != null ? name : "[unknown progream]";
            }

            void DoUpdate()
            {
                NotifyPropertyChanged(null);
            }

            public ImageSource Icon { get { return ImgFunc.GetIcon(args.ProgID.Path, 16); } }

            public string Name { get { return name; } }
            public string Program { get { return args.ProgID.FormatString(); } }
            public DateTime TimeStamp { get { return args.FwEvent.TimeStamp; } }
            public string Action
            {
                get
                {
                    switch (args.FwEvent.Action)
                    {
                        case FirewallRule.Actions.Allow: return Translate.fmt("str_allow");
                        case FirewallRule.Actions.Block: return Translate.fmt("str_block");
                        default: return Translate.fmt("str_undefined");
                    }
                }
            }

            public string Direction
            {
                get
                {
                    switch (args.FwEvent.Direction)
                    {
                        case FirewallRule.Directions.Inbound: return Translate.fmt("str_inbound");
                        case FirewallRule.Directions.Outboun: return Translate.fmt("str_outbound");
                        default: return Translate.fmt("str_undefined");
                    }
                }
            }
            public string Protocol { get { return (args.FwEvent.Protocol == (int)NetFunc.KnownProtocols.Any) ? Translate.fmt("pro_any") : NetFunc.Protocol2Str(args.FwEvent.Protocol); } }
            public string DestAddress
            {
                get
                {
                    if (args.HasHostName())
                        return args.FwEvent.RemoteAddress.ToString() + " (" + args.GetHostName() + ")";
                    return args.FwEvent.RemoteAddress.ToString();
                }
            }
            public string DestPorts { get { return args.FwEvent.RemotePort.ToString(); } }
            public string SrcAddress { get { return args.FwEvent.LocalAddress.ToString(); } }
            public string SrcPorts { get { return args.FwEvent.LocalPort.ToString(); } }

            public string ActionColor
            {
                get
                {
                    if (args.State == PrivateWin10.Program.LogEntry.States.RuleError) return "warn";
                    switch (args.FwEvent.Action)
                    {
                        case FirewallRule.Actions.Allow:
                            if (NetFunc.IsMultiCast(args.FwEvent.RemoteAddress))
                                return "blue2";
                            else if (FirewallRule.MatchAddress(args.FwEvent.RemoteAddress, "LocalSubnet"))
                                return "blue";
                            else
                                return "green";
                        case FirewallRule.Actions.Block:
                            if (NetFunc.IsMultiCast(args.FwEvent.RemoteAddress))
                                return "yellow2";
                            else if (FirewallRule.MatchAddress(args.FwEvent.RemoteAddress, "LocalSubnet"))
                                return "yellow";
                            else
                                return "red";
                        default: return "";
                    }
                }
            }

            void UpdateValue<T>(ref T value, T new_value, string Name)
            {
                if (value == null ? new_value == null : value.Equals(new_value))
                    return;
                value = new_value;
                NotifyPropertyChanged(Name);
            }

            internal void Update(Program.LogEntry new_args)
            {
                if(args.Update(new_args))
                    NotifyPropertyChanged("DestAddress");
                // The rest can't change
            }


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
