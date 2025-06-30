using System;
#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;

public class FlightControlSystem : MonoBehaviour, IEnergyConsumer
{
    Rigidbody rb;
    public Action<float> pitchOutput;
    public Action<float> rollOutput;
    public Action<float> flapOutput;
    /// <summary>
    /// first one is flap and the second one is roll
    /// </summary>
    public Action<float, float> flapAndRollOutput;
    public Action<float> yawOutput;
    public Action<float> thrustOutput;
    public Action<float> slatOutput;

    public string NameE => gameObject.name;
    public bool IsPoweredE => isPowered;
    public float PowerConsumptionE => powerConsumption;
    public string SystemIdE => systemId;

    [SerializeField] bool isPowered;
    [SerializeField] float powerConsumption;
    [SerializeField] string systemId;


    public Action<float> pitchInput;
    public Action<float> yawInput;
    public Action<float> rollInput;
    public Action<float> throttleInput;
    
    [Header("Control Variables")]
    [Tooltip("How big the input area will be? The more, more precise.")]
    [SerializeField] int inputSize = 100;
    [Tooltip("How much player should use force before actually affecting?")]
    [SerializeField] int zeroInputAreaSize = 12;
    [Tooltip("How fast the yaw should decay? Def = 0.02f")]
    [SerializeField] float yawDecayingRate = 0;
    [SerializeField] float inputSensitivity;
    [SerializeField] bool PID;
    [SerializeField] bool LimitG;
    [SerializeField] bool LimitAoA;

    public Action<int> setInputSize;
    public Action<int> setZeroInputAreaSize;

    

    [Header("Max Turn Rates (Maximum values, these do not come out from the FLCS)")]
    [Tooltip("Turn rate in deg/sec")]
    [SerializeField] float maxTurnRatePitch;
    [Tooltip("Turn rate in deg/sec")]
    [SerializeField] float maxTurnRateRoll;
    [Tooltip("Turn rate in deg/sec")]
    [SerializeField] float maxTurnRateYaw;

    [Header("PID Values")]
    [SerializeField] float pGain;
    [SerializeField] float iGain;
    [SerializeField] float dGain;
    [SerializeField] float iSaturation;

    [SerializeField] bool UseGainsFromCurves;
    [SerializeField] FLCSPIDData PIDData;
    [SerializeField] AnimationCurve PitchP;
    [SerializeField] AnimationCurve PitchI;
    [SerializeField] AnimationCurve PitchISat;
    [SerializeField] AnimationCurve PitchD;
    [SerializeField] AnimationCurve YawP;
    [SerializeField] AnimationCurve YawI;
    [SerializeField] AnimationCurve YawISat;
    [SerializeField] AnimationCurve YawD;
    [SerializeField] AnimationCurve RollP;
    [SerializeField] AnimationCurve RollI;
    [SerializeField] AnimationCurve RollISat;
    [SerializeField] AnimationCurve RollD;
    [SerializeField] bool SaveCurveChanges;


    [SerializeField] bool enableAxisLocking;
    [SerializeField] bool enableSpeedLocking;
    [SerializeField] bool preventFalling;

    [SerializeField] float speedLockVelocity;
    [SerializeField] float setAngularXVel;

    [SerializeField] float MaxAOALimit;
    [SerializeField] float MinAOALimit;
    [SerializeField] float MaxGLimit;
    [SerializeField] float MinGLimit;

    float iStoredP;
    float iStoredY;
    float iStoredR;
    Vector3 lastTurnRateError = Vector3.zero;




    /// <summary>
    /// All the values should be at range [-1, 1]
    /// </summary>
    public struct PilotInput
    {
        /// <summary>
        /// Pilot input for Pitch Axis
        /// </summary>
        public float x;

        /// <summary>
        /// Pilot input for Yaw Axis
        /// </summary>
        public float y;

        /// <summary>
        /// Pilot input for Roll Axis
        /// </summary>
        public float z;

        /// <summary>
        /// Pilot input for Flaps
        /// </summary>
        public bool f;

        /// <summary>
        /// Pilot or FLCS input for Slats or LEF's
        /// </summary>
        public float s;

        /// <summary>
        /// Pilot or FLCS input for Throttle
        /// </summary>
        public float t;
    }

    public PilotInput F16Input;

