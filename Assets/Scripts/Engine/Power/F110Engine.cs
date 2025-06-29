using System;
using UnityEngine;

public class F110Engine : MonoBehaviour
{
    public bool quickStart;
    [SerializeField] F110FuelPump f110FuelPump;
    [SerializeField] Transform F16;

    [SerializeField] float RPM;
    public int RPMPercent = 0;
    [SerializeField] float maxRPM;

    [SerializeField] float intakeFanRadius;
    [SerializeField] float intakeFanEfficiency;

    [SerializeField] float throttleInput;
    [SerializeField] float energyCoefficient;

    #region States
    [SerializeField] bool isWorking;
    [SerializeField] bool ignited;
    [SerializeField] bool idled;
    [SerializeField] bool afterburner;
    #endregion

    [SerializeField] float temperatureCelcius;

    [SerializeField] AnimationCurve consumeRate;
    [SerializeField] AnimationCurve RPMAssistedAcceleration;
    [SerializeField] AnimationCurve RPMIgnitionAcceleration;
    [SerializeField] AnimationCurve RPMThrust;
    [SerializeField] AnimationCurve RPMEmission;
    [SerializeField] float resistance;

    [SerializeField] float fullRPM;
    [SerializeField] float idleRPM;
    [SerializeField] float idleSoundRPM;
    [SerializeField] float assistedRPM;

    public float thrust;

    [SerializeField] float accel;
    [SerializeField] float transmittedAccel;
    [SerializeField] float totalWastedFuel;
    [SerializeField] float currentAirFlowPerFrame;
    [SerializeField] float currentAirFlowPerSecond;
    [SerializeField] float atmosphereOxygenDensity;
    
    FlightControlSystem FCS;

    #region Timing Control Variables
    bool reachedAsisted = false;
    bool reachedIdle = false;
    bool reachedShutOff = false;
    #endregion

    [SerializeField] float timeToAssisted = 0;
    [SerializeField] float timeToIdle = 0;
    [SerializeField] float timeToShutOff = 0;

    #region Engine Stability
    [Header("Engine Stability")]
    [SerializeField] bool stable = false;
    [SerializeField] float unstability = 0;
    [SerializeField] float stableAfterRPM;
    [SerializeField] float unstableRPMAddition;
    [SerializeField] float unstableFTITAddition;
    #endregion

    #region VFX Variables
    Material engineMat;
    [SerializeField][ColorUsage(true, true)] Color engineEmissionColor;
    [SerializeField] float emission;
    [SerializeField] AnimationCurve DistortionSize;
    [SerializeField] AnimationCurve DistortionLength;
    [SerializeField] AnimationCurve DistortionSpeed;
    
    [SerializeField] AnimationCurve AfterburnerAlpha;
    [SerializeField] AnimationCurve AfterburnerEmission;
    [SerializeField] AnimationCurve AfterburnerSize;
    [SerializeField] AnimationCurve AfterburnerLength;

    [SerializeField] Transform AfterburnerCone;
    Material burner;

    [SerializeField] Transform distortionObject;
    Material distortion;
    #endregion

