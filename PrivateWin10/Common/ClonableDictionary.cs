using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

public class CloneableDictionary<TKey, TValue> : Dictionary<TKey, TValue>
{
    public CloneableDictionary<TKey, TValue> Clone() // shallow copy!
    {
        CloneableDictionary<TKey, TValue> clone = new CloneableDictionary<TKey, TValue>();
        foreach (KeyValuePair<TKey, TValue> kvp in this)
        {
            clone.Add(kvp.Key, kvp.Value);
        }
        return clone;
    }
}