using System;
using System.Collections.Generic;
using System.IO.Pipes;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.Serialization.Formatters.Binary;
using System.IO;
using System.Threading;

namespace PipeIPC
{
    [Serializable()]
    public class IPCSession
    {
        //public string version;
        public bool duplicate;
    }

    [Serializable()]
    public class RemoteCall
    {
        public int seqID;
        public string type;
        public string func;
        public object args;

        public static T GetArg<T>(object args, int index)
        {
            return (T)(((object[])args)[index]);
        }
    }

    public class IPCStream<T> where T : PipeStream
    {
        protected T pipeStream = null;
        public event EventHandler<byte[]> DataReceived;
        public event EventHandler<EventArgs> PipeClosed;

        bool revcRunning = false;
        Thread revcThread = null;

        public virtual void Close()
        {
            if (!pipeStream.IsConnected)
                return;

            pipeStream.Close();

            if (revcRunning)
            {
                revcRunning = false;
                revcThread.Join();
            }

            /*pipeStream.Flush();
            pipeStream.WaitForPipeDrain();
            pipeStream.Close();*/
        }

        public bool IsConnected() { return pipeStream.IsConnected; }

        public bool Send(byte[] bytes)
        {
            byte[] data = BitConverter.GetBytes(bytes.Length);
            byte[] buff = data.Concat(bytes).ToArray();
            try
            {
                pipeStream.Write(buff, 0, buff.Length);
                pipeStream.WaitForPipeDrain();
            }
            catch {
                return false;
            }
            return true;
        }

        protected void startRecv()
        {
            revcRunning = true;
            revcThread = new Thread(recvProc);
            revcThread.Start();
        }

        int SafeRead(byte[] buffer, int count)
        {
            int read = 0;
            for (; read < count; )
            {
                if (!pipeStream.IsConnected)
                    return -1;
                read += pipeStream.Read(buffer, read, count);
            }
            return read;
        }

        void recvProc()
        {
            while(revcRunning)
            {
                int len = sizeof(int);
                byte[] buff = new byte[len];
                int ret = SafeRead(buff, len);
                if (ret < 0)
                {
                    PipeClosed?.Invoke(this, EventArgs.Empty);
                    return;
                }

                len = BitConverter.ToInt32(buff, 0);
                buff = new byte[len];
                ret = SafeRead(buff, len);
                if (ret < 0)
                {
                    PipeClosed?.Invoke(this, EventArgs.Empty);
                    return;
                }

                DataReceived?.Invoke(this, buff);
            }
        }
        public static byte[] ObjectToByteArray(RemoteCall obj)
        {
            BinaryFormatter bf = new BinaryFormatter();
            var ms = new MemoryStream();
            bf.Serialize(ms, obj);
            return ms.ToArray();
        }

        public static RemoteCall ByteArrayToObject(byte[] arrBytes)
        {
            var memStream = new MemoryStream();
            var binForm = new BinaryFormatter();
            memStream.Write(arrBytes, 0, arrBytes.Length);
            memStream.Seek(0, SeekOrigin.Begin);
            var obj = binForm.Deserialize(memStream);
            return (RemoteCall)obj;
        }
    }
}