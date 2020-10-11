using MiscHelpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;

namespace PrivateWin10
{
    public interface IControlItem<U>
    {
        event RoutedEventHandler Click;

        void SetFocus(bool set = true);
        //bool GetFocus();

        void DoUpdate(U value);
    }

    public class ControlList<T, U> where T: UserControl, IControlItem<U>
    {
        public SortedDictionary<string, T> Items = new SortedDictionary<string, T>();
        public List<T> SelectedItems = new List<T>();
        public event EventHandler<EventArgs> SelectionChanged;
        public bool SingleSellection = false;

        ScrollViewer ItemScroll;
        Grid ItemGrid;
        Func<U, T> ItemFactory;
        Func<U, string> GetGuid;
        Action<List<T>> Sorter;
        Func<T, bool> Filter;

        public ControlList(ScrollViewer itemScroll, Func<U, T>itemFactory, Func<U, string> getGuid, Action<List<T>> sorter = null, Func<T, bool> filter = null)
        {
            ItemScroll = itemScroll;
            ItemScroll.PreviewKeyDown += process_KeyEventHandler;

            ItemGrid = (Grid)ItemScroll.Content;

            ItemFactory = itemFactory;
            GetGuid = getGuid;
            Sorter = sorter;
            Filter = filter;
        }

        public void UpdateItems(List<U> newItems)
        {
            SortedDictionary<string, T> OldItems = new SortedDictionary<string, T>(Items);

            if (newItems != null)
            {
                foreach (U value in newItems)
                {
                    T item;
                    if (Items.TryGetValue(GetGuid(value), out item))
                    {
                        item.DoUpdate(value);
                        OldItems.Remove(GetGuid(value));
                    }
                    else
                        item = AddItem(value);
                }
            }

            foreach (var guid in OldItems.Keys)
            {
                Items.Remove(guid);
                T item;
                if (OldItems.TryGetValue(guid, out item))
                    ItemGrid.Children.Remove(item);
            }

            SortAndFitlerList();
        }

        public T AddItem(U value)
        {
            T item = ItemFactory(value);
            Items.Add(GetGuid(value), item);
            //item.Tag = process;
            item.VerticalAlignment = VerticalAlignment.Top;
            item.HorizontalAlignment = HorizontalAlignment.Stretch;
            item.Margin = new Thickness(1, 1, 1, 1);
            //item.MouseDown += new MouseButtonEventHandler(process_Click);
            item.Click += new RoutedEventHandler(process_Click);

            ItemGrid.Children.Add(item);

            return item;
        }

        public void UpdateItem(U value)
        {
            T item;
            if (Items.TryGetValue(GetGuid(value), out item))
                item.DoUpdate(value);
            else
                item = AddItem(value);   
        }

        public void SortAndFitlerList()
        {
            List<T> OrderList = Items.Values.ToList();

            Sorter?.Invoke(OrderList);

            for (int i = 0; i < OrderList.Count; i++)
            {
                if (i >= ItemGrid.RowDefinitions.Count)
                    ItemGrid.RowDefinitions.Add(new RowDefinition());
                RowDefinition row = ItemGrid.RowDefinitions[i];
                //ProcessControl item = tmp.Item2;
                T item = OrderList[i];
                if (row.Height.Value != item.Height)
                    row.Height = GridLength.Auto; //new GridLength(item.Height + 2);
                Grid.SetRow(item, i);

                item.Visibility = Filter?.Invoke(item) == true ? Visibility.Collapsed : Visibility.Visible;
            }

            while (OrderList.Count < ItemGrid.RowDefinitions.Count)
                ItemGrid.RowDefinitions.RemoveAt(OrderList.Count);
        }

        void process_Click(object sender, RoutedEventArgs e)
        {
            T curItem = (T)sender;

            if (!SingleSellection && (Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control)
            {
                if (SelectedItems.Contains(curItem))
                {
                    curItem.SetFocus(false);
                    SelectedItems.Remove(curItem);
                }
                else
                {
                    curItem.SetFocus(true);
                    SelectedItems.Add(curItem);
                }
            }
            else
            {
                foreach (T cur in SelectedItems)
                    cur.SetFocus(false);
                SelectedItems.Clear();
                SelectedItems.Add(curItem);
                curItem.SetFocus(true);
            }

            SelectionChanged?.Invoke(this, new EventArgs());
        }

        void process_KeyEventHandler(object sender, KeyEventArgs e)
        {
            if ((e.Key == Key.Up || e.Key == Key.Down) )//&& !((Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control))
            {
                T curItem = default(T);
                if (SelectedItems.Count > 0)
                {
                    foreach (T cur in SelectedItems)
                        cur.SetFocus(false);
                    curItem = SelectedItems[SelectedItems.Count - 1];
                    SelectedItems.Clear();
                }

                e.Handled = true;
                int curRow = Grid.GetRow(curItem);
                if (e.Key == Key.Up)
                {
                    while (curRow > 0)
                    {
                        curRow--;
                        T cut = ItemGrid.Children.Cast<T>().First((c) => Grid.GetRow(c) == curRow);
                        if (cut.Visibility == Visibility.Visible)
                        {
                            curItem = cut;
                            ItemScroll.ScrollToVerticalOffset(ItemScroll.VerticalOffset - (curItem.ActualHeight + 2));
                            break;
                        }
                    }
                }
                else if (e.Key == Key.Down)
                {
                    while (curRow < ItemGrid.Children.Count - 1)
                    {
                        curRow++;
                        T curProg = ItemGrid.Children.Cast<T>().First((c) => Grid.GetRow(c) == curRow);
                        if (curProg.Visibility == Visibility.Visible)
                        {
                            curItem = curProg;
                            ItemScroll.ScrollToVerticalOffset(ItemScroll.VerticalOffset + (curItem.ActualHeight + 2));
                            break;
                        }
                    }
                }

                if (curItem == null)
                    return;

                curItem.SetFocus(true);
                SelectedItems.Add(curItem);

                SelectionChanged?.Invoke(this, new EventArgs());
            }
        }
    }
}
