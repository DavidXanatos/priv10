using PrivateWin10.Controls;
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
    /// Interaction logic for NotificationWnd.xaml
    /// </summary>
    public partial class NotificationWnd : Window
    {
        public NotificationWnd()
        {
            InitializeComponent();

            this.Topmost = true;

            cmbAccess.Items.Add(new ComboBoxItem() { Content = Translate.fmt("acl_none"), Tag = Program.Config.AccessLevels.Unconfigured });
            cmbAccess.Items.Add(new ComboBoxItem() { Content = Translate.fmt("acl_silence"), Tag = Program.Config.AccessLevels.StopNotify });
            cmbAccess.Items.Add(new ComboBoxItem() { Content = Translate.fmt("acl_allow"), Tag = Program.Config.AccessLevels.FullAccess });
            cmbAccess.Items.Add(new ComboBoxItem() { Content = Translate.fmt("acl_edit"), Tag = Program.Config.AccessLevels.CustomConfig });
            cmbAccess.Items.Add(new ComboBoxItem() { Content = Translate.fmt("acl_lan"), Tag = Program.Config.AccessLevels.LocalOnly });
            cmbAccess.Items.Add(new ComboBoxItem() { Content = Translate.fmt("acl_block"), Tag = Program.Config.AccessLevels.BlockAccess });
            foreach (ComboBoxItem item in cmbAccess.Items)
                item.Background = ProgramControl.GetAccessColor((Program.Config.AccessLevels)item.Tag);

            cmbRemember.Items.Add(new ComboBoxItem() { Content = Translate.fmt("lbl_permanent"), Tag = 0 });
#if DEBUG
            cmbRemember.Items.Add(new ComboBoxItem() { Content = Translate.fmt("lbl_temp", "1 min"), Tag = 60 });
#endif
            cmbRemember.Items.Add(new ComboBoxItem() { Content = Translate.fmt("lbl_temp", "5 min"), Tag = 5 * 60 });
            cmbRemember.Items.Add(new ComboBoxItem() { Content = Translate.fmt("lbl_temp", "15 min"), Tag = 15 * 60 });
            cmbRemember.Items.Add(new ComboBoxItem() { Content = Translate.fmt("lbl_temp", "1 h"), Tag = 60 * 60 });
            cmbRemember.Items.Add(new ComboBoxItem() { Content = Translate.fmt("lbl_temp", "24 h"), Tag = 24 * 60 * 60 });
            cmbRemember.SelectedIndex = 0;

            WpfFunc.LoadWnd(this, "Notify");
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            WpfFunc.StoreWnd(this, "Notify");
        }

        int curIndex = 0;
        private SortedDictionary<ProgramList.ID, Tuple<Program, List<Program.LogEntry>>> mEvents = new SortedDictionary<ProgramList.ID, Tuple<Program, List<Program.LogEntry>>>();
        private List<ProgramList.ID> mEventList = new List<ProgramList.ID>();

        public void Add(Program prog, Program.LogEntry entry)
        {
            ProgramList.ID id = entry.mID;

            Tuple<Program, List<Program.LogEntry>> list;
            if (!mEvents.TryGetValue(id, out list))
            {
                list = new Tuple<Program, List<Program.LogEntry>>(prog, new List<Program.LogEntry>());
                mEvents.Add(id, list);
                mEventList.Add(id);
            }

            list.Item2.Add(entry);

            if (curIndex >= mEventList.Count)
                curIndex = mEventList.Count - 1;

            UpdateIndex();

            int index =  mEventList.FindIndex((x) => { return id.CompareTo(x) == 0; });
            if (curIndex == index)
                LoadCurrent();
        }

        private void UpdateIndex()
        {
            lblIndex.Text = string.Format("{0}/{1}", curIndex + 1, mEventList.Count);
        }

        private void LoadCurrent()
        {
            Program.Config.AccessLevels NetAccess = Program.Config.AccessLevels.Unconfigured;

            cmbAccess.Background = ProgramControl.GetAccessColor(NetAccess);
            WpfFunc.CmbSelect(cmbAccess, NetAccess.ToString());

            btnApply.IsEnabled = false;

            ProgramList.ID id = mEventList.ElementAt(curIndex);
            Tuple<Program, List<Program.LogEntry>> list = mEvents[id];

            int PID = list.Item2.Count > 0 ? list.Item2.First().PID : 0;

            imgIcon.Source = ImgFunc.GetIcon(list.Item1.GetIcon(), imgIcon.Width);
            //lblName.Text = id.GetDisplayName(false);
            grpBox.Header = id.GetDisplayName(false);
            lblPID.Text = string.Format("{0} ({1})", System.IO.Path.GetFileName(id.Path), PID);
            switch (id.Type)
            {
                //case ProgramList.Types.Program: lblSubName.Text = ""; break;
                case ProgramList.Types.Service: lblSubName.Text = id.Name; break;
                case ProgramList.Types.App: lblSubName.Text = App.engine.appMgr.GetAppName(id.Name); break;
                default: lblSubName.Text = ""; break;
            }
            lblPath.Text = id.Path;

            /*lstEvents.Items.Clear();
            foreach (Program.LogEntry entry in list.Item2)
            {
                string info = "";
                info += NetFunc.Protocol2SStr(entry.Protocol) + "://";

                switch (entry.Protocol)
                {
                    case (int)FirewallRule.KnownProtocols.TCP:
                    case (int)FirewallRule.KnownProtocols.UDP:
                        info += entry.RemoteAddress + ":" + entry.RemotePort;
                        break;
                }

                lstEvents.Items.Add(new ListBoxItem() { Content = info});
            }*/

            consGrid.Items.Clear();
            foreach (Program.LogEntry entry in list.Item2)
                consGrid.Items.Insert(0, new ConEntry(entry));
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
            ProgramList.ID id = mEventList.ElementAt(curIndex);

            mEventList.RemoveAt(curIndex);
            mEvents.Remove(id);

            if (curIndex >= mEventList.Count)
                curIndex = mEventList.Count - 1;
            if (curIndex < 0)
            {
                curIndex = 0;
                this.Close();
                return;
            }

            UpdateIndex();
            LoadCurrent();
        }

        private void btnIgnore_Click(object sender, RoutedEventArgs e)
        {
            PopEntry();
        }

        private void cmbAccess_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            Program.Config.AccessLevels NetAccess = (Program.Config.AccessLevels)(cmbAccess.SelectedItem as ComboBoxItem).Tag;
            cmbAccess.Background = ProgramControl.GetAccessColor(NetAccess);
            btnApply.IsEnabled = NetAccess != Program.Config.AccessLevels.Unconfigured;
        }

        private bool MakeCustom(ProgramList.ID id, long expiration, ConEntry entry = null)
        {
            FirewallRule rule = new FirewallRule() { guid = Guid.Empty, Profile = (int)Firewall.Profiles.All, Interface = (int)Firewall.Interfaces.All, Enabled = true };
            rule.mID = id;
            rule.Name = Translate.fmt("custom_rule", id.GetDisplayName());
            rule.Grouping = FirewallRule.RuleGroupe;
            rule.Expiration = expiration;

            if (entry != null)
            {
                rule.Direction = entry.Entry.Direction;
                rule.Protocol = entry.Entry.Protocol;
                switch (entry.Entry.Protocol)
                {
                    /*case (int)FirewallRule.KnownProtocols.ICMP:
                    case (int)FirewallRule.KnownProtocols.ICMPv6:
                        
                        break;*/
                    case (int)FirewallRule.KnownProtocols.TCP:
                    case (int)FirewallRule.KnownProtocols.UDP:
                        rule.LocalPorts = "*";
                        rule.RemotePorts = entry.Entry.RemotePort.ToString();
                        break;
                }
                rule.LocalAddresses = "*";
                rule.RemoteAddresses = entry.Entry.RemoteAddress.ToString();
            }
            else
            {
                rule.Direction = Firewall.Directions.Bidirectiona;
            }

            RuleWindow ruleWnd = new RuleWindow(new List<ProgramList.ID>() { id }, rule);
            if (ruleWnd.ShowDialog() != true)
                return false;

            if (!App.itf.UpdateRule(rule))
            {
                MessageBox.Show(Translate.fmt("msg_rule_failed"), App.mName, MessageBoxButton.OK, MessageBoxImage.Exclamation);
                return false;
            }

            return true;
        }

        private long GetExpiration()
        {
            ComboBoxItem remember = (cmbRemember.SelectedItem as ComboBoxItem);
            if (remember != null && (int)remember.Tag != 0)
                return DateTimeOffset.UtcNow.ToUnixTimeSeconds() + (int)remember.Tag;
            return 0;
        }

        private void btnApply_Click(object sender, RoutedEventArgs e)
        {
            ProgramList.ID id = mEventList.ElementAt(curIndex);
            long expiration = GetExpiration();

            Program.Config.AccessLevels NetAccess = (Program.Config.AccessLevels)(cmbAccess.SelectedItem as ComboBoxItem).Tag;

            if (NetAccess == Program.Config.AccessLevels.CustomConfig)
            {
                if (!MakeCustom(id, expiration))
                    return;
            }
            else if (NetAccess == Program.Config.AccessLevels.StopNotify)
            {
                Program prog = App.itf.GetProgram(id);

                if (expiration != 0)
                    prog.config.SilenceUntill = expiration;
                else
                    prog.config.Notify = false;
                App.itf.UpdateProgram(prog.guid, prog.config);
            }
            else
            {
                switch (NetAccess)
                {
                    case Program.Config.AccessLevels.FullAccess:

                        // add and enable allow all rule
                        App.itf.UpdateRule(FirewallRule.MakeAllowRule(id, Firewall.Directions.Bidirectiona, expiration));
                        break;
                    case Program.Config.AccessLevels.LocalOnly:

                        // create block rule only of we operate in blacklist mode
                        //if (App.itf.GetFilteringMode() == Firewall.FilteringModes.BlackList)
                        //{
                            //add and enable block rules for the internet
                            App.itf.UpdateRule(FirewallRule.MakeBlockInetRule(id, Firewall.Directions.Bidirectiona, expiration));
                        //}

                        //add and enable allow rules for the lan
                        App.itf.UpdateRule(FirewallRule.MakeAllowLanRule(id, Firewall.Directions.Bidirectiona, expiration));
                        break;
                    case Program.Config.AccessLevels.BlockAccess:

                        // add and enable broad block rules
                        App.itf.UpdateRule(FirewallRule.MakeBlockRule(id, Firewall.Directions.Bidirectiona, expiration));
                        break;
                }
            }
            PopEntry();
        }

        private void consGrid_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            ConEntry entry = (ConEntry)consGrid.SelectedItem;
            if (entry == null)
                return;

            ProgramList.ID id = mEventList.ElementAt(curIndex);
            long expiration = GetExpiration();
            if (MakeCustom(id, expiration, entry))
                PopEntry();
        }

        public class ConEntry : INotifyPropertyChanged
        {
            public Program.LogEntry Entry;

            public ConEntry(Program.LogEntry entry)
            {
                Entry = entry;
            }

            public string Protocol { get { return NetFunc.Protocol2SStr(Entry.Protocol) + (Entry.Direction == Firewall.Directions.Inbound ? " <<<" : " >>>"); } }

            public string Address { get { if (Entry.RemoteAddress == null || Entry.RemoteAddress.Length == 0) return ""; return Entry.RemoteAddress + ":" + Entry.RemotePort.ToString(); } }

            public string TimeStamp { get { return Entry.TimeStamp.ToString("HH:mm:ss"); } }

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
