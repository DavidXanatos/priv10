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

namespace PipeIPC
{
    public partial class PipeHost
    {
        public static string Name = "pipeName";

        protected class PipeListener : IPCStream<NamedPipeServerStream>
        {
            public event EventHandler<EventArgs> Connected;
            private bool forceClose = false;

            public PipeListener(string pipeName)
            {
                PipeSecurity pipeSa = new PipeSecurity();
                pipeSa.SetAccessRule(new PipeAccessRule(new SecurityIdentifier(WellKnownSidType.WorldSid, null), PipeAccessRights.FullControl, AccessControlType.Allow));
                int buffLen = 2 * 1024 * 1024; // 2MB buffer should be plany ;)
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
                startRecv();
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
            for (int i = 0; i < 3; i++)
                AddListener();

            return true;
        }

        public int CountSessions(int SessionID)
        {
            int count = 0;
            foreach (PipeListener serverPipes in serverPipes)
            {
                if (serverPipes.SessionID == SessionID)
                    count++;
            }
            return count;
        }

        public void AddListener()
        {
            PipeListener serverPipe = new PipeListener(Name);
            serverPipes.Add(serverPipe);

            serverPipe.DataReceived += (sndr, data) =>
            {
                //mDispatcher.BeginInvoke(new Action(() => {
                    RemoteCall call = PipeListener.ByteArrayToObject(data);

                    if (call.func == "InitSession")
                    {
                        int SessionId = (int)call.args;

                        IPCSession session = new IPCSession();
                        //session.version = App.mVersion;
                        session.duplicate = mDispatcher.Invoke(new Func<bool>(() => {
                            return CountSessions(SessionId) > 0;
                        }));
                        call.args = session;

                        serverPipe.SessionID = SessionId;
                    }
                    else
                        call = Process(call);

                    serverPipe.Send(PipeListener.ObjectToByteArray(call));
                //}));
            };

            serverPipe.Connected += (sndr, args) =>
            {
                mDispatcher.BeginInvoke(new Action(() => {
                    // if we used a listener allocate a replacement one
                    AddListener();
                }));
            };

            serverPipe.PipeClosed += (sndr, args) =>
            {
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

        protected virtual RemoteCall Process(RemoteCall call)
        {
            throw new NotImplementedException();
        }
    }
}