using System;
using System.Collections.Generic;
using System.IO.Pipes;
using System.Linq;
using System.Security.AccessControl;
using System.Security.Principal;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Threading;

namespace PrivateWin10.IPC
{
    public class PipeHost
    {
        protected class PipeListener : PipeIPC<NamedPipeServerStream>
        {
            public event EventHandler<EventArgs> Connected;
            private bool forceClose = false;

            public PipeListener(string pipeName)
            {
                PipeSecurity pipeSa = new PipeSecurity();
                pipeSa.SetAccessRule(new PipeAccessRule(new SecurityIdentifier(FileOps.SID_Worls), PipeAccessRights.FullControl, AccessControlType.Allow));
                int buffLen = 2*1024*1024; // 2MB buffer should be plany ;)
                pipeStream = new NamedPipeServerStream(pipeName, PipeDirection.InOut, NamedPipeServerStream.MaxAllowedServerInstances, PipeTransmissionMode.Message, PipeOptions.Asynchronous, buffLen, buffLen, pipeSa);
                pipeStream.BeginWaitForConnection(new AsyncCallback(PipeConnected), null);
            }

            protected void PipeConnected(IAsyncResult asyncResult)
            {
                pipeStream.EndWaitForConnection(asyncResult);
                if (forceClose)
                {
                    pipeStream.Disconnect(); 
                    return;
                }
                Connected?.Invoke(this, new EventArgs());
                initAsyncReader();   
            }

            override
            public void Close()
            {
                // Note: there does not seam to be a way to abort WaitForConnection, so we set a flag and let the pipe disconnect right after connct.
                forceClose = true;
                base.Close();
            }

            public int SessionID = -1;
        }

        private Dispatcher mDispatcher;

        private List<PipeListener> serverPipes = new List<PipeListener>();

        public PipeHost()
        {
            mDispatcher = Dispatcher.CurrentDispatcher;
        }

        public bool Listen()
        {
            // alocate 3 listeners
            for(int i=0; i < 3; i++)
                AddListener();

            return true;
        }

        public int CountSessions(int SessionID)
        {
            int count = 0;
            foreach (PipeListener serverPipes in serverPipes) {
                if (serverPipes.SessionID == SessionID)
                    count++;
            }
            return count;
        }

        public void AddListener()
        {
            PipeListener serverPipe = new PipeListener(PipeListener.Name);
            serverPipes.Add(serverPipe);

            serverPipe.DataReceived += (sndr, data) => {
                mDispatcher.BeginInvoke(new Action(() => {
                    RemoteCall call = PipeListener.ByteArrayToObject(data);

                    if (call.func == "InitSession")
                    {
                        int SessionId = (int)call.args;

                        IPCSession session = new IPCSession();
                        session.version = App.mVersion;
                        session.duplicate = CountSessions(SessionId) > 0;
                        call.args = session;

                        serverPipe.SessionID = SessionId;
                    }
                    else
                        call = Process(call);

                    serverPipe.Send(PipeListener.ObjectToByteArray(call));
                }));
            };

            serverPipe.Connected += (sndr, args) => {
                mDispatcher.BeginInvoke(new Action(() => {
                    // if we used a listener allocate a replacement one
                    AddListener();
                }));
            };

            serverPipe.PipeClosed += (sndr, args) => {
                mDispatcher.BeginInvoke(new Action(() => {
                    serverPipes.Remove(serverPipe);
                }));
            };
        }

        public void Close()
        {
            foreach (PipeListener serverPipes in serverPipes)
                serverPipes.Close();
            serverPipes.Clear();
        }

        private T GetArg<T>(object args, int index)
        {
            return (T)(((object[])args)[index]);
        }

