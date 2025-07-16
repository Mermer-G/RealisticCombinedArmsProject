using System.Collections.Generic;
using System;
using UnityEngine;
using TMPro;
using Unity.VectorGraphics;
using UnityEngine.Rendering;

class TGPMFDFormat : MonoBehaviour, IMFDFormat, ISensorOfInterest
{
    [SerializeField] TargettingPod TGP;
    [SerializeField] RectTransform podExport;
    [SerializeField] SVGImage pointTrackRect;
    [SerializeField] SVGImage trackingCursor;
    [SerializeField] SVGImage INRTrackinCursor;
    [SerializeField] SVGImage narrowRect;
    [SerializeField] RectTransform sitationalAwarenessCue;

    [SerializeField] GameObject elementsParent;

    [SerializeField] TextMeshProUGUI meterstickLength;
    [SerializeField] TextMeshProUGUI timeToGoImpact;
    [SerializeField] TextMeshProUGUI zoomLevel;
    [SerializeField] TextMeshProUGUI positionText;
    [SerializeField] TextMeshProUGUI trackingMode;
    [SerializeField] TextMeshProUGUI LaserStatus;
    [SerializeField] TextMeshProUGUI radarAlt;
    [SerializeField] TextMeshProUGUI laserCode;

    [SerializeField] float exposureChange;
    [SerializeField] float contrastChange;

    public Dictionary<string, Action> osbCommands = new();

    public Stack<(int index, string label)> OSBUpdates => osbToUpdate;

    public string Context { get; set; }

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
        CameraMode = TargettingPod.CameraMode.TV;

