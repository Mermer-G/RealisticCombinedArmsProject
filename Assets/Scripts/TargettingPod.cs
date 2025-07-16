using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;
using UnityEngine.Rendering.Universal;
using ColorAdjustments = UnityEngine.Rendering.HighDefinition.ColorAdjustments;
using ColorCurves = UnityEngine.Rendering.HighDefinition.ColorCurves;
using Random = UnityEngine.Random;

public class TargettingPod : MonoBehaviour, ISensorOfInterest
{
    public Camera podCamera;
    [SerializeField] Rotor azimuth;
    [SerializeField] Hinge elevation;
    [SerializeField] RenderTexture RT;
    [SerializeField] LayerMask mask;
    [SerializeField] int renderingFPS;
    [SerializeField] float timeToBoot;
    [SerializeField] VolumeProfile volumeProfile;
    [SerializeField] ColorAdjustments colorAdjustments;
    [SerializeField] ColorCurves colorCurves;

    EnergyConsumerComponent consumer;
    Transform azimuthHead;
    Transform elevationHead;

    public float azimuthSpeed; // Yatay eksen dönüþ hýzý
    public float elevationSpeed; // Dikey eksen dönüþ hýzý
    
    [Header("Format values")]
    public bool enableRendering;
    public bool narrowZoom;

    //System values
    bool active;
    float bootStartedAt;
    bool SOI;

    public float exposure;
    public float contrast;

    //Dynamic vectors
    Vector3 manualControlVector = Vector3.zero;
    Vector3 trackingPoint = Vector3.zero;
    public Vector3 target;
    public Vector2 targetAngles;

    //Still vectors
    Vector2 CursorZero = new Vector2(0, -90);
    Vector2 STBY = new Vector2(0, 90);

    //Enums
    public TrackType trackingType = TrackType.Locked;
    public MasterMode masterMode = MasterMode.STBY;
    public CameraMode cameraMode = CameraMode.TV;
    public enum TrackType
    {
        Locked, // Locked gimbals and not moving.
        FreeLook, //Not traking just keeping the same direction.
        SlaveOnly, // SPI
        INR, // INR POINT track
        Area, // AREA track
        Point // POINT track
    }
    
    public enum MasterMode
    {
        STBY,
        AG,
        AA
    }

    public enum CameraMode
    {
        TV,
        WHOT,
        BHOT
    }

    [System.Serializable]
    public class CameraModeValues
    {
        public float contrast;
        public float exposure;
    }

    [SerializeField] CameraModeValues TV = new() { contrast = 100 , exposure = 10};
    [SerializeField] CameraModeValues WHOT = new() { contrast = 100, exposure = 10 };
    [SerializeField] CameraModeValues BHOT = new() { contrast = 100, exposure = 10 };

    // Start is called before the first frame update
    void Start()
    {
        consumer = GetComponent<EnergyConsumerComponent>();
        azimuthHead = azimuth.transform.GetChild(0);
        elevationHead = elevation.transform.GetChild(0);

        volumeProfile.TryGet(out colorAdjustments);
        volumeProfile.TryGet(out colorCurves);
    }

    // Update is called once per frame
    void Update()
    {
        //if (InputManager.instance.GetInput("TMSDown").ToBool() && SOI) ControlByState("SearchMode");

        if (enableRendering && !consumer.IsPoweredE)
        {
            consumer.ChangePowerStatusE(true);
        }
        if (!enableRendering)
        {
            consumer.ChangePowerStatusE(false);
        }

        if (InputManager.instance.GetInput("TMSLeft").ToBool())
        {
            CycleCameraMode();
        }
        
        
        
        Zoom();
        Render();
        Boot();
        input = GetInput();
    }

    void SaveVisionModeValues()
    {
        switch (cameraMode)
        {
            case CameraMode.TV:
                TV.contrast = contrast;
                TV.exposure = exposure;
                break;

            case CameraMode.WHOT:
                WHOT.contrast = contrast;
                WHOT.exposure = exposure;
                break;

            case CameraMode.BHOT:
                BHOT.contrast = contrast;
                BHOT.exposure = exposure;
                break;
        }
    }

    void LoadVisionModeValues()
    {
        switch (cameraMode)
        {
            case CameraMode.TV:
                contrast = TV.contrast;
                exposure = TV.exposure;
                break;

            case CameraMode.WHOT:
                contrast = WHOT.contrast;
                exposure = WHOT.exposure;
                break;

            case CameraMode.BHOT:
                contrast = BHOT.contrast;
                exposure = BHOT.exposure;
                break;
        }
    }

