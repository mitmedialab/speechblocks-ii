// GetOrSpawn is based on Daniel Jonsson's idea in https://stackoverflow.com/questions/15622622/analogue-of-pythons-defaultdict
using System;
using System.Collections.Generic;

public class DictUtil
{
    public static TValue GetOrSpawn<TKey, TValue>(Dictionary<TKey, TValue> dict, TKey key) where TValue : new()
    {
        TValue val;
        if (!dict.TryGetValue(key, out val))
        {
            val = new TValue();
            dict.Add(key, val);
        }
        return val;
    }

    public static TValue GetOrDefault<TKey, TValue>(Dictionary<TKey, TValue> dict, TKey key)
    {
        TValue val;
        if (!dict.TryGetValue(key, out val)) { return default(TValue); }
        return val;
    }

    public static TValue GetOrDefault<TKey, TValue>(Dictionary<TKey, TValue> dict, TKey key, TValue defaultValue)
    {
        TValue val;
        if (!dict.TryGetValue(key, out val)) { return defaultValue; }
        return val;
    }
}
