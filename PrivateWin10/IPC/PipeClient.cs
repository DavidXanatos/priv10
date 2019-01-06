using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO.Pipes;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;

namespace PrivateWin10.IPC
{
    public class PipeClient : IPCCallback, IPCInterface
    {
        protected class PipeConnector : PipeIPC<NamedPipeClientStream>
        {
            ManualResetEvent done = new ManualResetEvent(false);
            RemoteCall retObj = null;
            int seqIDctr = 0;
            public event EventHandler<RemoteCall> PushNotification;

            public PipeConnector(string serverName, string pipeName)
            {
                pipeStream = new NamedPipeClientStream(serverName, pipeName, PipeDirection.InOut, PipeOptions.Asynchronous);
            }

            public bool Connect(int TimeOut = 10000)
            {
                try
                {
                    pipeStream.Connect(TimeOut);
                }
                catch
                {
                    return false; // timeout
                }

                DataReceived += (sndr, data) =>
                {
                    RemoteCall call = ByteArrayToObject(data);

                    if (call.type == "push")
                    {
                        PushNotification?.Invoke(this, call);
                    }
                    else if (call.type == "call" && call.seqID == seqIDctr)
                    {
                        retObj = call;
                        done.Set();
                    }
                };

                PipeClosed += (sndr, args) => {
                    done.Set();
                };

                initAsyncReader();
                return true;
            }

            public object RemoteExec(string fx, object args)
            {
                RemoteCall call = new RemoteCall();
                call.seqID = ++seqIDctr;
                call.type = "call";
                call.func = fx;
                call.args = args;

                retObj = null;
                done.Reset();
                Send(ObjectToByteArray(call));
                done.WaitOne();

                if (retObj == null)
                    throw new Exception("Pipe Broke!");

                if (retObj.args is Exception)
                    throw (Exception)retObj.args;

                return retObj.args;
            }
        }

        private Dispatcher mDispatcher;

        private PipeConnector clientPipe;

        public PipeClient()
        {
            mDispatcher = Dispatcher.CurrentDispatcher;
        }

        public bool DoConnect(int TimeOut = 10000)
        {
            if (clientPipe != null)
                return false;

            clientPipe = new PipeConnector(".", PipeConnector.Name);
            if (!clientPipe.Connect(TimeOut))
            {
                clientPipe = null;
                return false;
            }

            clientPipe.PipeClosed += (sndr, args) => {
                clientPipe = null;
            };

            clientPipe.PushNotification += (sndr, call) => {
                HandlePushNotification(call.func, call.args);
            };

            return true;
        }

        public int Connect(int TimeOut = 10000, bool mNoDouble = false)
        {
            // Note: when we close a IPC host without terminating its process we are left with some still active listeners, so we test communication and reconnect if needed
            for (long endTime = (long)MiscFunc.GetTickCount64() + (long)TimeOut; TimeOut > 0; TimeOut = (int)(endTime - (long)MiscFunc.GetTickCount64()))
            {
                if (!DoConnect(TimeOut))
                    continue;

                IPCSession session = RemoteExec<IPCSession>("InitSession", Process.GetCurrentProcess().SessionId, null);
                if (session != null)
                    return (mNoDouble || session.duplicate == false) ? 1 : -1;
            }
            return 0;
        }

        public T RemoteExec<T>(string fx, object args, T defRet)
        {
            try
            {
                if (clientPipe == null && Connect(3000, true) == 0)
                    throw new Exception("Connection Failed");

                return (T)(clientPipe.RemoteExec(fx, args));
            }
            catch (Exception err)
            {
                AppLog.Line("Error in {0}: {1}", MiscFunc.GetCurrentMethod(), err.Message);
                return defRet;
            }
        }

        public bool IsConnected()
        {
            return clientPipe != null && clientPipe.IsConnected();
        }

        public void Close()
        {
            if (clientPipe == null)
                return;

            clientPipe.Close();
            clientPipe = null;
        }


        public Firewall.FilteringModes GetFilteringMode()
        {
            return RemoteExec("GetFilteringMode", null, Firewall.FilteringModes.Unknown);
        }

        public bool SetFilteringMode(Firewall.FilteringModes Mode)
        {
            return RemoteExec("SetFilteringMode", Mode, false);
        }

        public Firewall.Auditing GetAuditPol()
        {
            return RemoteExec("GetAuditPol", null, Firewall.Auditing.Off);
        }

