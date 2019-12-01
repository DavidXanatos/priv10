using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;

namespace PrivateWin10
{
    public class DataGridExt
    {
        DataGrid dataGrid;
        ContextMenu headerMenu;

        string defaults = null;
        bool Hold = false;

        public DataGridExt(DataGrid dataGrid)
        {
            this.dataGrid = dataGrid;

            dataGrid.RowHeaderWidth = 0;

            headerMenu = new ContextMenu();

            dataGrid.Loaded += (sender, e) => {

                var headersPresenter = WpfFunc.FindChild<DataGridColumnHeadersPresenter>(dataGrid);
                if (headersPresenter != null)
                    ContextMenuService.SetContextMenu(headersPresenter, this.headerMenu);

                if(headerMenu.Items.Count == 0)
                    CreateHeaderMenu();
            };

            dataGrid.ContextMenuOpening += (sender, e) => {  };

            dataGrid.ColumnReordered += (sender, e) => { if (!Hold) CreateHeaderMenu(); };

            //dataGrid.PreviewKeyDown += DataGrid_KeyDown;
        }

        /*private void DataGrid_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.C && Keyboard.Modifiers == ModifierKeys.Control)
            {
                //MiscFunc.ClipboardNative.CopyTextToClipboard("test");
                //e.Handled = true;
            }
        }*/

        private void CreateHeaderMenu()
        {
            headerMenu.Items.Clear();

            var showAll = new MenuItem() { Header = "Show All Columns" };
            showAll.Click += (sender, e) => { ShowAllColumns(); };
            headerMenu.Items.Add(showAll);

            /*var showNumbers = new MenuItem() { Header = "Show Row Numbers", IsCheckable = true };
            showNumbers.Click += (sender, e) => { dataGrid.RowHeaderWidth = showNumbers.IsChecked ? double.NaN: 0; };
            headerMenu.Items.Add(showNumbers);*/

            var resetSort = new MenuItem() { Header = "Reset Sorting" };
            resetSort.Click += (sender, e) => { ResetSorting(); };
            headerMenu.Items.Add(resetSort);

            var menuRestore = new MenuItem() { Header = "Restore Defaults" };
            menuRestore.Click += (sender, e) => { ResetDefaults(); };
            headerMenu.Items.Add(menuRestore);

            headerMenu.Items.Add(new Separator());

            var menuItems = new MenuItem[dataGrid.Columns.Count];

            for (int i = 0; i < dataGrid.Columns.Count; i++)
            {
                var Column = dataGrid.Columns[i];
                var menuItem = new MenuItem() { Header = Column.Header, IsCheckable = true, IsChecked = (Column.Visibility == Visibility.Visible) };
                menuItem.Click += (sender, e) => {
                    Column.Visibility = menuItem.IsChecked ? Visibility.Visible : Visibility.Collapsed;
                };
                menuItems[Column.DisplayIndex == -1 ? i : Column.DisplayIndex] = menuItem;
            }

            foreach(var menuItem in menuItems)
                headerMenu.Items.Add(menuItem);
        }

        public void ShowAllColumns()
        {
            foreach (var Column in dataGrid.Columns)
                Column.Visibility = Visibility.Visible;

            CreateHeaderMenu();
        }

        public void ResetDefaults()
        {
            if (defaults != null && Restore(defaults))
                return;

            Hold = true;
            for (int i = 0; i < dataGrid.Columns.Count; i++)
            {
                var Column = dataGrid.Columns[i];
                Column.Visibility = Visibility.Visible;
                Column.DisplayIndex = i;
            }
            Hold = false;

            CreateHeaderMenu();
        }

        public void ResetSorting()
        {
            dataGrid.Items.SortDescriptions.Clear();
            foreach (DataGridColumn column in dataGrid.Columns)
                column.SortDirection = null;
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
            var stateTemp = TextHelpers.Split2(state, "#"); // in case we want to add some more info
            List<string> State = TextHelpers.SplitStr(stateTemp.Item1, "|", true);
            if (State.Count != dataGrid.Columns.Count)
                return false;

            Hold = true;
            for (int i = 0; i < dataGrid.Columns.Count; i++)
            {
                var Column = dataGrid.Columns[i];
                if (State[i].Length == 0)
                    Column.Visibility = Visibility.Collapsed;
                else
                {
                    Column.Visibility = Visibility.Visible;

                    var PosWidth = TextHelpers.Split2(State[i], ";");

                    int Pos = MiscFunc.parseInt(PosWidth.Item1, -1);
                    if (Pos == -1)
                        continue;

                    Column.DisplayIndex = Pos;
                    Column.Width = new DataGridLength(MiscFunc.parseInt(PosWidth.Item2, 50));
                }

            }
            Hold = false;

            CreateHeaderMenu();
            return true;
        }

        public string Save()
        {
            List<string> State = new List<string>();

            for (int i = 0; i < dataGrid.Columns.Count; i++)
            {
                var Column = dataGrid.Columns[i];
                var PosWidth = "";
                if (Column.Visibility == Visibility.Visible)
                {
                    PosWidth = Column.DisplayIndex.ToString() + ";" + ((int)Column.Width.DisplayValue).ToString();
                }
                State.Add(PosWidth);
            }

            // todo: also save sort config

            return string.Join("|", State);
        }
    }
}