    void SetValuesToVolume()
    {
        colorAdjustments.postExposure.value = exposure;
        colorAdjustments.contrast.value = contrast;
    }

    public void CycleCameraMode()
    {
        switch (cameraMode)
        {
            case CameraMode.TV:
                cameraMode = CameraMode.WHOT;
                colorAdjustments.postExposure.overrideState = true;
                colorCurves.active = false;
                LoadVisionModeValues();
                SetValuesToVolume();
                break;

            case CameraMode.WHOT:
                cameraMode = CameraMode.BHOT;
                colorAdjustments.postExposure.overrideState = true;
                colorCurves.active = true;
                LoadVisionModeValues();
                SetValuesToVolume();
                break;

            case CameraMode.BHOT:
                cameraMode = CameraMode.TV;
                colorAdjustments.postExposure.overrideState = false;
                colorCurves.active = false;
                LoadVisionModeValues();
                SetValuesToVolume();
                break;
        }
    }

    public void AdjustColorAdjustmentValue(float ct = 0, float exp = 0)
    {
        contrast += ct;
        exposure += exp;
        SaveVisionModeValues();
        SetValuesToVolume();
    }

    public void ClearOutRenderTexture(RenderTexture renderTexture)
    {
        RenderTexture rt = RenderTexture.active;
        RenderTexture.active = renderTexture;
        GL.Clear(true, true, Color.black);
        RenderTexture.active = rt;
    }

    
    float lastRendered = 0;
    void Render()
    {
        float interval = 1f / renderingFPS;

        if (Time.time - lastRendered < interval) return;

        lastRendered = Time.time;

        if (enableRendering && consumer.IsPoweredE)
        {
            podCamera.Render();
            //Debug.Log("Rendering!");
        }
        else
        {
            ClearOutRenderTexture(RT);
        }
    }

    private void FixedUpdate()
    {
        ControlByState();
    }
    
    /// <summary>
    /// Stabilizes the target to the given point prevents jittering.
    /// </summary>
    /// <param name="pos"></param>
    void Stabilize(Vector3 tgt)
    {
        var dir = tgt - elevationHead.position;
        podCamera.transform.rotation = Quaternion.LookRotation(dir, elevationHead.up);   
    }

    private void OnEnable()
    {
        FloatingOrigin.originShifted += OriginShifted;
    }

    private void OnDisable()
    {
        FloatingOrigin.originShifted -= OriginShifted;
    }

    bool shifted;
    void OriginShifted(Vector3 shiftingAmount)
    {
        shifted = true;
        trackingPoint -= shiftingAmount;
        target -= shiftingAmount;
        INRPoint -= shiftingAmount;
    }


    [SerializeField] float proportionalGain;
    [SerializeField] float integralGain;
    [SerializeField] float derivativeGain;

    float integrationStored = 0;
    [SerializeField] float integralSaturation;
    float lastAzimuthError;
    float lastElevationError;
    float errorRateOfChange;
    enum PIDCalculationType
    {
        azimuth,
        elevation
    }
    float PID(float error, PIDCalculationType type)
    {
        float distance = Vector3.Distance(target, elevationHead.position);
        float distanceFactor = Math.Clamp( 1000 / distance, 0.01f, 1f);

        //calculate P
        float P = error * proportionalGain * distanceFactor;

        //Calculate D
        lastAzimuthError = CalculateAzimuthAngle(target);
        lastElevationError = CalculateElevationAngle(target);
        
        switch (type)
        {
            case PIDCalculationType.azimuth:
                errorRateOfChange = (error - lastAzimuthError);
                break;
            case PIDCalculationType.elevation:
                errorRateOfChange = (error - lastElevationError);
                break;
        }
        float D = errorRateOfChange * derivativeGain;

        //Calculate I
        integrationStored = Math.Clamp(integrationStored + error, -integralSaturation, integralSaturation);
        float I = integralGain * integrationStored;


        return P + I + D;
    }


    float[] wideZooms = new float[9]
    {
    1.000f,
    1.222f,
    1.494f,
    1.826f,
    2.232f,
    2.728f,
    3.333f,
    4.073f,
    5.000f
    };

    float[] narrowZooms = new float[9]
    {
    5.000f,
    6.111f,
    7.471f,
    9.131f,
    11.160f,
    13.640f,
    16.665f,
    20.365f,
    25.000f
    };
    
