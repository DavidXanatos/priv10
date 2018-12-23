using PrivateWin10.Controls;
using PrivateWin10.ViewModels;
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

namespace PrivateWin10.Windows
{
    /// <summary>
    /// Interaction logic for ProgramWnd.xaml
    /// </summary>
    public partial class ProgramWnd : Window
    {
        public ProgramList.ID ID;

        public ObservableCollection<PathEntry> Paths;

        public class PathEntry : ContentControl
        {
            public string Groupe { get; set; }
        }

        int SuspendChange = 0;

        public ProgramWnd(ProgramList.ID id)
        {
            InitializeComponent();

            ID = id;

            SuspendChange++;

            Paths = new ObservableCollection<PathEntry>();

            ListCollectionView lcv = new ListCollectionView(Paths);
            lcv.GroupDescriptions.Add(new PropertyGroupDescription("Groupe"));
            cmbPath.ItemsSource = lcv;

            Paths.Add(new PathEntry() { Content = Translate.fmt("pro_browse"), Tag="*", Groupe = Translate.fmt("lbl_selec") });
            PathEntry itemAll = new PathEntry() { Content = Translate.fmt("pro_all"), Tag = null, Groupe = Translate.fmt("lbl_selec") };
            Paths.Add(itemAll);

            if (ID != null && ID.Path.Length > 0)
            {
                PathEntry itemPath;
                if (ID.Path.Equals("system"))
                    itemPath = new PathEntry() { Content = Translate.fmt("pro_sys"), Tag = ID.Path, Groupe = Translate.fmt("lbl_selec") };
                else
                    itemPath = new PathEntry() { Content = ID.Path, Tag = ID.Path, Groupe = Translate.fmt("lbl_known") };
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
                ID = new ProgramList.ID(ProgramList.Types.Program);
            }
            else
            {
                switch (ID.Type)
                {
                    case ProgramList.Types.Program:
                        radProgram.IsChecked = true;
                        break;
                    case ProgramList.Types.Service:
                        radService.IsChecked = true;
                        foreach (ServiceModel.Service service in cmbService.Items)
                        {
                            if (MiscFunc.StrCmp(service.Value,ID.Name))
                            {
                                cmbService.SelectedItem = service;
                                break;
                            }
                        }
                        if (cmbService.SelectedItem == null)
                            cmbService.Text = ID.Name;
                        break;
                    case ProgramList.Types.App:
                        radApp.IsChecked = true;
                        foreach (AppModel.App app in cmbApp.Items)
                        {
                            if (MiscFunc.StrCmp(app.Value, ID.Name))
                            {
                                cmbService.SelectedItem = app;
                                break;
                            }
                        }
                        if (cmbApp.SelectedItem == null)
                            cmbApp.Text = AppManager.SidToPackageID(ID.Name);
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
            if (radProgram.IsChecked == true)
            {
                ID.Type = ProgramList.Types.Program;
                ContentControl path = (cmbPath.SelectedItem as ContentControl);
                ID.Path = path != null ? (path.Tag as string) : cmbPath.Text;
                ID.Name = "";
            }
            else if (radService.IsChecked == true)
            {
                ID.Type = ProgramList.Types.Service;
                ContentControl path = (cmbPath.SelectedItem as ContentControl);
                ID.Path = path != null ? (path.Tag as string) : cmbPath.Text;

                ServiceModel.Service name = (cmbService.SelectedItem as ServiceModel.Service);
                ID.Name = name != null ? name.Value : cmbService.Text;
            }
            else if(radProgram.IsChecked == true)
            {
                ID.Type = ProgramList.Types.Program;
                ContentControl path = (cmbPath.SelectedItem as ContentControl);
                ID.Path = path != null ? (path.Tag as string) : cmbPath.Text;

                AppModel.App name = (cmbApp.SelectedItem as AppModel.App);
                ID.Name = name != null ? name.Value : cmbApp.Text;
            }

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
                    PathEntry path = new PathEntry() { Content = FileName, Tag = FileName, Groupe = Translate.fmt("lbl_known") };
                    Paths.Add(path);
                    cmbPath.SelectedItem = path;
                }
            }
        }
    }
}
