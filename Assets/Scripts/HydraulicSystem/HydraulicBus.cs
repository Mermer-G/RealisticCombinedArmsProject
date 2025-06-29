using Mono.Cecil;
using OpenCover.Framework.Model;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.VersionControl;
using UnityEngine;
using static EnergyBus;

public class HydraulicBus : MonoBehaviour
{
    public static HydraulicBus Instance;
    List<HydraulicSystem> systemList = new List<HydraulicSystem>();
    [SerializeField] float controlInterval;

    private void Awake()
    {
        if (Instance == null) Instance = this;
    }

    // Update is called once per frame
    void Start()
    {
        InvokeRepeating("ManageSystems", 0, controlInterval);
    }

    //1 Bus confirmed application
    //0 Bus rejected application
    //-1 Unusual case happened
    public int ApplyToBus(string SystemId, object applier, float demand = 0)
    {

        if (applier is not HydraulicSystem && applier is not IHydraulicProvider && applier is not IHydraulicConsumer)
        {
            Debug.LogError("The applier has a wrong type! Applier type: " + applier.GetType());
            return -1;
        }

        if (SystemId == "")
        {
            if (applier is HydraulicSystem)
                Debug.LogError("This applier: " + (applier as HydraulicSystem).name + " doesn't have a system ID!");
            else if (applier is IHydraulicProvider)
                Debug.LogError("This applier: " + (applier as IHydraulicProvider).NameH + " doesn't have a system ID!");
            else 
                Debug.LogError("This applier: " + (applier as IHydraulicConsumer).NameH + " doesn't have a system ID!");
            return -1;
        }

        //System should be pre created so we don't create if it is not allready been created.
        HydraulicSystem System = FindSystem(SystemId);


        switch (applier)
        {
            case HydraulicSystem t:
                if (System is null)
                {
                    System = t;
                    systemList.Add(System);
                    print("System named " + System.name + " has been added to the list.");
                    return 1;
                }
                break;

            case IHydraulicProvider p:
                print("Applier: " + p);
                //Check if the provider is not inside the list.
                if (!System.hydraulicProviders.Contains(p))
                {
                    System.hydraulicProviders.Add(p);
                    print("Provider " + p.NameH + " has been added to system: " + System.name);
                }

                if (!p.IsEnabledH)
                {
                    Debug.LogError("Provider was off and still applied to the bus! Name:" + p.NameH);
                    return -1;
                }

                return 1;


            case IHydraulicConsumer c:
                if (System is null)
                {
                    Debug.LogError("System was null!");
                    return -1;
                }
                print("Applier: " + c);
                if (System.hydraulicConsumerList == null) print("List was null!");
                //Check if the consumer is not inside the list.
                if (!System.hydraulicConsumerList.Contains(c)) System.hydraulicConsumerList.Add(c);

                else if (demand != 0)
                {
                    ManageSystems();
                    return 1;
                }

                break;
        }

        return 0;
    }

    float lastInterval = 0;
    void ManageSystems()
    {
        float deltaTime = Time.time - lastInterval;
        lastInterval = Time.time;
        foreach (var system in systemList)
        {
            //PROVIDER MAY BE NULL!
            system.currentProvider = FindBestProvider(system);

            //System must has full fluid amount
            if (system.currentFluidAmount < system.requiredFluidAmount)
            {
                if (system.currentProvider != null && system.currentProvider.IsEnabledH && system.reservoir > 0)
                {
                    var fluidDraw = Mathf.Clamp(system.requiredFluidAmount - system.currentFluidAmount, 0, system.reservoir);
                    system.currentFluidAmount += fluidDraw;
                    system.reservoir -= fluidDraw;
                    print("Fluid drained from reservoir for the system: " + system.name + " amount: " + fluidDraw);
                }
            }

            //Draw the consumers demand first
            foreach (var consumer in system.hydraulicConsumerList)
            {
                if (!consumer.IsActuatedH) continue;
                if (system.currentPressure < consumer.MinPressureH) continue;
                if (system.currentFluidAmount < system.requiredFluidAmount)
                {
                    system.currentPressure = 0;
                    continue;
                }

                system.currentPressure -= consumer.AccumulatedPressureDrawH;
                consumer.ResetAccumulatedDraw();
            }

            //then refill the system with providers
            

            if (system.currentProvider != null && system.currentFluidAmount == system.requiredFluidAmount)
            {
                var generatedPressure = system.currentProvider.GeneratePressure(system.optimalPressure - system.currentPressure);
                system.currentPressure += generatedPressure;
            }

            //Use reliefValve if needed
            if(system.currentPressure > system.optimalPressure)
            {
                Debug.LogWarning("This is not an actual warning. But " + system.SystemId + " has been overpressured.");
                system.currentPressure = system.currentFluidAmount - system.reliefValveRemoveRate;
                system.reservoir += system.reliefValveRemoveRate;
            }

            //Send the remaining pressure data for 2 reasons speed of the consumers will change and damage will occur if exceeds max pressure
            foreach (var consumer in system.hydraulicConsumerList)
            {
                consumer.SendRemainingPressure(system.currentPressure * system.currentFluidAmount / system.requiredFluidAmount);
            }

        }
    }

    private IHydraulicProvider FindBestProvider(HydraulicSystem system)
    {
        //First find the best provider.
        IHydraulicProvider powerProvider = null;

        //List<Order> SortedList = objListOrder.OrderBy(o => o.OrderDate).ToList();
        system.hydraulicProviders = system.hydraulicProviders.OrderBy(p => p.PriorityH).ToList();
        for (int i = 0; i < system.hydraulicProviders.Count; i++)
        {
            if (!system.hydraulicProviders[i].IsEnabledH) continue;
            powerProvider = system.hydraulicProviders[i];
        }

        return powerProvider;
    }

    HydraulicSystem FindSystem(string Id)
    {
        foreach (var s in systemList)
        {
            if (s.SystemId == Id) return s;
        }
        return null;
    }

    
}

public interface IHydraulicConsumer
{
    string NameH { get; }
    bool IsActuatedH { get; }
    float AccumulatedPressureDrawH { get; }
    float MaxPressureH { get; }
    float OptimalPressureH { get; }
    float MinPressureH { get; }
    string SystemIdH { get; }
    void SendRemainingPressure(float pressure);
    void ResetAccumulatedDraw();
}

public interface IHydraulicProvider
{
    string NameH { get; }
    bool IsEnabledH { get; }
    float MaxPressureRateH { get; }
    int PriorityH { get; }
    string SystemIdH { get; }

    float GeneratePressure(float systemPressure);
    void ShutDownH();

}