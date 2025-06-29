using System;
using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;

public static class GenericEventManager
{
    private static Dictionary<Type, Dictionary<string, Delegate>> eventTables = new();

    public static void Subscribe<T>(string key, Action<T> callback)
    {
        var type = typeof(T);
        if (!eventTables.ContainsKey(type))
            eventTables[type] = new Dictionary<string, Delegate>();

        var table = eventTables[type];

        if (!table.ContainsKey(key))
            table[key] = null;

        table[key] = (Action<T>)table[key] + callback;
    }

    public static void Unsubscribe<T>(string key, Action<T> callback)
    {
        var type = typeof(T);
        if (eventTables.ContainsKey(type) && eventTables[type].ContainsKey(key))
            eventTables[type][key] = (Action<T>)eventTables[type][key] - callback;
    }

    public static void Invoke<T>(string key, T value)
    {
        var type = typeof(T);
        if (eventTables.ContainsKey(type) && eventTables[type].ContainsKey(key))
            ((Action<T>)eventTables[type][key])?.Invoke(value);
    }
}