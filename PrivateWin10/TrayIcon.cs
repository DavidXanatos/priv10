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
        private NotifyIcon notifyIcon;
        private ContextMenu contextMenu;
        private MenuItem menuBlock;
        private MenuItem menuExit;
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
            this.contextMenu = new ContextMenu();

            // Initialize menuItem1
            this.menuBlock = new MenuItem();
            this.menuBlock.Index = 0;
            this.menuBlock.Text = Translate.fmt("mnu_block");

            ProgramID id = ProgramID.NewID(ProgramID.Types.Global);
            ProgramSet prog = App.client.GetProgram(id, true);

            if (prog == null)
                this.menuBlock.Enabled = false;
            else
                this.menuBlock.Checked = (prog.config.CurAccess == ProgramSet.Config.AccessLevels.BlockAccess);

            this.menuBlock.Click += new System.EventHandler(this.menuBlock_Click);

            // Initialize menuItem1
            this.menuExit = new MenuItem();
            this.menuExit.Index = 0;
            this.menuExit.Text = Translate.fmt("mnu_exit");
            this.menuExit.Click += new System.EventHandler(this.menuExit_Click);

            // Initialize contextMenu1
            this.contextMenu.MenuItems.AddRange(new MenuItem[] { this.menuBlock, new MenuItem("-"), this.menuExit });

            // Create the NotifyIcon.
            this.notifyIcon = new NotifyIcon(this.components);

            string exePath = System.Reflection.Assembly.GetExecutingAssembly().Location;

            // The Icon property sets the icon that will appear
            // in the systray for this application.
            //notifyIcon1.Icon = new Icon("wu.ico");
            notifyIcon.Icon = System.Drawing.Icon.ExtractAssociatedIcon(exePath);

            // The ContextMenu property sets the menu that will
            // appear when the systray icon is right clicked.
            notifyIcon.ContextMenu = this.contextMenu;

            // The Text property sets the text that will be displayed,
            // in a tooltip, when the mouse hovers over the systray icon.
            notifyIcon.Text = FileVersionInfo.GetVersionInfo(exePath).FileDescription;

            // Handle the DoubleClick event to activate the form.
            notifyIcon.DoubleClick += new System.EventHandler(this.notifyIcon1_DoubleClick);
            notifyIcon.Click += new System.EventHandler(this.notifyIcon1_Click);
        }

        public bool Visible { get { return notifyIcon.Visible; } set { notifyIcon.Visible = value; } }

        public void DestroyNotifyicon()
        {
            notifyIcon.Visible = false;
            notifyIcon.Dispose();
        }

        public void Notify(string Message, ToolTipIcon Icon = ToolTipIcon.Info)
        {
            notifyIcon.ShowBalloonTip(5000, App.mName, Message, Icon);
        }

        private void notifyIcon1_Click(object Sender, EventArgs e)
        {
            //MessageBox.Show("clicked");
        }

        private void notifyIcon1_DoubleClick(object Sender, EventArgs e)
        {
            if ((e as MouseEventArgs).Button != MouseButtons.Left)
                return;

            TrayEventArgs args = new TrayEventArgs();
            args.Action = Actions.ToggleWindow;
            Action(this, args);
        }

        private void menuBlock_Click(object Sender, EventArgs e)
        {
            this.menuBlock.Checked = !this.menuBlock.Checked;

            App.client.BlockInternet(this.menuBlock.Checked);
        }

        private void menuExit_Click(object Sender, EventArgs e)
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