    Vector3 angularVelocity;
    bool control = false;
    // Start is called before the first frame update
    void Start()
    {
        rb = GetComponent<Rigidbody>();
        setInputSize?.Invoke(inputSize);
        setZeroInputAreaSize?.Invoke(zeroInputAreaSize);

        SetPIDCurves();
    }

    bool keepAltByAltChange;
    bool keepAltByAimedAlt;
    float aimedAlt = -1;
    // Update is called once per frame
    void Update()
    {
        if (Input.GetKey(KeyCode.Mouse1)) control = true;
        else control = false;


        if (Input.GetKeyDown(KeyCode.G)) LimitG = !LimitG;

        if (Input.GetKeyDown(KeyCode.O)) keepAltByAltChange = !keepAltByAltChange;
        if (Input.GetKeyDown(KeyCode.P))
        {
            keepAltByAimedAlt = !keepAltByAimedAlt;
            if (aimedAlt == -1) aimedAlt = AerodynamicModel.alt;
            else aimedAlt = -1;
        }
        GetPilotInput();
        UpdateAlt();
        CalculateAltError();

        AxisLocking();
        SpeedLocking();
        AddDataToPIDCurves();
        PreventFalling();
        TextFieldManager.Instance.CreateOrUpdateScreenField("Turn Rate").Value($"Turn Rate: {angularVelocity.x * Mathf.Rad2Deg:F1}" + "Deg/Sec");
    }

    float smoothing = 0.1f; // Ayarlayabilirsin
    float averageAltChange = 0f; // Başlangıçta sıfır ya da ilk değerinle başlat
    float lastAlt;
    float altError;
    void UpdateAlt()
    {
        var alt = AerodynamicModel.alt;
        var altChangePerSecond = (alt - lastAlt) / Time.deltaTime;

        // Exponential Moving Average (EMA)
        averageAltChange = averageAltChange + smoothing * (altChangePerSecond - averageAltChange);

        //print("Average Alt Change per second: " + averageAltChange.ToString("0Feet"));

        lastAlt = alt;
    }

    void CalculateAltError()
    {
        var alt = AerodynamicModel.alt;
        altError = aimedAlt - alt;
    }

    private void FixedUpdate()
    {

        if (originshifted) originshifted = false;

        var plin = RemapAndInvokeInputs(F16Input);

        if (!isPowered) return;

        angularVelocity = GetAngularVelocity();
        plin = CalculateOptimalOutputs(plin);
        InvokeOutputs(plin);

    }

    private void OnEnable()
    {
        FloatingOrigin.originShifted += DetectOriginShift;
        ClickableEventHandler.Subscribe("SetParkingBrakes", SetParkingBrakes);
        ClickableEventHandler.Subscribe("SetAntiSkid", SetAntiSkid);
        ClickableEventHandler.Subscribe("DisableBrakes", DisableBrakes);
        ClickableEventHandler.Subscribe("EnableFCS", EnableFCS);
        ClickableEventHandler.Subscribe("DisableFCS", DisableFCS);
        EditorApplication.playModeStateChanged += UpdateCurvesOnTheObject;
    }

    void OnDisable() 
    {
        FloatingOrigin.originShifted -= DetectOriginShift;
        ClickableEventHandler.Unsubscribe("SetParkingBrakes", SetParkingBrakes);
        ClickableEventHandler.Unsubscribe("SetAntiSkid", SetAntiSkid);
        ClickableEventHandler.Unsubscribe("DisableBrakes", DisableBrakes);
        ClickableEventHandler.Unsubscribe("EnableFCS", EnableFCS);
        ClickableEventHandler.Unsubscribe("DisableFCS", DisableFCS);
    }

    bool power;

