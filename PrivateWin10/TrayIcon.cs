using System;
using System.Diagnostics;
using System.Windows.Forms;
using System.ComponentModel;
using System.Windows.Threading;
using System.Windows;
using MessageBox = System.Windows.MessageBox;
using Application = System.Windows.Application;
using MiscHelpers;
using System.Drawing;

namespace PrivateWin10
{
    public class TrayIcon
    {
        private NotifyIcon notifyIcon;
        private ContextMenu contextMenu;
        
        private MenuItem menuBlock;
        private MenuItem menuWhitelist;
        private MenuItem menuBlacklist;
        private MenuItem menuFwDisabled;

        private MenuItem menuPresets;

        private MenuItem menuExit;

        private IContainer components;

        DispatcherTimer mTimer = new DispatcherTimer();

        Icon icon;
        Icon icon_ex;

        public TrayIcon()
        {
            this.components = new Container();
            this.contextMenu = new ContextMenu();

            this.menuBlock = new MenuItem() { Text = Translate.fmt("mnu_block") };
            this.menuBlock.Click += new System.EventHandler(menuBlock_Click);

            this.menuWhitelist = new MenuItem() { Text = Translate.fmt("mnu_whitelist") };
            this.menuWhitelist.Click += new System.EventHandler(menuMode_Click);

            this.menuBlacklist = new MenuItem() { Text = Translate.fmt("mnu_blacklist") };
            this.menuBlacklist.Click += new System.EventHandler(menuMode_Click);

            this.menuFwDisabled = new MenuItem() { Text = Translate.fmt("mnu_open_fw") };
            this.menuFwDisabled.Click += new System.EventHandler(menuMode_Click);

            UpdateFwMode();

            this.menuPresets = new MenuItem() { Text = Translate.fmt("mnu_presets") };

            App.client.SettingsChangedNotification += Client_SettingsChangedNotification;

            UpdatePresets();

            App.presets.PresetChange += OnPresetChanged;

            // Initialize menuItem1
            this.menuExit = new MenuItem() { Text = Translate.fmt("mnu_exit") };
            this.menuExit.Click += new System.EventHandler(this.menuExit_Click);

            // Initialize contextMenu1
            this.contextMenu.MenuItems.AddRange(new MenuItem[] { 
                this.menuBlock, 
                new MenuItem("-"), 
                this.menuWhitelist, 
                this.menuBlacklist, 
                this.menuFwDisabled, 
                new MenuItem("-"),  
                this.menuPresets, 
                new MenuItem("-"), 
                this.menuExit 
            });

            // Create the NotifyIcon.
            this.notifyIcon = new NotifyIcon(this.components);

            // The Icon property sets the icon that will appear
            // in the systray for this application.
            notifyIcon.Icon = System.Drawing.Icon.ExtractAssociatedIcon(App.exePath);

            // The ContextMenu property sets the menu that will
            // appear when the systray icon is right clicked.
            notifyIcon.ContextMenu = this.contextMenu;

            // The Text property sets the text that will be displayed,
            // in a tooltip, when the mouse hovers over the systray icon.
            notifyIcon.Text = FileVersionInfo.GetVersionInfo(App.exePath).FileDescription;

            // Handle the DoubleClick event to activate the form.
            notifyIcon.DoubleClick += new System.EventHandler(this.notifyIcon_DoubleClick);
            notifyIcon.Click += new System.EventHandler(this.notifyIcon_Click);

            System.IO.Stream iconStream = Application.GetResourceStream(new Uri("pack://application:,,,/PrivateWin10;component/Resources/icons8-major.png")).Stream;
            using (var bitmap = new Bitmap(iconStream))
            {
                var iconHandle = bitmap.GetHicon();
                icon = Icon.FromHandle(iconHandle);
            }

            System.IO.Stream iconStream_ex = Application.GetResourceStream(new Uri("pack://application:,,,/PrivateWin10;component/Resources/icons8-major_ex_red.png")).Stream;
            using (var bitmap_ex = new Bitmap(iconStream_ex))
            {
                var iconHandle_ex = bitmap_ex.GetHicon();
                icon_ex = Icon.FromHandle(iconHandle_ex);
            }

            mTimer.Tick += new EventHandler(OnTimerTick);
            mTimer.Interval = new TimeSpan(0, 0, 0, 0, 500);
            mTimer.Start();
        }

        // todo dispose and DestroyIcon(newIcon.Handle); 

        private void Client_SettingsChangedNotification(object sender, EventArgs e)
        {
            // todo: fix-me somehow notice also rule removel from block internet mode
            UpdateMode();
        }

        private void UpdateFwMode()
        {
            ProgramID id = ProgramID.NewID(ProgramID.Types.Global);
            ProgramSet prog = App.client.GetProgram(id, true);
            if (prog == null)
                this.menuBlock.Enabled = false;
            else
                this.menuBlock.Checked = prog.config.CurAccess == ProgramSet.Config.AccessLevels.BlockAccess;


            UpdateMode();
        }

