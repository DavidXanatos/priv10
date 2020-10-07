using System;
using System.Collections.Generic;
using System.IO.Pipes;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.Serialization.Formatters.Binary;
using System.IO;
using System.Threading;

namespace PrivateAPI
{
    public enum EMessageTypes
    {
        eCall = 0,
        ePush = 1
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

        public bool SendPacket(EMessageTypes type, int seqID, string func, List<byte[]> args)
        {
            using (MemoryStream dataStream = new MemoryStream())
            {
                var dataWriter = new BinaryWriter(dataStream);

                dataWriter.Write((byte)type);

                dataWriter.Write(seqID);

                dataWriter.Write(func);

                dataWriter.Write(args.Count);
                foreach (byte[] arg in args)
                {
                    dataWriter.Write(arg.Length);
                    dataWriter.Write(arg);
                }

                dataWriter.Dispose();

                return Send(dataStream.ToArray());
            }
        }

        public static List<byte[]> ParsePacket(byte[] packet, ref EMessageTypes type, ref int seqID, ref string func)
        {
            try
            {
                using (MemoryStream dataStream = new MemoryStream(packet))
                {
                    var dataReader = new BinaryReader(dataStream);

                    type = (EMessageTypes)dataReader.ReadByte();

                    seqID = dataReader.ReadInt32();

                    func = dataReader.ReadString();

                    List<byte[]> args = new List<byte[]>();

                    int count = dataReader.ReadInt32();
                    for (int i = 0; i < count; i++)
                    {
                        int length = dataReader.ReadInt32();
                        args.Add(dataReader.ReadBytes(length));
                    }

                    return args;
                }
            }
            catch 
            {
                return null;
            }
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
    }
}