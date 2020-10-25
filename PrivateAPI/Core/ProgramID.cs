using MiscHelpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace PrivateAPI
{
    [Serializable()]
    [DataContract(Name = "ProgramID", Namespace = "http://schemas.datacontract.org/")]
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

        [DataMember()]
        public Types Type { get; protected set; } = Types.Program;
        [DataMember()]
        public string Path { get; protected set; } = ""; // process path
        [DataMember()]
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
                return NtUtilities.NtOsKrnlPath;
            return Path;
        }

        public string GetPackageSID()
        {
            if (Type != Types.App)
                return null;
            return Aux;
        }

        public string GetServiceId()
        {
            if (Type != Types.Service)
                return null;
            return Aux;
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
                WindowsBinary = NtUtilities.IsWindowsBinary(Path);
            return WindowsBinary.Value;
        }

        public bool IsApp()
        {
            return Type == Types.App;
        }

        private string AppPackageName = null;
        public static Func<string, string> PackageNameResolver = null;

        public string GetPackageName()
        {
            if (Type != Types.App)
                return null;
            if (AppPackageName == null && PackageNameResolver != null)
                AppPackageName = PackageNameResolver(Aux);
            return AppPackageName;
        }


        /////////////////////////////////////////////
        // comparison helper

        public enum FuzzyModes : int
        {
            No = 0,
            Tag = 1,
            Path = 2,
            Any = 3
        };

        public static T GetProgramFuzzy<T>(SortedDictionary<ProgramID, T> Programs, ProgramID progID, FuzzyModes fuzzyMode) where T : class
        {
            T prog = null;
            if (Programs.TryGetValue(progID, out prog))
                return prog;

            // Only works for services and apps 
            if (!(progID.Type == ProgramID.Types.Service || progID.Type == ProgramID.Types.App))
                return null;

            if ((fuzzyMode & FuzzyModes.Tag) != 0 && progID.Aux.Length > 0)
            {
                // first drop path and try to get by serviceTag or application SID
                ProgramID auxId = new ProgramID(progID.Type, null, progID.Aux);
                if (Programs.TryGetValue(auxId, out prog))
                    return prog;
            }

            if ((fuzzyMode & FuzzyModes.Path) != 0 && progID.Path.Length > 0
             && (progID.Type == ProgramID.Types.Service || progID.Type == ProgramID.Types.App)
             && System.IO.Path.GetFileName(progID.Path).Equals("svchost.exe", StringComparison.OrdinalIgnoreCase) == false) // dont use this for svchost.exe
            {
                // than try to get an entry by path only
                ProgramID pathId = new ProgramID(ProgramID.Types.Program, progID.Path, null);
                if (Programs.TryGetValue(pathId, out prog))
                    return prog;
            }

            return null;
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
