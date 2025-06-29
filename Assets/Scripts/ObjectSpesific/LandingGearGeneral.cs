using UnityEngine;

public class LandingGearGeneral : MonoBehaviour, IHydraulicConsumer, IEnergyConsumer
{
    [SerializeField] GameObject taxiLights;
    [SerializeField] GameObject landingLights;

    [Header("Wheel Colliders")]
    [SerializeField] WheelCollider noseGear;
    [SerializeField] WheelCollider right;
    [SerializeField] WheelCollider left;

    [Header("Wheel Meshes")]
    [SerializeField] GameObject noseWheel;
    [SerializeField] GameObject rightWheel;
    [SerializeField] GameObject leftWheel;

    [SerializeField] AnimationClip retractingAnim;
    [SerializeField] AnimationCurve animCurve;
    [SerializeField] float playTime;

    FlightControlSystem FCS;

    [Header("NWS")]
    [SerializeField] float steeringAngle;

    [Header("Brakes")]
    [SerializeField] float maxBrakePower;

    [Header("Hydraulic Consumer Values")]
    [SerializeField] string systemIdH;
    [SerializeField] bool isPoweredH;
    [SerializeField] float accumulatedPressureDrawH;
    [SerializeField] float maxPressureH;
    [SerializeField] float optimalPressureH;
    [SerializeField] float minPressureH;

    [Header("Energy Consumer Values")]
    [SerializeField] string systemIdE;
    [SerializeField] bool isPoweredE;
    [SerializeField] float powerConsumptionE;

    [Header("Pressure Uses")]
    [SerializeField] float maxBrakePressure;
    [SerializeField] float gearPressureUsePerSecond;
    [SerializeField] float currentSystemPressure;

    public string SystemIdH => systemIdH;
    public string NameH => gameObject.name;
    public bool IsActuatedH => isPoweredH;
    public float AccumulatedPressureDrawH => accumulatedPressureDrawH;
    public float MaxPressureH => maxPressureH;
    public float OptimalPressureH => optimalPressureH;
    public float MinPressureH => minPressureH;

    public string NameE => gameObject.name;
    public bool IsPoweredE => isPoweredE;
    public float PowerConsumptionE => powerConsumptionE;
    public string SystemIdE => systemIdE;



    [SerializeField] bool NWS;
    float rudder;
    float throttle;
    bool leftBrake;
    bool rightBrake;
    BrakingType brakingType = BrakingType.Off;
    LightingType lightingType = LightingType.Off;


    enum BrakingType
    {
        Off,
        AntiSkid,
        ParkingBrake
    }

    enum LightingType
    {
        Off,
        Taxi,
        Landing
    }

    bool gearsDeployed = true;
    private float elapsed = 0f;
    private bool playing = false;
    bool reverse;

    public void ChangePowerStatusE(bool status)
    {
        isPoweredE = status;
    }

    public void SendRemainingPressure(float pressure)
    {
        currentSystemPressure = pressure;
    }

    public void ResetAccumulatedDraw()
    {
        accumulatedPressureDrawH = 0f;
    }

    private void Awake()
    {
        elapsed = playTime;
    }

    private void Start()
    {
        isPoweredH = true;
        HydraulicBus.Instance.ApplyToBus(systemIdH, this);
        noseGear.brakeTorque = 0;
        right.brakeTorque = 0;
        left.brakeTorque = 0;
    }

    private void Update()
    {
        retractingAnimation();
        Brakes();
        NoseWheelSteering();
        Lights();
    }

