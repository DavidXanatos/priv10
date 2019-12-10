using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
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

namespace PrivateSetup
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class SetupWindow : Window
    {
        SetupData Data;

        bool Uninstaller = false;
        SetupWorker Worker = null;
        bool Completed = false;

        enum Panels
        {
            None = 0,
            Type,
            Eula,
            Action,
            Progress,
        }
        private Panels CurPanel = Panels.None;


        public static readonly DependencyProperty CaptionProperty =
            DependencyProperty.Register("Caption", typeof(string),
            typeof(SetupWindow), new PropertyMetadata(Guid.NewGuid().ToString())
        );
        public static readonly DependencyProperty VersionProperty =
            DependencyProperty.Register("Version", typeof(string),
            typeof(SetupWindow), new PropertyMetadata(Guid.NewGuid().ToString())
        );

        public SetupWindow(SetupData Data, bool Uninstaller = false)
        {
            this.Data = Data;
            this.Uninstaller = Uninstaller;

            InitializeComponent();

            this.Title = App.Title;

            var eulaStream = Assembly.GetExecutingAssembly().GetManifestResourceStream("PrivateSetup.Resources.LICENSE.TXT");
            StreamReader eulaReader = new StreamReader(eulaStream, Encoding.Default, true);
            string EULA = eulaReader.ReadToEnd();
            using (var md5 = MD5.Create())
            {
                eulaStream.Seek(0, SeekOrigin.Begin);
                var hash = md5.ComputeHash(eulaStream);
                if (BitConverter.ToString(hash).Replace("-", "") != "7DAA7D6DD7B69094B8DA8CF3F00F0360")
                    Environment.Exit(-1);
            }
            txtEula.Text = EULA;

            SetValue(CaptionProperty, SetupData.AppTitle);
            if(Uninstaller)
                SetValue(VersionProperty, Data.CurVersion);
            else
                SetValue(VersionProperty, Data.AppVersion);

            if (Data.Action != SetupData.Actions.Undefined)
                StartOperation();
            else if (Data.Use == SetupData.Uses.Undefined && !Uninstaller && !Data.IsInstalled)
                ShowPanel(Panels.Type);
            else
                ShowPanel(Panels.Action);

            if (Uninstaller && !Data.IsInstalled)
            {
                App.ShowMessage("No {0} instalation found.", SetupData.AppTitle);
                Environment.Exit(0);
            }
        }

        private void ShowPanel(Panels panel)
        {
            this.panelType.Visibility = panel == Panels.Type ? Visibility.Visible : Visibility.Collapsed;
            this.panelEula.Visibility = panel == Panels.Eula ? Visibility.Visible : Visibility.Collapsed;
            this.panelAction.Visibility = panel == Panels.Action ? Visibility.Visible : Visibility.Collapsed;
            this.panelProgress.Visibility = panel == Panels.Progress ? Visibility.Visible : Visibility.Collapsed;


            switch (panel)
            {
                case Panels.Type:
                    this.btnBack.IsEnabled = false;
                    this.btnNext.IsEnabled = Data.Use != SetupData.Uses.Undefined;

                    if (Data.Use == SetupData.Uses.Commertial)
                        this.radBusiness.IsChecked = true;
                    else if (Data.Use == SetupData.Uses.Personal)
                        this.radPersonal.IsChecked = true;

                    this.txtEval.Visibility = Data.Use == SetupData.Uses.Commertial && Data.LicenseFile.Length == 0 ? Visibility.Visible : Visibility.Collapsed;
                    this.txtLicense.Visibility = Data.Use == SetupData.Uses.Commertial && Data.LicenseFile.Length > 0 ? Visibility.Visible : Visibility.Collapsed;
                    this.txtLicFile.Text = Data.LicenseFile;

                    this.btnNext.Content = "Next >";
                    break;
                case Panels.Eula:
                    this.btnBack.IsEnabled = true;
                    this.btnNext.IsEnabled = false;
                    this.btnBack.Content = "< Back";
                    break;
                case Panels.Action:
                    this.btnBack.IsEnabled = !Uninstaller && !Data.IsInstalled;
                    this.btnNext.IsEnabled = Data.Action != SetupData.Actions.Undefined;
                    this.btnBack.Content = "< Back";

                    radInstall.Visibility = !Uninstaller && !Data.IsInstalled ? Visibility.Visible : Visibility.Collapsed;
                    radUpdate.Visibility = !Uninstaller && Data.IsInstalled ? Visibility.Visible : Visibility.Collapsed;
                    radExtract.Visibility = !Uninstaller ? Visibility.Visible : Visibility.Collapsed;
                    radRemove.Visibility = Uninstaller || Data.IsInstalled ? Visibility.Visible : Visibility.Collapsed;

                    if (Data.Action == SetupData.Actions.Install)
                        this.radInstall.IsChecked = true;
                    else if (Data.Action == SetupData.Actions.Update)
                        this.radUpdate.IsChecked = true;
                    else if (Data.Action == SetupData.Actions.Extract)
                        this.radExtract.IsChecked = true;
                    else if (Data.Action == SetupData.Actions.Uninstall)
                        this.radRemove.IsChecked = true;

                    if (radExtract.IsChecked == true)
                        this.btnNext.Content = "Extact";
                    else if (radInstall.IsChecked == true)
                        this.btnNext.Content = "Install";
                    else if (radUpdate.IsChecked == true)
                        this.btnNext.Content = "Update";
                    else if (radRemove.IsChecked == true)
                        this.btnNext.Content = "Uninstall";
                    else
                        this.btnNext.Content = "Next >";

                    if (Data.Action == SetupData.Actions.Extract)
                        txtInstallDir.Text = App.appPath + @"\" +  SetupData.AppKey;
                    else
                        txtInstallDir.Text = Data.InstallationPath;

                    btnBrowse.IsEnabled = Data.Action == SetupData.Actions.Extract || Data.Action == SetupData.Actions.Install;

                    this.frameInstall.Visibility = Data.Action == SetupData.Actions.Install ? Visibility.Visible : Visibility.Collapsed;
                    chkAutoStart.IsChecked = Data.AutoStart;

                    this.frameUninstall.Visibility = Data.Action == SetupData.Actions.Uninstall ? Visibility.Visible : Visibility.Collapsed;
                    chkResetFW.IsChecked = Data.ResetFirewall;
                    chkClearData.IsChecked = Data.RemoveUserData;


                    break;
                case Panels.Progress:
                    this.btnBack.IsEnabled = false;
                    this.btnNext.IsEnabled = false;

                    if (radExtract.IsChecked == true)
                        this.txtOperation.Text = "Extraction Progress:";
                    else if (radInstall.IsChecked == true)
                        this.txtOperation.Text = "Installation Progress:";
                    else if (radUpdate.IsChecked == true)
                        this.txtOperation.Text = "Update Progress:";
                    else if (radRemove.IsChecked == true)
                        this.txtOperation.Text = "Uninstallation Progress:";

                    if (Completed)
                    {
                        if (Data.Action == SetupData.Actions.Install)
                        {
                            this.btnNext.IsEnabled = true;
                            this.btnBack.IsEnabled = true;
                            this.btnBack.Content = "Close";
                            this.btnNext.Content = "Run";
                        }
                        else if (Data.Action == SetupData.Actions.Update)
                        {
                            this.btnNext.IsEnabled = true;
                            this.btnNext.Content = "Run";
                        }
                        else
                        {
                            this.btnNext.IsEnabled = true;
                            this.btnNext.Content = "Close";
                        }
                    }

                    break;
            }

            if (panel != Panels.Eula)
                CurPanel = panel;
        }

        private void UpdatePanel()
        {
            ShowPanel(CurPanel);
        }

        private void BtnEula_MouseDown(object sender, MouseButtonEventArgs e)
        {
            ShowPanel(Panels.Eula);
        }

        private void BtnNext_Click(object sender, RoutedEventArgs e)
        {
            if (this.panelEula.IsVisible)
                ShowPanel(CurPanel);
            else if (CurPanel == Panels.Type)
                ShowPanel(Panels.Action);
            else if (CurPanel == Panels.Action)
            {
                switch (Data.Action)
                {
                    case SetupData.Actions.Install:
                        Data.AutoStart = chkAutoStart.IsChecked == true;
                        goto case SetupData.Actions.Extract;
                    case SetupData.Actions.Extract:
                        Data.InstallationPath = txtInstallDir.Text;
                        break;
                    case SetupData.Actions.Uninstall:
                        Data.ResetFirewall = chkResetFW.IsChecked == true;
                        Data.RemoveUserData = chkClearData.IsChecked == true;
                        break;
                }

                StartOperation();
            }
            else if (CurPanel == Panels.Progress)
            {
                if (Data.Action == SetupData.Actions.Install || Data.Action == SetupData.Actions.Update)
                {
                    try
                    {
                        Process.Start(Data.InstallationPath + @"\" + SetupData.AppBinary);
                    }
                    catch { }
                }
                this.Close();
            }
        }

        private void BtnBack_Click(object sender, RoutedEventArgs e)
        {
            if (this.panelEula.IsVisible)
                ShowPanel(CurPanel);
            else if (CurPanel == Panels.Action)
                ShowPanel(Panels.Type);
            else if (CurPanel == Panels.Progress)
                this.Close();
        }

        private bool TempRequired()
        {
            if (App.TestArg("-temp")) // just in case 
                return false;
            if(Data.Action != SetupData.Actions.Uninstall)
                return false;
            return App.exePath.Contains(Data.InstallationPath);
        }

        private void StartOperation()
        {
            bool tempRequired = TempRequired();
            if (tempRequired || (!App.IsAdministrator() && Data.Action != SetupData.Actions.Extract))
            {
                if (!App.Restart(Data.MakeArgs(), tempRequired))
                    App.ShowMessage("The setup can not proceed without Administrative permissions.");
                return;
            }
            
            ShowPanel(Panels.Progress);

            prgress.Value = 0;
            Worker = new SetupWorker(Data);
            Worker.Progress += (s, e) =>
            {
                this.Dispatcher.Invoke(() => 
                {
                    if (e.Message != null)
                    {
                        txtProgress.Text += e.Message + "\r\n";

                        txtProgress.Focus();
                        txtProgress.CaretIndex = txtProgress.Text.Length;
                        txtProgress.ScrollToEnd();

                        if (e.Show)
                            App.ShowMessage(e.Message);
                    }
                    if(e.Progress >= 0 && e.Progress <= 100)
                        prgress.Value = e.Progress;
                });
            };
            Worker.Finished += (s, e) =>
            {
                this.Dispatcher.Invoke(() =>
                {
                    prgress.Value = 100;
                    Worker = null;
                    Completed = true;
                    UpdatePanel();
                });
            };
            Worker.Start();
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            /*MessageBoxResult result = MessageBox.Show("Do you really want to close teh Setup Wizrd?", "Warning", MessageBoxButton.YesNo);
            if (result != MessageBoxResult.Yes)
            {
                e.Cancel = true;
            }*/

            if(Worker != null) // don't close when doing something
                e.Cancel = true;
        }

        private void RadUsage_Click(object sender, RoutedEventArgs e)
        {
            if (sender == this.radBusiness)
                Data.Use = SetupData.Uses.Commertial;
            else if (sender == this.radPersonal)
                Data.Use = SetupData.Uses.Personal;
            UpdatePanel();
        }

        private void SelectLic_RequestNavigate(object sender, RequestNavigateEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            if (openFileDialog.ShowDialog() == true)
                Data.LicenseFile = openFileDialog.FileName;
            else
                Data.LicenseFile = "";
            this.radBusiness.IsChecked = true;
        }


        private void RadMode_Click(object sender, RoutedEventArgs e)
        {
            if (sender == this.radInstall)
                Data.Action = SetupData.Actions.Install;
            else if (sender == this.radUpdate)
                Data.Action = SetupData.Actions.Update;
            else if (sender == this.radExtract)
                Data.Action = SetupData.Actions.Extract;
            else if (sender == this.radRemove)
                Data.Action = SetupData.Actions.Uninstall;
            UpdatePanel();
        }

        private void BtnBrowse_Click(object sender, RoutedEventArgs e)
        {
            using (var folderBrowserDialog = new System.Windows.Forms.FolderBrowserDialog())
            {
                folderBrowserDialog.Description = "Please select a directory";
                folderBrowserDialog.RootFolder = Environment.SpecialFolder.MyComputer;
                folderBrowserDialog.SelectedPath = txtInstallDir.Text;
                if (folderBrowserDialog.ShowDialog() != System.Windows.Forms.DialogResult.OK)
                    return;

                if (SetupWorker.IsUnsafePath(folderBrowserDialog.SelectedPath))
                    txtInstallDir.Text = folderBrowserDialog.SelectedPath + @"\" + SetupData.AppKey;
                else
                    txtInstallDir.Text = folderBrowserDialog.SelectedPath;
            }
        }
    }
}