    public int currentZoom = 0;
    bool previousZoomMode;
    void Zoom()
    {
        if (!SOI) return;

        if (InputManager.instance.GetInput("FOV").ToBool())
            narrowZoom = !narrowZoom;

        float zoomInput = InputManager.instance.GetInput("MANRNG");

        if (zoomInput > 0)
            currentZoom += 1;
        else if (zoomInput < 0)
            currentZoom -= 1;
        currentZoom = Math.Clamp(currentZoom, 0, 8);

        float targetFocalLength = 60f * (narrowZoom ? narrowZooms[currentZoom] : wideZooms[currentZoom]);

        if (narrowZoom != previousZoomMode)
        {
            // Zoom modu deðiþtiyse direkt atla
            podCamera.focalLength = targetFocalLength;
        }
        else
        {
            // Ayný modda kalýndýysa yavaþça git
            float speed = 3f * podCamera.focalLength;
            podCamera.focalLength = Mathf.MoveTowards(podCamera.focalLength, targetFocalLength, speed * Time.deltaTime);
        }

        previousZoomMode = narrowZoom;
    }

    Vector3 horizontalAxis;
    Vector3 verticalAxis;
    //This method slaves pod to a DIRECTION so it is not a targeting method. Might be unneccesary.
    Vector3 AdvencedControl(Vector2 input)
    {
        if (!SOI) return manualControlVector + elevationHead.position;

        float horizontal = input.x;
        horizontal *= 20 / podCamera.focalLength;

        float vertical = input.y;
        vertical *= 20 / podCamera.focalLength;

        if (manualControlVector == Vector3.zero)
        {
            manualControlVector = podCamera.transform.forward;
        }
        else if (horizontal != 0 || vertical != 0)
        {
            horizontalAxis = Vector3.up;
            verticalAxis = Vector3.Cross(manualControlVector, Vector3.up);

            // Quaternion kullanarak eksen etrafýnda döndürme iþlemi oluþtur
            Quaternion rotation1 = Quaternion.AngleAxis(horizontal, horizontalAxis);
            Quaternion rotation2 = Quaternion.AngleAxis(vertical, verticalAxis);


            // Vektörü döndürme
            manualControlVector = rotation1 * rotation2 * manualControlVector;
        }
        //Debug.DrawLine(elevationHead.position, elevationHead.position + horizontalAxis * 10, Color.blue, 0.2f);
        //Debug.DrawLine(elevationHead.position, elevationHead.position + verticalAxis * 10, Color.red, 0.2f);
        //Debug.DrawLine(elevationHead.position, elevationHead.position + manualControlVector * 10, Color.magenta, 0.2f);
        return manualControlVector + elevationHead.position;
    }

    Vector2 GetInput() 
    {
        Vector2 input = new Vector2(InputManager.instance.GetInput("RDRHorizontal"), InputManager.instance.GetInput("RDRVertical"));
        return input;
    }

    //Will be used with INR and Area track
    Vector3 SearchModeControl(Vector2 input)
    {
        if (!SOI) return trackingPoint;

        if (shifted)
        {
            shifted = false;
            return trackingPoint; // Bu frame'de sadece shifting uygulanýr
        }
        // Input handling
        float distance = Vector3.Distance(trackingPoint, podCamera.transform.position);
        float horizontal = input.x * distance / podCamera.focalLength;
        float vertical = input.y * distance / podCamera.focalLength;

        Vector3 forward = podCamera.transform.forward;

        // Gravity-referenced "horizontal" düzlemde yönler
        Vector3 up = Vector3.ProjectOnPlane(Vector3.up, forward).normalized;
        Vector3 right = Vector3.Cross(up, forward);

        // Hareket vektörünü oluþtur
        Vector3 movement = up * vertical + right * horizontal;

        trackingPoint += movement;
        return trackingPoint;
    }
    Vector3 INRPoint;
    [SerializeField] float INRErrorMultiplier;
    Vector3 INRTrack(Vector3 tgt)
    {
        if (INRPoint == Vector3.zero) INRPoint = tgt;

        Vector3 noise = new Vector3(
            Random.Range(-INRErrorMultiplier, INRErrorMultiplier),
            Random.Range(-INRErrorMultiplier, INRErrorMultiplier),
            Random.Range(-INRErrorMultiplier, INRErrorMultiplier)
        );

        INRPoint += noise;

        print("error: " + Vector3.Distance(INRPoint, tgt));
        return INRPoint;
    }

