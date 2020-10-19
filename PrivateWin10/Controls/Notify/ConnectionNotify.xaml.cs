using PrivateWin10.Windows;
using System;
using System.Collections.Generic;
using System.ComponentModel;
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
using MiscHelpers;
using PrivateAPI;
using WinFirewallAPI;

namespace PrivateWin10.Controls
{
    /// <summary>
    /// Interaction logic for ConnectionNotify.xaml
    /// </summary>
    public partial class ConnectionNotify : UserControl, INotificationTab
    {
        DataGridExt consGridExt;

        public event EventHandler<EventArgs> Emptied;

        public ConnectionNotify()
        {
            InitializeComponent();

            this.btnPrev.Content = Translate.fmt("lbl_prev");
            this.btnNext.Content = Translate.fmt("lbl_next");
            this.lblRemember.Content = Translate.fmt("lbl_remember");

            
            var Ignore = this.IgnoreSB.Content as TextBlock;

            Ignore.Text = Translate.fmt("lbl_ignore");
            (this.IgnoreSB.MenuItemsSource[0] as MenuItem).Header = Translate.fmt("lbl_ignore_all");

            //this.btnIgnore.Content = Translate.fmt("lbl_ignore");
            this.btnApply.Content = Translate.fmt("lbl_apply");

            this.consGrid.Columns[0].Header = Translate.fmt("lbl_protocol");
            this.consGrid.Columns[1].Header = Translate.fmt("lbl_ip_port");
            this.consGrid.Columns[2].Header = Translate.fmt("lbl_remote_host");
            this.consGrid.Columns[3].Header = Translate.fmt("lbl_time_stamp");
            this.consGrid.Columns[4].Header = Translate.fmt("lbl_pid");

            consGridExt = new DataGridExt(consGrid);
            consGridExt.Restore(App.GetConfig("GUI", "consGrid_Columns", ""));


            cmbAccess.Items.Add(new ComboBoxItem() { Content = Translate.fmt("acl_none"), Tag = ProgramConfig.AccessLevels.Unconfigured });
            cmbAccess.Items.Add(new ComboBoxItem() { Content = Translate.fmt("acl_silence"), Tag = ProgramConfig.AccessLevels.StopNotify });
            cmbAccess.Items.Add(new ComboBoxItem() { Content = Translate.fmt("acl_allow"), Tag = ProgramConfig.AccessLevels.FullAccess });
            cmbAccess.Items.Add(new ComboBoxItem() { Content = Translate.fmt("acl_outbound"), Tag = ProgramConfig.AccessLevels.OutBoundAccess });
            cmbAccess.Items.Add(new ComboBoxItem() { Content = Translate.fmt("acl_inbound"), Tag = ProgramConfig.AccessLevels.InBoundAccess });
            cmbAccess.Items.Add(new ComboBoxItem() { Content = Translate.fmt("acl_edit"), Tag = ProgramConfig.AccessLevels.CustomConfig });
            cmbAccess.Items.Add(new ComboBoxItem() { Content = Translate.fmt("acl_lan"), Tag = ProgramConfig.AccessLevels.LocalOnly });
            cmbAccess.Items.Add(new ComboBoxItem() { Content = Translate.fmt("acl_block"), Tag = ProgramConfig.AccessLevels.BlockAccess });
            foreach (ComboBoxItem item in cmbAccess.Items)
                item.Background = ProgramControl.GetAccessColor((ProgramConfig.AccessLevels)item.Tag);

#if DEBUG
            cmbRemember.Items.Add(new ComboBoxItem() { Content = Translate.fmt("lbl_temp", "1 min"), Tag = 60 });
#endif
            cmbRemember.Items.Add(new ComboBoxItem() { Content = Translate.fmt("lbl_temp", "5 min"), Tag = 5 * 60 });
            cmbRemember.Items.Add(new ComboBoxItem() { Content = Translate.fmt("lbl_temp", "15 min"), Tag = 15 * 60 });
            cmbRemember.Items.Add(new ComboBoxItem() { Content = Translate.fmt("lbl_temp", "1 h"), Tag = 60 * 60 });
            cmbRemember.SelectedIndex = cmbRemember.Items.Count - 1; // default is 1h
            cmbRemember.Items.Add(new ComboBoxItem() { Content = Translate.fmt("lbl_temp", "24 h"), Tag = 24 * 60 * 60 });
            cmbRemember.Items.Add(new ComboBoxItem() { Content = Translate.fmt("lbl_permanent"), Tag = 0 });
        }

