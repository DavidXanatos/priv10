using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

public class MultiValueDictionary<TKey, TValue> : Dictionary<TKey, CloneableList<TValue>>
{
    public MultiValueDictionary() : base()
    {
    }

    public MultiValueDictionary<TKey, TValue> Clone() // shallow copy!
    {
        MultiValueDictionary<TKey, TValue> clone = new MultiValueDictionary<TKey, TValue>();
        foreach (KeyValuePair<TKey, CloneableList<TValue>> kvp in this)
        {
            clone.Add(kvp.Key, kvp.Value.Clone());
        }
        return clone;
    }

    public void Add(TKey key, TValue value)
    {
        CloneableList<TValue> container = null;
        if (!this.TryGetValue(key, out container))
        {
            container = new CloneableList<TValue>();
            base.Add(key, container);
        }
        container.Add(value);
    }

    public bool ContainsValue(TKey key, TValue value)
    {
        bool toReturn = false;
        CloneableList<TValue> values = null;
        if (this.TryGetValue(key, out values))
        {
            toReturn = values.Contains(value);
        }
        return toReturn;
    }
    
    public void Remove(TKey key, TValue value)
    {
        CloneableList<TValue> container = null;
        if (this.TryGetValue(key, out container))
        {
            container.Remove(value);
            if (container.Count <= 0)
            {
                this.Remove(key);
            }
        }
    }
    
    public CloneableList<TValue> GetValues(TKey key, bool returnEmptySet = true)
    {
        CloneableList<TValue> toReturn = null;
        if (!base.TryGetValue(key, out toReturn) && returnEmptySet)
        {
            toReturn = new CloneableList<TValue>();
        }
        return toReturn;
    }

    public CloneableList<TValue> GetOrAdd(TKey key)
    {
        CloneableList<TValue> toReturn = null;
        if (!base.TryGetValue(key, out toReturn))
        {
            toReturn = new CloneableList<TValue>();
            base.Add(key, toReturn);
        }
        return toReturn;
    }

    public int GetCount()
    {
        int Count = 0;
        foreach (KeyValuePair<TKey, CloneableList<TValue>> pair in this)
            Count += pair.Value.Count;
        return Count;
    }

    public TValue GetAt(int index)
    {
        int Count = 0;
        foreach (KeyValuePair<TKey, CloneableList<TValue>> pair in this)
        {
            if (Count + pair.Value.Count > index)
                return pair.Value[index - Count];
            Count += pair.Value.Count;
        }
        throw new IndexOutOfRangeException();
    }

    public TKey GetKey(int index)
    {
        int Count = 0;
        foreach (KeyValuePair<TKey, CloneableList<TValue>> pair in this)
        {
            if (Count + pair.Value.Count > index)
                return pair.Key;
            Count += pair.Value.Count;
        }
        throw new IndexOutOfRangeException();
    }

    public CloneableList<TValue> GetAllValues()
    {
        CloneableList<TValue> toReturn = new CloneableList<TValue>();
        foreach (List<TValue> values in this.Values)
        {
            foreach (TValue value in values)
                toReturn.Add(value);
        }
        return toReturn;
    }
}
