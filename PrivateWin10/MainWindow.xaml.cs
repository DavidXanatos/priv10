using PrivateWin10.Pages;
using PrivateWin10.Windows;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace PrivateWin10
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        #region Win32 API Stuff

        // Define the Win32 API methods we are going to use
        [DllImport("user32.dll")]
        private static extern IntPtr GetSystemMenu(IntPtr hWnd, bool bRevert);

        [DllImport("user32.dll")]
        private static extern bool InsertMenu(IntPtr hMenu, Int32 wPosition, Int32 wFlags, Int32 wIDNewItem, string lpNewItem);

        /// Define our Constants we will use
        public const Int32 WM_SYSCOMMAND = 0x112;
        public const Int32 MF_SEPARATOR = 0x800;
        public const Int32 MF_BYPOSITION = 0x400;
        public const Int32 MF_STRING = 0x0;

        /// <summary>
        /// This is the Win32 Interop Handle for this Window
        /// </summary>
        public IntPtr Handle
        {
            get
            {
                return new WindowInteropHelper(this).Handle;
            }
        }


        private void UpdateSysMenu(object sender, RoutedEventArgs e)
        {
            /// Get the Handle for the Forms System Menu
            IntPtr systemMenuHandle = GetSystemMenu(this.Handle, false);

            /// Create our new System Menu items just before the Close menu item
            InsertMenu(systemMenuHandle, 5, MF_BYPOSITION | MF_SEPARATOR, 0, string.Empty); // <-- Add a menu seperator
            InsertMenu(systemMenuHandle, 6, MF_BYPOSITION, _SettingsSysMenuID, "Settings...");
            InsertMenu(systemMenuHandle, 7, MF_BYPOSITION, _AboutSysMenuID, "About...");

            // Attach our WndProc handler to this Window
            HwndSource source = HwndSource.FromHwnd(this.Handle);
            source.AddHook(new HwndSourceHook(WndProc));
        }

        private static IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            // Check if a System Command has been executed
            if (msg == WM_SYSCOMMAND)
            {
                // Execute the appropriate code for the System Menu item that was clicked
                switch (wParam.ToInt32())
                {
                    case _SettingsSysMenuID:
                        MessageBox.Show("\"Settings\" was clicked");
                        handled = true;
                        break;
                    case _AboutSysMenuID:
                        MessageBox.Show("\"About\" was clicked");
                        handled = true;
                        break;
                }
            }

            return IntPtr.Zero;
        }


        // The constants we'll use to identify our custom system menu items
        public const Int32 _SettingsSysMenuID = 1000;
        public const Int32 _AboutSysMenuID = 1001;


        #endregion

        private Dictionary<String, UserControl> mPages = new Dictionary<String, UserControl>();
        UserControl mCurPage = null;

        public MainWindow()
        {
            InitializeComponent();

            //if (!MiscFunc.IsRunningAsUwp())
            this.Title = string.Format("{0} v{1} by David Xanatos", App.mName, App.mVersion);
            if (!App.lic.CommercialUse)
                this.Title += " - for Private non Commercial Use";

            WpfFunc.LoadWnd(this, "Main");

            mPages.Add("Overview", new OverviewPage());
            mPages.Add("Privacy", new PrivacyPage());
            if(App.client.IsConnected())
                mPages.Add("Firewall", new FirewallPage());
            else
                mPages.Add("Firewall", null);
            //mPages.Add("VPN", new VPNPage());
            mPages.Add("Settings", new SettingsPage());
            mPages.Add("About", new AboutPage());

            foreach (UserControl page in mPages.Values)
            {
                if (page == null)
                    continue;
                page.Visibility = Visibility.Collapsed;
                this.Main.Children.Add(page);
            }

            Brush brushOn = (TryFindResource("SidePanel.on") as Brush);
            Brush brushOff = (TryFindResource("SidePanel.off") as Brush);
            foreach (string name in mPages.Keys)
            {
                /*TabItem item = new TabItem();
                item.KeyDown += SidePanel_Click;
                item.MouseLeftButtonUp += SidePanel_Click;
                item.Name = "PanelItem_" + name;
                item.Style = (TryFindResource("SidePanelItem") as Style);

                StackPanel panel = new StackPanel();

                Image image = new Image();
                image.Width = 32;
                image.Height = 32;
                image.Name = "PanelItem_" + name + "_Image";

                panel.Children.Add(image);
                item.Content = panel;
                this.SidePanel.Items.Add(item);*/

                Geometry geometry = (TryFindResource("Icon_" + name) as Geometry);
                Image image = (FindName("PanelItem_" + name + "_Image") as Image);
                image.Tag = new Tuple<DrawingImage, DrawingImage>(new DrawingImage(new GeometryDrawing(brushOn, null, geometry)), new DrawingImage(new GeometryDrawing(brushOff, null, geometry)));
            }

#if DEBUG
            SwitchPage("Firewall");
#else
            SwitchPage("Overview");
#endif

        }

    private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            WpfFunc.StoreWnd(this, "Main");

            foreach (IUserPage page in mPages.Values)
            {
                if(page != null)
                    page.OnClose();
            }

            if (App.mTray.Visible)
            {
                e.Cancel = true;
                this.Hide();
            }
        }

        private void Window_Closed(object sender, EventArgs e)
        {

        }

        public void SidePanel_Click(object sender, RoutedEventArgs e)
        {
            if (e.ToString() == "System.Windows.Input.KeyEventArgs")
            {
                KeyEventArgs keyEventArgs = e as KeyEventArgs;
                if (!(keyEventArgs.Key.ToString() == "Space") && !(keyEventArgs.Key.ToString() == "Return"))
                    return;
            }

            string name = TextHelpers.get2nd((sender as Control).Name, "_");
            SwitchPage(name);
        }

        public void SwitchPage(string name)
        {
            if (mCurPage != null)
            {
                mCurPage.Visibility = Visibility.Collapsed;
                (mCurPage as IUserPage).OnHide();
            }
            if (!mPages.TryGetValue(name, out UserControl page))
                return;
            if (page == null)
                return;
            mCurPage = page;
            mCurPage.Visibility = Visibility.Visible;
            (mCurPage as IUserPage).OnShow();
            foreach (TabItem item in this.SidePanel.Items)
            {
                bool isThis = item.Name == "PanelItem_" + name;

                (FindName(item.Name + "_Pin") as Path).Visibility = isThis ? Visibility.Visible : Visibility.Hidden;

                Image image = (FindName(item.Name + "_Image") as Image);
                image.Source = isThis ? (image.Tag as Tuple<DrawingImage, DrawingImage>).Item1 : (image.Tag as Tuple<DrawingImage, DrawingImage>).Item2;
            }
        }

        long lastReminder = 0;

        private void Window_Activated(object sender, EventArgs e)
        {
            if (App.lic.LicenseStatus == QLicense.LicenseStatus.VALID)
                return;

            long curTime = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

            if (lastReminder == 0) {
                long InstallAge = curTime - App.GetInstallDate();
                if (InstallAge < 5 * 24 * 3600) // 5 days old or younger
                    lastReminder = curTime + 2 * 3600; // first reminder after 3 hours
                else if (InstallAge < 10 * 24 * 3600) // 10 to 5 days old
                    lastReminder = curTime + 1 * 3600; // first reminder after 2 hours
            }

            if (lastReminder + 1 * 3600 < curTime) // reind every hour
            {
                lastReminder = curTime;

                Reminder reminder = new Reminder();
                reminder.Owner = this;
                if (reminder.ShowDialog() == true) // user promissed to support
                    lastReminder = curTime + 5 * 3600; // for a total of 6h no reminding
            }
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            //UpdateSysMenu(); // Install system menu

            if (App.GetConfigInt("Startup", "ShowSetup", 1) == 1 && !(App.IsAutoStart() || App.svc.IsInstalled()))
            {
                SetupWnd wnd = new SetupWnd();
                wnd.Owner = this;
                wnd.ShowDialog();
            }
        }
    }
}