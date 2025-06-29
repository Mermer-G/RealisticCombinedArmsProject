using System;
using System.Collections.Generic;

public static class BoolEventManager
{
    private static Dictionary<string, Action<bool>> eventTable = new();

    public static void Subscribe(string key, Action<bool> callback)
    {
        if (!eventTable.ContainsKey(key))
            eventTable[key] = delegate { };

        eventTable[key] += callback;
    }

    public static void Unsubscribe(string key, Action<bool> callback)
    {
        if (eventTable.ContainsKey(key))
            eventTable[key] -= callback;
    }

    public static void Invoke(string key, bool value)
    {
        if (eventTable.ContainsKey(key))
            eventTable[key].Invoke(value);
    }
}
