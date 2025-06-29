using System.Collections.Generic;
using UnityEngine;

public class HydraulicSystem : MonoBehaviour
{
    public string SystemId;
    [SerializeField] public IHydraulicProvider currentProvider;
    public List<IHydraulicProvider> hydraulicProviders = new List<IHydraulicProvider>();
    public List<IHydraulicConsumer> hydraulicConsumerList = new List<IHydraulicConsumer>();

    public float optimalPressure;
    public float currentPressure;

    public float requiredFluidAmount;
    public float currentFluidAmount;

    public float reservoir;

    public float reliefValveRemoveRate;

    private void Start()
    {
        HydraulicBus.Instance.ApplyToBus(SystemId, this);
    }


}
