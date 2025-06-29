using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class JFSFuelPump : MonoBehaviour, IFuelProvider
{
    public IFuelTank FuelTank => tank;
    public IFuel fuelType => tank.FuelType;
    public bool EnableFlow { get => enableFlow; set => enableFlow = value; }
    public float FlowRate { get => flowRate; }


    [SerializeField] TempFuelTank tank;
    [SerializeField] bool enableFlow;
    [SerializeField] float flowRate;
    [SerializeField] float maxFlowRate;
    //[SerializeField] float minRequiredEnergy;
    //How much resistance the pmup will create for the JFS
    //[SerializeField] float resistanceMultiplier;

    public float PumpFuel(float JFSRPM)
    {
        flowRate = ProjectUtilities.Map(JFSRPM, 0, 3542, 0, 0.1f);
        flowRate = Mathf.Clamp(flowRate, 0, maxFlowRate);
        var flowRatePerFrame = flowRate / 60;
        FuelTank.FuelAmount -= flowRatePerFrame;
        return flowRatePerFrame;
    }
}

