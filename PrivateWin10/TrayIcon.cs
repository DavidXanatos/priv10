using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.ComponentModel;

namespace PrivateWin10
{
    public class TrayIcon
    {
        private NotifyIcon notifyIcon1;
        private ContextMenu contextMenu1;
        private MenuItem menuItem1;
        private IContainer components;

        public enum Actions
        {
            ToggleWindow,
            CloseApplication,
        }

        public class TrayEventArgs : EventArgs
        {
            public TrayIcon.Actions Action { get; set; }
        }

        public event EventHandler<TrayEventArgs> Action;

        public TrayIcon()
        {
            this.components = new Container();
            this.contextMenu1 = new ContextMenu();
            this.menuItem1 = new MenuItem();

            // Initialize menuItem1
            this.menuItem1.Index = 0;
            this.menuItem1.Text = "E&xit";
            this.menuItem1.Click += new System.EventHandler(this.menuItem1_Click);

            // Initialize contextMenu1
            this.contextMenu1.MenuItems.AddRange(new MenuItem[] { this.menuItem1 });

            // Create the NotifyIcon.
            this.notifyIcon1 = new NotifyIcon(this.components);

            string exePath = System.Reflection.Assembly.GetExecutingAssembly().Location;

            // The Icon property sets the icon that will appear
            // in the systray for this application.
            //notifyIcon1.Icon = new Icon("wu.ico");
            notifyIcon1.Icon = System.Drawing.Icon.ExtractAssociatedIcon(exePath);

            // The ContextMenu property sets the menu that will
            // appear when the systray icon is right clicked.
            notifyIcon1.ContextMenu = this.contextMenu1;

            // The Text property sets the text that will be displayed,
            // in a tooltip, when the mouse hovers over the systray icon.
            notifyIcon1.Text = FileVersionInfo.GetVersionInfo(exePath).FileDescription;

            // Handle the DoubleClick event to activate the form.
            notifyIcon1.DoubleClick += new System.EventHandler(this.notifyIcon1_DoubleClick);
            notifyIcon1.Click += new System.EventHandler(this.notifyIcon1_Click);
        }

        public bool Visible { get { return notifyIcon1.Visible; } set { notifyIcon1.Visible = value; } }

        public void DestroyNotifyicon()
        {
            notifyIcon1.Visible = false;
            notifyIcon1.Dispose();
        }

        private void notifyIcon1_Click(object Sender, EventArgs e)
        {
            //MessageBox.Show("clicked");
        }

        private void notifyIcon1_DoubleClick(object Sender, EventArgs e)
        {
            //MessageBox.Show("Double clicked");
            TrayEventArgs args = new TrayEventArgs();
            args.Action = Actions.ToggleWindow;
            Action(this, args);
        }

        private void menuItem1_Click(object Sender, EventArgs e)
        {
            //notifyIcon1.Visible = false;

            // Close the form, which closes the application.
            //Application.Exit();

            TrayEventArgs args = new TrayEventArgs();
            args.Action = Actions.CloseApplication;
            Action(this, args);
        }
    }
}