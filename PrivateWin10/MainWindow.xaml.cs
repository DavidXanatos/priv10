using PrivateWin10.Pages;
using PrivateWin10.Windows;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
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
        public const Int32 MF_ENABLED = 0x02;
        public const Int32 MF_DISABLED = 0x00;
        public const Int32 MF_STRING = 0x00;

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

        // The constants we'll use to identify our custom system menu items
        public const Int32 SysMenu_Setup = 1000;
        public const Int32 SysMenu_Uninstall = 1001;

        private void UpdateSysMenu()
        {
            /// Get the Handle for the Forms System Menu
            IntPtr systemMenuHandle = GetSystemMenu(this.Handle, false);

            /// Create our new System Menu items just before the Close menu item
            InsertMenu(systemMenuHandle, 5, MF_BYPOSITION | MF_SEPARATOR, 0, string.Empty); // <-- Add a menu seperator
            InsertMenu(systemMenuHandle, 6, MF_BYPOSITION | (AdminFunc.IsAdministrator() ? MF_DISABLED : MF_ENABLED), SysMenu_Setup, Translate.fmt("menu_setup"));
            InsertMenu(systemMenuHandle, 7, MF_BYPOSITION | (AdminFunc.IsAdministrator() ? MF_DISABLED : MF_ENABLED), SysMenu_Uninstall, Translate.fmt("menu_uninstall"));

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
                    case SysMenu_Setup:
                        App.mMainWnd.ShowSetup();
                        handled = true;
                        break;
                    case SysMenu_Uninstall:
                        App.mMainWnd.RunUninstall();
                        handled = true;
                        break;
                }
            }

            return IntPtr.Zero;
        }
        #endregion

        public class PageItem
        {
            public PageItem(UserControl ctrl) { this.ctrl = ctrl; }
            public UserControl ctrl;
            public TabItem tab;
        }

        private Dictionary<String, PageItem> mPages = new Dictionary<String, PageItem>();
        UserControl mCurPage = null;

        public MainWindow()
        {
            InitializeComponent();

            //if (!MiscFunc.IsRunningAsUwp())
            this.Title = string.Format("{0} v{1} by David Xanatos", App.mName, App.mVersion);
            if (!App.lic.CommercialUse)
                this.Title += " - Freeware for Private NOT Commercial Use";

            WpfFunc.LoadWnd(this, "Main");

            bool HasEngine = App.client.IsConnected();
            mPages.Add("Overview", new PageItem(new OverviewPage()));
            mPages.Add("Privacy", new PageItem(new PrivacyPage()));
            mPages.Add("Firewall", new PageItem(HasEngine ? new FirewallPage() : null));
            mPages.Add("Dns", new PageItem(new DnsPage()));
            //mPages.Add("VPN", new PageItem(new VPNPage()));
            mPages.Add("Settings", new PageItem(new SettingsPage()));
            mPages.Add("About", new PageItem(new AboutPage()));

            foreach (var page in mPages.Values)
            {
                if (page.ctrl != null)
                {
                    page.ctrl.Visibility = Visibility.Collapsed;
                    this.Main.Children.Add(page.ctrl);
                }
            }

            Brush brushOn = (TryFindResource("SidePanel.on") as Brush);
            Brush brushOff = (TryFindResource("SidePanel.off") as Brush);
            foreach (var val in mPages)
            {
                string name = val.Key;

                TabItem item = new TabItem();
                val.Value.tab = item;
                this.SidePanel.Items.Add(item);

                item.KeyDown += SidePanel_Click;
                item.MouseLeftButtonUp += SidePanel_Click;
                item.Name = "PanelItem_" + name;
                item.Style = (TryFindResource("SidePanelItem") as Style);

                StackPanel panel = new StackPanel();
                item.Header = panel;

                Image image = new Image();
                image.Width = 32;
                image.Height = 32;
                image.SnapsToDevicePixels = true;
                image.Name = "PanelItem_" + name + "_Image";
                panel.Children.Add(image);

                Path pin = new Path();
                pin.Width = 4;
                pin.Height = 24;
                pin.Margin = new Thickness(-43, -32, 0, 0);
                pin.Fill = TryFindResource("SidePanel.Pin") as SolidColorBrush;
                pin.IsHitTestVisible = false;
                pin.Name = "PanelItem_" + name + "_Pin";
                pin.Data = new RectangleGeometry(new Rect(new Point(0, 0), new Point(4, 24)));
                panel.Children.Add(pin);

                Geometry geometry = (TryFindResource("Icon_" + name) as Geometry);
                image.Tag = new Tuple<DrawingImage, DrawingImage>(new DrawingImage(new GeometryDrawing(brushOn, null, geometry)), new DrawingImage(new GeometryDrawing(brushOff, null, geometry)));
            }

            SwitchPage(App.GetConfig("GUI", "CurPage", "Overview"));

            UpdateEnabled();
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            WpfFunc.StoreWnd(this, "Main");

            foreach (var page in mPages.Values)
            {
                if (page.ctrl != null)
                    (page.ctrl as IUserPage).OnClose();
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

        public void UpdateEnabled()
        {
            bool HasEngine = App.client.IsConnected();
            foreach (var val in mPages)
            {
                string name = val.Key;

                if (name == "Firewall")
                    val.Value.tab.IsEnabled = HasEngine;
                else if (name == "Dns")
                    val.Value.tab.IsEnabled = HasEngine && App.GetConfigInt("DnsProxy", "Enabled", 0) != 0;
            }
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
            App.SetConfig("GUI", "CurPage", name);
            SwitchPage(name);
        }

        public void SwitchPage(string name)
        {
            if (mCurPage != null)
            {
                mCurPage.Visibility = Visibility.Collapsed;
                (mCurPage as IUserPage).OnHide();
            }
            PageItem page;
            if (!mPages.TryGetValue(name, out page) || !page.tab.IsEnabled || page.ctrl == null)
                return;
            mCurPage = page.ctrl;
            mCurPage.Visibility = Visibility.Visible;
            (mCurPage as IUserPage).OnShow();
            foreach (TabItem item in this.SidePanel.Items)
            {
                bool isThis = item.Name == "PanelItem_" + name;

                //(FindName(item.Name + "_Pin") as Path).Visibility = isThis ? Visibility.Visible : Visibility.Hidden;
                ((item.Header as StackPanel).Children[1] as Path).Visibility = isThis ? Visibility.Visible : Visibility.Hidden;

                //Image image = (FindName(item.Name + "_Image") as Image);
                Image image = ((item.Header as StackPanel).Children[0] as Image);
                image.Source = isThis ? (image.Tag as Tuple<DrawingImage, DrawingImage>).Item1 : (image.Tag as Tuple<DrawingImage, DrawingImage>).Item2;
            }
        }

        //long lastReminder = 0;

        private void Window_Activated(object sender, EventArgs e)
        {
            // todo: 
            /*if (App.lic.LicenseStatus == QLicense.LicenseStatus.VALID)
                return;

            UInt64 curTime = MiscFunc.GetUTCTime();

            if (lastReminder == 0)
            {
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
            }*/
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            UpdateSysMenu(); // Install system menu

            if (AdminFunc.IsAdministrator() && App.GetConfigInt("Startup", "ShowSetup", 1) == 1 && !App.svc.IsInstalled())
                ShowSetup();
        }

        private void ShowSetup()
        {
            SetupWnd wnd = new SetupWnd();
            wnd.Owner = this;
            wnd.ShowDialog();
        }

        private void RunUninstall()
        {
            if (MessageBox.Show(Translate.fmt("msg_uninstall_this", App.mName), App.mName, MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.No)
                return;

            var exeName = Process.GetCurrentProcess().MainModule.FileName;
            string arguments = "-console -uninstall -wait";
            ProcessStartInfo startInfo = new ProcessStartInfo(exeName, arguments);
            startInfo.UseShellExecute = true;
            startInfo.Verb = "runas";
            Process.Start(startInfo);
            Environment.Exit(-1);
        }
    }
}