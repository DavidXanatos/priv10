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

        string mSockFilter = "";

        ObservableCollection<SocketItem> SocketList;

        public NetworkSocketList()
        {
            InitializeComponent();

            this.socksGrid.Columns[1].Header = Translate.fmt("lbl_name");
            this.socksGrid.Columns[2].Header = Translate.fmt("lbl_time_stamp");
            this.socksGrid.Columns[3].Header = Translate.fmt("lbl_state");
            this.socksGrid.Columns[4].Header = Translate.fmt("lbl_protocol");
            this.socksGrid.Columns[5].Header = Translate.fmt("lbl_remote_ip");
            this.socksGrid.Columns[6].Header = Translate.fmt("lbl_remote_port");
            this.socksGrid.Columns[7].Header = Translate.fmt("lbl_local_ip");
            this.socksGrid.Columns[8].Header = Translate.fmt("lbl_local_port");
            this.socksGrid.Columns[9].Header = Translate.fmt("lbl_upload");
            this.socksGrid.Columns[10].Header = Translate.fmt("lbl_download");
            this.socksGrid.Columns[11].Header = Translate.fmt("lbl_program");

            socksGridExt = new DataGridExt(socksGrid);
            socksGridExt.Restore(App.GetConfig("GUI", "socksGrid_Columns", ""));

            SocketList = new ObservableCollection<SocketItem>();
            socksGrid.ItemsSource = SocketList;

            mSockFilter = App.GetConfig("GUI", "SockFilter", "");
            txtSockFilter.Text = mSockFilter;

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

            Dictionary<Guid, List<NetworkSocket>> entries = App.client.GetSockets(firewallPage.GetCurGuids(mSockFilter));
            foreach (var entrySet in entries)
            {
                ProgramControl item = null;
                ProgramSet prog = firewallPage.GetProgSet(entrySet.Key, null, out item);
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
            /*if (sockGrid.Items.SortDescriptions.Count > 0)
            {
                sockGrid.Items.SortDescriptions.Insert(0, sockGrid.Items.SortDescriptions.First());
                sockGrid.Items.SortDescriptions.RemoveAt(0);
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

        private void txtSockFilter_TextChanged(object sender, TextChangedEventArgs e)
        {
            mSockFilter = txtSockFilter.Text;
            App.SetConfig("GUI", "SockFilter", mSockFilter);
            UpdateSockets(true);
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

            void DoUpdate()
            {
                NotifyPropertyChanged(null);
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


            public UInt64 Upload { get { return sock.Stats.UploadRate.ByteRate; } }
            //public string UploadTxt { get { return FileOps.FormatSize(sock.Stats.UploadRate.ByteRate) + "/s"; } }
            public UInt64 Download { get { return sock.Stats.UploadRate.ByteRate; } }
            //public string DownloadTxt { get { return FileOps.FormatSize(sock.Stats.UploadRate.ByteRate) + "/s"; } }

            void UpdateValue<T>(ref T value, T new_value, string Name)
            {
                if (value == null ? new_value == null : value.Equals(new_value))
                    return;
                value = new_value;
                NotifyPropertyChanged(Name);
            }

            internal void Update(NetworkSocket new_sock)
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
                    // todo: other
                }
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
