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

namespace PipeIPC
{
    public class PipeClient
    {
        public static string Name = "pipeName";

        protected class PipeConnector : IPCStream<NamedPipeClientStream>
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
                    else if (call.type == "call")
                    {
                        if (call.seqID != seqIDctr)
                            AppLog.Debug("call.seqID != seqIDctr");
                        else
                        {
                            retObj = call;
                            done.Set();
                        }
                    }
                };

                PipeClosed += (sndr, args) =>
                {
                    done.Set();
                };

                startRecv();
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
                if(Send(ObjectToByteArray(call)))
#if DEBUG
                    done.WaitOne(); // give us time to debug
#else
                    done.WaitOne(10000);
#endif

                if (retObj == null)
                    throw new Exception("Pipe Broke!");

                if (retObj.args is Exception)
                    throw (Exception)retObj.args;

                return retObj.args;
            }
        }

        //private Dispatcher mDispatcher;

        private PipeConnector clientPipe;

        public PipeClient()
        {
            //mDispatcher = Dispatcher.CurrentDispatcher;
        }

        public bool DoConnect(int TimeOut = 10000)
        {
            if (clientPipe != null)
                return false;

            clientPipe = new PipeConnector(".", Name);
            if (!clientPipe.Connect(TimeOut))
            {
                clientPipe = null;
                return false;
            }

            clientPipe.PipeClosed += (sndr, args) =>
            {
                clientPipe = null;
            };

            clientPipe.PushNotification += (sndr, call) =>
            {
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
            T ret;
            try
            {
                if (clientPipe == null && Connect(3000, true) == 0)
                    throw new Exception("Connection Failed");

                ret = (T)(clientPipe.RemoteExec(fx, args));
            }
            catch (Exception err)
            {
                AppLog.Exception(err);
                ret = defRet;
            }
            return ret;
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

        public virtual void HandlePushNotification(string func, object args)
        {
            throw new NotImplementedException();
        }
    }
}