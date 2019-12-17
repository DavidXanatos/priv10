using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace PrivateWin10
{
    [Serializable()]
    public class ProgramID : IComparable
    {
        public enum Types
        {
            Global = 0,
            System,
            Service,
            Program,
            App
        }

        public Types Type { get; private set; } = Types.Program;
        public string Path { get; private set; }  = ""; // process path
        public string RawPath { get; private set; } = null;
        public string Aux { get; private set; } = ""; // service name or app package sid

        //[NonSerialized()]
        //public string Description = "";

        public static ProgramID NewID(Types Type)
        {
            return new ProgramID(Type, "", "");
        }

        public static ProgramID NewProgID(string Path, bool Expand = false)
        {
            return new ProgramID(Types.Program, Path, "", Expand);
        }

        public static ProgramID NewSvcID(string Svc, string Path = "", bool Expand = false)
        {
            return new ProgramID(Types.Service, Path == null ? "" : Path, Svc, Expand);
        }

        public static ProgramID NewAppID(string SID, string Path = "", bool Expand = false)
        {
            return new ProgramID(Types.App, Path == null ? "" : Path, SID, Expand);
        }

        public static ProgramID New(Types type, string path, string aux)
        {
            return new ProgramID(type, path, aux);
        }

        public ProgramID()
        {
        }

        private ProgramID(Types type, string path, string aux, bool expand = false)
        {
            Type = type;
            if (expand)
            {
                RawPath = path;
                Path = Environment.ExpandEnvironmentVariables(RawPath);
                if (Path.Equals(RawPath))
                    RawPath = null;
            }
            else
                Path = path;
            Aux = aux;
        }

        public ProgramID Duplicate()
        {
            return new ProgramID(Type, Path, Aux);
        }

        public int CompareTo(object obj)
        {
            if ((int)Type > (int)(obj as ProgramID).Type)
                return 1;
            else if ((int)Type < (int)(obj as ProgramID).Type)
                return -1;

            if (Aux != null)
            {
                int ret = string.Compare(Aux, (obj as ProgramID).Aux, true);
                if (ret != 0)
                    return ret;
            }

            return Path == null ? 0 : string.Compare(Path, (obj as ProgramID).Path, true);
        }

        public string GetPath()
        {
            if (Type == Types.Global)
                return null;
            if (Type == Types.System)
                return MiscFunc.NtOsKrnlPath;
            return Path;
        }

        public string GetPackageSID()
        {
            if (Type != Types.App)
                return null;
            return Aux;
        }

        public string GetPackageName()
        {
            if (Type != Types.App)
                return null;
            return App.engine.FirewallManager.GetAppPkgBySid(Aux)?.ID;
        }

        public string GetServiceId()
        {
            if (Type != Types.Service)
                return null;
            return Aux;
        }

        public string GetServiceName()
        {
            if (Type != Types.Service)
                return null;
            return ServiceHelper.GetServiceName(Aux);
        }

        public void Store(XmlWriter writer, string nodeName = "ID")
        {
            writer.WriteStartElement(nodeName);

            writer.WriteElementString("Type", Type.ToString());
            writer.WriteElementString("Path", Path);
            writer.WriteElementString("Aux", Aux);

            writer.WriteEndElement();
        }

        public bool Load(XmlNode idNode)
        {
            try
            {
                Type = (Types)Enum.Parse(typeof(Types), idNode.SelectSingleNode("Type").InnerText);
                Path = idNode.SelectSingleNode("Path").InnerText;
                Aux = idNode.SelectSingleNode("Aux").InnerText;
            }
            catch {
                return false;
            }
            return true;
        }

        public string FormatString()
        {
            switch (Type)
            {
                case Types.System: return Translate.fmt("name_system");
                case Types.Service: return Translate.fmt("name_service", Path, Aux);
                case Types.Program: return Path;
                case Types.App: return Translate.fmt("name_app", Path, GetPackageName());
                default:
                case Types.Global: return Translate.fmt("name_global");
            }
        }

        public string AsString()
        {
            List<string> tokens = new List<string>();
            tokens.Add("Type=" + Type.ToString());
            if (Path != null && Path.Length > 0)
                tokens.Add("Path=" + Path);
            if (Path != null && Aux.Length > 0)
                tokens.Add("Aux=" + Path);
            return string.Join("|", tokens);
        }

        public static ProgramID Parse(string Str)
        {
            try
            {
                ProgramID progID = new ProgramID();
                foreach (string token in TextHelpers.SplitStr(Str, "|"))
                {
                    var IdVal = TextHelpers.Split2(token, "=");
                    if (IdVal.Item1 == "Type")
                        progID.Type = (Types)Enum.Parse(typeof(Types), IdVal.Item2);
                    else if (IdVal.Item1 == "Path")
                        progID.Path = IdVal.Item2;
                    else if (IdVal.Item1 == "Aux")
                        progID.Aux = IdVal.Item2;
                }
                return progID;
            }
            catch
            {
                return null;
            }
        }
    }
}
