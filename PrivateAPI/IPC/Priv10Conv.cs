using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace PrivateAPI
{
    public static class Priv10Conv
    {
        public static byte[] PutStr(object value)
        {
            if (value == null)
                return new byte[0];
            return Encoding.UTF8.GetBytes(value.ToString());
        }

        public static string GetStr(byte[] value)
        {
            if (value.Length == 0)
                return null;
            return Encoding.UTF8.GetString(value);
        }

        public static byte[] PutStrList(List<string> list)
        {
            return PutList(list, PutStr);
        }

        public static List<string> GetStrList(byte[] value)
        {
            return GetList(value, GetStr);
        }

        public static byte[] PutBool(bool value)
        {
            return BitConverter.GetBytes(value);
        }

        public static bool GetBool(byte[] value)
        {
            return BitConverter.ToBoolean(value, 0);
        }

        public static byte[] PutBoolx(bool? value)
        {
            if(value == null)
                return new byte[0];
            return BitConverter.GetBytes(value.Value);
        }

        public static bool? GetBoolx(byte[] value)
        {
            if(value.Length == 0)
                return null;
            return BitConverter.ToBoolean(value, 0);
        }

        public static byte[] PutInt(int value)
        {
            return BitConverter.GetBytes(value);
        }

        public static int GetInt(byte[] value)
        {
            return BitConverter.ToInt32(value, 0);
        }

        public static byte[] PutUInt64(UInt64 value)
        {
            return BitConverter.GetBytes(value);
        }

        public static UInt64 GetUInt64(byte[] value)
        {
            return BitConverter.ToUInt64(value, 0);
        }

        public static T GetEnum<T>(byte[] value)
        {
            return (T)Enum.Parse(typeof(T), Encoding.ASCII.GetString(value));
        }

        public static Guid GetGuid(byte[] data)
        {
            return new Guid(data);
        }

        public static byte[] PutGuid(Guid guid)
        {
            return guid.ToByteArray();
        }

        public static byte[] PutGuids(List<Guid> list)
        {
            return PutList(list, PutGuid);
        }

        public static List<Guid> GetGuids(byte[] data)
        {
            return GetList(data, GetGuid);
        }

        /////////////////////////////////////////
        // 

        public static byte[] PutObjXml<T>(T obj, Action<T, XmlWriter> store)
        {
            if(obj == null)
                return new byte[0];
            using (MemoryStream xmlStream = new MemoryStream())
            {
                using (XmlWriter writer = XmlWriter.Create(xmlStream))
                {
                    writer.WriteStartDocument();
                    store(obj, writer);
                    writer.WriteEndDocument();
                }
                return xmlStream.ToArray();
            }
        }

        public static T GetXmlObj<T>(byte[] data, Action<T, XmlElement> load) where T: new()
        {
            if (data.Length == 0)
                return default(T);
            using (var memStream = new MemoryStream(data))
            {
                XmlDocument xDoc = new XmlDocument();
                xDoc.Load(memStream);

                T value = new T();
                load(value, xDoc.DocumentElement);
                return value;
            }
        }

        public static byte[] PutList<T>(List<T> list, Func<T, byte[]> store)
        {
            if (list == null)
                return new byte[0];
            using (MemoryStream dataStream = new MemoryStream())
            {
                using (var dataWriter = new BinaryWriter(dataStream))
                {
                    dataWriter.Write(list.Count);

                    foreach (T item in list)
                    {
                        byte[] data = store(item);
                        dataWriter.Write(data.Length);
                        dataWriter.Write(data);
                    }
                }
                return dataStream.ToArray();
            }
        }

        public static List<T> GetList<T>(byte[] data, Func<byte[], T> load)
        {
            if (data.Length == 0)
                return null;
            List<T> list = new List<T>();
            using (MemoryStream dataStream = new MemoryStream(data))
            {
                var dataReader = new BinaryReader(dataStream);

                int count = dataReader.ReadInt32();
                for (int i = 0; i < count; i++)
                {
                    int length = dataReader.ReadInt32();
                    list.Add(load(dataReader.ReadBytes(length)));
                }
            }
            return list;
        }

        public static byte[] PutGuidMMap<T>(Dictionary<Guid, List<T>> MMap, Func<T, byte[]> store)
        {
            if (MMap == null)
                return new byte[0];
            using (MemoryStream dataStream = new MemoryStream())
            {
                using (var dataWriter = new BinaryWriter(dataStream))
                {
                    long countPos = dataWriter.BaseStream.Position;
                    int counter = 0;
                    dataWriter.Write(counter);

                    foreach (var list in MMap)
                    {
                        foreach (T item in list.Value)
                        {
                            dataWriter.Write(list.Key.ToByteArray());
                            byte[] data = store(item);
                            dataWriter.Write(data.Length);
                            dataWriter.Write(data);
                            counter++;
                        }
                    }

                    dataWriter.Seek((int)countPos, SeekOrigin.Begin);
                    dataWriter.Write(counter);
                }
                return dataStream.ToArray();
            }
        }

        public static Dictionary<Guid, List<T>> GetGuidMMap<T>(byte[] data, Func<byte[], T> load)
        {
            if (data.Length == 0)
                return null;
            Dictionary<Guid, List<T>> rules = new Dictionary<Guid, List<T>>();
            using (MemoryStream dataStream = new MemoryStream(data))
            {
                var dataReader = new BinaryReader(dataStream);

                int count = dataReader.ReadInt32();
                for (int i = 0; i < count; i++)
                {
                    Guid id = new Guid(dataReader.ReadBytes(16));
                    int length = dataReader.ReadInt32();
                    List<T> list;
                    if (!rules.TryGetValue(id, out list))
                    {
                        list = new List<T>();
                        rules.Add(id, list);
                    }
                    list.Add(load(dataReader.ReadBytes(length)));
                }
            }
            return rules;
        }

        public static byte[] PutXmlObj<T>(T obj)
        {
            DataContractSerializer xmlSerializer = new DataContractSerializer(typeof(T));
            using (MemoryStream xmlStream = new MemoryStream())
            {
                xmlSerializer.WriteObject(xmlStream, obj);
                return xmlStream.ToArray();
            }
        }

        public static T GetXmlObj<T>(byte[] value)
        {
            DataContractSerializer xmlSerializer = new DataContractSerializer(typeof(T));
            using (MemoryStream stream = new MemoryStream(value))
            {
                T obj = (T)xmlSerializer.ReadObject(stream);
                return obj;
            }
        }
    }
}