        public void Closing()
        {
            App.SetConfig("GUI", "consGrid_Columns", consGridExt.Save());
        }

        int curIndex = -1;
        private SortedDictionary<ProgramID, Tuple<Program, List<Priv10Engine.FwEventArgs>>> mEvents = new SortedDictionary<ProgramID, Tuple<Program, List<Priv10Engine.FwEventArgs>>>();

        private List<ProgramID> mEventList = new List<ProgramID>();

        public bool IsEmpty()
        {
            return mEventList.Count == 0;
        }

        public bool Add(ProgramSet progs, Priv10Engine.FwEventArgs args)
        {
            ProgramID id = args.entry.ProgID;
            Program prog = null;
            if (!progs.Programs.TryGetValue(id, out prog))
                return false;

            Tuple<Program, List<Priv10Engine.FwEventArgs>> list;
            if (!mEvents.TryGetValue(id, out list))
            {
                if (args.update)
                    return false;

                list = new Tuple<Program, List<Priv10Engine.FwEventArgs>>(prog, new List<Priv10Engine.FwEventArgs>());
                mEvents.Add(id, list);
                mEventList.Add(id);
            }

            if (args.update)
            {
                foreach (var oldEntry in list.Item2)
                {
                    if (oldEntry.entry.guid.Equals(args.entry.guid))
                    {
                        oldEntry.entry.Update(args.entry);
                        break;
                    }
                }
            }
            else
            {
                list.Item2.Add(args);
            }

            int oldIndex = curIndex;

            if (curIndex < 0)
                curIndex = 0;
            else if (curIndex >= mEventList.Count)
                curIndex = mEventList.Count - 1;

            UpdateIndex();

            // don't update if the event is for a different entry
            int index = mEventList.FindIndex((x) => { return id.CompareTo(x) == 0; });
            if (curIndex == index)
                LoadCurrent(oldIndex == curIndex);

            return true;
        }

        private void UpdateIndex()
        {
            lblIndex.Text = string.Format("{0}/{1}", curIndex + 1, mEventList.Count);
            btnPrev.IsEnabled = curIndex + 1 > 1;
            btnNext.IsEnabled = curIndex + 1 < mEventList.Count;
        }

        private void LoadCurrent(bool bUpdate = false)
        {
            if (!bUpdate)
            {
                ProgramConfig.AccessLevels NetAccess = ProgramConfig.AccessLevels.Unconfigured;

                cmbAccess.Background = ProgramControl.GetAccessColor(NetAccess);
                WpfFunc.CmbSelect(cmbAccess, NetAccess.ToString());

                btnApply.IsEnabled = false;
            }

            ProgramID id = mEventList.ElementAt(curIndex);
            Tuple<Program, List<Priv10Engine.FwEventArgs>> list = mEvents[id];

            //int PID = list.Item2.Count > 0 ? list.Item2.First().FwEvent.ProcessId : 0;
            string FilePath = list.Item2.Count > 0 ? list.Item2.First().entry.FwEvent.ProcessFileName : "";

            imgIcon.Source = ImgFunc.GetIcon(FilePath, imgIcon.Width); // todo: use .progSet.GetIcon instead?
            //lblName.Text = id.GetDisplayName(false);
            grpBox.Header = list.Item1.Description;
            //lblPID.Text = string.Format("{0} ({1})", System.IO.Path.GetFileName(id.Path), PID);
            lblPID.Text = System.IO.Path.GetFileName(id.Path);

            List<string> services = new List<string>();

            //btnIgnore.IsEnabled = true;
            IgnoreSB.IsEnabled = true;
            consGrid.Items.Clear();
            foreach (Priv10Engine.FwEventArgs args in list.Item2)
            {
                consGrid.Items.Insert(0, new ConEntry(args.entry));

                if (args.services != null)
                {
                    foreach (var service in args.services)
                    {
                        if (!services.Contains(service))
                            services.Add(service);
                    }
                }
            }

            if (services.Count > 0)
            {
                cmbService.Visibility = Visibility.Visible;
                cmbService.Items.Clear();
                foreach (var service in services)
                    cmbService.Items.Add(service);
                cmbService.SelectedIndex = -1;
                cmbService.Text = Translate.fmt("msg_pick_svc");
            }
            else
            {
                cmbService.Visibility = Visibility.Collapsed;
                switch (id.Type)
                {
                    //case ProgramList.Types.Program: lblSubName.Text = ""; break;
                    case ProgramID.Types.Service: 
                        lblSubName.Text = id.GetServiceId(); 
                        break;
                    case ProgramID.Types.App: 
                        string SID = id.GetPackageSID();
                        lblSubName.Text = SID != null ? AppModel.GetInstance().GetAppPkgBySid(SID)?.ID ?? SID : "";
                        break;
                    default: lblSubName.Text = ""; break;
                }
            }
            lblPath.Text = id.Path;
        }

