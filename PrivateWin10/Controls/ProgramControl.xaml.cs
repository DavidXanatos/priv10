using MiscHelpers;
using PrivateWin10.Windows;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace PrivateWin10
{
    /// <summary>
    /// Interaction logic for ProcessControl.xaml
    /// </summary>
    public partial class ProgramControl : UserControl, IControlItem<ProgramSet>
    {
        public event RoutedEventHandler Click;

        public ProgramSet progSet;

        private CategoryModel CatModel;

        //private Brush mBorderBrush;

        public static SolidColorBrush GetAccessColor(ProgramSet.Config.AccessLevels NetAccess)
        {
            switch (NetAccess)
            {
                case ProgramSet.Config.AccessLevels.FullAccess: return new SolidColorBrush(Colors.LightGreen);
                //case ProgramSet.Config.AccessLevels.OutBoundAccess: return new SolidColorBrush(Colors.YellowGreen);
                //case ProgramSet.Config.AccessLevels.InBoundAccess: return new SolidColorBrush(Colors.Goldenrod);
                case ProgramSet.Config.AccessLevels.CustomConfig: return new SolidColorBrush(Colors.Gold);
                case ProgramSet.Config.AccessLevels.LocalOnly: return new SolidColorBrush(Colors.LightSkyBlue);
                case ProgramSet.Config.AccessLevels.BlockAccess: return new SolidColorBrush(Colors.LightPink);
                case ProgramSet.Config.AccessLevels.WarningState: return new SolidColorBrush(Colors.Yellow);
                default: return new SolidColorBrush(Colors.White);
            }
        }

        public static void PrepAccessCmb(ComboBox cmbAccess)
        {
            cmbAccess.Items.Add(new ComboBoxItem() { Content = Translate.fmt("acl_none"), Tag = ProgramSet.Config.AccessLevels.Unconfigured });
            cmbAccess.Items.Add(new ComboBoxItem() { Content = Translate.fmt("acl_allow"), Tag = ProgramSet.Config.AccessLevels.FullAccess });
            //cmbAccess.Items.Add(new ComboBoxItem() { Content = Translate.fmt("acl_outbound"), Tag = ProgramSet.Config.AccessLevels.OutBoundAccess });
            //cmbAccess.Items.Add(new ComboBoxItem() { Content = Translate.fmt("acl_inbound"), Tag = ProgramSet.Config.AccessLevels.InBoundAccess });
            cmbAccess.Items.Add(new ComboBoxItem() { Content = Translate.fmt("acl_edit"), Tag = ProgramSet.Config.AccessLevels.CustomConfig });
            cmbAccess.Items.Add(new ComboBoxItem() { Content = Translate.fmt("acl_lan"), Tag = ProgramSet.Config.AccessLevels.LocalOnly });
            cmbAccess.Items.Add(new ComboBoxItem() { Content = Translate.fmt("acl_block"), Tag = ProgramSet.Config.AccessLevels.BlockAccess });
            foreach (ComboBoxItem item in cmbAccess.Items)
                item.Background = GetAccessColor((ProgramSet.Config.AccessLevels)item.Tag);
        }

        public ProgramControl(ProgramSet prog, CategoryModel Categories)
        {
            InitializeComponent();

            chkNotify.Content = Translate.fmt("lbl_notify");
            btnAdd.Content = Translate.fmt("lbl_add");
            btnSplit.Content = Translate.fmt("lbl_split");
            btnRemove.Content = Translate.fmt("lbl_remove");

            progGrid.Columns[1].Header = Translate.fmt("lbl_name");
            progGrid.Columns[2].Header = Translate.fmt("lbl_program");

            SuspendChange++;

            progArea.Visibility = Visibility.Collapsed;

            CatModel = Categories;
            //category.ItemsSource = CatModel.Categorys;
            category.ItemsSource = CatModel.GetCategorys();

            //mBorderBrush = name.BorderBrush;
            //name.BorderBrush = Brushes.Transparent;

            PrepAccessCmb(cmbAccess);

            SuspendChange--;

            DoUpdate(prog);

            ProgramID id = prog.Programs.First().Key;
            if (id.Type == ProgramID.Types.Global || id.Type == ProgramID.Types.System)
            {
                btnIDs.IsEnabled = false;
                //btnCustimize.Visibility = Visibility.Hidden;
                cmbAccess.Visibility = Visibility.Hidden;
                //category.Visibility = Visibility.Hidden;
            }
            if (id.Type == ProgramID.Types.Global)
            {
                chkNotify.Visibility = Visibility.Hidden;
            }

            rect.MouseDown += new MouseButtonEventHandler(rect_Click);
            //name.MouseDown += new MouseButtonEventHandler(rect_Click);
            name.PreviewMouseDown += new MouseButtonEventHandler(rect_Click);
            //progGrid.PreviewMouseDown += new MouseButtonEventHandler(rect_Click);
            icon.MouseDown += new MouseButtonEventHandler(rect_Click);
            info.MouseDown += new MouseButtonEventHandler(rect_Click);

            category.PreviewMouseWheel += ctrl_PreviewMouseWheel;
            //progGrid.PreviewMouseWheel += ctrl_PreviewMouseWheel;
        }

        int SuspendChange = 0;

        public void DoUpdate(ProgramSet progSet)
        {
            this.progSet = progSet;

            SuspendChange++;

            ImgFunc.GetIconAsync(progSet.GetIcon(), icon.Width, (ImageSource src) => {
                if (Application.Current != null)
                {
                    Application.Current.Dispatcher.Invoke(new Action(() =>
                    {
                        icon.Source = src;
                    }));
                }
                return 0;
            });

            //name.Content = process.Name;
            name.Text = progSet.config.Name;

            int blockedConnections = 0;
            int allowedConnections = 0;
            int socketCount = 0;
            UInt64 uploadRate = 0;
            UInt64 downloadRate = 0;
            foreach (Program prog in progSet.Programs.Values)
            {
                blockedConnections += prog.BlockedCount;
                allowedConnections += prog.AllowedCount;

                socketCount += prog.SocketCount;

                uploadRate += prog.UploadRate;
                downloadRate += prog.DownloadRate;
            }
            info.Content = Translate.fmt("lbl_prog_info", blockedConnections, allowedConnections, socketCount, 
                FileOps.FormatSize((decimal)uploadRate), FileOps.FormatSize((decimal)downloadRate)); 

            WpfFunc.CmbSelect(category, progSet.config.Category == null ? "" : progSet.config.Category);

            WpfFunc.CmbSelect(cmbAccess, progSet.config.GetAccess().ToString());
            if (progSet.config.NetAccess != ProgramSet.Config.AccessLevels.Unconfigured && progSet.config.NetAccess != progSet.config.CurAccess)
                cmbAccess.Background /*grid.Background*/ = FindResource("Stripes") as DrawingBrush;
            else
                cmbAccess.Background = GetAccessColor(progSet.config.GetAccess());

            chkNotify.IsChecked = progSet.config.GetNotify();

            progGrid.Items.Clear();

            foreach (Program prog in progSet.Programs.Values)
                progGrid.Items.Insert(0, new ProgEntry(prog));

            btnSplit.IsEnabled = progSet.Programs.Count > 1;
            SuspendChange--;
        }

        private bool mHasFocus = false;

        public bool GetFocus() { return mHasFocus; }

        public void SetFocus(bool set = true)
        {
            this.rect.StrokeThickness = 2;
            this.rect.Stroke = set ? new SolidColorBrush(Color.FromArgb(255, 51, 153, 255)) : null;
            //this.rect.Fill = new SolidColorBrush(set ? Color.FromArgb(255, 153, 204, 255) : Colors.Transparent);
            //this.rect.Fill = new SolidColorBrush(set ? Color.FromArgb(255, 230, 240, 255) : Colors.Transparent);

            mHasFocus = set;
        }

        private bool mHasError = false;

        public void Flash(Color color)
        {
            if (mHasError)
                return;

            var changeColor = FindResource("RecoverColor") as Storyboard;
            //changeColor.Stop();
            this.rect.Fill = new SolidColorBrush(color);
            changeColor.Begin();
        }

        public void SetError(bool set)
        {
            mHasError = set;
            if (set)
                this.rect.Fill = FindResource("Stripes") as DrawingBrush;
            else {
                var changeColor = FindResource("RecoverColor") as Storyboard;
                changeColor.Begin();
            }
        }

        private void rect_Click(object sender, RoutedEventArgs e)
        {
            Click?.Invoke(this, e);
        }

        private void category_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (SuspendChange > 0)
                return;

            CategoryModel.Category cat = (category.SelectedItem as CategoryModel.Category);

            string Value;
            if (cat.SpecialCat == CategoryModel.Category.Special.AddNew)
            {
                InputWnd wnd = new InputWnd(Translate.fmt("msg_cat_name"), Translate.fmt("msg_cat_some"), App.Title);
                if (wnd.ShowDialog() != true || wnd.Value.Length == 0)
                    return;

                Value = wnd.Value;

                Value = Value.Replace(',', '.'); // this is our separator so it cant be in the name

                bool found = false;
                foreach (CategoryModel.Category curCat in CatModel.Categorys)
                {
                    if (Value.Equals((curCat.Content as String), StringComparison.OrdinalIgnoreCase))
                        found = true;
                }
                if (!found)
                {
                    CatModel.Categorys.Insert(0, new CategoryModel.Category() { Content = Value, Group = Translate.fmt("cat_cats") });
                    category.SelectedIndex = 0;
                }
            }
            else if (cat.SpecialCat == CategoryModel.Category.Special.SetNone)
                Value = "";
            else
                Value = (cat.Content as String);

            progSet.config.Category = Value;
            App.client.UpdateProgram(progSet.guid, progSet.config);
        }

        private void name_LostFocus(object sender, RoutedEventArgs e)
        {
            if (SuspendChange > 0)
                return;

            if (!name.IsReadOnly)
            {
                //name.BorderBrush = Brushes.Transparent;
                name.IsReadOnly = true;

                progSet.config.Name = name.Text;
                App.client.UpdateProgram(progSet.guid, progSet.config);
            }
        }

        private void name_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            //mBorderBrush = name.BorderBrush;
            name.IsReadOnly = false;
        }

        private void name_KeyDown(object sender, KeyEventArgs e)
        {
            if (SuspendChange > 0)
                return;

            if (e.Key == Key.Enter || e.Key == Key.Escape)
            {
                //name.BorderBrush = Brushes.Transparent;
                name.IsReadOnly = true;
                if (e.Key == Key.Enter)
                {
                    progSet.config.Name = name.Text;
                    App.client.UpdateProgram(progSet.guid, progSet.config); 
                }
            }
        }

        private void ctrl_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
        {
            e.Handled = true;
            //DependencyObject parentObject = VisualTreeHelper.GetParent(category);
            MouseWheelEventArgs args = new MouseWheelEventArgs(e.MouseDevice, e.Timestamp, e.Delta);
            args.RoutedEvent = UIElement.MouseWheelEvent;
            args.Source = sender;
            this.RaiseEvent(args);
        }

        private void icon_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (SuspendChange > 0)
                return;

            if (e.ChangedButton == MouseButton.Left && e.ClickCount == 2) // double click
            {
                string iconFile = ProgramControl.OpenIconPicker(progSet.GetIcon());
                if (iconFile != null)
                {
                    progSet.config.Icon = iconFile;
                    App.client.UpdateProgram(progSet.guid, progSet.config);
                    icon.Source = ImgFunc.GetIcon(progSet.GetIcon(), icon.Width);
                }
            }
        }


        static public string OpenIconPicker(string iconFile)
        {
            IconExtractor.PickerDialog picker = new IconExtractor.PickerDialog();
            var pathIndex = TextHelpers.Split2(iconFile, "|");
            picker.FileName = pathIndex.Item1;
            picker.IconIndex = MiscFunc.parseInt(pathIndex.Item2);
            if (picker.ShowDialog() != System.Windows.Forms.DialogResult.OK)
                return null;

            IconExtractor extractor = new IconExtractor(picker.FileName);
            if (extractor.Count == 0)
                return null;

            return picker.FileName + "|" + picker.IconIndex;
        }

        public class ProgEntry : INotifyPropertyChanged
        {
            public Program Prog;

            public ProgEntry(Program prog)
            {
                Prog = prog;
            }

            public ImageSource Icon { get { return ImgFunc.GetIcon(Prog.ID.Path, 16); } }

            public string Name { get { return Prog.Description; } }

            public string Program { get { return Prog.ID.FormatString(); } } 

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

        private void btnIDs_Click(object sender, RoutedEventArgs e)
        {
            progArea.Visibility = (btnIDs.IsChecked == true) ? Visibility.Visible : Visibility.Collapsed;
        }

        private void btnAdd_Click(object sender, RoutedEventArgs e)
        {
            ProgramWnd progWnd = new ProgramWnd(null);
            if (progWnd.ShowDialog() != true)
                return;

            if (!App.client.AddProgram(progWnd.ID, progSet.guid))
                MessageBox.Show(Translate.fmt("msg_already_exist"), App.Title, MessageBoxButton.OK, MessageBoxImage.Exclamation);
        }

        private void btnSplit_Click(object sender, RoutedEventArgs e)
        {
            if (progGrid.SelectedItems.Count == progGrid.Items.Count)
            {
                MessageBox.Show(Translate.fmt("msg_no_split_all"), App.Title, MessageBoxButton.OK, MessageBoxImage.Exclamation);
                return;
            }

            ProgEntry[] Temp = new ProgEntry[progGrid.SelectedItems.Count];
            progGrid.SelectedItems.CopyTo(Temp, 0);
            foreach (ProgEntry item in Temp)
                App.client.SplitPrograms(progSet.guid, item.Prog.ID);
        }

        private void btnRemove_Click(object sender, RoutedEventArgs e)
        {
            if (MessageBox.Show(Translate.fmt("msg_remove_progs"), App.Title, MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.No)
                return;

            ProgEntry[] Temp = new ProgEntry[progGrid.SelectedItems.Count];
            progGrid.SelectedItems.CopyTo(Temp, 0);
            foreach (ProgEntry item in Temp)
                App.client.RemoveProgram(progSet.guid, item.Prog.ID);
        }

        private void progGrid_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            ProgEntry entry = (progGrid.SelectedItem as ProgEntry);
            if (entry == null)
                return;

            ProgramWnd progWnd = new ProgramWnd(entry.Prog.ID);
            if (progWnd.ShowDialog() != true)
                return;

            // no editing
        }

        private void chkNotify_Click(object sender, RoutedEventArgs e)
        {
            if (SuspendChange > 0)
                return;

            progSet.config.SetNotify(chkNotify.IsChecked);
            App.client.UpdateProgram(progSet.guid, progSet.config); 
        }

        private void cmbAccess_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (SuspendChange > 0)
                return;

            progSet.config.NetAccess = (ProgramSet.Config.AccessLevels)(cmbAccess.SelectedItem as ComboBoxItem).Tag;
            App.client.UpdateProgram(progSet.guid, progSet.config);
        }

        /*private void btnCustimize_Click(object sender, RoutedEventArgs e)
        {
            // todo dialog

            App.itf.UpdateProgram(Program.guid, Program.config);
        }*/
    }
}
