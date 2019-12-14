using ICSharpCode.TreeView;
using PrivateWin10.Pages;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace PrivateWin10.Controls
{
    /// <summary>
    /// Interaction logic for ProgramTreeControl.xaml
    /// </summary>
    public partial class ProgramTreeControl : UserControl
    {
        ProgTreeRoot root;

        public event EventHandler<EventArgs> SelectionChanged;

        FirewallPage.FilterPreset CurFilter = null;

        string defaults = null;
        GridViewHeaderRowPresenter headerPresenter;
        GridViewColumn[] allColumns;
        ContextMenu headerMenu;

        public FirewallPage firewallPage = null;

        public MenuItem menuAdd;
        public MenuItem menuAddSub;
        public MenuItem menuRemove;
        public MenuItem menuMerge;
        public MenuItem menuSplit;
        public MenuItem menuAccess;
        public MenuItem menuAccessNone;
        public MenuItem menuAccessAllow;
        public MenuItem menuAccessCustom;
        public MenuItem menuAccessLan;
        public MenuItem menuAccessBlock;
        public MenuItem menuNotify;
        public MenuItem menuRename;
        public MenuItem menuSetIcon;
        public MenuItem menuCategory;


        public ProgramTreeControl()
        {
            InitializeComponent();

            this.hdrDescr.Content = Translate.fmt("lbl_descr");
            this.hdrCat.Content = Translate.fmt("lbl_log_type");
            this.hdrAccess.Content = Translate.fmt("lbl_access");
            this.hdrRules.Content = Translate.fmt("lbl_rules");
            this.hdrAllowed.Content = Translate.fmt("filter_recent_allowed");
            this.hdrBlocked.Content = Translate.fmt("filter_recent_blocked");
            this.hdrActivity.Content = Translate.fmt("sort_act");
            this.hdrSockets.Content = Translate.fmt("lbl_socks");
            this.hdrUpRate.Content = Translate.fmt("lbl_upload");
            this.hdrDownRate.Content = Translate.fmt("lbl_download");
            this.hdrUpTotal.Content = Translate.fmt("lbl_uploaded");
            this.hdrDownTotal.Content = Translate.fmt("lbl_downloaded");
            this.hdrProg.Content = Translate.fmt("lbl_program");

            treeView.Loaded += (sender, e) => {

                headerPresenter = WpfFunc.FindChild<GridViewHeaderRowPresenter>(treeView);
                if (headerPresenter == null)
                    return;

                allColumns = headerPresenter.Columns.Cast<GridViewColumn>().ToArray();

                Restore(App.GetConfig("GUI", "progTree_Columns", ""));

                headerMenu = new ContextMenu();
                ContextMenuService.SetContextMenu(headerPresenter, headerMenu);

                if (headerMenu.Items.Count == 0)
                    CreateHeaderMenu();
            };

            treeView.Root = root = new ProgTreeRoot("");
            treeView.ShowRoot = false;
            treeView.ShowLines = false;

            treeView.PreviewKeyDown += TreeView_KeyDown;


            var contextMenu = new ContextMenu();


            menuAdd = WpfFunc.AddMenu(contextMenu, Translate.fmt("btn_add_prog"), null, TryFindResource("Icon_Plus"));
            menuAddSub = WpfFunc.AddMenu(contextMenu, Translate.fmt("btn_add_to_set"), null, TryFindResource("Icon_Plus"));
            contextMenu.Items.Add(new Separator());
            menuRemove = WpfFunc.AddMenu(contextMenu, Translate.fmt("btn_del_progs"), null, TryFindResource("Icon_Remove"));
            contextMenu.Items.Add(new Separator());
            menuMerge = WpfFunc.AddMenu(contextMenu, Translate.fmt("btn_merge_progs"), null, TryFindResource("Icon_Merge"));
            menuSplit = WpfFunc.AddMenu(contextMenu, Translate.fmt("btn_split_progs"), null, TryFindResource("Icon_Split"));
            contextMenu.Items.Add(new Separator());

            menuAccess = WpfFunc.AddMenu(contextMenu, Translate.fmt("btn_access_prog"), null, TryFindResource("Icon_NetAccess"));
            menuAccessNone = WpfFunc.AddMenu(menuAccess, Translate.fmt("acl_none"), null, null, ProgramSet.Config.AccessLevels.Unconfigured);
            menuAccessAllow = WpfFunc.AddMenu(menuAccess, Translate.fmt("acl_allow"), null, null, ProgramSet.Config.AccessLevels.FullAccess);
            menuAccessCustom = WpfFunc.AddMenu(menuAccess, Translate.fmt("acl_edit"), null, null, ProgramSet.Config.AccessLevels.CustomConfig);
            menuAccessLan = WpfFunc.AddMenu(menuAccess, Translate.fmt("acl_lan"), null, null, ProgramSet.Config.AccessLevels.LocalOnly);
            menuAccessBlock = WpfFunc.AddMenu(menuAccess, Translate.fmt("acl_block"), null, null, ProgramSet.Config.AccessLevels.BlockAccess);
            foreach (MenuItem item in menuAccess.Items)
            {
                item.Background = ProgramControl.GetAccessColor((ProgramSet.Config.AccessLevels)item.Tag);
                //item.IsCheckable = true;
            }

            menuNotify = WpfFunc.AddMenu(contextMenu, Translate.fmt("lbl_notify"), null);
            menuNotify.IsCheckable = true;

            contextMenu.Items.Add(new Separator());
            menuRename = WpfFunc.AddMenu(contextMenu, Translate.fmt("btn_rename_prog"), null, TryFindResource("Icon_Rename"));
            menuSetIcon = WpfFunc.AddMenu(contextMenu, Translate.fmt("btn_icon_prog"), null, TryFindResource("Icon_SetIcon"));
            menuCategory = WpfFunc.AddMenu(contextMenu, Translate.fmt("btn_cat_prog"), null, TryFindResource("Icon_Category"));
            


            treeView.ContextMenu = contextMenu;
        }

        public void OnClose()
        {
            if(headerPresenter != null)
                App.SetConfig("GUI", "progTree_Columns", Save());
        }

        private void TreeView_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.C && Keyboard.Modifiers == ModifierKeys.Control)
            {
                List<string> Lines = new List<string>();

                foreach (var item in treeView.SelectedItems)
                {
                    List<string> Cells = new List<string>();
                    
                    // todo: improve that formating and so on
                    foreach (var Column in headerPresenter.Columns)
                    {
                        var temp = (typeof(TreeItem).GetProperty((Column.Header as GridViewColumnHeader).Tag.ToString()).GetValue(item, null) as IComparable);
                        Cells.Add(temp != null ? temp.ToString() : "");
                    }

                    Lines.Add(String.Join("\t", Cells));
                }

                MiscFunc.ClipboardNative.CopyTextToClipboard(String.Join("\r\n", Lines));
                e.Handled = true;
            }
        }

        public void UpdateProgramList(List<ProgramSet> progs)
        {
            root.UpdateProgAllSets(progs);

            SortAndFitlerProgList();
        }

        public void UpdateProgramItems(List<ProgramSet> progs)
        {
            root.UpdateProgSets(progs);

            SortAndFitlerProgList();
        }

        public void SortAndFitlerProgList(FirewallPage.FilterPreset Filter)
        {
            CurFilter = Filter;

            SortAndFitlerProgList();
        }

        private void SortAndFitlerProgList()
        {
            root.ApplyFilter(CurFilter);
            SortTree();
        }

        public List<object> GetSelectedItems()
        {
            List<object> SelectedItems = new List<object>();
            foreach (var item in treeView.SelectedItems)
            {
                var setItem = item as ProgSetTreeItem;
                if (setItem != null)
                    SelectedItems.Add(setItem.progSet);
                
                var progItem = item as ProgramTreeItem;
                if (progItem != null)
                    SelectedItems.Add(progItem.prog);
            }
            return SelectedItems;
        }

        public ProgramSet GetProgSet(Guid guid)
        {
            ProgSetTreeItem item = null;
            if (root.progSets.TryGetValue(guid, out item))
                return item.progSet;
            return null;
        }

        private void treeView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            SelectionChanged?.Invoke(this, new EventArgs());
        }

        private async void treeView_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {

        }

        private async void hdrSort_ClickAsync(object sender, RoutedEventArgs e)
        {
            var header = sender as GridViewColumnHeader;
            var Member = header.Tag?.ToString();
            if (Member != null)
                SortTreeBy(Member);
        }

        string SortMember = "";
        ListSortDirection Direction = ListSortDirection.Descending;

        public void SortTreeBy(string Member)
        {
            if (SortMember.Equals(Member))
            {
                if (Direction == ListSortDirection.Descending)
                    Direction = ListSortDirection.Ascending;
                else
                    Direction = ListSortDirection.Descending;
            }
            else
            {
                var type = typeof(TreeItem).GetProperty(Member);
                if (type == null)
                    return;

                var temp = type.GetValue(root, null).GetType();
                if (temp == typeof(string))
                    Direction = ListSortDirection.Ascending;
                else
                    Direction = ListSortDirection.Descending;

                SortMember = Member;
            }

            SortTree();
        }

        public void SortTree()
        {
            if (SortMember.Length == 0)
                return;

            //CollectionView view = (CollectionView)CollectionViewSource.GetDefaultView(treeView.ItemsSource);
            //view.SortDescriptions.Clear();
            //root.SetSortBy(Member);
            //view.SortDescriptions.Add(new SortDescription(nameof(ITreeItem.SortKey), Direction));

            // workaround for selection working properly
            var items = treeView.SelectedItems.Cast<object>().ToList();

            ManualTreeSorter.Sort(root.Children, SortMember, Direction);

            treeView.SelectedItems.Clear();
            foreach (var item in items)
                treeView.SelectedItems.Add(item);
        }


        private void CreateHeaderMenu()
        {
            headerMenu.Items.Clear();

            var showAll = new MenuItem() { Header = "Show All Columns" };
            showAll.Click += (sender, e) => { ShowAllColumns(); };
            headerMenu.Items.Add(showAll);

            var resetSort = new MenuItem() { Header = "Disable Sorting" };
            resetSort.Click += (sender, e) => { this.SortMember = ""; };
            headerMenu.Items.Add(resetSort);

            if (defaults != null)
            {
                var menuRestore = new MenuItem() { Header = "Restore Defaults" };
                menuRestore.Click += (sender, e) => { ResetDefaults(); };
                headerMenu.Items.Add(menuRestore);
            }

            headerMenu.Items.Add(new Separator());

            var menuItems = new MenuItem[allColumns.Length];

            for (int i = 0; i < allColumns.Length; i++)
            {
                var Column = allColumns[i];
                var Header = Column.Header as GridViewColumnHeader;

                var menuItem = new MenuItem() { Header = Header.Content, IsCheckable = true };
                menuItem.IsChecked = headerPresenter.Columns.Contains(Column);
                menuItem.Click += (sender, e) => {
                    treeView.Root = new ProgTreeRoot("");
                    if (menuItem.IsChecked)
                    {
                        int index = Array.IndexOf(allColumns, Column);
                        if (index < 0 || index > headerPresenter.Columns.Count)
                            index = headerPresenter.Columns.Count;
                        headerPresenter.Columns.Insert(index, Column);
                    }
                    else
                    {
                        headerPresenter.Columns.Remove(Column);
                    }
                    treeView.Root = root;
                };
                menuItems[i] = menuItem;
            }

            foreach (var menuItem in menuItems)
                headerMenu.Items.Add(menuItem);
        }

        public void ShowAllColumns()
        {
            treeView.Root = new ProgTreeRoot("");

            headerPresenter.Columns.Clear();
            foreach (var Column in allColumns)
                headerPresenter.Columns.Add(Column);

            treeView.Root = root;

            CreateHeaderMenu();
        }

        public void ResetDefaults()
        {
            if (defaults == null)
                return;

            Restore(defaults);
        }

        public bool Restore(string state, string defaults)
        {
            this.defaults = defaults;
            if (Restore(state))
                return true;
            return Restore(defaults);
        }

        public bool Restore(string state)
        {
            if (state == null || state.Length == 0)
                return false;

            treeView.Root = new ProgTreeRoot("");
            
            var stateTemp = TextHelpers.Split2(state, "#"); // in case we want to add some more info
            List<string> State = TextHelpers.SplitStr(stateTemp.Item1, "|", true);

            headerPresenter.Columns.Clear();

            for (int i = 0; i < State.Count; i++)
            {
                var PosWidth = TextHelpers.Split2(State[i], ";");

                int Pos = MiscFunc.parseInt(PosWidth.Item1, -1);
                if (Pos < 0 || Pos >= allColumns.Length)
                    continue;

                var Column = allColumns[Pos];
                if(!headerPresenter.Columns.Contains(Column))
                    headerPresenter.Columns.Add(Column);

                Column.Width = MiscFunc.parseInt(PosWidth.Item2, 50);
            }

            treeView.Root = root;

            return true;
        }

        public string Save()
        {
            List<string> State = new List<string>();

            for (int i = 0; i < headerPresenter.Columns.Count; i++)
            {
                var Column = headerPresenter.Columns[i];
                var PosWidth = "";
                PosWidth = Array.IndexOf(allColumns, Column).ToString() + ";" + ((int)Column.ActualWidth).ToString();
                State.Add(PosWidth);
            }

            // todo: also save sort config

            return string.Join("|", State);
        }
    }
}