    void GetPilotInput()
    {
        if (!control) return;

        var a = inputSensitivity * Time.time;

        F16Input.x += Input.GetAxis("Mouse Y") / inputSize; //* a;
        F16Input.z += Input.GetAxis("Mouse X") / inputSize; //* a;
        F16Input.y += (Input.GetAxis("Horizontal") / inputSize); //* a;

        //Yaw decaying rate
        if (yawDecayingRate != 0) F16Input.y = Mathf.Lerp(F16Input.y, 0, yawDecayingRate);

        if (Input.GetKeyDown(KeyCode.F))
        {
            F16Input.f = !F16Input.f;
        }

        F16Input.s = ProjectUtilities.MapWithSign(AerodynamicModel.alpha, 0, 15, -0.08f, 1);
        F16Input.s = Mathf.Clamp(F16Input.s, -0.08f, 1);
        if (rb.velocity.magnitude < 30) F16Input.s = 0;

        var upSnap = Input.GetKey(KeyCode.LeftShift) && Input.GetKeyDown(KeyCode.W);
        var downSnap = Input.GetKey(KeyCode.LeftShift) && Input.GetKeyDown(KeyCode.S);

        var powerUpSnap = Input.GetKey(KeyCode.LeftControl) && Input.GetKey(KeyCode.LeftShift) && Input.GetKeyDown(KeyCode.W);
        var powerDownSnap = Input.GetKey(KeyCode.LeftControl) && Input.GetKey(KeyCode.LeftShift) && Input.GetKeyDown(KeyCode.S);

        //Up Snaps
        if (F16Input.t < 0.1f && powerUpSnap)
        {
            F16Input.t = 0.1f;
            power = true;
        }
        else if (F16Input.t >= 0.1f && F16Input.t < 1 && upSnap)
        {
            F16Input.t = 1f;
        }
        else if (F16Input.t >= 1f && upSnap)
        {
            F16Input.t = 1.25f;
        }
        //Down Snaps
        if (F16Input.t > 1 && downSnap)
        {
            F16Input.t = 1;
        }
        else if (F16Input.t <= 1 && F16Input.t > 0.1f && downSnap)
        {
            F16Input.t = 0.1f;
        }
        else if (F16Input.t == 0.1f && powerDownSnap)
        {
            F16Input.t = 0;
            power = false;
        }

        //Thrust can only be varied after 0.1f
        if (F16Input.t >= 0.1f && !Input.GetKey(KeyCode.LeftShift)) F16Input.t += Input.GetAxis("Vertical") / inputSize * inputSensitivity;
        if (power) F16Input.t = Mathf.Clamp(F16Input.t, 0.1f, 1.25f);
        var outputThrust = F16Input.t;


        if (Input.GetKeyDown(KeyCode.Space))
        {
            iStoredP = 0;
            iStoredY = 0;
            iStoredR = 0;
            F16Input.x = 0;
            F16Input.y = 0;
            F16Input.z = 0;
        }

        F16Input.x = Math.Clamp(F16Input.x, -1, 1);
        F16Input.y = Math.Clamp(F16Input.y, -1, 1);
        F16Input.z = Math.Clamp(F16Input.z, -1, 1);
        F16Input.t = Math.Clamp(F16Input.t, 0, 1.25f);
    }

    PilotInput RemapAndInvokeInputs(PilotInput plinput)
    {
        float availableArea = zeroInputAreaSize / (float)inputSize;

        //Zero input area
        if (plinput.x < availableArea && plinput.x > -availableArea) plinput.x = availableArea;
        if (plinput.y < availableArea && plinput.y > -availableArea) plinput.y = availableArea;
        if (plinput.z < availableArea && plinput.z > -availableArea) plinput.z = availableArea;

        //Remapping from input areas to [-1, 1]
        plinput.x = ProjectUtilities.MapWithSign(plinput.x, availableArea, 1, 0, 1);
        plinput.y = ProjectUtilities.MapWithSign(plinput.y, availableArea, 1, 0, 1);
        plinput.z = ProjectUtilities.MapWithSign(plinput.z, availableArea, 1, 0, 1);

        pitchInput.Invoke(plinput.x);
        yawInput.Invoke(plinput.y);
        rollInput.Invoke(plinput.z);
        throttleInput.Invoke(plinput.t);

        return plinput;
    }

    Vector3 GetAngularVelocity()
    {
        var vel = Quaternion.Inverse(transform.rotation) * rb.angularVelocity;
        return vel;
    }

    void SetLocalAngularVelocity(Vector3 localAV_degPerSec)
    {
        // 1. Dereceden radyana çevir
        Vector3 localAV_rad = localAV_degPerSec; //* Mathf.Deg2Rad;

        // 2. Local'dan world uzayına çevir
        Vector3 worldAV = transform.rotation * localAV_rad;

        // 3. Rigidbody'ye uygula
        rb.angularVelocity = worldAV;
    }

