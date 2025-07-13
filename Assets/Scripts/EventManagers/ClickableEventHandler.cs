using System;
using System.Collections.Generic;

public static class ClickableEventHandler
{
    private static Dictionary<string, Action> eventTable = new();
    private static Dictionary<string, Action<object>> eventTableWithSender = new();

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

    // Yeni sistem – sender destekli
    public static void Subscribe(string key, Action<object> callback)
    {
        if (!eventTableWithSender.ContainsKey(key))
            eventTableWithSender[key] = delegate { };

        eventTableWithSender[key] += callback;
    }

    public static void Unsubscribe(string key, Action<object> callback)
    {
        if (eventTableWithSender.ContainsKey(key))
            eventTableWithSender[key] -= callback;
    }

    public static void Invoke(string key, object sender)
    {
        if (eventTableWithSender.ContainsKey(key))
            eventTableWithSender[key].Invoke(sender);
    }
}