    float azimuthError;
    float elevationError;
    /// <summary>
    /// Main targeting method of pod.
    /// </summary>
    /// <param name="target"></param>
    void AimWithPID(Vector3 tgt)
    {
        // Azimuth'u döndür
        azimuthError = CalculateAzimuthAngle(target);

        var dangerAngle = CalculateDangerAngle(target);

        float azimuthTargetSpeed = Math.Clamp(PID(azimuthError, PIDCalculationType.azimuth), -10, 10);
        azimuth.speed = dangerAngle < 0.1f ? dangerAngle : azimuthTargetSpeed;

        // Elevation'ý döndür
        elevationError = CalculateElevationAngle(target);
        float elevationTargetSpeed = Math.Clamp(PID(elevationError, PIDCalculationType.elevation), -10, 10);
        elevation.speed = elevationTargetSpeed;
        target = tgt;
    }
    
    void RotateTo(Vector2 angles)
    {
        if(Mathf.Abs(azimuth.rotation - angles.x) < 0.1f)
            azimuth.speed = 0;
        else
        {
            var azimuthAngleError = angles.x - azimuth.rotation;
            azimuth.speed = Math.Clamp(azimuthAngleError, -.5f, .5f);
        }


        if (Mathf.Abs(elevation.rotation - angles.y) < 0.1f)
            elevation.speed = 0;
        else
        {
            var elevationAngleError = angles.y - elevation.rotation;
            elevation.speed = Math.Clamp(elevationAngleError, -.5f, .5f);
        }
    }

    /// <summary>
    /// Calculates how close the target direction to the azimuth axis.
    /// </summary>
    /// <returns></returns>
    float CalculateDangerAngle(Vector3 target)
    {
        var azimuthAxis = azimuthHead.up;
        var dir = target - azimuthHead.position;
        return Vector3.Angle(dir, azimuthAxis);
    }

    public float SignedAngleBetweenVectors(Vector3 vector1, Vector3 vector2, Vector3 axis)
    {
        // Vektörleri normalize et
        Vector3 v1Normalized = vector1.normalized;
        Vector3 v2Normalized = vector2.normalized;

        // Dot product'ý hesapla
        float dotProduct = Vector3.Dot(v1Normalized, v2Normalized);

        // Dot product'ý -1 ile 1 arasýnda tutarak güvenli hale getir
        dotProduct = Mathf.Clamp((float)dotProduct, -1.0f, 1.0f);

        // Arccos ile açýyý bul (radyan cinsinden)
        float angleInRadians = Mathf.Acos(dotProduct);

        // Çapraz çarpýmý hesapla (cross product)
        Vector3 crossProduct = Vector3.Cross(v1Normalized, v2Normalized);

        // Eksenle cross product'ýn ayný yönde olup olmadýðýný kontrol et
        float sign = Vector3.Dot(crossProduct, axis) < 0 ? -1.0f : 1.0f;

        // Açýyý dereceye çevir
        float angleInDegrees = angleInRadians * (180.0f / Mathf.PI);

        // Ýþaretli açýyý döndür (derece cinsinden)
        return angleInDegrees * sign;
    }

    float CalculateAzimuthAngle(Vector3 target)
    {
        // Ýki vektör arasýndaki açýyý hesapla
        Vector3 targetDirection = (target - azimuthHead.position).normalized;
        Vector3 currentDirection = azimuthHead.forward.normalized;

        Vector3 rotorAxis = azimuthHead.up.normalized;

        // Hedef yönü rotor eksenine göre düzleme yansýt
        Vector3 projectedTargetDirection = Vector3.ProjectOnPlane(targetDirection, rotorAxis).normalized;
        Vector3 projectedCurrentDirection = Vector3.ProjectOnPlane(currentDirection, rotorAxis).normalized;

        // Açýyý hesapla (Yönlü açý)
        float projectedAngleError = Vector3.SignedAngle(projectedCurrentDirection, projectedTargetDirection, rotorAxis);

        return projectedAngleError;
    }