    Vector3 SetWantedTurnRates(Vector3 input)
    {
        float x = ProjectUtilities.MapWithSign(input.x, -1, 1, -maxTurnRatePitch * Mathf.Deg2Rad, maxTurnRatePitch * Mathf.Deg2Rad);
        float y = ProjectUtilities.MapWithSign(input.y, -1, 1, -maxTurnRateYaw * Mathf.Deg2Rad, maxTurnRateYaw * Mathf.Deg2Rad);
        float z = ProjectUtilities.MapWithSign(input.z, -1, 1, -maxTurnRateRoll * Mathf.Deg2Rad, maxTurnRateRoll * Mathf.Deg2Rad);
        return new Vector3(x, y, -z);
    }

    bool originshifted;
    void DetectOriginShift(Vector3 shiftingAmount)
    {
        originshifted = true;
    }

    /// <summary>
    /// Uses the input to give the best resulting outputs
    /// </summary>
    PilotInput CalculateOptimalOutputs(PilotInput plinput)
    {

        //Calculating turn rate error
        Vector3 angularVelocity = GetAngularVelocity();
        if (keepAltByAltChange) plinput.x = ProjectUtilities.MapWithSign(averageAltChange, 0, 1000, 0, 1);
        if (keepAltByAimedAlt)
        {
            plinput.x += ProjectUtilities.MapWithSign(-altError, 0, 1000, 0, 0.3f);
            //print("Alt Error: " + altError + " Aimed Alt: " + aimedAlt);
        }
        Vector3 wantedAV = SetWantedTurnRates(new Vector3(plinput.x, plinput.y, plinput.z));
        

        if (LimitG) wantedAV = Glimiter(rb.velocity.magnitude, angularVelocity, wantedAV); //LimitAngularVelocity(rb.velocity.magnitude, angularVelocity, 9, -4.5f); 

        if (LimitAoA) wantedAV = AoALimiter(wantedAV);


        Vector3 turnRateError = wantedAV - angularVelocity;
        
        if (PID)
        {
            var velocity = rb.velocity.magnitude;
            //Pitch
            if (UseGainsFromCurves)
            {
                var pPitch = PitchP.Evaluate(velocity);
                var iPitch = PitchI.Evaluate(velocity);
                var iSatPitch = PitchISat.Evaluate(velocity);
                var dPitch = PitchD.Evaluate(velocity);
                plinput.x += ProjectUtilities.PID(pPitch, dPitch, iPitch, ref iStoredP, iSatPitch, turnRateError.x, ref lastTurnRateError.x);
            }
            else
                plinput.x += ProjectUtilities.PID(pGain, dGain, iGain, ref iStoredP, iSaturation, turnRateError.x, ref lastTurnRateError.x);


            //Yaw
            if (UseGainsFromCurves)
            {
                var pYaw = YawP.Evaluate(velocity);
                var iYaw = YawI.Evaluate(velocity);
                var iSatYaw = YawISat.Evaluate(velocity);
                var dYaw = YawD.Evaluate(velocity);
                plinput.y += ProjectUtilities.PID(pYaw, dYaw, iYaw, ref iStoredY, iSatYaw, turnRateError.y, ref lastTurnRateError.y);
            }
            else 
                plinput.y += ProjectUtilities.PID(pGain, dGain, iGain, ref iStoredY, iSaturation, turnRateError.y, ref lastTurnRateError.y);


            //Roll
            float a;
            if (UseGainsFromCurves)
            {
                var pRoll = RollP.Evaluate(velocity);
                var iRoll = RollI.Evaluate(velocity);
                var iSatRoll = RollISat.Evaluate(velocity);
                var dRoll = RollD.Evaluate(velocity);
                a = ProjectUtilities.PID(pRoll, dRoll, iRoll, ref iStoredR, iSatRoll, turnRateError.z, ref lastTurnRateError.z);
            }
            else
                a = ProjectUtilities.PID(pGain, dGain, iGain, ref iStoredR, iSaturation, turnRateError.z, ref lastTurnRateError.z);
            plinput.z -= a;
        }
        lastTurnRateError = turnRateError;

        //Invoking Actions
        if (Input.GetMouseButton(2))
        {
            iStoredP = 0;
            iStoredY = 0;
            iStoredR = 0;
            plinput.x = 0;
            plinput.y = 0;
            plinput.z = 0;
        }

        plinput.x = Mathf.Clamp(plinput.x, -1, 1);
        plinput.y = Mathf.Clamp(plinput.y, -1, 1);
        plinput.z = Mathf.Clamp(plinput.z, -1, 1);

        return plinput;
    }

