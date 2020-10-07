using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Ribbon;
using System.Windows.Media;

namespace MiscHelpers
{
    public class WpfFunc
    {
        public class ViewModelHelper : INotifyPropertyChanged//, IDisposable
        {
            /*public void Dispose()
            {
            }*/

            protected void SetProperty<T>(string Name, T newValue, ref T curValue)
            {
                if (Equals(newValue, curValue))
                    return;
                curValue = newValue;
                RaisePropertyChanged(Name);
            }

            protected void SetPropertyCmb(string Name, ContentControl newValue, ref ContentControl curValue, ref string curText)
            {
                SetProperty(Name, newValue, ref curValue);
                if (curValue != null)
                    curText = curValue.Content.ToString();
            }


            #region INotifyPropertyChanged Members

            public event PropertyChangedEventHandler PropertyChanged;

            /// <summary>
            /// Raises the PropertyChanged event if needed.
            /// </summary>
            /// <param name="propertyName">The name of the property that changed.</param>
            protected virtual void RaisePropertyChanged(string propertyName)
            {
                if (PropertyChanged != null)
                {
                    PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
                }
            }

            #endregion
        }

        static public ContentControl CmbPick(ComboBox box, string tag)
        {
            if (tag == null)
                return null;
            for (int i = 0; i < box.Items.Count; i++)
            {
                ContentControl item = (box.Items[i] as ContentControl);
                if (item.Tag != null && item.Tag.ToString().Equals(tag))
                    return item;
            }
            return null;
        }

        static public bool CmbSelect(ComboBox box, string tag)
        {
            ContentControl item = CmbPick(box, tag);
            if (item != null)
            {
                box.SelectedItem = item;
                return true;
            }
            return false;

        }

        static public ComboBoxItem CmbAdd(ComboBox box, string text, object tag)
        {
            var item = new ComboBoxItem() { Content = text, Tag = tag };
            box.Items.Add(item);
            return item;
        }


        static public RibbonGalleryItem CmbPick(RibbonGalleryCategory cat, string tag)
        {
            if (tag == null)
                return null;
            for (int i = 0; i < cat.Items.Count; i++)
            {
                RibbonGalleryItem item = (cat.Items[i] as RibbonGalleryItem);
                if (item.Tag != null && item.Tag.ToString().Equals(tag))
                    return item;
            }
            return null;
        }

        static public bool CmbSelect(RibbonGallery gal, string tag)
        {
            ContentControl item = CmbPick(gal.Items[0] as RibbonGalleryCategory, tag);
            if (item != null)
            {
                gal.SelectedItem = item;
                return true;
            }
            return false;
        }

        static public void CmbAdd(RibbonGallery gal, string text, object tag)
        {
            (gal.Items[0] as RibbonGalleryCategory).Items.Add(new RibbonGalleryItem { Content = text, Tag = tag });
        }

        public static List<string> SplitAndValidate(string Values, ref bool? duplicate)
        {
            if (Values == null)
                return null;
            List<string> ValueList = new List<string>();
            foreach (string Value in Values.Split(','))
            {
                string temp = Value.Trim();
                if (duplicate != null && ValueList.Contains(temp))
                {
                    duplicate = true;
                    return null;
                }
                ValueList.Add(temp);
            }
            if (ValueList.Count == 0)
                return null;
            return ValueList;
        }

        public static T FindChild<T>(DependencyObject parentObj) where T : DependencyObject
        {
            if (parentObj == null)
                return null;

            try
            {
                if (parentObj is T)
                    return parentObj as T;

                for (int i = 0; i < System.Windows.Media.VisualTreeHelper.GetChildrenCount(parentObj); i++)
                {
                    T childObj = FindChild<T>(System.Windows.Media.VisualTreeHelper.GetChild(parentObj, i));
                    if (childObj != null)
                        return childObj;
                }
            }
            catch (Exception err)
            {
                AppLog.Exception(err);
            }
            return null;
        }

        public static MenuItem AddMenu(ItemsControl menu, string label, RoutedEventHandler handler, object icon = null, object Tag = null)
        {
            var item = new MenuItem() { Header = label, Tag = Tag };
            if (handler != null)
                item.Click += handler;
            if (icon != null)
                item.Icon = new System.Windows.Controls.Image() { Source = icon as ImageSource };
            menu.Items.Add(item);
            return item;
        }

        private void DumpVisualTree(DependencyObject parent, int level)
        {
            string typeName = parent.GetType().Name;
            string name = (string)(parent.GetValue(FrameworkElement.NameProperty) ?? "");

            Console.WriteLine(string.Format("{0}: {1}", typeName, name));

            if (parent == null) return;

            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
            {
                DependencyObject child = VisualTreeHelper.GetChild(parent, i);
                DumpVisualTree(child, level + 1);
            }
        }

        private void DumpLogicalTree(object parent, int level)
        {
            string typeName = parent.GetType().Name;
            string name = null;
            DependencyObject doParent = parent as DependencyObject;
            // Not everything in the logical tree is a dependency object
            if (doParent != null)
            {
                name = (string)(doParent.GetValue(FrameworkElement.NameProperty) ?? "");
            }
            else
            {
                name = parent.ToString();
            }

            Console.WriteLine(string.Format("{0}: {1}", typeName, name));

            if (doParent == null) return;

            foreach (object child in LogicalTreeHelper.GetChildren(doParent))
            {
                DumpLogicalTree(child, level + 1);
            }
        }
    }

    public static class DependencyObjectExtensions
    {
        private static readonly PropertyInfo InheritanceContextProp = typeof(DependencyObject).GetProperty("InheritanceContext", BindingFlags.NonPublic | BindingFlags.Instance);

        public static IEnumerable<DependencyObject> GetParents(this DependencyObject child)
        {
            while (child != null)
            {
                var parent = LogicalTreeHelper.GetParent(child);
                if (parent == null)
                {
                    if (child is FrameworkElement)
                    {
                        parent = VisualTreeHelper.GetParent(child);
                    }
                    if (parent == null && child is ContentElement)
                    {
                        parent = ContentOperations.GetParent((ContentElement)child);
                    }
                    if (parent == null)
                    {
                        parent = InheritanceContextProp.GetValue(child, null) as DependencyObject;
                    }
                }
                child = parent;
                yield return parent;
            }
        }
    }
}