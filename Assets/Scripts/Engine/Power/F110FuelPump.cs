using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class F110FuelPump : MonoBehaviour, IFuelProvider
{
    public IFuelTank FuelTank => tank;
    public IFuel fuelType => tank.FuelType;
    public bool EnableFlow { get => enableFlow; set => enableFlow = value; }
    public float FlowRate { get => flowRate; }


    [SerializeField] TempFuelTank tank;
    [SerializeField] bool enableFlow;
    [SerializeField] float flowRate;
    [SerializeField] float maxFlowRate;
    public float PumpFuel(float incomingFlowRate)//float thrustInput)
    {
        flowRate = incomingFlowRate; //ProjectUtilities.Map(thrustInput, 0, 12000, 0, 1500);
        flowRate = Mathf.Clamp(flowRate, 0, maxFlowRate);
        var flowRatePerFrame = flowRate / 3600;
        FuelTank.FuelAmount -= flowRatePerFrame;
        return flowRatePerFrame;
    }
}
