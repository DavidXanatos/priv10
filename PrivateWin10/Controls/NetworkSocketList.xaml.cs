using PrivateWin10.Pages;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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
    /// Interaction logic for NetworkSocketList.xaml
    /// </summary>
    public partial class NetworkSocketList : UserControl
    {
        DataGridExt socksGridExt;

        public FirewallPage firewallPage = null;

        FirewallPage.FilterPreset.Socket socketFilter = FirewallPage.FilterPreset.Socket.Any;
        string textFilter = "";

        ObservableCollection<SocketItem> SocketList;

        public NetworkSocketList()
        {
            InitializeComponent();

            WpfFunc.CmbAdd(sockType, Translate.fmt("filter_sockets_all"), FirewallPage.FilterPreset.Socket.Any);
            WpfFunc.CmbAdd(sockType, Translate.fmt("filter_sockets_web"), FirewallPage.FilterPreset.Socket.Web).Background = new SolidColorBrush(Colors.DodgerBlue);
            WpfFunc.CmbAdd(sockType, Translate.fmt("filter_sockets_tcp"), FirewallPage.FilterPreset.Socket.TCP).Background = new SolidColorBrush(Colors.Turquoise);
            WpfFunc.CmbAdd(sockType, Translate.fmt("filter_sockets_client"), FirewallPage.FilterPreset.Socket.Client).Background = new SolidColorBrush(Colors.Gold);
            WpfFunc.CmbAdd(sockType, Translate.fmt("filter_sockets_server"), FirewallPage.FilterPreset.Socket.Server).Background = new SolidColorBrush(Colors.DarkOrange);
            WpfFunc.CmbAdd(sockType, Translate.fmt("filter_sockets_udp"), FirewallPage.FilterPreset.Socket.UDP).Background = new SolidColorBrush(Colors.Violet);
            //WpfFunc.CmbAdd(sockType, Translate.fmt("filter_sockets_raw"), FirewallPage.FilterPreset.Socket.Raw);

            this.lblFilter.Content = Translate.fmt("lbl_filter");
            this.chkNoINet.ToolTip = Translate.fmt("str_no_inet");
            this.chkNoLAN.ToolTip = Translate.fmt("str_no_lan");
            //this.chkNoMulti.ToolTip = Translate.fmt("str_no_multi");
            this.chkNoLocal.ToolTip = Translate.fmt("str_no_local");

            this.socksGrid.Columns[1].Header = Translate.fmt("lbl_name");
            this.socksGrid.Columns[2].Header = Translate.fmt("lbl_time_stamp");
            this.socksGrid.Columns[3].Header = Translate.fmt("lbl_state");
            this.socksGrid.Columns[4].Header = Translate.fmt("lbl_protocol");
            this.socksGrid.Columns[5].Header = Translate.fmt("lbl_remote_ip");
            this.socksGrid.Columns[6].Header = Translate.fmt("lbl_remote_port");
            this.socksGrid.Columns[7].Header = Translate.fmt("lbl_local_ip");
            this.socksGrid.Columns[8].Header = Translate.fmt("lbl_local_port");
            this.socksGrid.Columns[9].Header = Translate.fmt("lbl_access");
            this.socksGrid.Columns[10].Header = Translate.fmt("lbl_upload");
            this.socksGrid.Columns[11].Header = Translate.fmt("lbl_download");
            this.socksGrid.Columns[12].Header = Translate.fmt("lbl_uploaded");
            this.socksGrid.Columns[13].Header = Translate.fmt("lbl_downloaded");
            this.socksGrid.Columns[14].Header = Translate.fmt("lbl_program");

            socksGridExt = new DataGridExt(socksGrid);
            socksGridExt.Restore(App.GetConfig("GUI", "socksGrid_Columns", ""));

            SocketList = new ObservableCollection<SocketItem>();
            socksGrid.ItemsSource = SocketList;

            try
            {
                textFilter = App.GetConfig("NetSocks", "Filter", "");
                txtSockFilter.Text = textFilter;
                WpfFunc.CmbSelect(sockType, ((FirewallPage.FilterPreset.Socket)App.GetConfigInt("NetSocks", "Types", 0)).ToString());
                this.chkNoLocal.IsChecked = App.GetConfigInt("NetSocks", "NoLocal", 0) == 1;
                //this.chkNoMulti.IsChecked = App.GetConfigInt("NetSocks", "NoMulti", 0) == 1;
                this.chkNoLAN.IsChecked = App.GetConfigInt("NetSocks", "NoLan", 0) == 1;
                this.chkNoINet.IsChecked = App.GetConfigInt("NetSocks", "NoINet", 0) == 1;
            }
            catch { }

            CheckSockets();
        }

        public void OnClose()
        {
            App.SetConfig("GUI", "socksGrid_Columns", socksGridExt.Save());
        }

        public void UpdateSockets(bool clear = false)
        {
            if (firewallPage == null)
                return;

            if (clear)
                SocketList.Clear();

            Dictionary<Guid, SocketItem> oldLog = new Dictionary<Guid, SocketItem>();
            foreach (SocketItem oldItem in SocketList)
                oldLog.Add(oldItem.sock.guid, oldItem);

            Dictionary<Guid, List<NetworkSocket>> entries = App.client.GetSockets(firewallPage.GetCurGuids());
            foreach (var entrySet in entries)
            {
                ProgramSet prog = firewallPage.GetProgSet(entrySet.Key);
                if (prog == null)
                    continue;

                foreach (NetworkSocket socket in entrySet.Value)
                {
                    SocketItem entry = null;
                    if (!oldLog.TryGetValue(socket.guid, out entry))
                    {
                        oldLog.Remove(socket.guid);

                        Program program = ProgramList.GetProgramFuzzy(prog.Programs, socket.ProgID, ProgramList.FuzzyModes.Any);

                        SocketList.Insert(0, new SocketItem(socket, program != null ? program.Description : prog.config.Name));
                    }
                    else // update entry
                    {
                        oldLog.Remove(socket.guid);
                        entry.Update(socket);
                    }
                }
            }

            foreach (SocketItem item in oldLog.Values)
                SocketList.Remove(item);


            // force sort
            // todo: improve that
            /*if (socksGrid.Items.SortDescriptions.Count > 0)
            {
                socksGrid.Items.SortDescriptions.Insert(0, socksGrid.Items.SortDescriptions.First());
                socksGrid.Items.SortDescriptions.RemoveAt(0);
            }*/
        }

        private void CheckSockets()
        {
            // todo:
        }

        private void SockGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            CheckSockets();
        }

        static int VALIDATION_DELAY = 1000;
        System.Threading.Timer timer = null;

        private void txtSockFilter_TextChanged(object sender, TextChangedEventArgs e)
        {
            textFilter = txtSockFilter.Text;
            App.SetConfig("NetSocks", "Filter", textFilter);
            //UpdateSockets(true);

            DisposeTimer();
            timer = new System.Threading.Timer(TimerElapsed, null, VALIDATION_DELAY, VALIDATION_DELAY);

        }

        private void TimerElapsed(Object obj)
        {
            this.Dispatcher.Invoke(new Action(() => {
                socksGrid.Items.Filter = new Predicate<object>(item => SocksFilter(item));
            }));
            
            DisposeTimer();
        }

        private void DisposeTimer()
        {
            if (timer != null)
            {
                timer.Dispose();
                timer = null;
            }
        }

        private void sockType_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            socketFilter = (FirewallPage.FilterPreset.Socket)(sockType.SelectedItem as ComboBoxItem).Tag;
            sockType.Background = (sockType.SelectedItem as ComboBoxItem).Background;
            App.SetConfig("NetSocks", "Types", (int)socketFilter);
            //UpdateRules(true);

            socksGrid.Items.Filter = new Predicate<object>(item => SocksFilter(item));
        }

        private void SocketTypeFilters_Click(object sender, RoutedEventArgs e)
        {
            App.SetConfig("NetSocks", "NoLocal", this.chkNoLocal.IsChecked == true ? 1 : 0);
            //App.SetConfig("NetSocks", "NoMulti", this.chkNoMulti.IsChecked == true ? 1 : 0);
            App.SetConfig("NetSocks", "NoLan", this.chkNoLAN.IsChecked == true ? 1 : 0);
            App.SetConfig("NetSocks", "NoINet", this.chkNoINet.IsChecked == true ? 1 : 0);
            //UpdateConnections(true);

            socksGrid.Items.Filter = new Predicate<object>(item => SocksFilter(item));
        }

        private bool SocksFilter(object obj)
        {
            var item = obj as SocketItem;

            if (socketFilter != FirewallPage.FilterPreset.Socket.Any)
            {
                switch (socketFilter)
                {
                    case FirewallPage.FilterPreset.Socket.TCP:
                        if ((item.sock.ProtocolType & (UInt32)IPHelper.AF_PROT.TCP) == 0)
                            return false;
                        break;
                    case FirewallPage.FilterPreset.Socket.Client:
                        if ((item.sock.ProtocolType & (UInt32)IPHelper.AF_PROT.TCP) == 0 || item.sock.State == (int)IPHelper.MIB_TCP_STATE.LISTENING)
                            return false;
                        break;
                    case FirewallPage.FilterPreset.Socket.Server:
                        if ((item.sock.ProtocolType & (UInt32)IPHelper.AF_PROT.TCP) == 0 || item.sock.State != (int)IPHelper.MIB_TCP_STATE.LISTENING)
                            return false;
                        break;
                    case FirewallPage.FilterPreset.Socket.UDP:
                        if ((item.sock.ProtocolType & (UInt32)IPHelper.AF_PROT.UDP) == 0)
                            return false;
                        break;
                    case FirewallPage.FilterPreset.Socket.Web:
                        if ((item.sock.ProtocolType & (UInt32)IPHelper.AF_PROT.TCP) == 0 || !(item.sock.RemotePort == 80 || item.sock.RemotePort == 443))
                            return false;
                        break;
                }
            }

            if (item.sock.RemoteAddress != null)
            {
                if (NetFunc.IsLocalHost(item.sock.RemoteAddress))
                {
                    if (chkNoLocal.IsChecked == true)
                        return false;
                }
                /*else if (NetFunc.IsMultiCast(item.sock.RemoteAddress))
                {
                    if (chkNoMulti.IsChecked == true)
                        return false;
                }*/
                else if (FirewallRule.MatchAddress(item.sock.RemoteAddress, FirewallRule.AddrKeywordLocalSubnet))
                {
                    if (chkNoLAN.IsChecked == true)
                        return false;
                }
                else if (chkNoINet.IsChecked == true)
                    return false;
            }

            if (item.TestFilter(textFilter))
                return false;
            return true;
        }

        /////////////////////////////////
        /// SocketItem

        public class SocketItem : INotifyPropertyChanged
        {
            public NetworkSocket sock;
            public string name;

            public SocketItem(NetworkSocket args, string name)
            {
                this.sock = args;
                this.name = name != null ? name : "[unknown progream]";
            }

            public bool TestFilter(string textFilter)
            {
                string strings = this.Name;
                strings += " " + this.TimeStamp;
                strings += " " + this.State;
                strings += " " + this.Protocol;
                strings += " " + this.DestAddress;
                strings += " " + this.DestPorts;
                strings += " " + this.SrcAddress;
                strings += " " + this.SrcPorts;
                strings += " " + this.Access;
                strings += " " + this.Upload;
                strings += " " + this.Download;
                strings += " " + this.Uploaded;
                strings += " " + this.Downloaded;
                return FirewallPage.DoFilter(textFilter, strings, new List<ProgramID>() { this.sock.ProgID });
            }

            public ImageSource Icon { get { return ImgFunc.GetIcon(sock.ProgID.Path, 16); } }

            public string Name { get { return name; } }
            public string Program { get { return sock.ProgID.FormatString(); } }
            public DateTime TimeStamp { get { return sock.CreationTime; } }
            public string State { get { return sock.GetStateString(); } }

            public string Protocol { get { return ((sock.ProtocolType & 0xff) == (int)NetFunc.KnownProtocols.Any) ? Translate.fmt("pro_any") : NetFunc.Protocol2Str(sock.ProtocolType & 0xff); } }
            public string DestAddress { get {
                    if (sock.HasHostName())
                        return sock.RemoteAddress.ToString() + " (" + sock.GetHostName() + ")";
                    return sock.RemoteAddress?.ToString();
                } }
            public string DestPorts { get { return sock.RemotePort.ToString(); } }
            public string SrcAddress { get { return sock.LocalAddress?.ToString(); } }
            public string SrcPorts { get { return sock.LocalPort.ToString(); } }

            public string Access { get {
                    List<string> profiles = new List<string>();

                    bool CanIn = true;
                    bool CanOut = true;
                    if ((sock.ProtocolType & (UInt32)IPHelper.AF_PROT.TCP) == (UInt32)IPHelper.AF_PROT.TCP)
                    {
                        if (sock.State != (int)IPHelper.MIB_TCP_STATE.LISTENING)
                            CanIn = false;
                        else
                            CanOut = false;
                    }

                    if ((sock.Access.Item1 & (int)FirewallRule.Profiles.Private) != 0 && (sock.Access.Item2 & (int)FirewallRule.Profiles.Private) != 0 && CanIn && CanOut)
                        profiles.Add(Translate.fmt("str_private") + " In/Out");
                    else if ((sock.Access.Item1 & (int)FirewallRule.Profiles.Private) != 0 && CanOut)
                        profiles.Add(Translate.fmt("str_private") + " Out");
                    else if((sock.Access.Item2 & (int)FirewallRule.Profiles.Private) != 0 && CanIn)
                        profiles.Add(Translate.fmt("str_private") + " In");

                    if ((sock.Access.Item1 & (int)FirewallRule.Profiles.Domain) != 0 && (sock.Access.Item2 & (int)FirewallRule.Profiles.Domain) != 0 && CanIn && CanOut)
                        profiles.Add(Translate.fmt("str_domain") + " In/Out");
                    else if ((sock.Access.Item1 & (int)FirewallRule.Profiles.Domain) != 0 && CanOut)
                        profiles.Add(Translate.fmt("str_domain") + " Out");
                    else if ((sock.Access.Item2 & (int)FirewallRule.Profiles.Domain) != 0 && CanIn)
                        profiles.Add(Translate.fmt("str_domain") + " In");

                    if ((sock.Access.Item1 & (int)FirewallRule.Profiles.Public) != 0 && (sock.Access.Item2 & (int)FirewallRule.Profiles.Public) != 0 && CanIn && CanOut)
                        profiles.Add(Translate.fmt("str_public") + " In/Out");
                    else if ((sock.Access.Item1 & (int)FirewallRule.Profiles.Public) != 0 && CanOut)
                        profiles.Add(Translate.fmt("str_public") + " Out");
                    else if ((sock.Access.Item2 & (int)FirewallRule.Profiles.Public) != 0 && CanIn)
                        profiles.Add(Translate.fmt("str_public") + " In");

                    return string.Join(",", profiles.ToArray().Reverse());
                } }

            public UInt64 Upload { get { return sock.Stats.UploadRate.ByteRate; } }
            
            public UInt64 Download { get { return sock.Stats.UploadRate.ByteRate; } }

            public UInt64 Uploaded { get { return sock.Stats.SentBytes; } }

            public UInt64 Downloaded { get { return sock.Stats.ReceivedBytes; } }

            void UpdateValue<T>(ref T value, T new_value, string Name)
            {
                if (value == null ? new_value == null : value.Equals(new_value))
                    return;
                value = new_value;
                NotifyPropertyChanged(Name);
            }

            public void Update(NetworkSocket new_sock)
            {
                UpdateValue(ref sock.CreationTime, new_sock.CreationTime, "TimeStamp");
                UpdateValue(ref sock.State, new_sock.State, "State");
                UpdateValue(ref sock.RemoteAddress, new_sock.RemoteAddress, "DestAddress");
                UpdateValue(ref sock.RemotePort, new_sock.RemotePort, "DestPorts");
                UpdateValue(ref sock.LocalAddress, new_sock.LocalAddress, "SrcAddress");
                UpdateValue(ref sock.LocalPort, new_sock.LocalPort, "SrcPorts");

                if(sock.Update(new_sock))
                    NotifyPropertyChanged("DestAddress");

                if (!sock.Stats.Equals(new_sock.Stats))
                {
                    sock.Stats = new_sock.Stats;
                    NotifyPropertyChanged("Upload");
                    NotifyPropertyChanged("Download");
                    NotifyPropertyChanged("Uploaded");
                    NotifyPropertyChanged("Downloaded");
                    // todo: other
                }
            }

            #region INotifyPropertyChanged Members

            public event PropertyChangedEventHandler PropertyChanged;

            #endregion

            #region Private Helpers

            private void NotifyPropertyChanged(string propertyName)
            {
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
            }

            #endregion
        }
    }
}