    float CalculateElevationAngle(Vector3 target)
    {
        // Hedef yönü ve mevcut yönü hesapla
        Vector3 targetDirection = (target - elevationHead.position).normalized;
        Vector3 currentDirection = elevationHead.forward.normalized;

        // Rotorun dönme ekseni (Elevation için: sað-sol ekseni genellikle 'right' olur)
        Vector3 rotorAxis = elevationHead.right.normalized;

        // Her iki yönü de bu eksene dik düzleme yansýt
        Vector3 projectedTargetDirection = Vector3.ProjectOnPlane(targetDirection, rotorAxis).normalized;
        Vector3 projectedCurrentDirection = Vector3.ProjectOnPlane(currentDirection, rotorAxis).normalized;

        // Ýmzalý açýyý hesapla (eksi gerekmez burada)
        float projectedAngleError = Vector3.SignedAngle(projectedCurrentDirection, projectedTargetDirection, rotorAxis);

        //Debug
        //Debug.DrawRay(elevationHead.position, targetDirection, UnityEngine.Color.blue);
        //Debug.DrawRay(elevationHead.position, projectedTargetDirection, UnityEngine.Color.red);
        //Debug.Log("Elevation Projected Angle: " + projectedAngleError);

        return projectedAngleError;
    }

    public float CalculateExportRotation()
    {
        var up = Vector3.up;
        var cameraUp = podCamera.transform.up;
        var axis = podCamera.transform.forward;

        var refPoint = podCamera.transform.position + podCamera.transform.forward * 2;
        var projectedCamUp = Vector3.ProjectOnPlane(cameraUp, axis).normalized;
        var projectedWorldUp = Vector3.ProjectOnPlane(up, axis).normalized;
        //Debug lines
        var origin = podCamera.transform.position + axis * 2f;

        Debug.DrawLine(origin, origin + axis * 0.5f, Color.yellow, 0.1f); // Forward (axis)
        Debug.DrawLine(origin, origin + projectedCamUp * 0.5f, Color.blue, 0.1f); // Camera up
        Debug.DrawLine(origin, origin + projectedWorldUp * 0.5f, Color.green, 0.1f); // World up

        //var project = Vector3.Project(cameraUp, axis);
        return -Vector3.SignedAngle(projectedCamUp, projectedWorldUp, axis);
    }

    public Vector2 CalculateSAC()
    {
        Vector2 position;
        var front = podCamera.transform.forward;

        var projectedFront = Vector3.ProjectOnPlane(front, transform.right);

        var angle = -Vector3.SignedAngle(projectedFront, transform.up, transform.right);
        angle = ProjectUtilities.Map(angle, 0, 135, -1, 1);
        position.y = -Mathf.Clamp(angle, -1, 1);

        

        angle = Vector3.Dot(front, transform.right);
        position.x = Mathf.Clamp(angle, -1, 1);

        Debug.DrawLine(transform.position, transform.position + -transform.forward, Color.green, 0.001f);
        Debug.DrawLine(transform.position, transform.position + transform.up, Color.blue, 0.001f);
        Debug.DrawLine(transform.position, transform.position + projectedFront, Color.red, 0.001f);

        return position;
    }

    public float ExportFocalLentgh()
    {
        return podCamera.focalLength;
    }

    public void AirToGroundMode()
    {
        trackingType = TrackType.Locked;
        targetAngles = CursorZero;
        enableRendering = true;
        masterMode = MasterMode.AG;
    }

    public void AirToAirMode()
    {

    }

