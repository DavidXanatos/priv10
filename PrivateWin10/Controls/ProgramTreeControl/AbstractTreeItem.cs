using System;
using System.Linq;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Windows.Media;
using ICSharpCode.TreeView;
using System.IO;
using System.Windows;

namespace PrivateWin10.Controls
{
	public interface ITreeItem
	{
		String Title { get; }
        Object SortKey { get; }
        //String SortKey { get; }
        //String RelativePath { get; }

        // columns BEGIN
        string Category { get; }
        string Access{ get; }

        int Rules { get; }
        int Allowed { get; }
        int Blocked { get; }
        DateTime LastActivity { get; }

        int Sockets { get; }
        UInt64 UpRate { get; }
        UInt64 DownRate { get; }
        UInt64 UpTotal { get; }
        UInt64 DownTotal { get; }

        string Program { get; }
        // columns END


        ITreeItem ParentTreeItem { get; }
        //IEnumerable<ITreeItem> AllChildren { get; }
        IRootItem RootTreeItem { get; }

        SharpTreeNodeCollection Children { get; }
		SharpTreeNode Parent { get; }
		Object Text { get; }
		Object Icon { get; }
		Object ToolTip { get; }
		Int32 Level { get; }
		Boolean IsRoot { get; }
		Boolean IsHidden { get; set; }
		Boolean IsVisible { get; }
		Boolean IsSelected { get; set; }
	}

    public interface IRootItem : ITreeItem
    {
        //string SortMember { get; }
    }

    public interface IBranchItem : ITreeItem { }

    public interface ILeafItem : ITreeItem { }

    /*public class TreeSortProxy : IComparable
    {
        TreeItem This;

        string Key = Guid.NewGuid().ToString();

        List<TreeItem> TreePath = new List<TreeItem>();

        static void MkPath(TreeItem Parent, ref List<TreeItem> TreePath)
        {
            if (Parent == null)
                return;
            TreePath.Insert(0, Parent);
            MkPath(Parent.Parent as TreeItem, ref TreePath);
        }

        public TreeSortProxy(TreeItem item)
        {
            This = item;
            //root = item.RootTreeItem;
            MkPath(item.Parent as TreeItem, ref TreePath);
        }

        int IComparable.CompareTo(object obj)
        {
            var That = (obj as TreeSortProxy).This as TreeItem;

            if (This.Parent != That.Parent)
            {
                var LP = This.SortKey as TreeSortProxy;
                var RP = That.SortKey as TreeSortProxy;
                int Level = MiscFunc.Min(LP.TreePath.Count, RP.TreePath.Count);
                if (Level > 0)
                {
                    var L = LP.TreePath[Level - 1];
                    var R = RP.TreePath[Level - 1];
                    int ret = (L.SortKey as IComparable).CompareTo(R.SortKey);
                    if (ret != 0)
                        return ret;
                }

                if (LP.TreePath.Count > RP.TreePath.Count)
                {
                    int ret = (That.SortKey as IComparable).CompareTo(LP.TreePath[Level].SortKey);
                    if (ret != 0)
                        return ret;

                    return RP.TreePath.Count - LP.TreePath.Count;
                }
                else if (LP.TreePath.Count < RP.TreePath.Count)
                {
                    int ret = (This.SortKey as IComparable).CompareTo(RP.TreePath[Level].SortKey);
                    if (ret != 0)
                        return ret;

                    return LP.TreePath.Count - RP.TreePath.Count;
                }

                return 0;
            }
            else // same parent easy...
            {
                string Member = This.RootTreeItem.SortMember;

                var L = (typeof(TreeItem).GetProperty(Member).GetValue(This, null) as IComparable);
                var R = (typeof(TreeItem).GetProperty(Member).GetValue(That, null) as IComparable);

                if (L == null && R == null) return 0;
                else if (L == null) return 1;
                else if (R == null) return -1;

                int ret = L.CompareTo(R);
                if (ret == 0)
                {
                    // Note: Sorting is a mess with this list view immitating a tree controll, we need to have a deterministic sort even for values that are same.
                    //          Hence we use the random key whenever the values are identical.
                    // Todo: Add multi level sorting to mitigate this issue
                    return (This.SortKey as TreeSortProxy).Key.CompareTo((That.SortKey as TreeSortProxy).Key);
                }
                return ret;
            }
        }
    }*/

    public abstract class TreeItem : SharpTreeNode, ITreeItem
	{
		public virtual String Title { get; }

        //private TreeSortProxy sortProxy;
        //public virtual object SortKey => this.sortProxy ?? (sortProxy = new TreeSortProxy(this));
        public virtual object SortKey => null;

        public ITreeItem ParentTreeItem => this.Parent as ITreeItem;

        //private String _sortKey;
        //public virtual String SortKey => 
        //	this._sortKey = this._sortKey ??
        //	$"{this.ParentTreeItem?.SortKey}\\{this.Text}".Trim('\\');


        //private String _relativePath;
        //public virtual String RelativePath =>
        //	this._relativePath = this._relativePath ?? 
        //	$"{this.ParentTreeItem?.RelativePath}\\{this.Text}".Trim('\\');

        //private IEnumerable<ITreeItem> _allChildren;
        //public virtual IEnumerable<ITreeItem> AllChildren =>
        //	this._allChildren = this._allChildren ??
        //	this.Children
        //		.OfType<ITreeItem>()
        //		.SelectMany(c => c.AllChildren.Union(new[] { c }))
        //		.OfType<ITreeItem>()
        //		.ToArray();

        public virtual IRootItem RootTreeItem => ParentTreeItem.RootTreeItem;

        public override Object Text => this.Title;

        protected ImageSource cachedIcon;
        protected ImageSource GetIcon(string iconPath)
        {
            if (cachedIcon == null)
            {
                cachedIcon = ImgFunc.ExeIcon16; // set a temporary stand in
                ImgFunc.GetIconAsync(iconPath, 16, (ImageSource src) => {
                    if (Application.Current != null)
                    {
                        Application.Current.Dispatcher.Invoke(new Action(() =>
                        {
                            cachedIcon = src;
                            this.RaisePropertyChanged(nameof(Icon));
                        }));
                    }
                    return 0;
                });
            }
            return cachedIcon;
        }

        // columns BEGIN
        public abstract string Category { get; }
        public abstract string Access { get; }
        public abstract string AccessTag { get; }


        public abstract int Rules { get; }
        public abstract int Allowed { get; }
        public abstract int Blocked { get; }
        public abstract DateTime LastActivity { get; }

        public abstract int Sockets { get; }
        public abstract UInt64 UpRate { get; }
        public abstract UInt64 DownRate { get; }
        public abstract UInt64 UpTotal { get; }
        public abstract UInt64 DownTotal { get; }

        public abstract string Program { get; }
        // columns END

        internal TreeItem(String title)
		{
			this.Title = title;
		}
    }
}
