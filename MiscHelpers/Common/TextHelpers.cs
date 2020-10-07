using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace MiscHelpers
{
    public class TextHelpers
    {
        public static String GetLeft(ref String Line, String sep = " ")
        {
            int pos = Line.IndexOf(sep);
            String ret;
            if (pos == -1)
            {
                ret = Line;
                Line = "";
            }
            else
            {
                ret = Line.Substring(0, pos);
                Line = Line.Remove(0, pos + 1);
            }
            return ret;
        }

        public static String get2nd(String Line, String sep = ":")
        {
            int pos = Line.IndexOf(sep);
            if (pos == -1)
                return "";
            return Line.Substring(pos + 1).Trim();
        }

        public static String get1st(String Line, String sep = ":")
        {
            int pos = Line.IndexOf(sep);
            if (pos == -1)
                return Line;
            return Line.Substring(0, pos).Trim();
        }

        public static Tuple<string, string> Split2(string str, String sep = ":", bool rev = false)
        {
            int pos = rev ? str.LastIndexOf(sep) : str.IndexOf(sep);
            if (pos == -1)
                return new Tuple<string, string>(str.Trim(), "");
            return new Tuple<string, string>(str.Substring(0, pos).Trim(), str.Substring(pos + 1).Trim());
        }

        public static bool CompareWildcard(string str, string find)
        {
            if (str == null)
                return false;
            //var like = "^" + Regex.Escape(find).Replace("_", ".").Replace("%", ".*") + "$";
            var like = Regex.Escape(find).Replace("_", ".").Replace("\\*", ".*");
            return Regex.IsMatch(str, like, RegexOptions.IgnoreCase);
        }

        public static List<string> TokenizeStr(string input)
        {
            List<String> output = new List<String>();
            foreach (string str in Regex.Matches(input, @"[\""].+?[\""]|[^ ]+").Cast<Match>().Select(m => m.Value).ToList())
            {
                if (str.Length > 2 && str.ElementAt(0) == '"')
                    output.Add(str.Substring(1, str.Length - 2));
                else
                    output.Add(str);
            }
            return output;
        }

        public static List<string> SplitStr(string str, string sep, bool bKeepEmpty = false)
        {
            List<string> strList = new List<string>();
            String[] spearator = { sep };
            foreach (var curStr in str.Split(spearator, StringSplitOptions.None))
            {
                var tmpStr = curStr.Trim();
                if (tmpStr.Length > 0 || bKeepEmpty)
                    strList.Add(tmpStr);
            }
            return strList;
        }
    }
}
