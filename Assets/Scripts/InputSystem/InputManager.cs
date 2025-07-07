using System.Collections.Generic;
using UnityEngine;
using Newtonsoft.Json;
using System.IO;
using Newtonsoft.Json.Converters;
using System.Text;
using System.Reflection;
using System.Linq;
using Directory = System.IO.Directory;
using File = System.IO.File;
using System;
using UnityEngine.Rendering;
using Palmmedia.ReportGenerator.Core.Parser.Analysis;
using UnityEngine.WSA;
using Application = UnityEngine.Application;
using System.IO.Compression;

public class InputManager : MonoBehaviour
{
    public static InputManager instance;

    InputContainer container;
    public Dictionary<string, InputMap> maps = new Dictionary<string, InputMap>();
    Dictionary<string, InputAction> actions = new Dictionary<string, InputAction>();


    public 
    // Start is called before the first frame update
    void Awake()
    {
        if (instance == null) instance = this;
        //container = InptCfgLoader.LoadContainer("TestInput", "D:\\Unity Projects\\RealisticCombinedArmsProject\\Assets\\Inputs");
        LoadMaps("TestInput");
    }


    bool value1 = false;
    bool value2 = false;

    // Update is called once per frame
    void Update()
    {
        UpdateInputs();

        if (GetInput("MoveUp") == 1 ? true : false) value1 = !value1;
        TextFieldManager.Instance.CreateOrUpdateScreenField("W").Value("W value: " + value1).End();

        //value2 = GetInput("HoldFire") == 1 ? true : false;
        //TextFieldManager.Instance.CreateOrUpdateScreenField("Space").Value("Space value: " + value2).End();

        if (UnityEngine.Input.GetKeyDown(KeyCode.N)) SaveMaps("TestInput");
    }

    public float GetInput(string actionName)
    {
        if (!actions.ContainsKey(actionName) || actions[actionName]?.trigger == null)
        {
            Debug.LogError($"Requested action name: {actionName} not found or trigger is null!");
            return 0;
        }

        var mapName = actions[actionName].trigger.mapName;
        if (!maps.TryGetValue(mapName, out var map) || map == null)
        {
            Debug.LogError($"Map not found: {mapName}");
            return 0;
        }

        if (!map.isActive) return 0;

        Debug.Log("requested input for: " + actionName + "value is: " + actions[actionName].trigger.GetValue());

        return actions[actionName].trigger.GetValue();
    }

    void UpdateInputs()
    {
        foreach (var map in maps)
        {
            if (map.Value.inputActions == null) { Debug.LogError("There's a null in input maps dictionary!"); continue; }
            if (!map.Value.isActive) continue;

            foreach (var action in map.Value.inputActions)
            {
                if (action.trigger == null) { Debug.LogError("There's a null in input maps dictionary!"); continue; }
                if (!action.trigger.requiresUpdate) continue;

                else 
                { 
                    action.trigger.UpdateTrigger();
                    Debug.Log("Updated action: " + action.name);
                }
            }
        }
    }

    public InputContainer LoadMaps(string containerName)
    {
        string rootFolder = "Inputs";
        string basePath = Application.dataPath;
        string containerFolder = Path.Combine(basePath, rootFolder, containerName);

        if (!Directory.Exists(containerFolder))
        {
            Debug.LogWarning($"Container folder not found: {containerFolder}");
            return null;
        }


        // 1. Container objesi oluşturuluyor
        var container = new InputContainer
        {
            containerName = containerName
        };


        // 2. Tüm JSON dosyaları okunuyor
        string[] mapFiles = Directory.GetFiles(containerFolder, "*.json");

        var settings = new JsonSerializerSettings
        {
            TypeNameHandling = TypeNameHandling.All,
            Formatting = Formatting.Indented,
            Converters = { new StringEnumConverter() }
        };

        foreach (string path in mapFiles)
        {
            try
            {
                string json = File.ReadAllText(path);
                InputMap map = JsonConvert.DeserializeObject<InputMap>(json, settings);
                maps[map.mapName] = map;
                
            }
            catch (Exception ex)
            {
                Debug.LogError($"Failed to load map from {path}: {ex.Message}");
            }
        }

        foreach (InputMap map in maps.Values)
        {
            foreach (var action in map.inputActions)
            {
                actions[action.name] = action; 
            }
        }

        return container;
    }

    void SaveMaps(string containerName)
    {
        string rootFolder = "Inputs";
        string basePath = Application.dataPath;

        // 1. Container klasörü oluşturuluyor
        string containerFolder = Path.Combine(rootFolder, container.containerName);
        containerFolder = Path.Combine(basePath, containerFolder);
        Directory.CreateDirectory(containerFolder);

        // 3. Her bir map ayrı dosya olarak export ediliyor
        foreach (var map in container.inputMaps)
        {
            string path = Path.Combine(containerFolder, map.mapName + ".json");
            var settings = new JsonSerializerSettings
            {
                TypeNameHandling = TypeNameHandling.All,
                Formatting = Formatting.Indented,
                Converters = { new StringEnumConverter() }
            };

            string json = JsonConvert.SerializeObject(map, settings);
            //json = json.Replace("$type","type");
            //json = json.Replace("$values", "values");
            File.WriteAllText(path, json);
        }
    }
}
