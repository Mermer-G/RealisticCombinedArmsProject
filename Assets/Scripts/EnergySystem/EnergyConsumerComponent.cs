using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnergyConsumerComponent : MonoBehaviour, IEnergyConsumer
{
    public string NameE => gameObject.name;
    /// <summary>
    /// If you want to change the status, use ChangePowerStatusE()
    /// </summary>
    public bool IsPoweredE => isPoweredE;
    public float PowerConsumptionE { get { return powerConsumptionE; } set { powerConsumptionE = value; } }
    public string SystemIdE  { get { return systemIdE; } set { systemIdE = value; } }

    [Header("Energy Consumer Values")]
    [SerializeField] string systemIdE;
    [SerializeField] bool isPoweredE;
    [SerializeField] float powerConsumptionE;
    [SerializeField] int requestedPowerToEnable;
    [SerializeField] bool applyOnStart;

    public void ChangePowerStatusE(bool status)
    {
        isPoweredE = status;
        if (isPoweredE) EnergyBus.Instance.ApplyToBus(systemIdE, requestedPowerToEnable, null, this);
    }

    // Start is called before the first frame update
    void Start()
    {
        if (applyOnStart) EnergyBus.Instance.ApplyToBus(systemIdE, requestedPowerToEnable, null, this);
    }
}