        public bool SetAuditPol(Firewall.Auditing audit)
        {
            return RemoteExec("SetAuditPol", audit, false);
        }

        public List<Program> GetPrograms(List<Guid> guids)
        {
            return RemoteExec<List<Program>>("GetPrograms", new object[1] { guids }, null);
        }

        public Program GetProgram(ProgramList.ID id, bool canAdd = false)
        {
            return RemoteExec<Program>("GetProgram", new object[2] { id, canAdd }, null);
        }

        public bool AddProgram(ProgramList.ID id, Guid guid)
        {
            return RemoteExec("AddProgram", new object[2] { id, guid }, false);
        }

        public bool UpdateProgram(Guid guid, Program.Config config)
        {
            return RemoteExec("UpdateProgram", new object[2] { guid, config }, false);
        }

        public bool MergePrograms(Guid to, Guid from)
        {
            return RemoteExec("MergePrograms", new object[2] { to, from }, false);
        }

        public bool SplitPrograms(Guid from, ProgramList.ID id)
        {
            return RemoteExec("SplitPrograms", new object[2] { from, id }, false);
        }

        public bool RemoveProgram(Guid guid, ProgramList.ID id = null)
        {
            return RemoteExec("RemoveProgram", new object[2] { guid, id }, false);
        }

        public bool LoadRules()
        {
            return RemoteExec("LoadRules", null, false);
        }

        public List<FirewallRule> GetRules(List<Guid> guids = null)
        {
            return RemoteExec<List<FirewallRule>>("GetRules", guids, null);
        }

        public bool UpdateRule(FirewallRule rule)
        {
            return RemoteExec("UpdateRule", rule, false);
        }

        public bool ClearRules(ProgramList.ID id, bool bDisable)
        {
            return RemoteExec("ClearRules", new object[2] { id, bDisable }, false);
        }

        public bool RemoveRule(FirewallRule rule)
        {
            return RemoteExec("RemoveRule", rule, false);
        }

        public bool BlockInternet(bool bBlock)
        {
            return RemoteExec("BlockInternet", bBlock, false);
        }

        public bool ClearLog(bool ClearSecLog)
        {
            return RemoteExec("ClearLog", ClearSecLog, false);
        }

        public int CleanUpPrograms()
        {
            return RemoteExec("CleanUpPrograms", null, 0);
        }

        public List<Program.LogEntry> GetConnections(List<Guid> guids = null)
        {
            return RemoteExec<List<Program.LogEntry>>("GetConnections", guids, null);
        }

        public List<AppManager.AppInfo> GetAllApps()
        {
            return RemoteExec<List<AppManager.AppInfo>>("GetAllApps", null, null);
        }

        /*public bool ApplyTweak(Tweak tweak)
        {
            return RemoteExec("ApplyTweak", tweak, false);
        }

        public bool TestTweak(Tweak tweak)
        {
            return RemoteExec("TestTweak", tweak, false);
        }

        public bool UndoTweak(Tweak tweak)
        {
            return RemoteExec("UndoTweak", tweak, false);
        }*/


        public event EventHandler<Firewall.NotifyArgs> ActivityNotification;
        public event EventHandler<ProgramList.ChangeArgs> ChangeNotification;

        private T GetArg<T>(object args, int index)
        {
            return (T)(((object[])args)[index]);
        }

        public void HandlePushNotification(string func, object args)
        {
            try
            {
                if (func == "ActivityNotification")
                {
                    NotifyActivity(GetArg<Guid>(args, 0), GetArg<Program.LogEntry>(args, 1));
                }
                else if (func == "ChangeNotification")
                {
                    NotifyChange(GetArg<Guid>(args, 0));
                }
                else
                {
                    throw new Exception("Unknown Notificacion");
                }
            }
            catch (Exception err)
            {
                AppLog.Line("Error in {0}: {1}", MiscFunc.GetCurrentMethod(), err.Message);
            }
        }

        public void NotifyActivity(Guid guid, Program.LogEntry entry)
        {
            Application.Current.Dispatcher.BeginInvoke(new Action(() => {
                ActivityNotification?.Invoke(this, new Firewall.NotifyArgs() { guid = guid, entry = entry });
            }));
        }

        public void NotifyChange(Guid guid)
        {
            Application.Current.Dispatcher.BeginInvoke(new Action(() => {
                ChangeNotification?.Invoke(this, new ProgramList.ChangeArgs() { guid = guid });
            }));
        }
    }
}
