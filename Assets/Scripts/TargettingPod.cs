using JetBrains.Annotations;
using System;
using System.Collections.Generic;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using static UnityEditor.Searcher.SearcherWindow.Alignment;

public class TargettingPod : MonoBehaviour
{
    EnergyConsumerComponent consumer;
    [SerializeField] Camera podCamera;
    [SerializeField] Rotor azimuth;
    [SerializeField] Hinge elevation;
    [SerializeField] RenderTexture RT;

    Transform azimuthHead;
    Transform elevationHead;

    [SerializeField] LayerMask mask;
    public float azimuthSpeed; // Yatay eksen dönüþ hýzý
    public float elevationSpeed; // Dikey eksen dönüþ hýzý
    public float stabilizerSpeed;

    [SerializeField] GameObject cube;

    Vector3 target;

    [SerializeField] int renderingFPS;

    [SerializeField] bool gravityAlign;
    [SerializeField] bool control;
    Vector3 manualControlVector = Vector3.zero;

    Vector3 tempSearchPoint = Vector3.zero;

    List<Vector3> trackingPositions = new();
    int currentTrackingPosition;

    // Start is called before the first frame update
    void Start()
    {
        targetPrevPosition = target;
        consumer = GetComponent<EnergyConsumerComponent>();
        azimuthHead = azimuth.transform.GetChild(0);
        elevationHead = elevation.transform.GetChild(0);
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Keypad1)) podCamera.enabled = !podCamera.enabled;

        if (Input.GetKeyDown(KeyCode.Keypad2)) ControlByState("Control");

        if (InputManager.instance.GetInput("TMSDown").ToBool()) ControlByState("SearchMode");

        if (Input.GetKeyDown(KeyCode.Keypad4)) ControlByState("AddPos");

        if (Input.GetKeyDown(KeyCode.Keypad5)) ControlByState("DelPos");

        if (Input.GetKeyDown(KeyCode.Keypad6)) ControlByState("NextPos");

        //if (Input.GetKeyDown(KeyCode.Keypad7)) ControlByState("PrevPos");

        if (Input.GetKeyDown(KeyCode.Keypad9)) enableRendering = !enableRendering;

        if (enableRendering && !consumer.IsPoweredE)
        {
            consumer.ChangePowerStatusE(true);
        }
        if (!enableRendering)
        {
            consumer.ChangePowerStatusE(false);
        }


        Zoom();

        if (state == TGPState.Manual)
        {
            target = tempSearchPoint;
        }
        if (state == TGPState.PointTrack)
        {
            target = trackingPositions[currentTrackingPosition];
        }
        
        if (gravityAlign)
        {
            if (state == TGPState.Manual)
            {
                DoAbsoluteGravityAlign(Vector3.zero);
            }
            if (state == TGPState.Search)
            {
                DoAbsoluteGravityAlign(tempSearchPoint);
            }
            if (state == TGPState.PointTrack)
            {
                DoAbsoluteGravityAlign(trackingPositions[currentTrackingPosition]);
            }
        }
        Render();
    }

    public void ClearOutRenderTexture(RenderTexture renderTexture)
    {
        RenderTexture rt = RenderTexture.active;
        RenderTexture.active = renderTexture;
        GL.Clear(true, true, Color.black);
        RenderTexture.active = rt;
    }

    [SerializeField] bool enableRendering;
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
        
        switch (state)
        {
            case TGPState.Test:
                break;
            case TGPState.Manual:
                break;
            case TGPState.Search:
                cube.transform.position = tempSearchPoint;
                break;
            case TGPState.PointTrack:
                cube.transform.position = trackingPositions[currentTrackingPosition];
                break;
            default:
                break;
        }
    }
    
    void DoAbsoluteGravityAlign(Vector3 pos)
    {
        if (pos == Vector3.zero)
            podCamera.transform.rotation = Quaternion.LookRotation(elevationHead.forward, Vector3.up);
        
        else
        {
            var dir = pos - elevationHead.position;
            podCamera.transform.rotation = Quaternion.LookRotation(dir, Vector3.up);
        }
    }


    Vector3 targetSpeed;
    Vector3 targetPrevPosition;



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
        tempSearchPoint -= shiftingAmount;
        

        for (int i = 0; i < trackingPositions.Count; i++)
        {
            trackingPositions[i] -= shiftingAmount;
        }
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


    float currentZoom = 0;
    void Zoom()
    {
        if (Input.GetKeyDown(KeyCode.KeypadPlus)) currentZoom++;
        if (Input.GetKeyDown(KeyCode.KeypadMinus)) currentZoom--;


        float zoomInput = InputManager.instance.GetInput("MANRNG");

        if (zoomInput > 0)
            currentZoom *= 1.2f;
        else if (zoomInput < 0)
            currentZoom /= 1.2f;
        currentZoom = Math.Clamp(currentZoom, 1, 256);

        podCamera.focalLength = 60 * currentZoom;
    }
    Vector3 horizontalAxis;
    Vector3 verticalAxis;
    Vector3 AdvencedControl()
    {
        float horizontal = InputManager.instance.GetInput("RDRHorizontal");
        horizontal *= 20 / podCamera.focalLength;

        float vertical = InputManager.instance.GetInput("RDRVertical");
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

    Vector3 SearchModeControl()
    {
        if (shifted)
        {
            shifted = false;
            return tempSearchPoint; // Bu frame'de sadece shifting uygulanýr
        }
        print("A");
        // Input handling
        float distance = Vector3.Distance(tempSearchPoint, podCamera.transform.position);
        float horizontal = InputManager.instance.GetInput("RDRHorizontal") * distance / podCamera.focalLength;
        float vertical = InputManager.instance.GetInput("RDRVertical") * distance / podCamera.focalLength;

        Vector3 movement = podCamera.transform.up * vertical + podCamera.transform.right * horizontal;

        tempSearchPoint += movement;
        return tempSearchPoint;
    }

    float azimuthError;
    float elevationError;
    void AimWithPID(Vector3 target)
    {
        // Azimuth'u döndür
        azimuthError = CalculateAzimuthAngle(target);
        float azimuthTargetSpeed = Math.Clamp(PID(azimuthError, PIDCalculationType.azimuth), -10, 10);
        azimuth.speed = azimuthTargetSpeed;

        // Elevation'ý döndür
        elevationError = CalculateElevationAngle(target);
        float elevationTargetSpeed = Math.Clamp(PID(elevationError, PIDCalculationType.elevation), -10, 10);
        elevation.speed = elevationTargetSpeed;
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

        //// Debug çizgileri
        //Debug.DrawLine(azimuthHead.position, azimuthHead.position + currentDirection, Color.blue, 0.2f);
        //Debug.DrawLine(azimuthHead.position, azimuthHead.position + rotorAxis, Color.green, 0.2f);
        //Debug.DrawLine(azimuthHead.position, azimuthHead.position + projectedTargetDirection, Color.red, 0.2f);

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

    enum TGPState
    {
        Test,
        Manual,
        Search,
        PointTrack
    }

    bool SearchModeInputFlag;
    TGPState state = TGPState.Manual;
    void ControlByState(string argument = "")
    {
        if (argument != "")
        {
            if (argument == "SearchMode")
            {
                if (state != TGPState.Search)
                {
                    state = TGPState.Search;
                    manualControlVector = Vector3.zero;
                    Physics.Raycast(podCamera.transform.position, podCamera.transform.forward, out RaycastHit hitPoint, 15000, mask);
                    tempSearchPoint = hitPoint.point;
                    print("State has been set to Search Mode");
                }
                else
                {
                    state = TGPState.Manual;
                    print("State has been set to Manual");
                }
            }

            if (argument == "AddPos")
            {
                Physics.Raycast(podCamera.transform.position, podCamera.transform.forward, out RaycastHit hitPoint, 15000, mask);
                trackingPositions.Add(hitPoint.point);
                currentTrackingPosition = trackingPositions.Count - 1;

                state = TGPState.PointTrack;

                print("A new point added to the " + currentTrackingPosition + "th index and tracking.");
            }

            if (argument == "DelPos")
            {
                if (trackingPositions.Count > 1)
                {
                    if (currentTrackingPosition + 1 == trackingPositions.Count)
                    {
                        currentTrackingPosition--;
                        trackingPositions.RemoveAt(currentTrackingPosition + 1);
                        print("The point from the " + (currentTrackingPosition + 1) + "th index has been deleted. \n" +
                            "Current index is: " + currentTrackingPosition);
                    }
                    else if (currentTrackingPosition == 0)
                    {
                        currentTrackingPosition++;
                        trackingPositions.RemoveAt(currentTrackingPosition - 1);
                        print("The point from the " + (currentTrackingPosition - 1) + "th index has been deleted. \n" +
                            "Current index is: " + currentTrackingPosition);
                    }
                }
                else if (trackingPositions.Count == 1)
                {
                    trackingPositions.RemoveAt(currentTrackingPosition);
                    state = TGPState.Search;
                    print("No tracking point has been left in the list. Switching the state to Search Mode");
                }
            }

            if (argument == "NextPos")
            {
                if (state != TGPState.PointTrack)
                {
                    state = TGPState.PointTrack;
                    print("State set to Point Track");
                    return;
                }

                if (trackingPositions.Count > 1)
                {
                    if (currentTrackingPosition + 2 > trackingPositions.Count)
                    {
                        print("The zero;");
                        currentTrackingPosition = 0;
                    }
                    else
                    {
                        print("Increased;");
                        currentTrackingPosition++;
                    }
                    print("Tracking the " + currentTrackingPosition + "th point.");
                }
            }

            if (argument == "PrevPos")
            {
                if (state != TGPState.PointTrack)
                {
                    state = TGPState.PointTrack;
                    print("State set to Point Track");
                    return;
                }

                if (trackingPositions.Count > 1)
                {
                    if (currentTrackingPosition == 0)
                    {
                        currentTrackingPosition = trackingPositions.Count - 1;
                    }
                    else
                    {
                        currentTrackingPosition--;
                    }
                    print("Tracking the " + currentTrackingPosition + "th point.");
                }
            }

            if (argument == "Control")
            {
                control = !control;
                if (control) print("Control has been enabled.");

                else print("Control has been disabled.");
            }

            return;
        }


        switch (state)
        {
            case TGPState.Test:

                break;
            case TGPState.Manual:
                if (manualControlVector == Vector3.zero) manualControlVector = podCamera.transform.forward;
                if (tempSearchPoint != Vector3.zero) tempSearchPoint = Vector3.zero;

                if (control)
                {
                    AimWithPID(AdvencedControl());
                    //Debug.DrawLine(podCamera.transform.position, podCamera.transform.position + manualControlVector, Color.cyan, 0.1f);
                }

                break;
            case TGPState.Search:

                SetTempSearchPoint();

                //There's input.
                if (InputManager.instance.GetInput("RDRVertical") != 0 || InputManager.instance.GetInput("RDRHorizontal") != 0)
                {
                    AimWithPID(SearchModeControl());
                    LOSLastChecked = Time.time;
                }
                //There's no input.
                else
                {
                    LineOfSightControl();
                    AimWithPID(tempSearchPoint);
                }
                break;
            case TGPState.PointTrack:
                AimWithPID(trackingPositions[currentTrackingPosition]);
                break;
        }


    }

    private void SetTempSearchPoint()
    {
        if (tempSearchPoint == Vector3.zero || SearchModeInputFlag)
        {
            if (!Physics.Raycast(podCamera.transform.position, podCamera.transform.forward, out RaycastHit hitPoint, 60000, mask))
            {
                state = TGPState.Manual;
                print("NO TEMPORARY SEARCH POUNT HAS BEEN FOUND! State has been set to Manual");
                return;
            }
            tempSearchPoint = hitPoint.point;
            manualControlVector = Vector3.zero;
        }
    }
    float LOSLastChecked = 0;
    [SerializeField] float LOSCheckInterval;
    void LineOfSightControl()
    {
        if (Time.time - LOSLastChecked > LOSCheckInterval)
        {
            Vector3 direction = (tempSearchPoint - podCamera.transform.position).normalized;
            //There's an obstacle
            if (Physics.Raycast(podCamera.transform.position, direction, out RaycastHit hitPoint, 60000, mask))
            {
                tempSearchPoint = hitPoint.point;
                LOSLastChecked = Time.time;
                return;
            }
            //float jitterThreshold = 20f; // 10 cm gibi bir eþik koyabilirsin
            LOSLastChecked = Time.time;
        }
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
