using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using PrivateWin10.Controls;

namespace PrivateWin10.Windows
{
    /// <summary>
    /// Interaction logic for ProgramWnd.xaml
    /// </summary>
    public partial class ProgramWnd : Window
    {
        public ProgramID ID;

        public ObservableCollection<PathEntry> Paths;

        public class PathEntry : ContentControl
        {
            public string Group { get; set; }
        }

        int SuspendChange = 0;

        public ProgramWnd(ProgramID id)
        {
            InitializeComponent();

            this.Title = Translate.fmt("wnd_program");

            this.grpProgram.Header = Translate.fmt("lbl_program");
            this.radProgram.Content = Translate.fmt("lbl_exe");
            this.radService.Content = Translate.fmt("lbl_svc");
            this.radApp.Content = Translate.fmt("lbl_app");

            this.btnOK.Content = Translate.fmt("lbl_ok");
            this.btnCancel.Content = Translate.fmt("lbl_cancel");

            ID = id;

            SuspendChange++;

            Paths = new ObservableCollection<PathEntry>();

            ListCollectionView lcv = new ListCollectionView(Paths);
            lcv.GroupDescriptions.Add(new PropertyGroupDescription("Group"));
            cmbPath.ItemsSource = lcv;

            Paths.Add(new PathEntry() { Content = Translate.fmt("pro_browse"), Tag="*", Group = Translate.fmt("lbl_selec") });
            PathEntry itemAll = new PathEntry() { Content = Translate.fmt("pro_all"), Tag = null, Group = Translate.fmt("lbl_selec") };
            Paths.Add(itemAll);

            if (ID != null && ID.Path.Length > 0)
            {
                PathEntry itemPath;
                if (ID.Path.Equals("system"))
                    itemPath = new PathEntry() { Content = Translate.fmt("pro_sys"), Tag = ID.Path, Group = Translate.fmt("lbl_selec") };
                else
                    itemPath = new PathEntry() { Content = ID.Path, Tag = ID.Path, Group = Translate.fmt("lbl_known") };
                Paths.Add(itemPath);
                cmbPath.SelectedItem = itemPath;
            }
            else
                cmbPath.SelectedItem = itemAll;

            //if (ID != null &&  ((ID.Path.Length == 0 && ID.Name.Length == 0) || ID.Path.Equals("system")))
            if (ID != null)
            {
                radProgram.IsEnabled = false;
                radService.IsEnabled = false;
                radApp.IsEnabled = false;

                cmbPath.IsEnabled = false;
                cmbService.IsEnabled = false;
                cmbApp.IsEnabled = false;
            }
            
            cmbService.ItemsSource = ServiceModel.GetInstance().GetServices();
            cmbApp.ItemsSource = AppModel.GetInstance().GetApps();

            if (ID == null)
            {
                radProgram.IsChecked = true;
                ID = ProgramID.NewID(ProgramID.Types.Program);
            }
            else
            {
                switch (ID.Type)
                {
                    case ProgramID.Types.Program:
                        radProgram.IsChecked = true;
                        break;
                    case ProgramID.Types.Service:
                        radService.IsChecked = true;
                        foreach (ServiceModel.Service service in cmbService.Items)
                        {
                            if (MiscFunc.StrCmp(service.Value,ID.GetServiceId()))
                            {
                                cmbService.SelectedItem = service;
                                break;
                            }
                        }
                        if (cmbService.SelectedItem == null)
                            cmbService.Text = ID.GetServiceId();
                        break;
                    case ProgramID.Types.App:
                        radApp.IsChecked = true;
                        foreach (AppModel.AppPkg app in cmbApp.Items)
                        {
                            if (MiscFunc.StrCmp(app.Value, ID.GetPackageSID()))
                            {
                                cmbService.SelectedItem = app;
                                break;
                            }
                        }
                        if (cmbApp.SelectedItem == null)
                            cmbApp.Text = ID.GetPackageName();
                        break;
                }
            }

            if (UwpFunc.IsWindows7OrLower)
            {
                radApp.IsEnabled = false;
                cmbApp.IsEnabled = false;
            }

            SuspendChange--;
        }

        private void btnOK_Click(object sender, RoutedEventArgs e)
        {
            ContentControl path = (cmbPath.SelectedItem as ContentControl);
            string pathStr = path != null ? (path.Tag as string) : cmbPath.Text;
            if (radProgram.IsChecked == true)
            {
                ID = ProgramID.NewProgID(pathStr);
            }
            else if (radService.IsChecked == true)
            {
                ServiceModel.Service name = (cmbService.SelectedItem as ServiceModel.Service);
                ID = ProgramID.NewSvcID(name != null ? name.Value : cmbService.Text, pathStr);
            }
            else if (radApp.IsChecked == true)
            {
                AppModel.AppPkg name = (cmbApp.SelectedItem as AppModel.AppPkg);
                ID = ProgramID.NewAppID(name != null ? name.Value : cmbApp.Text, pathStr);
            }
            else
                ID = ProgramID.NewID(ProgramID.Types.Global);

            this.DialogResult = true;
        }

        private void radType_Checked(object sender, RoutedEventArgs e)
        {
            if (SuspendChange > 0)
                return;

            cmbService.IsEnabled = radService.IsChecked == true;
            cmbApp.IsEnabled = radApp.IsChecked == true;
        }

        private void cmbPath_DropDownClosed(object sender, EventArgs e)
        {
            if (SuspendChange > 0)
                return;

            ContentControl item = (cmbPath.SelectedItem as ContentControl);
            if (item == null)
                return;

            if ((item.Tag as string) == "*")
            {
                OpenFileDialog dlg = new OpenFileDialog();
                dlg.DefaultExt = "exe";
                dlg.Multiselect = false;
                dlg.CheckFileExists = true;
                dlg.CheckPathExists = true;
                dlg.Filter = "Programm Executables (*.exe; *.dll; *.bin; *.scr)|*.exe;*.dll;*.bin;*.scr";
                dlg.Title = Translate.fmt("pro_title");

                DialogResult ret = dlg.ShowDialog();
                if (ret != System.Windows.Forms.DialogResult.OK)
                    return;

                string FileName = dlg.FileName;

                cmbPath.SelectedItem = null;
                foreach (ContentControl path in Paths)
                {
                    if (MiscFunc.StrCmp((path.Tag as string), FileName))
                    {
                        cmbPath.SelectedItem = path;
                        break;
                    }
                }
                if (cmbPath.SelectedItem == null)
                {
                    PathEntry path = new PathEntry() { Content = FileName, Tag = FileName, Group = Translate.fmt("lbl_known") };
                    Paths.Add(path);
                    cmbPath.SelectedItem = path;
                }
            }
        }

        private void cmbApp_DropDownClosed(object sender, EventArgs e)
        {
            if (SuspendChange > 0)
                return;

            AppModel.AppPkg item = (cmbApp.SelectedItem as AppModel.AppPkg);
            if (item == null)
                return;

            if (item.Value == null && App.PkgMgr != null)
                App.PkgMgr.UpdateAppCache();
        }
    }
}
