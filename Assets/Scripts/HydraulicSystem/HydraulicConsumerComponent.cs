using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HydraulicConsumerComponent : MonoBehaviour, IHydraulicConsumer
{
    public string NameH => gameObject.name;
    public string SystemIdH
    {
        get { return systemIdH; }
        set { systemIdH = value; }
    }
    public bool IsActuatedH 
    {
        get { return isPoweredH; }
        set { isPoweredH = value; }
    }
    public float AccumulatedPressureDrawH
    {
        get { return accumulatedPressureDrawH; }
        set { accumulatedPressureDrawH = value; }
    }
    public float MaxPressureH => maxPressureH;
    public float OptimalPressureH => optimalPressureH;
    public float MinPressureH => minPressureH;
    public float SystemPressureH => systemPressure;

    [Header("hydraulic Consumer Values")]
    [SerializeField] bool isPoweredH;
    [SerializeField] string systemIdH;
    [SerializeField] float accumulatedPressureDrawH;
    [SerializeField] float maxPressureH;
    [SerializeField] float optimalPressureH;
    [SerializeField] float minPressureH;
    [SerializeField] float systemPressure;

    public void ResetAccumulatedDraw()
    {
        accumulatedPressureDrawH = 0;
    }

    public void SendRemainingPressure(float pressure)
    {
        systemPressure = pressure;
    }

    // Start is called before the first frame update
    void Start()
    {
        isPoweredH = true;
        HydraulicBus.Instance.ApplyToBus(systemIdH, this);
    }

}
