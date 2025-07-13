using System.Collections.Generic;
using System;
using UnityEngine;
using TMPro;

class TGPMFDFormat : MonoBehaviour, IMFDFormat, ISensorOfInterest
{
    [SerializeField] TargettingPod TGP;
    [SerializeField] RectTransform podExport;
    [SerializeField] RectTransform pointTrackRect;
    [SerializeField] RectTransform trackingCursor;
    [SerializeField] RectTransform INRTrackinCursor;
    [SerializeField] RectTransform narrowRect;
    [SerializeField] RectTransform sittationalAwarenessCue;

    [SerializeField] TextMeshProUGUI meterstickLength;
    [SerializeField] TextMeshProUGUI timeToGoImpact;
    [SerializeField] TextMeshProUGUI zoomLevel;
    [SerializeField] TextMeshProUGUI positionText;
    [SerializeField] TextMeshProUGUI trackingMode;
    [SerializeField] TextMeshProUGUI LaserStatus;
    [SerializeField] TextMeshProUGUI radarAlt;
    [SerializeField] TextMeshProUGUI laserCode;


    public Dictionary<string, Action> osbCommands = new();

    public Stack<(int index, string label)> OSBUpdates => osbToUpdate;
    Stack<(int index, string label)> osbToUpdate = new();

    public bool HandleArg(string arg)
    {
        if (!osbCommands.TryGetValue(arg, out Action key)) return false;

        osbCommands[arg].Invoke();
        return true;
    }

    public void OnFormatEnter()
    {
        gameObject.SetActive(true);
        osbCommands.Clear();

        osbCommands["WIDE"] = () =>
        {
            TGP.narrowZoom = !TGP.narrowZoom;
        };

        osbCommands["NARO"] = () =>
        {
            TGP.narrowZoom = !TGP.narrowZoom;
        };

        osbCommands["OVRD"] = () =>
        {
            podExport.gameObject.SetActive(!podExport.gameObject.activeInHierarchy);
        };

        //OSBs
        osbToUpdate.Push((0, "A-G"));
        osbToUpdate.Push((1, "MAN"));
        
        osbToUpdate.Push((3, "OVRD"));
        osbToUpdate.Push((4, "CNTL"));
        osbToUpdate.Push((5, "TV"));
        osbToUpdate.Push((6, "S\nP"));
        osbToUpdate.Push((8, "C\nZ"));
        osbToUpdate.Push((9, "T\nG\nT"));
        osbToUpdate.Push((14, "SWAP"));
        osbToUpdate.Push((10, "DCTL"));

        TGP.enableRendering = true;
    }

    public void OnFormatExit()
    {
        osbToUpdate.Push((0, ""));
        osbToUpdate.Push((1, ""));
        osbToUpdate.Push((2, ""));
        osbToUpdate.Push((3, ""));
        osbToUpdate.Push((4, ""));
        osbToUpdate.Push((5, ""));
        osbToUpdate.Push((6, ""));
        osbToUpdate.Push((7, ""));
        osbToUpdate.Push((8, ""));
        osbToUpdate.Push((9, ""));
        osbToUpdate.Push((10, ""));
        osbToUpdate.Push((11, ""));
        osbToUpdate.Push((12, ""));
        osbToUpdate.Push((13, ""));
        osbToUpdate.Push((14, ""));
        osbToUpdate.Push((15, ""));
        osbToUpdate.Push((16, ""));
        osbToUpdate.Push((17, ""));
        osbToUpdate.Push((18, ""));
        osbToUpdate.Push((19, ""));
        TGP.enableRendering = false;
        gameObject.SetActive(false);
    }

    public void OnFormatStay()
    {
        //This will get values from TGP.
        RotatePodExport();
        //Cue position
        //Wide narrow box visibility
        //Track type and according elements
        //Altitude
        //Time to go
        //Master Arm Status
        //Reticle size in meters
    }

    void RotatePodExport()
    {
        float angle = TGP.CalculateExportRotation();
        podExport.localEulerAngles = new Vector3(0, 0, angle);
    }

    void SetTrackingElements()
    {
        //Rects and tracking text
    }

    void SetZoomingElements()
    {
        osbToUpdate.Push((3, "WIDE"));
    }

    void SetSACPosition()
    {

    }

    void SetMeterstickLength()
    {

    }

    void SetTimeToGoImpact()
    {

    }

    void SetPosition()
    {

    }

    void SetLaserElements()
    {

    }

    void SetRadarAlt()
    {

    }

    public void SetSOI()
    {
        TGP.SetSOI();
        
    }

    public void UnSetSOI()
    {
        TGP.UnSetSOI();
    }
}