        protected RemoteCall Process(RemoteCall call)
        {
            //try
            {
                if (call.func == "GetFilteringMode")
                {
                    call.args = App.engine.GetFilteringMode();
                }
                else if (call.func == "SetFilteringMode")
                {
                    call.args = App.engine.SetFilteringMode((Firewall.FilteringModes)call.args);
                }
                else if (call.func == "GetAuditPol")
                {
                    call.args = App.engine.GetAuditPol();
                }
                else if (call.func == "SetAuditPol")
                {
                    call.args = App.engine.SetAuditPol((Firewall.Auditing)call.args);
                }
                else if (call.func == "GetPrograms")
                {
                    call.args = App.engine.GetPrograms(GetArg<List<Guid>>(call.args, 0));
                }
                else if (call.func == "GetProgram")
                {
                    call.args = App.engine.GetProgram(GetArg<ProgramList.ID>(call.args, 0), GetArg<bool>(call.args, 1));
                }
                else if (call.func == "AddProgram")
                {
                    call.args = App.engine.AddProgram(GetArg<ProgramList.ID> (call.args, 0), GetArg<Guid>(call.args, 1));
                }
                else if (call.func == "UpdateProgram")
                {
                    call.args = App.engine.UpdateProgram(GetArg<Guid>(call.args, 0), GetArg<Program.Config>(call.args, 1));
                }
                else if (call.func == "MergePrograms")
                {
                    call.args = App.engine.MergePrograms(GetArg<Guid>(call.args, 0), GetArg<Guid>(call.args, 1));
                }
                else if (call.func == "SplitPrograms")
                {
                    call.args = App.engine.SplitPrograms(GetArg<Guid>(call.args, 0), GetArg<ProgramList.ID>(call.args, 1));
                }
                else if (call.func == "RemoveProgram")
                {
                    call.args = App.engine.RemoveProgram(GetArg<Guid>(call.args, 0), GetArg<ProgramList.ID>(call.args, 1));
                }
                else if (call.func == "LoadRules")
                {
                    call.args = App.engine.LoadRules();
                }
                else if (call.func == "GetRules")
                {
                    call.args = App.engine.GetRules((List<Guid>)call.args);
                }
                else if (call.func == "UpdateRule")
                {
                    call.args = App.engine.UpdateRule((FirewallRule)call.args);
                }
                else if (call.func == "RemoveRule")
                {
                    call.args = App.engine.RemoveRule((FirewallRule)call.args);
                }
                else if (call.func == "BlockInternet")
                {
                    call.args = App.engine.BlockInternet((bool)call.args);
                }
                else if (call.func == "ClearLog")
                {
                    call.args = App.engine.ClearLog((bool)call.args);
                }
                else if (call.func == "CleanUpPrograms")
                {
                    call.args = App.engine.CleanUpPrograms();
                }
                else if (call.func == "GetConnections")
                {
                    call.args = App.engine.GetConnections((List<Guid>)call.args);
                }
                else if (call.func == "GetAllApps")
                {
                    call.args = App.engine.GetAllApps();
                }

                /*else if (call.func == "ApplyTweak")
                {
                    call.args = App.engine.ApplyTweak((Tweak)call.args);
                }
                else if (call.func == "TestTweak")
                {
                    call.args = App.engine.TestTweak((Tweak)call.args);
                }
                else if (call.func == "UndoTweak")
                {
                    call.args = App.engine.UndoTweak((Tweak)call.args);
                }*/

                else
                {
                    call.args = new Exception("Unknon FunctionCall");
                }
            }
            /*catch (Exception err)
            {
                AppLog.Line("Error in {0}: {1}", MiscFunc.GetCurrentMethod(), err.Message);
                call.args = err;
            }*/
            return call;
        }

        public void SendPushNotification(string func, object args)
        {
            foreach (PipeListener serverPipes in serverPipes)
            {
                if (!serverPipes.IsConnected())
                    continue;

                RemoteCall call = new RemoteCall();
                call.seqID = 0;
                call.type = "push";
                call.func = func;
                call.args = args;

                serverPipes.Send(PipeListener.ObjectToByteArray(call));
            }
        }

        public void NotifyActivity(Guid guid, Program.LogEntry entry)
        {
            SendPushNotification("ActivityNotification", new object[] { guid, entry });
        }

        public void NotifyChange(Guid guid)
        {
            SendPushNotification("ChangeNotification", new object[] { guid });
        }
    }
}