        Context = "STBY";

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
            TGP.targetAngles = new Vector2(0, 90);
            TGP.trackingType = TargettingPod.TrackType.Locked;
            if (podExport.gameObject.activeInHierarchy)
            {
                elementsParent.SetActive(false);
            }
            else
            {
                elementsParent.SetActive(true);
            }
        };
        

        osbCommands["TV"] = () =>
        {
            TGP.CycleCameraMode();
        };

        osbCommands["WHOT"] = () =>
        {
            TGP.CycleCameraMode();
        };

        osbCommands["BHOT"] = () =>
        {
            TGP.CycleCameraMode();
        };


        osbCommands["C\nZ"] = () =>
        {
            TGP.targetAngles = new Vector2(0, -90);
            TGP.trackingType = TargettingPod.TrackType.Locked;
        };

        osbCommands["S\nP"] = () =>
        {
            TGP.targetAngles = new Vector2(0, -45);
            TGP.trackingType = TargettingPod.TrackType.Locked;
        };

        osbCommands["STBY"] = () =>
        {
            //Return back to STBY
            if (Context == "STBY")
            {
                SelectionContextMenu();
            }
            osbToUpdate.Push((6, "A-G"));
            osbToUpdate.Push((20, "A-A"));
            //Get to A-G mode
            if (Context == "A-G")
            {
                STBYContextMenu();
            }
        };

        osbCommands["A-G"] = () =>
        {
            //Get to A-G mode
            if (Context != "A-G")
            {
                AGContextMenu();
            }
            //Get to STBY mode for mode selection
            else
            {
                SelectionContextMenu();
            }

        };

        //OSBs
        osbToUpdate.Push((1, "STBY"));
        STBYContextMenu();
    }

    public void OnFormatExit()
    {
        for (int i = 1; i < 20; i++)
        {
            osbToUpdate.Push((i, ""));
        }
        TGP.enableRendering = false;
        gameObject.SetActive(false);
    }

    public void OnFormatStay()
    {
        //This will get values from TGP.
        RotatePodExport();
        SetSACPosition();
        SetZoomingElements();
        SetTrackingElements();
        SetTimeToGoImpact();
        SetMeterstickLength();
        SetPosition();
        if (Context != "STBY")
        {
            SetCameraModeName();
        }

        //Altitude
        //Master Arm Status
    }

    TargettingPod.CameraMode CameraMode;
    void SetCameraModeName()
    {
        if (CameraMode == TGP.cameraMode) return;

        CameraMode = TGP.cameraMode;

        switch (CameraMode)
        {
            case TargettingPod.CameraMode.TV:
                osbToUpdate.Push((6, "TV"));
                break;

            case TargettingPod.CameraMode.WHOT:
                osbToUpdate.Push((6, "WHOT"));
                break;

            case TargettingPod.CameraMode.BHOT:
                osbToUpdate.Push((6, "BHOT"));
                break;
            
        }
    }

    void IncreaseContrast()
    {
        TGP.AdjustColorAdjustmentValue(contrastChange);
    }

    void DecreaseContrast()
    {
        TGP.AdjustColorAdjustmentValue(-contrastChange);
    }

    void IncreaseExposure()
    {
        TGP.AdjustColorAdjustmentValue(exposureChange);
    }

    void DecreaseExposure()
    {
        TGP.AdjustColorAdjustmentValue(-exposureChange);
    }

    private void OnEnable()
    {
        
    }

    private void OnDisable()
    {
        
    }

    void AGContextMenu()
    {
        osbToUpdate.Push((1, "A-G"));
        osbToUpdate.Push((3, "WIDE"));
        osbToUpdate.Push((20, "GRAY\nOFF"));
        osbToUpdate.Push((2, "MAN")); // A-G Missiles.
        osbToUpdate.Push((4, "OVRD"));
        osbToUpdate.Push((5, "CNTL"));
        osbToUpdate.Push((7, "S\nP"));
        osbToUpdate.Push((9, "C\nZ"));
        osbToUpdate.Push((10, "T\nG\nT"));
        osbToUpdate.Push((15, "SWAP"));
        osbToUpdate.Push((11, "DCTL"));

        elementsParent.SetActive(true);
        podExport.gameObject.SetActive(true);
        TGP.enableRendering = true;

        Context = "A-G";
    }
    //A-A STBY A-G
    //A-A A-G STBY
    //STBY A-A A-G
    void STBYContextMenu()
    {   
        osbToUpdate.Push((1, "STBY"));
        osbToUpdate.Push((6, "A-G"));
        osbToUpdate.Push((20, "A-A"));
        for (int i = 1; i < 20; i++)
        {
            if (i == 1 || i == 6 || i == 20) continue;
            osbToUpdate.Push((i, ""));
        }

        elementsParent.SetActive(false);
        podExport.gameObject.SetActive(false);

        

        Context = "STBY";
    }

    void SelectionContextMenu()
    {
        if (Context == "A-A")
        {
            osbToUpdate.Push((20, "STBY"));
            osbToUpdate.Push((6, "A-G"));

        }
        else if (Context == "A-G")
        {
            osbToUpdate.Push((20, "A-A"));
            osbToUpdate.Push((6, "STBY"));
        }
        else if (Context == "STBY")
        {
            osbToUpdate.Push((20, "A-A"));
            osbToUpdate.Push((6, "A-G"));
        }

        for (int i = 1; i < 20; i++)
        {
            if (i == 1 || i == 6 || i == 20) continue;
            osbToUpdate.Push((i, ""));
        }

        elementsParent.SetActive(false);

        Context = "Selection";
    }

    [SerializeField] float maxRotationSpeedPerSecond;
    void RotatePodExport()
    {
        float angle = TGP.CalculateExportRotation();
        var currentAngle = podExport.localEulerAngles.z;
        currentAngle = Mathf.MoveTowardsAngle(currentAngle, angle, maxRotationSpeedPerSecond * Time.deltaTime);
        podExport.localEulerAngles = new Vector3(0, 0, currentAngle);
    }

    void SetTrackingElements()
    {
        //Rects and tracking text
        
        switch (TGP.trackingType)
        {
            case TargettingPod.TrackType.Locked:
                INRTrackinCursor.enabled = true;
                trackingCursor.enabled = false;
                pointTrackRect.enabled = false;
                trackingMode.text = "";
                break;
            case TargettingPod.TrackType.FreeLook:
                INRTrackinCursor.enabled = true;
                trackingCursor.enabled = false;
                pointTrackRect.enabled = false;
                trackingMode.text = "";
                break;
            case TargettingPod.TrackType.SlaveOnly:
                INRTrackinCursor.enabled = true;
                trackingCursor.enabled = false;
                pointTrackRect.enabled = false;
                trackingMode.text = "";
                break;
            case TargettingPod.TrackType.INR:
                INRTrackinCursor.enabled = true;
                trackingCursor.enabled = false;
                pointTrackRect.enabled = false;
                trackingMode.text = "INR";
                break;
            case TargettingPod.TrackType.Area:
                INRTrackinCursor.enabled = false;
                trackingCursor.enabled = true;
                pointTrackRect.enabled = false;
                trackingMode.text = "AREA";
                break;
            case TargettingPod.TrackType.Point:
                INRTrackinCursor.enabled = false;
                trackingCursor.enabled = true;
                pointTrackRect.enabled = true;
                trackingMode.text = "POINT";
                break;
        }
    }

    bool narrow;
    void SetZoomingElements()
    {

        //Get current zoom for zoom level.
        zoomLevel.text = "Z" + TGP.currentZoom;

        //Get wide narrow, for rect.
        //Set the text of osb 3
        if (TGP.narrowZoom && !narrow)
        {
            osbToUpdate.Push((3, "NARO"));
            narrowRect.enabled = false;
            narrow = TGP.narrowZoom;
        }
        if (!TGP.narrowZoom && narrow)
        {
            osbToUpdate.Push((3, "WIDE"));
            narrowRect.enabled = true;
            narrow = TGP.narrowZoom;
        }

        //At narrow scale will set to 2 instantly
        if (TGP.narrowZoom)
        {
            podExport.localScale = new Vector3(1.5f, 1.5f, 1);
        }
        //At wide it will check fov
        else
        {
            var focal = TGP.ExportFocalLentgh();
            var scale = ProjectUtilities.Map(focal, 60 * 1.000f, 60 * 1.494f, 0, 1);
            scale = Mathf.Clamp01(scale);
            podExport.localScale = Vector3.Lerp(Vector3.one, new Vector3(1.5f, 1.5f, 1), scale);
        }
    }

    bool once;
    float sacLastflashed;
    void SetSACPosition()
    {
        var position = TGP.CalculateSAC();
        sitationalAwarenessCue.localPosition = position * 7;

        //print("Sac Position: " + position);

        if (position.y < -0.9f)
        {
            //print("Should be flashing.");
            if (sacLastflashed + 0.25f < Time.time)
            {
                sitationalAwarenessCue.gameObject.SetActive(!sitationalAwarenessCue.gameObject.activeInHierarchy);
                sacLastflashed = Time.time;
            }
        }
        else
            sitationalAwarenessCue.gameObject.SetActive(true);

        if (position.y <= -.99f)
        {
            podExport.gameObject.SetActive(false);
            once = true;
        }

        if (once)
        {
            podExport.gameObject.SetActive(true);
            once = false;
        }
    }

    void SetMeterstickLength()
    {
        //Use fov and percentage related to screen and cursor size and use raycast. 0.5 seconds.
        var angle = TGP.podCamera.fieldOfView / 2;
        var pC = TGP.podCamera.transform;

        if (TGP.trackingType == TargettingPod.TrackType.INR)
        {
            angle *= 6f / 16.5f;
        }
        else if (TGP.trackingType == TargettingPod.TrackType.Area || TGP.trackingType == TargettingPod.TrackType.Point)
        {
            angle *= 2f / 16.5f;
        }
        else
        {
            meterstickLength.text = "0M";
        }


        var cross = Vector3.Cross(pC.forward, Vector3.up);
        var upAxis = Vector3.Cross(cross, pC.forward);
        var quat = Quaternion.AngleAxis(angle, upAxis);

        var dir = quat * TGP.podCamera.transform.forward;

        var distance = Vector3.Distance(pC.position, TGP.target);
        dir = dir.normalized * distance;
        var pos = pC.position + dir;

        meterstickLength.text = (Vector3.Distance(TGP.target, pos)).ToString("0M");
        Debug.DrawLine(pC.position, pC.position + dir, Color.blue, 0.001f);
        Debug.DrawLine(pC.position, pC.position + pC.forward * distance, Color.blue, 0.001f);
    }

    Vector3 lastPosition;
    void SetTimeToGoImpact()
    {
        //If there is no impact
        var distance = Vector3.Distance(TGP.target, transform.position);
        var speed = (lastPosition - transform.position).magnitude * Time.deltaTime;
        lastPosition = transform.position;
        var time = distance / speed;
        timeToGoImpact.text = ((time / 60) % 1).ToString("00") + ":" + (time % 60).ToString("00");

        //IF there is impact

    }

    void SetPosition()
    {
        var target = TGP.target;
        var pRelatedToOrigin = target - StaticOrigin.instance.position;

        positionText.text = "X: " + pRelatedToOrigin.x.ToString("0.00") + " Z: " + pRelatedToOrigin.z.ToString("0.00") + "\nalt: " + pRelatedToOrigin.y.ToString("0.00");
    }

    void SetLaserElements()
    {
        //Get the laser code from MMC 
        //If pod triggered at AG mode (first detent/left mouse button) while laser arm on flash. 
        //Not flash when not triggering the laser.
    }

    void SetRadarAlt()
    {
        //Might be better to start the sensor script before this.
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