    float energyCreatedThisFrame = 0;
    void RunEngine()
    {
        RPMPercent = (int)(RPM / 12000 * 100);
        
        if (RPM > assistedRPM && !reachedAsisted)
        {
            reachedAsisted = true;
            timeToAssisted = Time.time;
        }

        if (RPM > idleRPM && !reachedIdle)
        {
            reachedIdle = true;
            timeToIdle = Time.time - timeToAssisted;
        }

        if (RPM > idleRPM && reachedIdle)
        {
            timeToShutOff = Time.time;
        }

        if (RPM == 0 && reachedIdle && !reachedShutOff)
        {
            timeToShutOff = Time.time - timeToShutOff;
            reachedShutOff = true;
        }

        if (!isWorking) return;
        maxRPM = Math.Max(RPM, maxRPM);

        if (throttleInput >= 0.1f && !ignited && isWorking)
        {
            ignited = true;
            SoundManager.instance.PlayInPool("Engine Ignition", transform, 1, 1);
        }
        

        if (RPM >= idleSoundRPM && !idled && isWorking)
        {
            idled = true;
            SoundManager.instance.PlayInPool("Engine Idle", transform, 1, 0.8f);
            SoundManager.instance.StopInPool("JFS Loop");
        }

        if (RPM > idleSoundRPM)
        {
            GenericEventManager.Invoke<bool>("SetMainGenStatus", true);
        }

        if (!SoundManager.instance.IsClipPlaying("Engine Idle") && 
            !SoundManager.instance.IsClipPlaying("Engine Loop") && idled) 
            SoundManager.instance.PlayInPool("Engine Loop", transform, 1, 0.8f, 0, true);

        var soundPitch = Mathf.Clamp(ProjectUtilities.Map(RPM, idleRPM, fullRPM, 0.8f, 2), 0.8f, 2);

        SoundManager.instance.AdjustInPool("Engine Idle", 1, soundPitch);
        SoundManager.instance.AdjustInPool("Engine Loop", 1, soundPitch);

        //Engine ShutDown
        if (soundPitch < 0.85f && RPM < idleRPM && SoundManager.instance.IsClipPlaying("Engine Loop") && throttleInput == 0)
        {
            SoundManager.instance.StopInPool("Engine Loop");
            SoundManager.instance.PlayInPool("Engine ShutDown", transform, 1, 1f, 0, false);
            isWorking = false;
            ignited = false;
            idled = false;
            GenericEventManager.Invoke<bool>("SetMainGenStatus", false);
        }

        //unstable again
        if (isWorking == false && RPM < idleRPM)
        {
            stable = false;
        }

        if (RPM > fullRPM)
        {

            var afterburnerIntensity = ProjectUtilities.Map(RPMPercent, 100, 110, 0.2f, 1);
            if (!afterburner)
            {
                SoundManager.instance.PlayInPool("Afterburner Start", transform, afterburnerIntensity, 1);
                SoundManager.instance.PlayAfterClip("Afterburner Start", "Afterburner Loop", transform, afterburnerIntensity, 1, 0, true);
                afterburner = true;
            }
            SoundManager.instance.AdjustInPool("Afterburner Start", afterburnerIntensity, 1);
            SoundManager.instance.AdjustInPool("Afterburner Loop", afterburnerIntensity, 1);

        }
        else
        {
            SoundManager.instance.StopInPool("Afterburner Start");
            SoundManager.instance.StopInPool("Afterburner Loop");
            afterburner = false;
        }
        //Power up the fuel pump.
        f110FuelPump.EnableFlow = true;
        //Use some RPM (increase resistance progressively)
        var flowRatePerFrame = f110FuelPump.PumpFuel(FuelControl()); //throttleInput >= 0.1 ? f110FuelPump.PumpFuel(ProjectUtilities.Map(throttleInput, 0.1f, 1, 1.83f, 36.6f)) : 0;


        var fuelType = f110FuelPump.fuelType;
        //If RPM is enough, ignite
            
            
        var ReqOxygenThisFrame = flowRatePerFrame * (float)fuelType.oxidizerFuelRatio;
        var BurnableFuelThisFrame = (currentAirFlowPerFrame * atmosphereOxygenDensity) / (float)fuelType.oxidizerFuelRatio;

        //Required oxygen exceeds what we have this frame. So the must be wasted fuel
        if(ReqOxygenThisFrame > (currentAirFlowPerFrame * atmosphereOxygenDensity))
        {
            //Oxygen limits our energy                     Oxygen                       Dividing with ratio to get fuel   Power allways being multiplied with fuel
            energyCreatedThisFrame = ((currentAirFlowPerFrame * atmosphereOxygenDensity) / (float)fuelType.oxidizerFuelRatio) * fuelType.PowerPerUnit;
            totalWastedFuel += flowRatePerFrame - BurnableFuelThisFrame;
        }
        //I have enough Oxygen. FlowRate is my limiter
        else
        {
            energyCreatedThisFrame = flowRatePerFrame * fuelType.PowerPerUnit;
        }



        accel = transmittedAccel + energyCreatedThisFrame * energyCoefficient;//37.40f;


        RPM += accel;

        CalculateUnstability();
        CalculateAndSendFTIT();
    }

