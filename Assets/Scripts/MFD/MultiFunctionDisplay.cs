using System.Linq;
using TMPro;
using Unity.VectorGraphics;
using UnityEngine;

public class MultiFunctionDisplay : MonoBehaviour, ISensorOfInterest
{
    [SerializeField] int activeFormatIndex;
    [SerializeField] string MFDID;
    [SerializeField] SVGImage SOIRect;
    [SerializeField] TextMeshProUGUI notSOIText;
    [SerializeField] GameObject mainMask;
    [SerializeField] float timeToBoot;
    float bootStartedAt;
    /// <summary>
    /// Powered up and booted
    /// </summary>
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

            OSBArray[update.index - 1].tmp.text = update.label;
        }
    }

    //Check if the SOI is not null if so unsetsoý
    //Check if the new format can be SOI so setsoý
    void SwitchFormat(IMFDFormat newFormat)
    {
        currentFormat?.OnFormatExit();
        currentFormat = newFormat;
        if (CheckSOI(currentFormat))
            SOIFormat = (ISensorOfInterest)newFormat;
        currentFormat.OnFormatEnter();
    }

    void GetFormats()
    {
        formats = GetComponentsInChildren<IMFDFormat>();
    }

    void GetOSBs()
    {
        OSBArray = GetComponentsInChildren<MFDOSB>();

        var temp = new MFDOSB[OSBArray.Length];
        for (int i = 0; i < temp.Length; i++)
        {
            temp[OSBArray[i].id] = OSBArray[i];
        }
        OSBArray = temp;
    }

    public void SetSOI()
    {
        if (SOIFormat is null) return;
        SOIFormat.SetSOI();
        SOIRect.enabled = true;
        notSOIText.enabled = false;
    }

    public void UnSetSOI()
    {
        if (SOIFormat is null) return;
        SOIFormat.UnSetSOI();
        SOIRect.enabled = false;
        notSOIText.enabled = true;
    }
    /// <summary>
    /// Checks if the given format can be SOI
    /// </summary>
    /// <returns></returns>
    bool CheckSOI(IMFDFormat format)
    {
        if (format is ISensorOfInterest) return true;
        else return false;
    }

    void Boot()
    {
        if (active) return; //Is the device powered up and booted
        if (!consumer.IsPoweredE) return; //Is the device powered up
            

        if (bootStartedAt == 0) //The value to check the boot time. this has to be ref
            bootStartedAt = Time.time;
        if (bootStartedAt + timeToBoot <= Time.time) //Time to boot is how much it will take to boot.
        {
            active = true; //This has to be ref
            mainMask.SetActive(true); //Next things are going to be passed with an void action.
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
            if (CheckSOI(currentFormat))
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
            {
                currentFormat.OnFormatExit();
                GetOSBUpdates();
                active = false;
                bootStartedAt = 0;
                mainMask.SetActive(false);
            }
            return;
        }

        Boot();
        GetOSBUpdates();
        currentFormat?.OnFormatStay();
    }

    
}