    void InvokeOutputs(PilotInput plinput)
    {
        pitchOutput?.Invoke(plinput.x);
        rollOutput?.Invoke(plinput.z);
        flapAndRollOutput?.Invoke(plinput.f ? 1 : 0, plinput.z);
        yawOutput?.Invoke(plinput.y);
        thrustOutput?.Invoke(plinput.t);
        slatOutput?.Invoke(plinput.s);
    }

    float lastGError;
    [SerializeField] float GLimiterP;
    [SerializeField] float GLimiterD;
    Vector3 Glimiter(float speed, Vector3 currentAV, Vector3 wantedAV)
    {
        var Gforce = AerodynamicModel.GForce;

        if (Gforce > MaxGLimit)
        {
            //print("Exceeding 9Gs");
            var GError = Gforce - MaxGLimit;
            wantedAV.x -= ProjectUtilities.PD(GLimiterP, GLimiterD, -GError, ref lastGError);
            lastGError = GError;
        }
        else if (Gforce < MinGLimit)
        {
            //print("Exceeding -4.5Gs");
            var GError = MinGLimit - Gforce;
            wantedAV.x -= ProjectUtilities.PD(GLimiterP, GLimiterD, GError, ref lastGError);
            lastGError = GError;
        }

        //float maxAVx = ((GLimitPlus - 1f) * ProjectUtilities.g) / speed;

        //float minAVx = ((1f - GLimitMinus) * ProjectUtilities.g) / speed;
        //minAVx = -minAVx;

        //print("Max AVX: " + maxAVx * Mathf.Rad2Deg + " Min AVX: " + minAVx * Mathf.Rad2Deg);

        //wantedAV.x = Mathf.Clamp(wantedAV.x, minAVx, maxAVx);

        //Debug.Log("MaxAVx: " + (maxAVx * Mathf.Rad2Deg) + " Current Av X: " + (wantedAV.x * Mathf.Rad2Deg));

        return wantedAV;
    }
    
    float lastAlphaError;
    [SerializeField] float AOALimiterP;
    [SerializeField] float AOALimiterD;
    Vector3 AoALimiter(Vector3 CurrentAV)
    {
        float alpha = AerodynamicModel.alpha;
        //print("AOA: " + alpha);

        if (alpha > MaxAOALimit)
        {
            print("Limiting Posiitive AOA");
            var alphaError = MaxAOALimit - alpha;
            CurrentAV.x = ProjectUtilities.PD(AOALimiterP, AOALimiterD, -alphaError, ref lastAlphaError);
            lastAlphaError = alphaError;
        }
        else if (alpha < MinAOALimit)
        {
            print("Limiting Negative AOA");
            var alphaError = alpha - MinAOALimit;
            CurrentAV.x = ProjectUtilities.PD(AOALimiterP, AOALimiterD, alphaError, ref lastAlphaError);
            lastAlphaError = alphaError;
        }
        return CurrentAV;
    }

    #region PID Curves
    void SetPIDCurves()
    {
        PitchP = PIDData.P_Pitch;
        PitchI = PIDData.I_Pitch;
        PitchISat = PIDData.ISat_Pitch;
        PitchD = PIDData.D_Pitch;

        YawP = PIDData.P_Yaw;
        YawI = PIDData.I_Yaw;
        YawISat = PIDData.ISat_Yaw;
        YawD = PIDData.D_Yaw;

        RollP = PIDData.P_Roll;
        RollI = PIDData.I_Roll;
        RollISat = PIDData.ISat_Roll;
        RollD = PIDData.D_Roll;
    }

    void UpdateCurvesOnTheObject(PlayModeStateChange state)
    {
        if (state == PlayModeStateChange.ExitingPlayMode && SaveCurveChanges)
        {
            Debug.Log("Values on PID curves saved.");

            PIDData.P_Pitch = PitchP;
            PIDData.I_Pitch = PitchI;
            PIDData.ISat_Pitch = PitchISat;
            PIDData.D_Pitch = PitchD;

            PIDData.P_Yaw = YawP;
            PIDData.I_Yaw = YawI;
            PIDData.ISat_Yaw = YawISat;
            PIDData.D_Yaw = YawD;

            PIDData.P_Roll = RollP;
            PIDData.I_Roll = RollI;
            PIDData.ISat_Roll = RollISat;
            PIDData.D_Roll = RollD;
        }
    }