        private void btnPrev_Click(object sender, RoutedEventArgs e)
        {
            if (curIndex > 0)
                curIndex--;
            UpdateIndex();
            LoadCurrent();
        }

        private void btnNext_Click(object sender, RoutedEventArgs e)
        {
            if (curIndex + 1 < mEventList.Count)
                curIndex++;
            UpdateIndex();
            LoadCurrent();
        }

        private void PopEntry()
        {
            if (curIndex == -1)
                return;
            ProgramID id = mEventList.ElementAt(curIndex);

            mEventList.RemoveAt(curIndex);
            mEvents.Remove(id);

            if (curIndex >= mEventList.Count)
                curIndex = mEventList.Count - 1;
            if (curIndex < 0)
            {
                //btnIgnore.IsEnabled = false;
                IgnoreSB.IsEnabled = false;
                consGrid.Items.Clear();
                curIndex = -1;
                Emptied?.Invoke(this, new EventArgs());
                return;
            }

            UpdateIndex();
            LoadCurrent();
        }

        private void BtnIgnore_Click(object sender, MouseButtonEventArgs e)
        {
            PopEntry();
        }

        private void BtnIgnoreAll_Click(object sender, RoutedEventArgs e)
        {
            mEventList.Clear();
            mEvents.Clear();

            IgnoreSB.IsEnabled = false;
            consGrid.Items.Clear();
            curIndex = -1;
            Emptied?.Invoke(this, new EventArgs());
        }

        private void cmbAccess_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (cmbService.Visibility == Visibility.Visible && cmbService.SelectedIndex == -1)
            {
                btnApply.IsEnabled = false;
                return;
            }

            ProgramConfig.AccessLevels NetAccess = (ProgramConfig.AccessLevels)(cmbAccess.SelectedItem as ComboBoxItem).Tag;
            cmbAccess.Background = ProgramControl.GetAccessColor(NetAccess);
            btnApply.IsEnabled = NetAccess != ProgramConfig.AccessLevels.Unconfigured;
        }

