using System;
using System.IO;
using System.Net.Sockets;
using DNS.Protocol;

namespace DNS.Client.RequestResolver {
    public class TcpRequestResolver : IRequestResolver {
        public ClientResponse Request(ClientRequest request) {
            TcpClient tcp = new TcpClient();

            try {
                tcp.Connect(request.Dns);

                Stream stream = tcp.GetStream();
                byte[] buffer = request.ToArray();
                byte[] length = BitConverter.GetBytes((ushort) buffer.Length);

                if (BitConverter.IsLittleEndian) {
                    Array.Reverse(length);
                }

                stream.Write(length, 0, length.Length);
                stream.Write(buffer, 0, buffer.Length);

                buffer = new byte[2];
                Read(stream, buffer);

                if (BitConverter.IsLittleEndian) {
                    Array.Reverse(buffer);
                }

                buffer = new byte[BitConverter.ToUInt16(buffer, 0)];
                Read(stream, buffer);

                Response response = Response.FromArray(buffer);

                return new ClientResponse(request, response, buffer);
            } finally {
                tcp.Close();
            }
        }

        private static void Read(Stream stream, byte[] buffer) {
            int length = buffer.Length;
            int offset = 0;
            int size = 0;

            while (length > 0 && (size = stream.Read(buffer, offset, length)) > 0) {
                offset += size;
                length -= size;
            }

            if (length > 0) {
                throw new IOException("Unexpected end of stream");
            }
        }
    }
}