    public void StandByMode()
    {
        trackingType = TrackType.Locked;
        targetAngles = STBY;
        enableRendering = false;
        masterMode = MasterMode.STBY;
        cameraMode = CameraMode.TV;
        colorAdjustments.postExposure.overrideState = false;
        colorCurves.active = false;
        LoadVisionModeValues();
        SetValuesToVolume();
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
            //Get to STBY mode
            //Mode is not something it knows. Just give the according values.
            
            StandByMode();
        }
    }

    Vector2 input;
    bool inputReleasedFlag;
    void ControlByState()
    {
        switch (trackingType)
        {
            case TrackType.Locked:
                //Will rotate to given rotation
                //Just set a fixed rotation vector. The method will always try to get to that value. When it gets it will just stop.
                RotateTo(targetAngles);
                if(manualControlVector != Vector3.zero) manualControlVector = Vector3.zero;
                //Tracking Type Changes
                if (input != Vector2.zero)
                {
                    //Change to free look.
                    trackingType = TrackType.FreeLook;
                }

                //Will be used for CZ and SnowPlow for now.
                break;

            case TrackType.FreeLook:
                if (manualControlVector == Vector3.zero) manualControlVector = podCamera.transform.forward;
                if (trackingPoint != Vector3.zero) trackingPoint = Vector3.zero;

                AimWithPID(AdvencedControl(input));

                //Tracking Type Changes
                if (InputManager.instance.GetInput("TMSRight").ToBool())
                {
                    trackingType = TrackType.INR;
                    inputReleasedFlag = true;
                    INRPoint = Vector3.zero;
                    SetTrackingPoint();
                }

                break;

            case TrackType.SlaveOnly:
                //This is basicaly manual control with no stabilization we will get the SPI from MMC and rotate to its direction with no stab.

                //Requires atleast the use of HMCS SPI setting. In order to work. We need another SOI to use this.
                break;

            case TrackType.INR:
                //Target point track with no stab. Drifts over time but never loses approximate position. (called approximate because it drifts)
                


                //There's input.
                if (input != Vector2.zero && SOI)
                {
                    AimWithPID(SearchModeControl(input));
                    inputReleasedFlag = true;
                    INRPoint = Vector3.zero;
                }
                //There's no input.
                else
                {
                    AimWithPID(INRTrack(trackingPoint));
                    SetTrackingPoint();
                    //If ^this raycast fails:
                    if (trackingPoint == Vector3.zero)
                    {
                        trackingType = TrackType.FreeLook;
                        return;
                    }
                }

                //Stabilize(INRPoint);
                //Tracking Type Changes
                if (InputManager.instance.GetInput("TMSRight").ToBool())
                    trackingType = TrackType.Area;

                if (InputManager.instance.GetInput("TMSDown").ToBool())
                    trackingType = TrackType.FreeLook;

                //Works with target
                break;

            case TrackType.Area:
                //constantly following with LOS check. 
                if (manualControlVector != Vector3.zero) manualControlVector = Vector3.zero;
                

                //There's input.
                if (input != Vector2.zero && SOI)
                {
                    AimWithPID(SearchModeControl(input));
                    LOSLastChecked = Time.time;
                }
                //There's no input.
                else
                {
                    LineOfSightControl();
                    AimWithPID(trackingPoint);
                }

                //Tracking Type Changes
                if (InputManager.instance.GetInput("TMSRight").ToBool())
                {
                    trackingType = TrackType.INR;
                    inputReleasedFlag = true;
                    INRPoint = Vector3.zero;
                    SetTrackingPoint();
                }

                if (InputManager.instance.GetInput("TMSDown").ToBool())
                    trackingType = TrackType.FreeLook;

                if (InputManager.instance.GetInput("TMSUp").ToBool())
                    trackingType = TrackType.Point;

                Stabilize(trackingPoint);

                //Works with target
                break;

            case TrackType.Point:
                //Constantly following a target with pointTargetable taging script.
                


                //Tracking Type Changes
                if (InputManager.instance.GetInput("TMSRight").ToBool())
                    trackingType = TrackType.INR;

                if (InputManager.instance.GetInput("TMSDown").ToBool())
                    trackingType = TrackType.FreeLook;

                //Works with target
                break;
        }
    }

    private void SetTrackingPoint()
    {
        if (inputReleasedFlag)
        {
            if (!Physics.Raycast(podCamera.transform.position, podCamera.transform.forward, out RaycastHit hitPoint, 60000, mask))
            {
                trackingType = TrackType.FreeLook;
                return;
            }
            trackingPoint = hitPoint.point;
            inputReleasedFlag = false;
        }
    }

    float LOSLastChecked = 0;
    [SerializeField] float LOSCheckInterval;
    void LineOfSightControl()
    {
        if (Time.time - LOSLastChecked > LOSCheckInterval)
        {
            Vector3 direction = (trackingPoint - podCamera.transform.position).normalized;
            //There's an obstacle
            if (Physics.Raycast(podCamera.transform.position, direction, out RaycastHit hitPoint, 60000, mask))
            {
                trackingPoint = hitPoint.point;
                LOSLastChecked = Time.time;
                return;
            }
            //float jitterThreshold = 20f; // 10 cm gibi bir eþik koyabilirsin
            LOSLastChecked = Time.time;
        }
    }

    public void SetSOI()
    {
        SOI = true;
    }

    public void UnSetSOI()
    {
        SOI = false;
    }
}


public class ForceSave : MonoBehaviour
{
    [MenuItem("Tools/Force Save PID Data")]
    public static void Save()
    {
        var pid = AssetDatabase.LoadAssetAtPath<FLCSPIDData>("Assets/Scripts/Aerodynamics/FLCSPIDDatas/F-16.asset");
        EditorUtility.SetDirty(pid);
        AssetDatabase.SaveAssets();
        Debug.Log("Manually saved PIDData.");
    }
}
