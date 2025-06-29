using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TempFuelTank : MonoBehaviour, IFuelTank
{
    public IFuel FuelType => fuelType;
    public float MaxFuelAmount => maxFuel;
    public float FuelAmount { get => fuelAmount; set => fuelAmount = value; }

    IFuel fuelType = new JP8();
    [SerializeField] float maxFuel = 1000; 
    [SerializeField] float fuelAmount = 1000;
    [SerializeField] float willBeDepletedInMinutes;
    [SerializeField] float depletedInOneSecond;
    float lastFuelAmount;

    private void Start()
    {
        CalculateDeplateRate();
    }

    void CalculateDeplateRate()
    {
        if (lastFuelAmount == 0)
        {
            lastFuelAmount = fuelAmount;
            Invoke("CalculateDeplateRate", 1);
        }
        else
        {
            depletedInOneSecond = lastFuelAmount - fuelAmount;
            var willBeDepletedInSeconds = fuelAmount / depletedInOneSecond;
            willBeDepletedInMinutes = willBeDepletedInSeconds / 60;
            lastFuelAmount = fuelAmount;
            Invoke("CalculateDeplateRate", 1);
        }
    }
}
