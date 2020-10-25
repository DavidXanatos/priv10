using ICSharpCode.TreeView;
using MiscHelpers;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PrivateWin10.Controls
{
    static public class ManualTreeSorter
    {
        static int SwapCount = 0;

        static void Swap(SharpTreeNodeCollection array, int i, int j)
        {
            SwapCount++;
            
            Debug.Assert(i < j);
            SharpTreeNode temp = array[i];
            SharpTreeNode tmp = array[j];
            array.RemoveAt(j);
            array[i] = tmp;
            array.Insert(j, temp);
            
            //array.Swap(i, j);
        }

        static void BubbleSort(SharpTreeNodeCollection num, Comparison<SharpTreeNode> comparison)
        {
            int i, j;
            bool flag = true;
            int numLength = num.Count;
            for (i = 1; (i <= numLength) && flag; i++)
            {
                flag = false;
                for (j = 0; j < (numLength - 1); j++)
                {
                    if (comparison(num[j + 1], num[j]) > 0)
                    {
                        Swap(num, j, j+1);
                        flag = true;
                    }
                }
            }
        }

        static void ExchangeSort(SharpTreeNodeCollection num, Comparison<SharpTreeNode> comparison)
        {
            int i, j;
            int numLength = num.Count;
            for (i = 0; i < (numLength - 1); i++)
            {
                for (j = (i + 1); j < numLength; j++)
                {
                    if (comparison(num[i], num[j]) < 0)
                        Swap(num, i, j);
                }
            }
        }

        static void SelectionSort(SharpTreeNodeCollection num, Comparison<SharpTreeNode> comparison)
        {
            int i, j, first;
            int numLength = num.Count;
            for (i = numLength - 1; i > 0; i--)
            {
                first = 0;
                for (j = 1; j <= i; j++)
                {
                    if (comparison(num[j], num[first]) < 0)
                        first = j;
                }
                if (first != i)
                    Swap(num, first, i);
            }
            return;
        }

        /*static void InsertionSort(SharpTreeNodeCollection num, Comparison<SharpTreeNode> comparison)
        {
            int i, j, key, numLength = num.Count;
            for (j = 1; j < numLength; j++)    // Start with 1 (not 0)
            {
                key = num[j];
                for (i = j - 1; (i >= 0) && (num[i] < key); i--)   // Smaller values move up
                {
                    num[i + 1] = num[i];
                }
                num[i + 1] = key;    //Put key into its proper location
            }
        }*/

        static void ShellSort(SharpTreeNodeCollection num, Comparison<SharpTreeNode> comparison)
        {
            int i, numLength = num.Count;
            bool flag = true;
            int d = numLength;
            while (flag || (d > 1))
            {
                flag = false;
                d = (d + 1) / 2;
                for (i = 0; i < (numLength - d); i++)
                {
                    if (comparison(num[i + d], num[i]) > 0)
                    {
                        Swap(num, i, i + d);
                        flag = true;
                    }
                }
            }
        }

        static void quicksort(SharpTreeNodeCollection num, int top, int bottom, Comparison<SharpTreeNode> comparison)
        {
            int middle;
            if (top < bottom)
            {
                middle = partition(num, top, bottom, comparison);
                quicksort(num, top, middle, comparison);
                quicksort(num, middle + 1, bottom, comparison);
            }
        }

        static int partition(SharpTreeNodeCollection array, int top, int bottom, Comparison<SharpTreeNode> comparison)
        {
            SharpTreeNode x = array[top];
            int i = top - 1;
            int j = bottom + 1;
            do
            {
                do
                {
                    j--;
                } while (comparison(x, array[j]) > 0);

                do
                {
                    i++;
                } while (comparison(x, array[i]) < 0);

                if (i < j)
                {
                    if (comparison(array[j], array[i]) != 0)
                        Swap(array, i, j);
                }
            } while (i < j);
            return j;
        }

        static void QuickSort(SharpTreeNodeCollection num, Comparison<SharpTreeNode> comparison)
        {
            quicksort(num, 0, num.Count - 1, comparison);
        }

        static void Sort(SharpTreeNodeCollection Children, string sortMember, ListSortDirection direction, Comparison<SharpTreeNode> comparison)
        {
            if (Children.Count == 0)
                return;

            QuickSort(Children, comparison);
            //SelectionSort(Children, comparison);
            //BubbleSort(Children, comparison);
            //ShellSort(Children, comparison);
            //ExchangeSort(Children, comparison);
            //InsertionSort(Children, comparison);

            foreach (var child in Children.OfType<TreeItem>())
                Sort(child.Children, sortMember, direction, comparison);
        }

        static public void Sort(SharpTreeNodeCollection Children, string sortMember, ListSortDirection direction)
        {
            Comparison<SharpTreeNode> comparison = (This, That) =>
            {
                var L = (typeof(TreeItem).GetProperty(sortMember).GetValue(This, null) as IComparable);
                var R = (typeof(TreeItem).GetProperty(sortMember).GetValue(That, null) as IComparable);

                int ret;
                if (L == null && R == null)
                    ret = 0;
                else if (L == null)
                    ret = 1;
                else if (R == null)
                    ret = -1;
                else
                    ret = L.CompareTo(R);

                if (direction == ListSortDirection.Ascending)
                    return -ret;
                return ret;
            };

            SwapCount = 0;
            var watch = System.Diagnostics.Stopwatch.StartNew();

            Sort(Children, sortMember, direction, comparison);

            watch.Stop();
            var elapsedMs = watch.ElapsedMilliseconds;

            if(elapsedMs > 100)
                AppLog.Debug("TreeView Sorting took very log: {0} ms and required {1} swaps", elapsedMs, SwapCount);
        }
    }
}
