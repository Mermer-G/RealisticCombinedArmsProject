using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class F16HydPump : MonoBehaviour, IHydraulicProvider
{
    public string NameH => gameObject.name;
    public bool IsEnabledH => isEnabled;
    public float MaxPressureRateH => maxPressureRate;
    public int PriorityH => priority;
    public string SystemIdH => systemId;

    [SerializeField] bool isEnabled;
    [SerializeField] float maxPressureRate;
    [SerializeField] int priority;
    [SerializeField] string systemId;

    float engineRPM = 0;
    bool hasApplied;
    private void OnEnable()
    {
        GenericEventManager.Subscribe<float>("F16_1RPM", GetRPM);
    }

    private void OnDisable()
    {
        GenericEventManager.Unsubscribe<float>("F16_1RPM", GetRPM);
    }

    private void FixedUpdate()
    {
        if (engineRPM > 3000)
        {
            if (!IsEnabledH)
            {
                isEnabled = true;
                HydraulicBus.Instance.ApplyToBus(systemId, this);
            }
        }
        else isEnabled = false;
    }

    void GetRPM(float RPM)
    {
        engineRPM = RPM;
    }

    public float GeneratePressure(float requestedPressure)
    {
        var topCap = Mathf.Clamp(maxPressureRate * engineRPM / 6000, 0, maxPressureRate);
        var generated = Mathf.Clamp(topCap, 0, requestedPressure);
        return generated;
    }

    public void ShutDownH()
    {
        isEnabled = false;
    }
}
