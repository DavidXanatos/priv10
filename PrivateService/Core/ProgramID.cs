using MiscHelpers;
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

        public Types Type { get; protected set; } = Types.Program;
        public string Path { get; protected set; } = ""; // process path
        public string Aux { get; protected set; } = ""; // service name or app package sid

        //[NonSerialized()]
        //public string Description = "";

        public static ProgramID NewID(Types Type)
        {
            return new ProgramID(Type, "", "");
        }

        public static ProgramID NewProgID(string Path)
        {
            return new ProgramID(Types.Program, Path, "");
        }

        public static ProgramID NewSvcID(string Svc, string Path = "")
        {
            return new ProgramID(Types.Service, Path == null ? "" : Path, Svc);
        }

        public static ProgramID NewAppID(string SID, string Path = "")
        {
            return new ProgramID(Types.App, Path == null ? "" : Path, SID);
        }

        public static ProgramID New(Types type, string path, string aux)
        {
            return new ProgramID(type, path, aux);
        }

        public ProgramID()
        {
        }

        private ProgramID(Types type, string path, string aux)
        {
            Type = type;
            Path = path;
            Aux = aux;
        }

        public ProgramID Duplicate()
        {
            return new ProgramID(Type, Path, Aux);
        }

        public virtual int CompareTo(object obj)
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
            // ToDo: xxx pull that through the core
            //return App.engine.FirewallManager.GetAppPkgBySid(Aux)?.ID; // this only works from core but we need to work from the UI to
            return AppManager.SidToAppPackage(Aux);
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
            catch
            {
                return false;
            }
            return true;
        }

        public string FormatString()
        {
            switch (Type)
            {
                case Types.System: return MiscFunc.NtOsKrnlPath;
                case Types.Service: return string.Format("{0} (service: {1})", Path, Aux); // Translate.fmt("name_service", Path, Aux);
                case Types.Program: return Path;
                case Types.App: return string.Format("{0} (app: {1})", Path, GetPackageName()); // Translate.fmt("name_app", Path, GetPackageName());
                default:
                case Types.Global: return "All Processes"; // Translate.fmt("name_global");
            }
        }

        public string AsString()
        {
            List<string> tokens = new List<string>();
            tokens.Add("Type=" + Type.ToString());
            if (Path != null && Path.Length > 0)
                tokens.Add("Path=" + Path);
            if (Aux != null && Aux.Length > 0)
                tokens.Add("Aux=" + Aux);
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

        private bool? WindowsBinary = null;

        public bool IsProgram()
        {
            return !IsSystem() && Type == Types.Program;
        }

        public bool IsSystem()
        {
            if (Type == ProgramID.Types.System || Type == ProgramID.Types.Global || Type == ProgramID.Types.Service)
                return true;

            if (WindowsBinary == null)
                WindowsBinary = MiscFunc.IsWindowsBinary(Path);
            return WindowsBinary.Value;
        }

        public bool IsApp()
        {
            return Type == Types.App;
        }
    }

    /*public class ProgIDSearch : ProgramID
    {
        public enum SearchMode
        {
            Strict = 0,
            ForRule
        }

        SearchMode Mode = SearchMode.Strict;

        public ProgIDSearch(ProgramID ID, SearchMode mode = SearchMode.Strict)
        {
            Type = ID.Type;
            Path = ID.Path;
            Aux = ID.Aux;
            Mode = mode;
        }

        public override int CompareTo(object obj)
        {
            if (Mode == SearchMode.ForRule)
            {
            }
            else
                return base.CompareTo(obj);
        }
    }*/
}
