using MiscHelpers;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;

namespace PrivateWin10.Controls
{
	public class ProgramTreeItem : TreeItem, ILeafItem
	{
        public override Object Text => prog.Description;

        public override Object Icon => GetIcon(prog.ID.Path);

        // columns BEGIN
        public override string Category => "";
        public override string Access => "";
        public override string AccessTag => "";

        public override int Rules => prog.RuleCount;
        public override int Allowed => prog.AllowedCount;
        public override int Blocked => prog.BlockedCount;
        public override DateTime LastActivity => prog.LastActivity;

        public override int Sockets => prog.SocketCount;
        public override UInt64 UpRate => prog.UploadRate;
        public override UInt64 DownRate => prog.DownloadRate;
        public override UInt64 UpTotal => prog.TotalUpload;
        public override UInt64 DownTotal => prog.TotalDownload;

        public override string Program => prog.ID.FormatString();
        // columns END

        public Program prog { get; protected set; }

        public ProgramTreeItem(Program prog) : base(prog.Description)
        {
            this.prog = prog;
        }

        public void Update(Program prog)
        {
            var old_prog = this.prog;
            this.prog = prog;

            if (!MiscFunc.IsEqual(old_prog.Description, prog.Description)) this.RaisePropertyChanged(nameof(Text));
            if (!MiscFunc.IsEqual(old_prog.ID.Path, prog.ID.Path)) { cachedIcon = null; this.RaisePropertyChanged(nameof(Icon)); }

            if (!MiscFunc.IsEqual(old_prog.RuleCount, prog.RuleCount)) this.RaisePropertyChanged(nameof(Rules));
            if (!MiscFunc.IsEqual(old_prog.AllowedCount, prog.AllowedCount)) this.RaisePropertyChanged(nameof(Allowed));
            if (!MiscFunc.IsEqual(old_prog.BlockedCount, prog.BlockedCount)) this.RaisePropertyChanged(nameof(Blocked));
            if (!MiscFunc.IsEqual(old_prog.LastActivity, prog.LastActivity)) this.RaisePropertyChanged(nameof(LastActivity));

            if (!MiscFunc.IsEqual(old_prog.SocketCount, prog.SocketCount)) this.RaisePropertyChanged(nameof(Sockets));
            if (!MiscFunc.IsEqual(old_prog.UploadRate, prog.UploadRate)) this.RaisePropertyChanged(nameof(UpRate));
            if (!MiscFunc.IsEqual(old_prog.DownloadRate, prog.DownloadRate)) this.RaisePropertyChanged(nameof(DownRate));
            if (!MiscFunc.IsEqual(old_prog.TotalUpload, prog.TotalUpload)) this.RaisePropertyChanged(nameof(UpTotal));
            if (!MiscFunc.IsEqual(old_prog.TotalDownload, prog.TotalDownload)) this.RaisePropertyChanged(nameof(DownTotal));

            // ProgID does not change ever!
        }
    }
}