    private void OnEnable()
    {
        if (FCS == null) FCS = FindAnyObjectByType<FlightControlSystem>();
        FCS.yawOutput += GetRudderOutput;
        FCS.thrustOutput += GetThrottleOutput;
        ClickableEventHandler.Subscribe("SetParkingBrakes", SetParkingBrakes);
        ClickableEventHandler.Subscribe("SetAntiSkid", SetAntiSkid);
        ClickableEventHandler.Subscribe("DisableBrakes", DisableBrakes);
        ClickableEventHandler.Subscribe("SetTaxiLightsOn", SetTaxiLightsOn);
        ClickableEventHandler.Subscribe("SetLandingLightsOn", SetLandingLightsOn);
        ClickableEventHandler.Subscribe("SetLandingLightsOff", SetLandingLightsOff);
        ClickableEventHandler.Subscribe("retractGears", retractGears);
        ClickableEventHandler.Subscribe("deployGears", deployGears);
    }

    private void OnDisable()
    {
        FCS.yawOutput -= GetRudderOutput;
        FCS.thrustOutput -= GetThrottleOutput;
        ClickableEventHandler.Unsubscribe("SetParkingBrakes", SetParkingBrakes);
        ClickableEventHandler.Unsubscribe("SetAntiSkid", SetAntiSkid);
        ClickableEventHandler.Unsubscribe("DisableBrakes", DisableBrakes);
        ClickableEventHandler.Unsubscribe("SetTaxiLightsOn", SetTaxiLightsOn);
        ClickableEventHandler.Unsubscribe("SetLandingLightsOn", SetLandingLightsOn);
        ClickableEventHandler.Unsubscribe("SetLandingLightsOff", SetLandingLightsOff);
        ClickableEventHandler.Unsubscribe("retractGears", retractGears);
        ClickableEventHandler.Unsubscribe("deployGears", deployGears);
    }

    void GetRudderOutput(float input)
    {
        rudder = input;
    }

    void GetThrottleOutput(float input)
    {
        throttle = input;
    }

    void NoseWheelSteering()
    {
        if (Input.GetKeyDown(KeyCode.Mouse3)) NWS = !NWS;
        //Nose Gear Steering
        if (NWS && currentSystemPressure > minPressureH && gearsDeployed)
        {
            var steerAngle = rudder * steeringAngle;
            var pressureForNWS = Mathf.Abs(steerAngle - noseGear.steerAngle);
            noseGear.steerAngle = steerAngle;
            noseWheel.transform.localEulerAngles = new Vector3(0, 0, steerAngle);
            accumulatedPressureDrawH += pressureForNWS * maxBrakePower / (steeringAngle * 4);
        }
    }

    #region Brakes
    void SetParkingBrakes()
    {
        brakingType = BrakingType.ParkingBrake;
    }

    void SetAntiSkid()
    {
        brakingType = BrakingType.AntiSkid;
    }

    void DisableBrakes()
    {
        brakingType = BrakingType.Off;
    }

    void Brakes()
    {
        bool rightBrakeInput = Input.GetKey(KeyCode.X);
        bool leftBrakeInput = Input.GetKey(KeyCode.Z);
        noseGear.motorTorque = 1;
        switch (brakingType)
        {
            case BrakingType.Off:
                if (rightBrakeInput && currentSystemPressure > minPressureH)
                {
                    right.brakeTorque = maxBrakePower;
                    if (!rightBrake)
                    {
                        rightBrake = true;
                        accumulatedPressureDrawH += maxBrakePressure;
                    }
                }
                else
                {
                    right.brakeTorque = 0;
                    rightBrake = false;
                }

                if (leftBrakeInput && currentSystemPressure > minPressureH)
                {
                    left.brakeTorque = maxBrakePower;
                    if (!leftBrake)
                    {
                        leftBrake = true;
                        accumulatedPressureDrawH += maxBrakePressure;
                    }
                }
                else
                {
                    left.brakeTorque = 0;
                    leftBrake = false;
                }
                break;

            case BrakingType.AntiSkid:
                if (rightBrakeInput && currentSystemPressure > minPressureH)
                {
                    right.brakeTorque = maxBrakePower;
                    if (!rightBrake)
                    {
                        rightBrake = true;
                        accumulatedPressureDrawH += maxBrakePressure;
                    }
                }
                else
                {
                    right.brakeTorque = 0;
                    rightBrake = false;
                }

                if (leftBrakeInput && currentSystemPressure > minPressureH)
                {
                    left.brakeTorque = maxBrakePower;
                    if (!leftBrake)
                    {
                        leftBrake = true;
                        accumulatedPressureDrawH += maxBrakePressure;
                    }
                }
                else
                {
                    left.brakeTorque = 0;
                    leftBrake = false;
                }
                break;

            case BrakingType.ParkingBrake:
                if (!rightBrake && currentSystemPressure > minPressureH)
                {
                    right.brakeTorque = maxBrakePower;
                    accumulatedPressureDrawH += maxBrakePressure;
                    rightBrake = true;
                }
                if (!leftBrake && currentSystemPressure > minPressureH)
                {
                    left.brakeTorque = maxBrakePower;
                    accumulatedPressureDrawH += maxBrakePressure;
                    leftBrake = true;
                }
                if (throttle > 0.1f)
                {
                    GenericEventManager.Invoke("ParkingBrakeSet", 2);
                }
                break;
        }
    }
    #endregion

