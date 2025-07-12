using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class InteriorLightController : MonoBehaviour
{
    EnergyConsumerComponent consumer;

    [SerializeField] float panelBrightness;
    [SerializeField] float consoleBrightness;
    [SerializeField] float brightnessMultiplier;

    #region MaterialGroups
    [System.Serializable]
    public class MaterialGroup
    {
        public string groupName;
        public GameObject rootObject;  // Grup kökü (root)
        public Material material;
        [HideInInspector]
        public List<Renderer> groupRenderers = new List<Renderer>(); // Burada o gruba atanmýþ renderlar toplanacak
    }

    public List<MaterialGroup> materialGroups = new List<MaterialGroup>();

    void GetMaterialGroups()
    {
        foreach (var group in materialGroups)
        {
            group.groupRenderers.Clear();

            if (group.rootObject == null) continue;

            group.groupRenderers = GetRenderersWithMaterial(group.rootObject, group.material);
            print("Group " + group.groupName + " has been added with " + group.groupRenderers.Count + "elements");
        }
    }

    // Belirli bir GameObject içinde ilgili materyali kullanan render'larý bulur
    List<Renderer> GetRenderersWithMaterial(GameObject root, Material reference)
    {
        List<Renderer> results = new List<Renderer>();
        Renderer[] allRenderers = root.GetComponentsInChildren<Renderer>(true);

        foreach (var rend in allRenderers)
        {
            foreach (var mat in rend.sharedMaterials)
            {
                if (mat == reference)
                {
                    results.Add(rend);
                    break;
                }
            }
        }

        return results;
    }

    public List<Renderer> GetGroupRenderers(string groupName)
    {
        var group = materialGroups.Find(g => g.groupName == groupName);
        if (group != null)
            return group.groupRenderers;
        return null;
    }
    #endregion

    private void OnEnable()
    {
        SlidableEventHandler.Subscribe("SetInstrumentPanelBrightness", SetInstrumentPanelBrightness);
        SlidableEventHandler.Subscribe("SetConsoleBrightness", SetConsoleBrightness);
    }

    private void OnDisable()
    {
        SlidableEventHandler.Unsubscribe("SetInstrumentPanelBrightness", SetInstrumentPanelBrightness);
        SlidableEventHandler.Unsubscribe("SetConsoleBrightness", SetConsoleBrightness);
    }

    void SetInstrumentPanelBrightness(float value)
    {
        panelBrightness = value;
        if (!consumer.IsPoweredE) consumer.ChangePowerStatusE(true);
    }

    void SetConsoleBrightness(float value)
    {
        consoleBrightness = value;
        if (!consumer.IsPoweredE) consumer.ChangePowerStatusE(true);
    }

    void SetPanelBrightness()
    {
        var b = panelBrightness * brightnessMultiplier;
        if (!consumer.IsPoweredE) b = 0;

        var panelElements = GetGroupRenderers("PanelElements");
        var FTIT = GetGroupRenderers("FTITGauge");
        var RPM = GetGroupRenderers("RPMPercentGauge");
        var NOZPOS = GetGroupRenderers("NOZPOSGauge");
        foreach (var renderer in panelElements)
        {
            for (int i = 0; i < renderer.materials.Length; i++)
            {
                if (renderer.materials[i].name.Contains("InstrumentEmission")) // veya doðrudan == targetMaterial
                {
                    renderer.materials[i].SetFloat("_EmissionS", b);
                }
            }
        }
        FTIT[0].material.SetFloat("_EmissionS", b);
        FTIT[1].material.SetFloat("_EmissionS", b);
        RPM[0].material.SetFloat("_EmissionS", b);
        RPM[1].material.SetFloat("_EmissionS", b);
        NOZPOS[0].material.SetFloat("_EmissionS", b);
        NOZPOS[1].material.SetFloat("_EmissionS", b);
    }

    void SetConsoleBrightness()
    {
        var b = consoleBrightness * brightnessMultiplier;
        if (!consumer.IsPoweredE) b = 0;

        var consoleElements = GetGroupRenderers("ConsoleElements");
        var consoleTexts = GetGroupRenderers("ConsoleTexts");
        foreach (var renderer in consoleElements)
        {
            for (int i = 0; i < renderer.materials.Length; i++)
            {
                if (renderer.materials[i].name.Contains("ConsoleEmission")) // veya doðrudan == targetMaterial
                {
                    renderer.materials[i].SetFloat("_EmissionS", b);
                }
            }
        }
        foreach (var renderer in consoleTexts)
        {
            renderer.material.SetFloat("_EmissionS", b);
        }
    }

    // Start is called before the first frame update
    void Start()
    {
        consumer = GetComponent<EnergyConsumerComponent>();
        GetMaterialGroups();
    }

    private void Update()
    {
        SetPanelBrightness();
        SetConsoleBrightness(); 
    }
}
