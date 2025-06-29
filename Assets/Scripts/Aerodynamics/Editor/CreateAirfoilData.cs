using UnityEngine;
using UnityEditor;
using System.IO;
using System.Collections.Generic;
using System;

public class CreateAirfoilData : EditorWindow
{
    [SerializeField] TextAsset txtFile;
    [SerializeField] string xfoilname;
    [MenuItem("Tools/Import XFOIL Data")]
    public static void ShowWindow()
    {
        GetWindow<CreateAirfoilData>("XFOIL Data Importer");
    }

    private void OnGUI()
    {
        GUILayout.Label("XFOIL Data Importer", EditorStyles.boldLabel);
        //filePath = EditorGUILayout.TextField("File Path:", filePath);
        txtFile = (TextAsset)EditorGUILayout.ObjectField("Select XFOIL File:", txtFile, typeof(TextAsset), false);

        xfoilname = EditorGUILayout.DelayedTextField("Name: ", xfoilname);

        if (GUILayout.Button("Import Data"))
        {
            ImportData(AssetDatabase.GetAssetPath(txtFile), xfoilname);
        }
    }

    private static void ImportData(string path, string name)
    {
        if (!File.Exists(path))
        {
            Debug.LogError("File not found: " + path);
            return;
        }

        List<Keyframe> clKeys = new List<Keyframe>();
        List<Keyframe> cdKeys = new List<Keyframe>();

        bool dataStarted = false;

        foreach (string line in File.ReadLines(path))
        {
            if (line.Contains("alpha")) // Baþlangýç satýrýný bul
            {
                dataStarted = true;
                continue;
            }

            if (dataStarted)
            {
                string[] values = line.Split(new char[] { ' ' }, System.StringSplitOptions.RemoveEmptyEntries);
                if (values.Length < 3) continue;

                if (float.TryParse(values[0].Replace('.',','), out float alpha) &&
                    float.TryParse(values[1].Replace('.', ','), out float cl) &&
                    float.TryParse(values[2].Replace('.', ','), out float cd))
                {
                    clKeys.Add(new Keyframe(alpha, cl));
                    cdKeys.Add(new Keyframe(alpha, cd));
                }
            }
        }

        // AnimationCurve oluþtur
        AnimationCurve clCurve = new AnimationCurve(clKeys.ToArray());
        AnimationCurve cdCurve = new AnimationCurve(cdKeys.ToArray());

        // ScriptableObject olarak kaydet
        XFoilData asset = ScriptableObject.CreateInstance<XFoilData>();
        asset.clCurve = clCurve;
        asset.cdCurve = cdCurve;

        string assetPath = "Assets/Scripts/Aerodynamics/AirfoilData/" + name + ".asset";
        AssetDatabase.CreateAsset(asset, assetPath);
        AssetDatabase.SaveAssets();

        Debug.Log("XFOIL Data imported successfully to " + assetPath);
    }
}