    #region Animation
    float animSpeed;
    void retractingAnimation()
    {
        if (!playing) return;

        animSpeed = ProjectUtilities.Map(currentSystemPressure, minPressureH, optimalPressureH, 0.5f, 1);
        if (animSpeed > 0.5f)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / playTime * animSpeed);

            accumulatedPressureDrawH += gearPressureUsePerSecond * Time.deltaTime;

            // Curve zamanýný yönüne göre belirle
            float curveT = reverse ? animCurve.Evaluate(1f - t) : animCurve.Evaluate(t);
            float sampleTime = curveT * retractingAnim.length;

            retractingAnim.SampleAnimation(gameObject, sampleTime);

            if (t >= 1f)
            {
                playing = false;
            }
        }
    }

    /// <summary>
    /// Animasyonu ileri yönde baþlatýr
    /// </summary>
    public void PlayForward()
    {
        elapsed = playTime - elapsed;
        reverse = false;
        playing = true;
        gearsDeployed = false;
        noseGear.isTrigger = false;
        left.isTrigger = false;
        right.isTrigger = false;
    }

    /// <summary>
    /// Animasyonu geri yönde baþlatýr
    /// </summary>
    public void PlayBackward()
    {
        elapsed = playTime - elapsed;
        reverse = true;
        playing = true;
        gearsDeployed = true;
        noseGear.isTrigger = true;
        left.isTrigger = true;
        right.isTrigger = true;
    }

    void retractGears()
    {
        if (!gearsDeployed) return;

        PlayForward();
    }

    void deployGears()
    {
        if (gearsDeployed) return;

        PlayBackward();
    }
    #endregion

    #region Lights
    void SetTaxiLightsOn()
    {
        lightingType = LightingType.Taxi;
        isPoweredE = true;
        EnergyBus.Instance.ApplyToBus(systemIdE, 10, null, this);
    }

    void SetLandingLightsOn()
    {
        lightingType = LightingType.Landing;
        isPoweredE = true;
        EnergyBus.Instance.ApplyToBus(systemIdE, 10, null, this);
    }

    void SetLandingLightsOff()
    {
        lightingType = LightingType.Off;
        ChangePowerStatusE(false);
    }

    void Lights()
    {
        switch (lightingType)
        {
            case LightingType.Off:
                taxiLights.SetActive(false);
                landingLights.SetActive(false);
                break;
            case LightingType.Taxi:
                if (gearsDeployed && isPoweredE)
                {
                    taxiLights.SetActive(true);
                    landingLights.SetActive(false);
                }
                else
                {
                    taxiLights.SetActive(false);
                    landingLights.SetActive(false);
                }
                break;
            case LightingType.Landing:
                if (gearsDeployed && isPoweredE)
                {
                    taxiLights.SetActive(false);
                    landingLights.SetActive(true);
                }
                else
                {
                    taxiLights.SetActive(false);
                    landingLights.SetActive(false);
                }
                break;
        }
    }

    
    #endregion
}
