using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
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

namespace PrivateWin10.Pages
{
    /// <summary>
    /// Interaction logic for OverviewPage.xaml
    /// </summary>
    public partial class OverviewPage : UserControl, IUserPage
    {
        DataGridExt logGridExt;

        public OverviewPage()
        {
            InitializeComponent();

            this.logGrid.Columns[0].Header = Translate.fmt("lbl_log_level");
            this.logGrid.Columns[1].Header = Translate.fmt("lbl_time_stamp");
            this.logGrid.Columns[2].Header = Translate.fmt("lbl_log_type");
            this.logGrid.Columns[3].Header = Translate.fmt("lbl_log_event");
            this.logGrid.Columns[4].Header = Translate.fmt("lbl_message");

            logGridExt = new DataGridExt(logGrid);
            logGridExt.Restore(App.GetConfig("GUI", "logGrid_Columns", ""));

            double logRowHeight = MiscFunc.parseDouble(App.GetConfig("GUI", "EventLogHeight", "0.0"));
            if (logRowHeight > 0.0)
                logRow.Height = new GridLength(logRowHeight, GridUnitType.Pixel);

            foreach (var entry in App.Log.GetFullLog())
            {
                logGrid.Items.Insert(0, new LogItem(entry));
            }

            App.Log.LogEvent += (object sender, AppLog.LogEventArgs args) => {
                this.Dispatcher.InvokeAsync(new Action(()=> {
                    OnLogEvent(args.entry);
                }));
            };
        }


        public void OnShow()
        {
            string running = Translate.fmt("lbl_run_as", Translate.fmt(AdminFunc.IsAdministrator() ? "str_admin" : "str_user"));
            if (App.svc.IsInstalled())
                running += Translate.fmt("lbl_run_svc");
            lblRunning.Content = running;

            lblFirewallInfo.Content = Translate.fmt((App.GetConfigInt("Firewall", "Enabled", 0) != 0) ? "str_enabled" : "str_disabled");
            // filterming mode
            // current profile
            lblRuleGuardInfo.Content = Translate.fmt(App.client.IsFirewallGuard() ? "str_enabled" : "str_disabled");
            // changed rules

            lblTweakGuardInfo.Content = Translate.fmt(App.GetConfigInt("TweakGuard", "AutoCheck", 1) != 0 ? "str_enabled" : "str_disabled");
            // mode
            // changed tweaks
        }

        public void OnHide()
        {
        }

        public void OnClose()
        {
            App.SetConfig("GUI", "logGrid_Columns", logGridExt.Save());

            App.SetConfig("GUI", "EventLogHeight", ((int)logRow.ActualHeight).ToString());
        }

        void OnLogEvent(AppLog.LogEntry entry)
        {
            logGrid.Items.Insert(0, new LogItem(entry));

            if (App.GetConfigInt("GUI", "ShowNotifications", 1) == 0)
                return;

            if ((entry.categoryID & (short)App.EventFlags.Notifications) != 0)
            {
                System.Windows.Forms.ToolTipIcon tipIcon = System.Windows.Forms.ToolTipIcon.Info;
                if(entry.entryType == EventLogEntryType.Error)
                    tipIcon = System.Windows.Forms.ToolTipIcon.Error;
                else if (entry.entryType == EventLogEntryType.Warning)
                    tipIcon = System.Windows.Forms.ToolTipIcon.Warning;
                App.mTray.Notify(entry.strMessage, tipIcon);
            }

            // todo: use wone window with a log
            /*if ((entry.categoryID & (short)App.EventFlags.PopUpMessages) != 0)
            {
                MessageBoxImage boxIcon = MessageBoxImage.Information;
                if (entry.entryType == EventLogEntryType.Error)
                    boxIcon = MessageBoxImage.Warning;
                else if (entry.entryType == EventLogEntryType.Warning)
                    boxIcon = MessageBoxImage.Error;
                MessageBox.Show(entry.strMessage, App.mName, MessageBoxButton.OK, boxIcon);
            }*/
        }

        private void LogGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {

        }

        /////////////////////////////////
        /// LogItem

        public class LogItem : INotifyPropertyChanged
        {
            public AppLog.LogEntry args;

            public LogItem(AppLog.LogEntry args)
            {
                this.args = args;
            }

            private ImageSource icoError = ImgFunc.ToImageSource(SystemIcons.Error);
            private ImageSource icoWarning = ImgFunc.ToImageSource(SystemIcons.Warning);
            private ImageSource icoInfo = ImgFunc.ToImageSource(SystemIcons.Information);

            public string Level { get {
                    switch (args.entryType)
                    {
                        case EventLogEntryType.Error: return Translate.fmt("log_error");
                        case EventLogEntryType.Warning: return Translate.fmt("log_warning");
                        default: return Translate.fmt("log_info");
                    }
                } }
            public ImageSource Icon { get {
                    switch (args.entryType)
                    {
                        case EventLogEntryType.Error: return icoError;
                        case EventLogEntryType.Warning: return icoWarning;
                        default: return icoInfo;
                    }
                } }
            public DateTime TimeStamp { get { return args.timeGenerated; } }
            public string Category { get {
                    if (args.eventID >= (int)App.EventIDs.FirewallBegin && args.eventID <= (int)App.EventIDs.FirewallEnd)
                        return Translate.fmt("log_firewall");
                    if (args.eventID >= (int)App.EventIDs.TweakBegin && args.eventID <= (int)App.EventIDs.TweakEnd)
                        return Translate.fmt("log_tweaks");
                    return Translate.fmt("log_other");
                } }
            public string Event { get { return ((App.EventIDs)args.eventID).ToString(); } } // Todo
            public string Message { get { return args.strMessage; } }

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
