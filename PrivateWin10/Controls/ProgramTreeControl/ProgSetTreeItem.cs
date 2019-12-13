using System;
using System.Linq;
using System.Collections.Generic;
using System.Windows.Media;
using System.Windows;
using System.ComponentModel;

namespace PrivateWin10.Controls
{
    public class ProgSetTreeItem : TreeItem, IBranchItem
    {
        public virtual Boolean Expanded { get; set; } = false;

        public override Object Text => progSet.config.Name;

        public override Object Icon => GetIcon(progSet.GetIcon());

        // columns BEGIN
        public override string Category => progSet.config.Category;
        public override string Access => progSet.config.GetAccess().ToString(); // progSet.config.NetAccess.ToString();

        public override int Rules => progSet.Programs.Values.Sum(t => t.RuleCount);
        public override int Allowed => progSet.Programs.Values.Sum(t => t.AllowedCount);
        public override int Blocked => progSet.Programs.Values.Sum(t => t.BlockedCount);
        public override DateTime LastActivity => progSet.GetLastActivity();
        //public override DateTime LastActivity => this.AllChildren.OfType<ILeafItem>().Max(t => t.LastActivity);
        //public override Int64 Length => this.AllChildren.OfType<ILeafItem>().Sum(t => t.Length);

        public override int Sockets => progSet.Programs.Values.Sum(t => t.SocketCount);
        public override UInt64 UpRate => (ulong)progSet.Programs.Values.Sum(t => (long)t.UploadRate);
        public override UInt64 DownRate => (ulong)progSet.Programs.Values.Sum(t => (long)t.DownloadRate);
        public override UInt64 UpTotal => (ulong)progSet.Programs.Values.Sum(t => (long)t.TotalUpload);
        public override UInt64 DownTotal => (ulong)progSet.Programs.Values.Sum(t => (long)t.TotalDownload);

        public override string Program => progSet.Programs.Count == 1 ? progSet.Programs.Values.First().ID.FormatString() : Translate.fmt("lbl_prog_set");
        // columns END

        //private String _sortKey;
		//public override String SortKey =>
		//	this._sortKey = this._sortKey ??
		//	$"{this.ParentTreeItem?.SortKey}\\__{this.Text}".Trim('\\');


        public ProgramSet progSet { get; protected set; }

        public ProgSetTreeItem(ProgramSet progSet) : base(progSet.guid.ToString())
        {
            this.progSet = progSet;
            UpdatePrograms(progSet.Programs);
        }

        public void Update(ProgramSet progSet)
        {
            var old_progSet = this.progSet;
            this.progSet = progSet;

            UpdatePrograms(progSet.Programs);

            var old_progs = old_progSet.Programs.Values;
            var progs = progSet.Programs.Values;

            if (!MiscFunc.IsEqual(old_progSet.config.Name, progSet.config.Name)) this.RaisePropertyChanged(nameof(Text));
            if (!MiscFunc.IsEqual(old_progSet.config.Icon, progSet.config.Icon)) { cachedIcon = null; this.RaisePropertyChanged(nameof(Icon)); }

            if (!MiscFunc.IsEqual(old_progSet.config.Category, progSet.config.Category)) this.RaisePropertyChanged(nameof(Category));
            if (!MiscFunc.IsEqual(old_progSet.config.NetAccess, progSet.config.NetAccess)) this.RaisePropertyChanged(nameof(Access));

            if (!MiscFunc.IsEqual(old_progs.Sum(t => t.RuleCount), progs.Sum(t => t.RuleCount))) this.RaisePropertyChanged(nameof(Rules));
            if (!MiscFunc.IsEqual(old_progs.Sum(t => t.AllowedCount), progs.Sum(t => t.AllowedCount))) this.RaisePropertyChanged(nameof(Allowed));
            if (!MiscFunc.IsEqual(old_progs.Sum(t => t.BlockedCount), progs.Sum(t => t.BlockedCount))) this.RaisePropertyChanged(nameof(Blocked));
            if (!MiscFunc.IsEqual(old_progSet.GetLastActivity(), progSet.GetLastActivity())) this.RaisePropertyChanged(nameof(LastActivity));

            if (!MiscFunc.IsEqual(old_progs.Sum(t => t.SocketCount), progs.Sum(t => t.SocketCount))) this.RaisePropertyChanged(nameof(Sockets));
            if (!MiscFunc.IsEqual(old_progs.Sum(t => (long)t.UploadRate), progs.Sum(t => (long)t.UploadRate))) this.RaisePropertyChanged(nameof(UpRate));
            if (!MiscFunc.IsEqual(old_progs.Sum(t => (long)t.DownloadRate), progs.Sum(t => (long)t.DownloadRate))) this.RaisePropertyChanged(nameof(DownRate));
            if (!MiscFunc.IsEqual(old_progs.Sum(t => (long)t.TotalUpload), progs.Sum(t => (long)t.TotalUpload))) this.RaisePropertyChanged(nameof(UpTotal));
            if (!MiscFunc.IsEqual(old_progs.Sum(t => (long)t.TotalDownload), progs.Sum(t => (long)t.TotalDownload))) this.RaisePropertyChanged(nameof(DownTotal));
        }

        protected void UpdatePrograms(SortedDictionary<ProgramID, Program> Programs)
        {
            Dictionary<ProgramID, ProgramTreeItem> oldProgs = new Dictionary<ProgramID, ProgramTreeItem>();
            foreach (ProgramTreeItem progItem in this.Children)
                oldProgs.Add(progItem.prog.ID, progItem);

            if (Programs.Count > 1)
            {
                foreach (var prog in Programs.Values)
                {
                    ProgramTreeItem progItem;
                    if (!oldProgs.TryGetValue(prog.ID, out progItem))
                    {
                        progItem = new ProgramTreeItem(prog);
                        this.Children.Add(progItem);
                    }
                    else
                    {
                        oldProgs.Remove(prog.ID);
                        progItem.Update(prog);
                    }
                }
            }

            foreach (ProgramTreeItem progItem in oldProgs.Values)
                this.Children.Remove(progItem);
        }
    }
}
