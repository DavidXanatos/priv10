using System;
using System.Linq;
using System.Collections.Generic;
using System.Windows.Media;
using System.Globalization;
using System.Diagnostics;
using System.Windows.Controls;
using System.Threading.Tasks;
using System.IO;
using PrivateWin10.Pages;

namespace PrivateWin10.Controls
{
    public class ProgTreeRoot : TreeItem, IRootItem
    {
        public virtual Boolean Expanded { get; set; } = false;

        //public override String RelativePath => String.Empty;

        public override IRootItem RootTreeItem => this;

        /*private string sortMember = "Text";
        public string SortMember => sortMember;
        public void SetSortBy(string Member) { sortMember = Member; }*/

        public override Object Text => "";
        public override Object Icon => null;

        // columns BEGIN
        public override string Category => "";
        public override string Access => "";
        public override string AccessTag => "";

        public override int Rules => 0;
        public override int Allowed => 0;
        public override int Blocked => 0;
        public override DateTime LastActivity => DateTime.MinValue;

        public override int Sockets => 0;
        public override UInt64 UpRate => 0;
        public override UInt64 DownRate => 0;
        public override UInt64 UpTotal => 0;
        public override UInt64 DownTotal => 0;

        public override string Program => "";
        // columns END

        public Dictionary<Guid, ProgSetTreeItem> progSets = new Dictionary<Guid, ProgSetTreeItem>();

        public ProgTreeRoot(String name = null)
            : base(name)
        {
        }

        public void UpdateProgAllSets(List<ProgramSet> progs)
        {
            Dictionary<Guid, ProgSetTreeItem> oldSets = new Dictionary<Guid, ProgSetTreeItem>(progSets);
            //foreach (ProgSetTreeItem setItem in this.Children)
            //    oldSets.Add(setItem.progSet.guid, setItem);

            foreach (var progSet in progs)
            {
                ProgSetTreeItem setItem;
                if (oldSets.TryGetValue(progSet.guid, out setItem))
                {
                    oldSets.Remove(progSet.guid);
                    setItem.Update(progSet);
                }
                else
                    AddItem(progSet);
            }

            foreach (var setItem in oldSets)
            {
                progSets.Remove(setItem.Key);
                this.Children.Remove(setItem.Value);
            }
        }

        private ProgSetTreeItem AddItem(ProgramSet progSet)
        {
            ProgSetTreeItem setItem = new ProgSetTreeItem(progSet);
            progSets.Add(progSet.guid, setItem);
            this.Children.Add(setItem);
            return setItem;
        }

        public void UpdateProgSets(List<ProgramSet> progs)
        {
            foreach (var progSet in progs)
            {
                ProgSetTreeItem setItem;
                if (progSets.TryGetValue(progSet.guid, out setItem))
                    setItem.Update(progSet);
                else
                    AddItem(progSet);
            }
        }

        public void ApplyFilter(FirewallPage.FilterPreset CurFilter)
        {
            foreach (var setItem in progSets.Values)
                setItem.IsHidden = (CurFilter != null && FirewallPage.DoFilter(CurFilter, setItem.progSet));
        }
    }
}
