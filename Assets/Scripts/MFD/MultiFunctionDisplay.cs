using System.Linq;
using Unity.VectorGraphics;
using UnityEngine;

public class MultiFunctionDisplay : MonoBehaviour, ISensorOfInterest
{
    [SerializeField] int activeFormatIndex;
    [SerializeField] string MFDID;
    [SerializeField] SVGImage SOIRect;
    [SerializeField] float timeToBoot;
    float bootStartedAt;
    //Powered up and booted
    bool active;

    MFDOSB[] OSBArray;
    IMFDFormat[] formats;
    EnergyConsumerComponent consumer;

    ISensorOfInterest SOIFormat;
    IMFDFormat currentFormat;

    void HandleOSB(object sender)
    {
        if (((MonoBehaviour)sender).TryGetComponent(out MFDOSB osb))
        {
            HandleArg(osb.tmp.text);
        }
        else
        {
            Debug.LogError($"Object named: {((GameObject)sender).name} doesn't have an MFDOSB comp!");
        }
    }

    void HandleArg(string arg)
    {
        if (currentFormat != null && currentFormat.HandleArg(arg))
            return;

        // Global format deðiþim komutlarý
        switch (arg)
        {
            case "TGP":
                foreach (var format in formats)
                {
                    if (format is TGPMFDFormat)
                    {
                        SwitchFormat(format);
                    }
                }
                
                break;
            //case "FCR":
            //    throw new System.NotImplementedException();
            //    break;
            case "MAIN":
                foreach (var format in formats)
                {
                    if (format is MainMFDFormat)
                    {
                        SwitchFormat(format);
                    }
                }
                break;
            default:
                Debug.LogWarning($"Unhandled OSB arg: {arg}");
                break;
        }
    }

    void GetOSBUpdates()
    {
        while (currentFormat.OSBUpdates.Count > 0)
        {
            var update = currentFormat.OSBUpdates.Pop();
            if (update.index >= OSBArray.Length) continue;

            OSBArray[update.index].tmp.text = update.label;
        }
    }

    void SwitchFormat(IMFDFormat newFormat)
    {
        currentFormat?.OnFormatExit();
        currentFormat = newFormat;
        SOIFormat = (ISensorOfInterest)newFormat;
        currentFormat.OnFormatEnter();
    }

    void GetFormats()
    {
        formats = GetComponentsInChildren<IMFDFormat>();
    }

    void GetOSBs()
    {
        OSBArray = GetComponentsInChildren<MFDOSB>()
            .OrderByDescending(osb => osb.id) // index'e göre büyükten küçüðe sýrala
            .ToArray(); // Sonucu tekrar diziye çevir
    }

    public void SetSOI()
    {
        SOIFormat.SetSOI();
        SOIRect.enabled = true;
    }

    public void UnSetSOI()
    {
        SOIFormat.UnSetSOI();
        SOIRect.enabled = false;
    }

    void Boot()
    {
        if (active) return;
        if (!consumer.IsPoweredE) return;
            

        if (bootStartedAt == 0)
            bootStartedAt = Time.time;
        if (bootStartedAt + timeToBoot <= Time.time)
        {
            active = true;
            currentFormat.OnFormatEnter();
        }
    }

    void Awake()
    {
        GetOSBs();
        GetFormats();
        consumer = GetComponent<EnergyConsumerComponent>();
        if (SOIFormat is null)
        {
            currentFormat = formats[activeFormatIndex];
            SOIFormat = (ISensorOfInterest)currentFormat;
            foreach (IMFDFormat format in formats)
            {
                print("Found format: " + ((MonoBehaviour)format).name);
                ((MonoBehaviour)format).gameObject.SetActive(false);
            }
        }
    }

    void MFDOn()
    {
        consumer.ChangePowerStatusE(true);
    }

    void MFDOff()
    {
        consumer.ChangePowerStatusE(false);
    }

    private void OnEnable()
    {
        ClickableEventHandler.Subscribe(MFDID, HandleOSB);
        ClickableEventHandler.Subscribe("MFDOn", MFDOn);
        ClickableEventHandler.Subscribe("MFDOff", MFDOff);
    }

    private void OnDisable()
    {
        ClickableEventHandler.Unsubscribe(MFDID, HandleOSB);
        ClickableEventHandler.Unsubscribe("MFDOn", MFDOn);
        ClickableEventHandler.Unsubscribe("MFDOff", MFDOff);
    }

    void Update()
    {
        if (!consumer.IsPoweredE)
        {
            if (((MonoBehaviour)currentFormat).isActiveAndEnabled)
                currentFormat.OnFormatExit();
            return;
        }

        Boot();
        GetOSBUpdates();
        currentFormat?.OnFormatStay();
    }

    
}