    /// <summary>
    /// Dynamicaly changes the resistance due to: 
    /// Wanted Net Acceleration,
    /// Consume Rate,
    /// RPM
    /// and Target Throttle
    /// This helps the engine to accelerate in wanted speeds.
    /// </summary>
    void CalculateSmartResistance()
    {
        float wantedNetAccel = 0;
        if (isWorking && !ignited && RPM < assistedRPM + 200)
        {
            wantedNetAccel = RPMAssistedAcceleration.Evaluate(RPM);
            resistance = accel - wantedNetAccel;
        }
        else if (isWorking && ignited && RPM > assistedRPM)
        {
            wantedNetAccel = RPMIgnitionAcceleration.Evaluate(RPM);
            resistance = accel - wantedNetAccel;
        }
        else
        {
            wantedNetAccel = -RPMIgnitionAcceleration.Evaluate(RPM);
            resistance = accel - wantedNetAccel;
        }

        if (!isWorking)
        {
            wantedNetAccel = -RPMIgnitionAcceleration.Evaluate(RPM);
            resistance = accel - wantedNetAccel;
        }

        if (RPM > idleRPM)
        {
            var targetRPM = ProjectUtilities.Map(throttleInput, 0.1f, 1f, 8000, 12000);
            var softeningRange = 10;
            //Target RPM altýnda ve range dýþýndayken 0
            resistance = accel - RPMIgnitionAcceleration.Evaluate(RPM) / 3;
            //target RPM altýndayken ve range içindeyken accel'e yaklaþacak.
            if (RPM < targetRPM + 1  && RPM + softeningRange > targetRPM + 1)
            {
                resistance = accel / Mathf.Clamp(targetRPM - RPM, 1, 10);
            }
            //Target rpm üstündeyken -(accel + wantedNetAccel)
            if (RPM > targetRPM)
            {
                resistance = (accel + RPMIgnitionAcceleration.Evaluate(RPM));
            }
        }

        if(RPM > 1) RPM -= resistance;
        else RPM = 0;
    }

    float FuelControl()
    {
        if (!isWorking) return 0;
        if (RPM < assistedRPM)
        {
            if (throttleInput >= 0.1) isWorking = false;
        }

        else if (assistedRPM < RPM && RPM < idleRPM)
        {
            if (throttleInput > 0.1f) isWorking = false;
        }

        return consumeRate.Evaluate(RPM);
    }


    float airSpeed;
    void GetAirSpeed(string airSpeedS)
    {
        airSpeed = (float)Convert.ToDecimal(airSpeedS);
    }
    

    void CalculateThrust()
    {
        thrust = RPMThrust.Evaluate(RPM);
    }


    private void Awake()
    {
        engineMat = GetComponent<MeshRenderer>().material;
        distortion = distortionObject.GetComponent<MeshRenderer>().material;
        burner = AfterburnerCone.GetComponent<MeshRenderer>().material;
    }

    private void Start()
    {
        
    }

    private void FixedUpdate()
    {
        RunEngine();
        CalculateSmartResistance();
        SuckAir();
        CalculateThrust();
        EngineEmission();
        EngineHeatDistortion();
        EngineAfterBurner();
        sendCurrentRPM();


        if (quickStart)
        {
            isWorking = true;
            idled = true;
            ignited = true;
            RPM = 8000;
            throttleInput = 1;
            quickStart = false;
        }
    }

    #region VFX
    void EngineEmission()
    {
        emission = Mathf.Lerp(emission, RPMEmission.Evaluate(RPMPercent), 0.05f);
        Color color = engineEmissionColor;
        color.r *= emission;
        color.g *= emission;
        color.b *= emission;

        engineMat.SetColor("_EmissionColor", color);
    }

    void EngineHeatDistortion()
    {
        var dSpeed = DistortionSpeed.Evaluate(RPMPercent);
        var dSize = DistortionSize.Evaluate(RPMPercent); 
        var dLength = DistortionLength.Evaluate(RPMPercent);

        distortionObject.transform.localScale = Vector3.Lerp(distortionObject.transform.localScale, new Vector3(dSize, dLength, dSize), 0.05f);

        if (dLength < 0.1f) distortionObject.gameObject.SetActive(false);
        else distortionObject.gameObject.SetActive(true);

        distortion.SetVector("_DistortionSpeed", new Vector4(dSpeed, dSpeed, 0, 0));
    }

    float burnerAlpha;
    float burnerEmission;
    float burnerSize;
    float burnerLength;

    void EngineAfterBurner()
    {
        burnerAlpha = Mathf.Lerp(burnerAlpha, AfterburnerAlpha.Evaluate(RPMPercent), 0.05f);
        burnerEmission = Mathf.Lerp(burnerEmission, AfterburnerEmission.Evaluate(RPMPercent), 0.05f);
        burnerSize = Mathf.Lerp(burnerSize, AfterburnerSize.Evaluate(RPMPercent), 0.05f);
        burnerLength = Mathf.Lerp(burnerLength, AfterburnerLength.Evaluate(RPMPercent), 0.05f);

        burner.SetFloat("_Alpha", burnerAlpha);
        burner.SetFloat("_EmissionStrength", burnerEmission);
        AfterburnerCone.localScale = new Vector3(burnerSize, burnerSize, burnerLength);

    }
    #endregion


