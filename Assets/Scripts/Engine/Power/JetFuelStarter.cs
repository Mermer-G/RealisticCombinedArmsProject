using System;
using System.ComponentModel;
using Unity.Collections;
using UnityEngine;

public class JetFuelStarter : MonoBehaviour
{
    [SerializeField] JFSFuelPump JFSFuelPump;
    [SerializeField] F110Engine F110Engine;

    [SerializeField] float F110RPM;
    [SerializeField] float RPM;
    [SerializeField] float maxRPM;
    public bool IsWorking;
    [SerializeField] float ignitionRPM;
    [SerializeField] float transmissionRPM;

    [SerializeField] float accel;
    [SerializeField] float resistance;
    [SerializeField] AnimationCurve resistanceCurve;

    [SerializeField] float intakeFanRadius;
    [SerializeField] float intakeFanEfficiency;

    [SerializeField] float temperatureCelcius;

    [SerializeField] float requiredFlowRate;
    [SerializeField] float currentAirFlowPerFrame;
    [SerializeField] float currentAirFlowPerSecond;
    [SerializeField] float atmoshpereOxygenDensity;
    [SerializeField] float totalWastedFuel = 0;

    [SerializeField] float timeElapsed = 0;
    [SerializeField] bool runLight;

    void RunEngine()
    {
        //Everything inside this scope MUST be per frame
        if (!IsWorking) return;
        maxRPM = Math.Max(RPM, maxRPM);
        //Power up the fuel pump.
        JFSFuelPump.EnableFlow = true;

        //This returns flowRatePerFrame
        var flowRatePerFrame = JFSFuelPump.PumpFuel(RPM);

        float energyCreatedThisFrame = 0;

        var fuelType = JFSFuelPump.fuelType;
        //If RPM is enough, ignite
        if (RPM >= ignitionRPM)
        {
            var ReqOxygenThisFrame = flowRatePerFrame * (float)fuelType.oxidizerFuelRatio;
            var BurnableFuelThisFrame = (currentAirFlowPerFrame * atmoshpereOxygenDensity) / (float)fuelType.oxidizerFuelRatio;

            //Required oxygen exceeds what we have this frame. So the must be wasted fuel
            if(ReqOxygenThisFrame > (currentAirFlowPerFrame * atmoshpereOxygenDensity))
            {
                //Oxygen limits our energy                     Oxygen                       Dividing with ratio to get fuel   Power allways being multiplied with fuel
                energyCreatedThisFrame = ((currentAirFlowPerFrame * atmoshpereOxygenDensity) / (float)fuelType.oxidizerFuelRatio) * fuelType.PowerPerUnit;
                totalWastedFuel += flowRatePerFrame - BurnableFuelThisFrame;
            }
            //I have enough Oxygen. FlowRate is my limiter
            else
            {
                energyCreatedThisFrame = flowRatePerFrame * fuelType.PowerPerUnit;
            }
        }

        accel = energyCreatedThisFrame * 840.20f;
        RPM += accel;
        //print("Energy Created This Frame: " + energyCreatedThisFrame +
        //      "Accel: " + accel);

        if (RPM < 10) IsWorking = false;

        if (RPM > transmissionRPM)
        {
            GenericEventManager.Invoke<int>("JFSStartSet", 1);
            F110RPM = F110Engine.ConnectPower(RPM);
            if (F110RPM > 6600)
            {
                IsWorking = false;
                JFSFuelPump.PumpFuel(0);
            }
        }

        if (RPM > 15000 && IsWorking && !runLight) GenericEventManager.Invoke<bool>("JFSRUN", true);
        else if (!IsWorking) GenericEventManager.Invoke<bool>("JFSRUN", false);

    }

    private void SuckAir()
    {
        if (RPM < 1) return;
        //Air density
        var rho = ProjectUtilities.CalculateAirDensity(transform.position, temperatureCelcius);

        currentAirFlowPerFrame = rho * (float)Math.Pow(Math.PI, 2) * intakeFanEfficiency * (float)Math.Pow(intakeFanRadius, 3) * (RPM / 60) / 60;
        currentAirFlowPerSecond = currentAirFlowPerFrame * 60; 
    }

    //0-10 JFS kendini çalýþtýrýr
    //10-25 Ana motor %20 RPM'e ulaþýr.
    void CalculateAndApplyResistance()
    {
        resistance = resistanceCurve.Evaluate(RPM);

        if (RPM > 1) RPM -= resistance;
        else RPM = 0;
    }
    bool startSound;
    bool loopSound;
    public void StartEngine(float incomingPressure)
    {
        //Calculate viscosity with temperature
        var temp = ProjectUtilities.CalculateTemparatureAtAltitude(temperatureCelcius, transform.position);
        var viscosityEffect = ProjectUtilities.Map(temp, -60, 100, 0.5f, 2);
        //Calculate RPM with viscosity and pressure dif
        RPM = viscosityEffect * incomingPressure;
        print("Starting RPM: " + RPM + " Viscosity Effect: " + viscosityEffect);
        IsWorking = true;
        if (RPM >= ignitionRPM)
        {
            SoundManager.instance.PlayInPool("JFS Start", transform, 1, 1);
            startSound = true;
            
        }
        else
            SoundManager.instance.PlayInPool("JFS Fail", transform, 1, 1);
    }

    private void FixedUpdate()
    {
        RunEngine();
        CalculateAndApplyResistance();
        SuckAir();
        timeElapsed = Time.time;
        if (!SoundManager.instance.IsClipPlaying("JFS Start") && startSound && !loopSound)
        {
            SoundManager.instance.PlayInPool("JFS Loop", transform, 1, 1, 0, true);
            loopSound = true;
        }
    }

    
}
