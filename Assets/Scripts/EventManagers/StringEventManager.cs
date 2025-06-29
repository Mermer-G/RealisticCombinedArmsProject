using System;
using System.Collections.Generic;

public static class StringEventManager
{
    private static Dictionary<string, Action<string>> eventTable = new Dictionary<string, Action<string>>();

    public static void Subscribe(string key, Action<string> callback)
    {
        if (!eventTable.ContainsKey(key))
            eventTable[key] = delegate { };

        eventTable[key] += callback;
    }

    public static void Unsubscribe(string key, Action<string> callback)
    {
        if (eventTable.ContainsKey(key))
        {
            eventTable[key] -= callback;
        }
    }

    public static void Invoke(string key, string value)
    {
        if (eventTable.ContainsKey(key))
            eventTable[key].Invoke(value);
    }
}