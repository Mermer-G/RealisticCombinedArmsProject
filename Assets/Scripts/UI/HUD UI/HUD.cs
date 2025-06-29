using System.Collections.Generic;
using TMPro;
using Unity.VectorGraphics;
using UnityEngine;

public class HUD : MonoBehaviour
{
    [SerializeField] GameObject mainPanel;
    [SerializeField] float alpha = 0;
    [SerializeField] bool showElements;

    List<TextMeshProUGUI> texts = new List<TextMeshProUGUI>();
    List<SVGImage> images = new List<SVGImage>();

    EnergyConsumerComponent consumer;

    // Start is called before the first frame update
    void Start()
    {
        consumer = GetComponent<EnergyConsumerComponent>();
        GetElementsInPanel();
    }

    private void OnEnable()
    {
        GenericEventManager.Subscribe<bool>("1-HUDShow", GetShowElements);
        SlidableEventHandler.Subscribe("1-HUDAlpha", GetHudAlpha);
    }

    private void OnDisable()
    {
        GenericEventManager.Unsubscribe<bool>("1-HUDShow", GetShowElements);
        SlidableEventHandler.Subscribe("1-HUDAlpha", GetHudAlpha);
    }

    // Update is called once per frame
    void Update()
    {
        AdjustElements();
    }

    void GetHudAlpha(float value)
    {
        if (!consumer.IsPoweredE && value > 0)
        {
            consumer.ChangePowerStatusE(true);
        }
        if (value == 0)
            consumer.ChangePowerStatusE(false);
        alpha = value;
    }

    void GetShowElements(bool value)
    {
        showElements = value;
    }

    void AdjustElements()
    {
        float a = alpha;

        if (!consumer.IsPoweredE) showElements = false;
        else showElements = true;

        foreach (var text in texts)
        {
            if (showElements)
            {
                text.enabled = true;
                var color = text.color;
                color.a = a;
                text.color = color;
            }
            else text.enabled = false;
        }
        foreach (var image in images)
        {
            if (showElements)
            {
                image.enabled = true;
                var color = image.color;
                color.a = a;
                image.color = color;
            }
            else image.enabled = false;
        }
    }
    void GetElementsInPanel()
    {
        var t = mainPanel.GetComponentsInChildren<TextMeshProUGUI>();
        var i = mainPanel.GetComponentsInChildren<SVGImage>();

        foreach (var text in t)
        {
            texts.Add(text);
        }

        foreach (var image in i)
        {
            images.Add(image);
        }
    }

}
