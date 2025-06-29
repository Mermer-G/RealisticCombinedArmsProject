using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.VirtualTexturing;

public class F16MainGen : MonoBehaviour, IEnergyProvider
{
    public string NameE => gameObject.name;
    public bool IsEnabledE => isEnabled;
    public float MaxPowerOutputE => maxPowerOutput;
    public float CurrentEnergyE => currentEnergy;
    public int PriorityE => priority;
    public string SystemIdE => systemId;

    [SerializeField] bool isEnabled;
    [SerializeField] float maxPowerOutput;

    [SerializeField] float currentEnergy;

    [SerializeField] int priority;
    [SerializeField] string systemId;

    bool isEnginePowering = false;
    bool isModePermitting = false;
    public void ShutDownE()
    {
        isEnabled = false;
    }

    public float SupplyPowerE(float totalRequestedPower)
    {
        currentEnergy = maxPowerOutput;
        return float.PositiveInfinity;
    }

    void PowerModeOff()
    {
        ShutDownE();
        isModePermitting = false;
    }

    void PowerModeBattery()
    {
        ShutDownE();
        isModePermitting = false;
    }

    void PowerModeMainPower()
    {
        isModePermitting = true;
    }

    void ApplyStatus()
    {
        if (!isModePermitting) return;
        if (!isEnginePowering)
        {
            isEnabled = false;
            return;
        }
        isEnabled = true;
        if (EnergyBus.Instance == null) print("That damn instance is null!");
        EnergyBus.Instance.ApplyToBus(systemId, 0, this);
    }

    void SetEnginepowering(bool value)
    {
        isEnginePowering = value;
        ApplyStatus();
    }

    private void OnEnable()
    {
        ClickableEventHandler.Subscribe("PowerModeBattery", PowerModeBattery);
        ClickableEventHandler.Subscribe("PowerModeMainPower", PowerModeMainPower);
        ClickableEventHandler.Subscribe("PowerModeOff", PowerModeOff);
        GenericEventManager.Subscribe<bool>("SetMainGenStatus", SetEnginepowering);
    }

    private void OnDisable()
    {
        ClickableEventHandler.Unsubscribe("PowerModeBattery", PowerModeBattery);
        ClickableEventHandler.Unsubscribe("PowerModeMainPower", PowerModeMainPower);
        ClickableEventHandler.Unsubscribe("PowerModeOff", PowerModeOff);
        GenericEventManager.Unsubscribe<bool>("SetMainGenStatus", SetEnginepowering);
    }
}
