using System;
using System.Collections.Generic;

public static class ClickableEventHandler
{
    private static Dictionary<string, Action> eventTable = new();

    public static void Subscribe(string key, Action callback)
    {
        if (!eventTable.ContainsKey(key))
            eventTable[key] = delegate { };

        eventTable[key] += callback;
    }

    public static void Unsubscribe(string key, Action callback)
    {
        if (eventTable.ContainsKey(key))
            eventTable[key] -= callback;
    }

    public static void Invoke(string key)
    {
        if (eventTable.ContainsKey(key))
            eventTable[key].Invoke();
    }
}
