using System;
using System.Collections.Generic;
using UnityEngine;

public static class Vector3EventManager
{
    private static Dictionary<string, Action<Vector3>> eventTable = new();

    public static void Subscribe(string key, Action<Vector3> callback)
    {
        if (!eventTable.ContainsKey(key))
            eventTable[key] = delegate { };

        eventTable[key] += callback;
    }

    public static void Unsubscribe(string key, Action<Vector3> callback)
    {
        if (eventTable.ContainsKey(key))
            eventTable[key] -= callback;
    }

    public static void Invoke(string key, Vector3 position)
    {
        if (eventTable.ContainsKey(key))
            eventTable[key].Invoke(position);
    }
}