        private void UpdateMode()
        {
            var mode = App.client.GetFilteringMode();

            this.menuWhitelist.Checked = mode == FirewallManager.FilteringModes.WhiteList;
            this.menuBlacklist.Checked = mode == FirewallManager.FilteringModes.BlackList;
            this.menuFwDisabled.Checked = mode == FirewallManager.FilteringModes.NoFiltering;
        }
        private void menuBlock_Click(object Sender, EventArgs e)
        {
            this.menuBlock.Checked = !this.menuBlock.Checked;

            App.client.BlockInternet(this.menuBlock.Checked);
        }

        private void menuMode_Click(object Sender, EventArgs e)
        {
            var mode = FirewallManager.FilteringModes.Unknown;
            if (Sender == this.menuWhitelist) mode = FirewallManager.FilteringModes.WhiteList;
            else if (Sender == this.menuBlacklist) mode = FirewallManager.FilteringModes.BlackList;
            else if (Sender == this.menuFwDisabled) mode = FirewallManager.FilteringModes.NoFiltering;
            else
                return;

            App.client.SetFilteringMode(mode);
            UpdateMode();
        }

        private void OnPresetChanged(object sender, PresetManager.PresetChangeArgs args)
        {
            if (args.preset == null)
            {
                UpdatePresets();
                return;
            }

            foreach (MenuItem item in menuPresets.MenuItems)
            {
                if (args.preset.guid.Equals((Guid)item.Tag))
                {
                    item.Name = args.preset.Name;
                    item.Checked = args.preset.State;
                    break;
                }
            }
        }

        private void UpdatePresets()
        {
            this.menuPresets.MenuItems.Clear();

            foreach (var preset in App.presets.Presets.Values)
            {
                var menuItem = new MenuItem() { Text = preset.Name };
                menuItem.Click += new System.EventHandler(this.menuPreset_Click);
                menuItem.Tag = preset.guid;
                menuItem.Checked = preset.State;
                menuPresets.MenuItems.Add(menuItem);
            }
        }

        private void menuPreset_Click(object Sender, EventArgs e)
        {
            App.presets.SetPreset((Guid)((MenuItem)Sender).Tag, !((MenuItem)Sender).Checked);
        }

        int TickTock = 0;

        private void OnTimerTick(object sender, EventArgs e)
        {
            if (clicked)
            {
                clicked = false;

                if (App.MainWnd == null || !App.MainWnd.FullyLoaded)
                    return;

                if (App.MainWnd.notificationWnd.IsVisible)
                    App.MainWnd.notificationWnd.HideWnd();
                else if (!App.MainWnd.notificationWnd.IsEmpty())
                    App.MainWnd.notificationWnd.ShowWnd();
            }

            if (TickTock != 0)
            {
                TickTock = 0;
                notifyIcon.Icon = icon;
            }
            else if(!App.MainWnd.notificationWnd.IsEmpty())
            {
                TickTock = 1;
                notifyIcon.Icon = icon_ex;
            }
        }

        public bool Visible { get { return notifyIcon.Visible; } set { notifyIcon.Visible = value; } }

        public void DestroyNotifyicon()
        {
            notifyIcon.Visible = false;
            notifyIcon.Dispose();
        }

        public void Notify(string Message, ToolTipIcon Icon = ToolTipIcon.Info)
        {
            notifyIcon.ShowBalloonTip(5000, App.Title, Message, Icon);
        }

        bool clicked = false;

        private void notifyIcon_Click(object Sender, EventArgs e)
        {
            if ((e as MouseEventArgs).Button != MouseButtons.Left)
                return;

            clicked = true;
        }

        private void notifyIcon_DoubleClick(object Sender, EventArgs e)
        {
            if ((e as MouseEventArgs).Button != MouseButtons.Left)
                return;

            if (App.MainWnd == null || !App.MainWnd.FullyLoaded)
                return;

            if (App.MainWnd.IsVisible)
                App.MainWnd.Hide();
            else
                App.MainWnd.Show();
        }

        private void menuExit_Click(object Sender, EventArgs e)
        {
            //notifyIcon1.Visible = false;

            // Close the form, which closes the application.
            //Application.Exit();

            if (Priv10Service.IsInstalled() && AdminFunc.IsAdministrator())
            {
                MessageBoxResult res = MessageBox.Show(Translate.fmt("msg_stop_svc"), App.Title, MessageBoxButton.YesNoCancel, MessageBoxImage.Question);
                switch (res)
                {
                    case MessageBoxResult.Yes:
                        if(!Priv10Service.Terminate())
                            MessageBox.Show(Translate.fmt("msg_stop_svc_err"), App.Title, MessageBoxButton.OK, MessageBoxImage.Stop);
                        break;
                    case MessageBoxResult.Cancel:
                        return;
                }
            }
            Application.Current.Shutdown();
        }
    }
}