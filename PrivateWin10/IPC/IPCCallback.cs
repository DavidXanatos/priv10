using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;
using System.Text;
using System.Threading.Tasks;

namespace PrivateWin10.IPC
{
    public interface IPCCallback
    {
        event EventHandler<Firewall.NotifyArgs> ActivityNotification;
        event EventHandler<ProgramList.ChangeArgs> ChangeNotification;

        [OperationContract(IsOneWay = true)]
        void NotifyActivity(Guid guid, Program.LogEntry entry);

        [OperationContract(IsOneWay = true)]
        void NotifyChange(Guid guid);
    }
}
