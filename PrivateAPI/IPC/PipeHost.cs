using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Pipes;
using System.Linq;
using System.Security.AccessControl;
using System.Security.Principal;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Threading;
using System.Xml;

namespace PrivateAPI
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

                    EMessageTypes type = EMessageTypes.eCall;
                    int seqID = 0;
                    string func = null;
                    List<byte[]> args = PipeListener.ParsePacket(data, ref type, ref seqID, ref func);

                    List<byte[]> ret = null;
                    if (func == "InitSession")
                    {
                        int SessionId = BitConverter.ToInt32(args[0], 0);
                        
                        bool Duplicate = mDispatcher.Invoke(new Func<bool>(() => {
                            return CountSessions(SessionId) > 0;
                        }));

                        ret = new List<byte[]>();

                        ret.Add(BitConverter.GetBytes(Duplicate));

                        serverPipe.SessionID = SessionId;
                    }
                    else if(args != null)
                        ret = Process(func, args);

                    if(ret != null)
                        serverPipe.SendPacket(type, seqID, func, ret);
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

        public void SendPushNotification(string func, List<byte[]> args)
        {
            foreach (PipeListener serverPipes in serverPipes)
            {
                if (!serverPipes.IsConnected())
                    continue;

                serverPipes.SendPacket(EMessageTypes.ePush, 0, func, args);
            }
        }

        protected virtual List<byte[]> Process(string func, List<byte[]> param)
        {
            throw new NotImplementedException();
        }
    }
}