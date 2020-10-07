using MiscHelpers;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Pipes;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;
using System.Xml;

namespace PrivateAPI
{
    public class PipeClient
    {
        public static string Name = "pipeName";

        protected class PipeConnector : IPCStream<NamedPipeClientStream>
        {
            ManualResetEvent done = new ManualResetEvent(false);
            Tuple<string, List<byte[]>> retObj = null;
            int seqIDctr = 0;
            public event EventHandler<Tuple<string, List<byte[]>>> PushNotification;

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
                    EMessageTypes type = EMessageTypes.eCall;
                    int seqID = 0;
                    string func = null;
                    List<byte[]> args = ParsePacket(data, ref type, ref seqID, ref func);

                    if (type == EMessageTypes.ePush)
                    {
                        PushNotification?.Invoke(this, new Tuple<string, List<byte[]>>(func, args));
                    }
                    else if (type == EMessageTypes.eCall)
                    {
                        if (seqID != seqIDctr)
                            Debug.Write("seqID != seqIDctr");
                        else
                        {
                            retObj = new Tuple<string, List<byte[]>>(func, args);
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

            public List<byte[]> RemoteExec(string func, List<byte[]> args)
            {
                retObj = null;
                done.Reset();
                if(SendPacket(EMessageTypes.eCall, ++seqIDctr, func, args))
#if DEBUG
                    done.WaitOne(); // give us time to debug
#else
                    done.WaitOne(10000);
#endif

                if (retObj == null)
                    throw new Exception("Pipe Broke!");

                //if (retObj.args is Exception)
                //    throw (Exception)retObj.args;

                return retObj.Item2;
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
                HandlePushNotification(call.Item1, call.Item2);
            };

            return true;
        }

        public int Connect(int TimeOut = 10000, bool mNoDouble = false)
        {
            // Note: when we close a IPC host without terminating its process we are left with some still active listeners, so we test communication and reconnect if needed
            for (DateTime endTime = DateTime.Now.AddMilliseconds(TimeOut); DateTime.Now < endTime; )
            {
                if (!DoConnect(TimeOut))
                    continue;


                List<byte[]> ret = null;
                using (MemoryStream xmlStream = new MemoryStream())
                {
                    List<byte[]> args = new List<byte[]>();

                    args.Add(BitConverter.GetBytes(Process.GetCurrentProcess().SessionId));

                    ret = RemoteExec("InitSession", args);
                }

                if (ret != null)
                {
                    bool Duplicate = BitConverter.ToBoolean(ret[0], 0);

                    return (mNoDouble || Duplicate == false) ? 1 : -1;
                }
            }
            return 0;
        }

        public List<byte[]> RemoteExec(string func, List<byte[]> args)
        {
            List<byte[]> ret = null;
#if !DEBUG
            try
#endif
            {
                if (clientPipe == null) // && Connect(3000, true) == 0)
                    throw new Exception("Not Connected");

                ret = clientPipe.RemoteExec(func, args);
            }
#if !DEBUG
            catch (Exception err)
            {
                AppLog.Exception(err);
            }
#endif
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

        public virtual void HandlePushNotification(string func, List<byte[]> param)
        {
            throw new NotImplementedException();
        }
    }
}