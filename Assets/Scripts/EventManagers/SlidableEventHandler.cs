using System;
using System.Collections.Generic;

public static class SlidableEventHandler
{
    private static Dictionary<string, Action<float>> eventTable = new();

    public static void Subscribe(string key, Action<float> callback)
    {
        if (!eventTable.ContainsKey(key))
            eventTable[key] = delegate { };

        eventTable[key] += callback;
    }

    public static void Unsubscribe(string key, Action<float> callback)
    {
        if (eventTable.ContainsKey(key))
            eventTable[key] -= callback;
    }

    public static void Invoke(string key, float value)
    {
        if (eventTable.ContainsKey(key))
            eventTable[key].Invoke(value);
    }
}
