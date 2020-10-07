using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MiscHelpers
{
    public class CloneableList<TValue> : List<TValue>
    {
        public CloneableList<TValue> Clone() // shallow copy!
        {
            CloneableList<TValue> clone = new CloneableList<TValue>();
            foreach (TValue v in this)
            {
                clone.Add(v);
            }
            return clone;
        }
    }
}