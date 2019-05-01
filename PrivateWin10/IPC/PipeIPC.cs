using System;
using System.Collections.Generic;
using System.IO.Pipes;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.Serialization.Formatters.Binary;
using System.IO;

namespace PrivateWin10.IPC
{
    [Serializable()]
    public class RemoteCall
    {
        public int seqID;
        public string type;
        public string func;
        public object args;
    }

    public class PipeIPC<T> where T : PipeStream
    {
//#if DEBUG
//        public static string Name = "priv10dbg";
//#else
        public static string Name = "priv10";
//#endif

        protected T pipeStream = null;
        public event EventHandler<byte[]> DataReceived;
        public event EventHandler<EventArgs> PipeClosed;

        public virtual void Close()
        {
            if (!pipeStream.IsConnected)
                return;
            
            pipeStream.Flush();
            pipeStream.WaitForPipeDrain();
            pipeStream.Close();
        }

        public bool IsConnected() { return pipeStream.IsConnected; }

        public Task Send(byte[] bytes)
        {
            byte[] data = BitConverter.GetBytes(bytes.Length);
            byte[] buff = data.Concat(bytes).ToArray();
            return pipeStream.WriteAsync(buff, 0, buff.Length);
        }

        protected void initAsyncReader()
        {
            new Action<PipeIPC<T>>((p) => { p.RunAsyncByteReader((b) => { DataReceived?.Invoke(this, b); }); })(this);
        }

        protected void RunAsyncByteReader(Action<byte[]> asyncReader)
        {
            int len = sizeof(int);
            byte[] buff = new byte[len];

            // read the length
            pipeStream.ReadAsync(buff, 0, len).ContinueWith((ret) =>
            {
                if (ret.Result == 0)
                {
                    PipeClosed?.Invoke(this, EventArgs.Empty);
                    return;
                }

                // read the data
                len = BitConverter.ToInt32(buff, 0);
                buff = new byte[len];
                pipeStream.ReadAsync(buff, 0, len).ContinueWith((ret2) =>
                {
                    if (ret2.Result == 0)
                    {
                        PipeClosed?.Invoke(this, EventArgs.Empty);
                        return;
                    }

                    asyncReader(buff);
                    RunAsyncByteReader(asyncReader);
                });
            });
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
