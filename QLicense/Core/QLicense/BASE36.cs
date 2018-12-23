using System;
using System.Text;

namespace QLicense
{
    class BASE36
    {
        private const string _charList = "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZ";
        private static readonly char[] _charArray = _charList.ToCharArray();

        public static long Decode(string input)
        {
            long _result = 0;
            double _pow = 0;
            for (int _i = input.Length - 1; _i >= 0; _i--)
            {
                char _c = input[_i];
                int pos = _charList.IndexOf(_c);
                if (pos > -1)
                    _result += pos * (long)Math.Pow(_charList.Length, _pow);
                else
                    return -1;
                _pow++;
            }
            return _result;
        }

        public static string Encode(ulong input)
        {
            StringBuilder _sb = new StringBuilder();
            do
            {
                _sb.Append(_charArray[input % (ulong)_charList.Length]);
                input /= (ulong)_charList.Length;
            } while (input != 0);

            return Reverse(_sb.ToString());
        }

        private static string Reverse(string s)
        {
            var charArray = s.ToCharArray();
            Array.Reverse(charArray);
            return new string(charArray);
        }
    }
}