        private bool MakeCustom(Program prog, UInt64 expiration, ConEntry entry = null)
        {
            FirewallRuleEx rule = new FirewallRuleEx() { guid = null, Profile = (int)FirewallRule.Profiles.All, Interface = (int)FirewallRule.Interfaces.All, Enabled = true };
            rule.SetProgID(prog.ID);
            rule.Name = FirewallManager.MakeRuleName(FirewallManager.CustomName, expiration != 0, prog.Description);
            rule.Grouping = FirewallManager.RuleGroup;

            if (entry != null)
            {
                rule.Direction = entry.Entry.FwEvent.Direction;
                rule.Protocol = (int)entry.Entry.FwEvent.Protocol;
                switch (entry.Entry.FwEvent.Protocol)
                {
                    /*case (int)FirewallRule.KnownProtocols.ICMP:
                    case (int)FirewallRule.KnownProtocols.ICMPv6:
                        
                        break;*/
                    case (int)FirewallRule.KnownProtocols.TCP:
                    case (int)FirewallRule.KnownProtocols.UDP:
                        rule.LocalPorts = "*";
                        rule.RemotePorts = entry.Entry.FwEvent.RemotePort.ToString();
                        break;
                }
                rule.LocalAddresses = "*";
                rule.RemoteAddresses = entry.Entry.FwEvent.RemoteAddress.ToString();
            }
            else
            {
                rule.Direction = FirewallRule.Directions.Bidirectiona;
            }

            RuleWindow ruleWnd = new RuleWindow(new List<Program>() { prog }, rule);
            ruleWnd.Topmost = true;
            if (ruleWnd.ShowDialog() != true)
                return false;

            if (!App.client.UpdateRule(rule, expiration))
            {
                MessageBox.Show(Translate.fmt("msg_rule_failed"), App.Title, MessageBoxButton.OK, MessageBoxImage.Exclamation);
                return false;
            }

            return true;
        }

        private UInt64 GetExpiration()
        {
            ComboBoxItem remember = (cmbRemember.SelectedItem as ComboBoxItem);
            if (remember != null && (int)remember.Tag != 0)
                return MiscFunc.GetUTCTime() + (UInt64)(int)remember.Tag;
            return 0;
        }

        private void btnApply_Click(object sender, RoutedEventArgs e)
        {
            ProgramID id = mEventList.ElementAt(curIndex);
            Tuple<Program, List<Priv10Engine.FwEventArgs>> list = mEvents[id];

            UInt64 expiration = GetExpiration();

            ProgramConfig.AccessLevels NetAccess = (ProgramConfig.AccessLevels)(cmbAccess.SelectedItem as ComboBoxItem).Tag;

            if (NetAccess == ProgramConfig.AccessLevels.CustomConfig)
            {
                if (!MakeCustom(list.Item1, expiration))
                    return;
            }
            else
            {
                ProgramSet prog = App.client.GetProgram(id);

                if (NetAccess == ProgramConfig.AccessLevels.StopNotify)
                {
                    if (expiration != 0)
                        prog.config.SilenceUntill = expiration;
                    else
                        prog.config.Notify = false;

                    App.client.UpdateProgram(prog.guid, prog.config);
                }
                else
                {
                    prog.config.NetAccess = NetAccess;

                    App.client.UpdateProgram(prog.guid, prog.config, expiration);
                }
            }
            PopEntry();
        }

        private void consGrid_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            ConEntry entry = (ConEntry)consGrid.SelectedItem;
            if (entry == null)
                return;

            ProgramID id = mEventList.ElementAt(curIndex);
            Tuple<Program, List<Priv10Engine.FwEventArgs>> list = mEvents[id];

            UInt64 expiration = GetExpiration();
            if (MakeCustom(list.Item1, expiration, entry))
                PopEntry();
        }

        public class ConEntry : INotifyPropertyChanged
        {
            public Program.LogEntry Entry;

            public ConEntry(Program.LogEntry entry)
            {
                Entry = entry;
            }

            public string Protocol { get { return Translate.fmt(Entry.FwEvent.Direction == FirewallRule.Directions.Inbound ? "str_in" : "str_out", NetFunc.Protocol2Str(Entry.FwEvent.Protocol)); } }

            public string Address { get { if (Entry.FwEvent.RemoteAddress == null) return ""; return Entry.FwEvent.RemoteAddress.ToString() + ":" + Entry.FwEvent.RemotePort.ToString(); } }

            public string RemoteHost { get { return Entry.RemoteHostName; } }

            public string TimeStamp { get { return Entry.FwEvent.TimeStamp.ToString("HH:mm:ss"); } }

            public string ProcessID { get { return Entry.FwEvent.ProcessId.ToString(); } }

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

        private void LblPath_MouseDown(object sender, MouseButtonEventArgs e)
        {
            string path = lblPath.Text;
            if (!string.IsNullOrEmpty(path))
                Process.Start("Explorer.exe", "/select, " + path);
        }
    }
}