    #region Enable Disable
    private void OnEnable()
    {
        FCS = FindAnyObjectByType<FlightControlSystem>();
        FCS.throttleInput += SetThrustInput;
        StringEventManager.Subscribe("1-Speed", GetAirSpeed);
    }

    private void OnDisable()
    {
        FCS = FindAnyObjectByType<FlightControlSystem>();
        if (FCS != null) FCS.throttleInput -= SetThrustInput;
        StringEventManager.Unsubscribe("1-Speed", GetAirSpeed);
    }
    #endregion


    void SetThrustInput(float input)
    {
        throttleInput = input;
    }

    private void SuckAir()
    {
        if (RPM < 1) return;
        //Air density
        var rho = ProjectUtilities.CalculateAirDensity(transform.position, temperatureCelcius);

        currentAirFlowPerFrame = rho * (float)Math.Pow(Math.PI, 2) * intakeFanEfficiency * (float)Math.Pow(intakeFanRadius, 3) * (RPM / 60) / 60;
        currentAirFlowPerSecond = currentAirFlowPerFrame * 60;
    }

    public float ConnectPower(float JFSRPM)
    {
        transmittedAccel = Math.Clamp(JFSRPM / 8000 - Math.Clamp(ProjectUtilities.Map(RPM - 2400, 0, 6600, 0, 10), 0, 10), 0, 20); //Mathf.Clamp(JFSRPM / 33.3f, 0, 1200); 
        if (!isWorking) isWorking = true;
        return RPM;
    }

    
    void CalculateUnstability()
    {
        //Increasing
        float duration = 10f;
        float fixedDelta = Time.fixedDeltaTime; // Genellikle 0.02
        float t = 1f - Mathf.Exp(-fixedDelta * 5f / duration);

        //Decreasing
        float totalTime = 4f;
        float speed = 1f / totalTime; // her saniye ne kadar azalmalý?

        if (!stable)
        {
            if (RPM + unstableRPMAddition * unstability> stableAfterRPM)
            {
                stable = true;
                return;
            }

            if (RPM > 6000)
                unstability = Mathf.Lerp(unstability, 1, t);
        }
        else
        {
            unstability -= speed * Time.fixedDeltaTime;
            unstability = Mathf.Max(unstability, 0f); // negatif olmasýn
        }
    }

    [SerializeField] float currentRPM = 0;
    float lastRPMSent = 0;
    float sendingInterval = 0.5f;
    void sendCurrentRPM()
    {
        if (Math.Abs(currentRPM - (int)RPM) < 1) return;
        if (lastRPMSent + sendingInterval > Time.time) return;
        currentRPM = (int)RPM;
        lastRPMSent = Time.time;

        GenericEventManager.Invoke("F16_1RPM", currentRPM + unstableRPMAddition * unstability);        
    }

    //To make FTIT look realistic.
    [SerializeField] float baseFTIT;
    void CalculateAndSendFTIT()
    {
        // Pseudo-code
        float throttle = throttleInput; // 0 - 1
        float afterburner = Mathf.Clamp01(ProjectUtilities.Map(throttleInput, 1, 1.25f, 0, 1));
        float altitude = ProjectUtilities.CalculateAltitude(transform.position); // meters


        // Temel sýcaklýk (idle düzeyinde)
        baseFTIT = Mathf.Clamp(ProjectUtilities.Map(RPMPercent, 25, 65, 200, 600), 0, 600); // °C
        float throttleAdditionFTIT = Mathf.Clamp(ProjectUtilities.Map(RPMPercent, 65, 100, 0, 400), 0, 400);

        // Afterburner etkisi
        float abEffect = afterburner * 100;

        // Soðutma etkisi (yükseklik ve hýzdan kaynaklý)
        float coolingEffect = (airSpeed * 0.05f) - (altitude * 0.01f);
        //coolingEffect *= ProjectUtilities.Map(RPMPercent,)

        // Toplam FTIT
        var FTIT = baseFTIT + throttleAdditionFTIT + abEffect - coolingEffect;
        GenericEventManager.Invoke("1-FTIT", FTIT + unstableFTITAddition * unstability);
            
        
    }
}