    void AddDataToPIDCurves()
    {
        if(Input.GetKey(KeyCode.LeftControl) && Input.GetKeyDown(KeyCode.Keypad7))
        {
            PitchP.AddKey((int)rb.velocity.magnitude, pGain);
            PitchI.AddKey((int)rb.velocity.magnitude, iGain);
            PitchISat.AddKey((int)rb.velocity.magnitude, iSaturation);
            PitchD.AddKey((int)rb.velocity.magnitude, dGain);
            print("Value added to pitch curves");
        }

        if (Input.GetKey(KeyCode.LeftControl) && Input.GetKeyDown(KeyCode.Keypad8))
        {
            YawP.AddKey((int)rb.velocity.magnitude, pGain);
            YawI.AddKey((int)rb.velocity.magnitude, iGain);
            YawISat.AddKey((int)rb.velocity.magnitude, iSaturation);
            YawD.AddKey((int)rb.velocity.magnitude, dGain);
            print("Value added to yaw curves");
        }

        if (Input.GetKey(KeyCode.LeftControl) && Input.GetKeyDown(KeyCode.Keypad9))
        {
            RollP.AddKey((int)rb.velocity.magnitude, pGain);
            RollI.AddKey((int)rb.velocity.magnitude, iGain);
            RollISat.AddKey((int)rb.velocity.magnitude, iSaturation);
            RollD.AddKey((int)rb.velocity.magnitude, dGain);
            print("Value added to roll curves");
        }
    }

    [ContextMenu("Copy Source To Target")]
    void Coppier()
    {
        //AnimationCurve sourceCurve = YawISat;
        //if (sourceCurve != null)
        //{
        //    RollISat = new AnimationCurve(sourceCurve.keys);
        //    Debug.Log("Copied curve from source to target.");
        //}
    }

    #endregion

    #region Debugging
    void AxisLocking()
    {
        if (!enableAxisLocking) return;
        if (Input.GetKey(KeyCode.LeftAlt) && Input.GetKeyDown(KeyCode.Alpha1))
        {
            rb.constraints = RigidbodyConstraints.FreezeRotationZ | RigidbodyConstraints.FreezeRotationY;
        }
        if (Input.GetKey(KeyCode.LeftAlt) && Input.GetKeyDown(KeyCode.Alpha2))
        {
            rb.constraints = RigidbodyConstraints.FreezeRotationZ | RigidbodyConstraints.FreezeRotationX;
        }
        if (Input.GetKey(KeyCode.LeftAlt) && Input.GetKeyDown(KeyCode.Alpha3))
        {
            rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationY;
        }
        if (Input.GetKey(KeyCode.LeftAlt) && Input.GetKeyDown(KeyCode.Alpha4))
        {
            rb.constraints = RigidbodyConstraints.None;
        }
    }

    void SpeedLocking()
    {
        if (Input.GetKey(KeyCode.LeftControl) && Input.GetKeyDown(KeyCode.L)) enableSpeedLocking = !enableSpeedLocking;

        if (Input.GetKey(KeyCode.LeftControl) && Input.GetAxis("Mouse ScrollWheel") != 0) speedLockVelocity += Math.Sign(Input.GetAxis("Mouse ScrollWheel")) * 5;

        if (!enableSpeedLocking) return;
        rb.velocity = rb.velocity.normalized * speedLockVelocity * 0.514444f;
    }

    void PreventFalling()
    {
        if (Input.GetKey(KeyCode.LeftControl) && Input.GetKeyDown(KeyCode.N)) preventFalling = !preventFalling;

        if (!preventFalling) return;

        var magnitude = Vector3.Dot(Vector3.up, rb.velocity);
        rb.velocity += Vector3.up * -magnitude;
    }

    #endregion

    void SetParkingBrakes()
    {
        yawDecayingRate = 0;
    }

    void SetAntiSkid()
    {
        yawDecayingRate = 0.01f;
    }

    void DisableBrakes()
    {
        yawDecayingRate = 0;
    }

    void EnableFCS()
    {
        isPowered = true;
        EnergyBus.Instance.ApplyToBus(systemId, 0, null, this);
    }

    void DisableFCS()
    {
        isPowered = false;
    }

    public void ChangePowerStatusE(bool status)
    {
        isPowered = status;
    }
}
