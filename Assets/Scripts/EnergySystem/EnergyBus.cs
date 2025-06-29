using System.Collections.Generic;
using System.Data;
using System.Linq;
using UnityEngine;

public class EnergyBus : MonoBehaviour
{
    public static EnergyBus Instance;
    List<EnergySystem> systemList = new List<EnergySystem>();
    [SerializeField] float controlInterval;

    public class EnergySystem
    {
        public string SystemId;
        public List<IEnergyProvider> energyProviders = new List<IEnergyProvider>();
        public List<IEnergyConsumer> energyConsumerList = new List<IEnergyConsumer>();
        public bool IsPowered;
        public float maxPowerOutput;
        public float currentPowerOutput;
    }

    // Start is called before the first frame update
    void Awake()
    {
        if (Instance == null) Instance = this;
    }

    private void Start()
    {
        InvokeRepeating("ManageSystems", 0, controlInterval);
    }

    //This method will be used when they are first subbing to bus, providing power for a system and also consuming power for a system.
    //They will apply for these things to the bus. And bus will control them by the status of the system.
    //If the system they are trying to sub is not yet created. Create the system and add it to the list.
    //If it is created then use the existing system and move forward.
    
    //The method will be called once when both providers and also consumers aplly when they are enabled.
    //Therefore everthing will be handled by BUS

    //So the bus needs to check the system every fixed intervals. A new method for that is needed.

    //If the applier is a provider that means it wants to provide power to the system.
    //Add it to the provider list.
    //check the existing providers and compare their priority (Higher priority will be used first: Main Gen -> Sec Gen -> EPU -> Battery)
    //Provider finding will start from lower. And t will check if the load available, stored power available etc.
    //Start using the chosen provider. (EPU and Battery will start to deplete themselves when being in use)
    
    //If the applier is a consumer do following checks:
    //  Is the system is powered? (If no turn the consumer off)
    //  Is added power load is inside the bounds of provider? (If no turn the provider off and the system off)
    
    //The provider that is being used will be asked to deplete their power. (If power is infinite provider doesn't need to do anything then)

    //Returns
    //-1 if something went wrong
    //0 if bus rejected request of being enabled
    //1 if bus applied the request
    //requested power if bus applied the power load
    public int ApplyToBus(string Id, int requestedPowerToEnable = 0, IEnergyProvider provider = null, IEnergyConsumer consumer = null)
    {
        if (provider != null && consumer != null)
        {
            Debug.LogError("Applier provided both provider and consumer at the same time for system id: " + Id);
            return -1;
        }

        if (consumer != null)
            print("This object has been applied: " + consumer.NameE);

        EnergySystem System = FindSystem(Id);
        //System has not been found. Create and add the system to the list and then use the list afterwards.
        if (System == null)
        {
            var newSystem = CrateEnergySystem(Id);
            systemList.Add(newSystem);
            System = newSystem;
        }
        //After this point system is created and it is inside the systemlist. So we will be using that.


        //Applier is a provider. So that means a provider is enabled.
        if (provider != null)
        {
            //Check if the provider is not inside the list.
            if (!System.energyProviders.Contains(provider)) System.energyProviders.Add(provider);


            if (!provider.IsEnabledE) 
            {
                Debug.LogError("Provider was off and still applied to the bus! Name:" + provider.NameE);
                return -1;
            }

            //Provider had no energy but has been tried to enable it.
            if (provider.CurrentEnergyE <= 0) 
            {
                Debug.LogError("Shutting down the provider because it has not energy left! THIS SHOULD NOT HAPPEN WHEN APPLYING! Name:" + provider.NameE);
                provider.ShutDownE();
                return -1;
            }
            
            //This provider can't handle the system by its own. But this control shouldn't be in here. It should be in manage systems.
            //if (provider.MaxPowerOutput < System.maxPowerOutput)

            //system has at least one provider so lets enable it. And set the provider as the power source
            if (!System.IsPowered)
            {
                System.IsPowered = true;
                System.maxPowerOutput = provider.MaxPowerOutputE;
            }
        }

        //Applier is a consumer.
        if (consumer != null)
        {
            //Check if the consumer is not inside the list.
            if (!System.energyConsumerList.Contains(consumer)) System.energyConsumerList.Add(consumer);

            //System is not powered.
            if (!System.IsPowered) consumer.ChangePowerStatusE(false);

            else if (requestedPowerToEnable != 0)
            {               
                ManageSystems();
                return DrawPowerAtOnce(Id, requestedPowerToEnable);
            }
        }

        ManageSystems();
        return 1;
    }

    public int DrawPowerAtOnce(string systemId, int requestedPower)
    {
        var system = FindSystem(systemId);

        //System overLoaded
        if (system.currentPowerOutput + requestedPower > system.maxPowerOutput)
        {
            foreach (var provider in system.energyProviders)
            {
                provider.ShutDownE();
            }

            foreach (var consumer in system.energyConsumerList)
            {
                consumer.ChangePowerStatusE(false);
            }

            return 0;
        }
        //Supply requested power
        else
        {
            IEnergyProvider provider = FindBestProvider(system);

            provider.SupplyPowerE(requestedPower);

            return requestedPower;
        }
    }

    float lastInterval = 0;
    void ManageSystems()
    {
        float deltaTime = Time.time - lastInterval;
        lastInterval = Time.time;
        foreach (var system in systemList)
        {
            //If there is no active providers which means system is unpowered.shut everything down.
            //This phase occurs when all providers either failed or depleted.
            if (!system.IsPowered)
            {
                foreach (var provider in system.energyProviders)
                {
                    provider.ShutDownE();
                }
                foreach (var consumer in system.energyConsumerList)
                {
                    consumer.ChangePowerStatusE(false);
                }
                continue;
            }

            //Find the needed power per unit time
            system.currentPowerOutput = 0;
            foreach (var consumer in system.energyConsumerList)
            {
                if (!consumer.IsPoweredE) continue;

                system.currentPowerOutput += consumer.PowerConsumptionE;
            }

            IEnergyProvider powerProvider = FindBestProvider(system);

            //If no suitable provider found. Than power down the system.
            if (powerProvider == null)
            {
                system.IsPowered = false;
                return;
            }

            //Provider has been found. Now the buss will want it to use its power.
            float remainingPower = powerProvider.SupplyPowerE(system.currentPowerOutput * deltaTime);
            //print(system.currentPowerOutput * deltaTime + " power has been drawn from " + powerProvider.Name + " Will be depleted in : " + (remainingPower / (system.currentPowerOutput * deltaTime)).ToString("0Second"));

            //And we will update the power load
            system.maxPowerOutput = powerProvider.MaxPowerOutputE;

            //If provider can't support the power load. Turn it off.
            if (powerProvider.MaxPowerOutputE < system.currentPowerOutput)
                powerProvider.ShutDownE();
        }
    }

    private IEnergyProvider FindBestProvider(EnergySystem system)
    {
        //First find the best provider.
        IEnergyProvider powerProvider = null;

        //List<Order> SortedList = objListOrder.OrderBy(o => o.OrderDate).ToList();
        system.energyProviders = system.energyProviders.OrderBy(p => p.PriorityE).ToList();
        for (int i = 0; i < system.energyProviders.Count; i++)
        {
            if (system.energyProviders[i].CurrentEnergyE <= 0 && system.energyProviders[i].IsEnabledE)
            {
                system.energyProviders[i].ShutDownE();
                continue;
            }
            if (!system.energyProviders[i].IsEnabledE) continue;
            if (system.energyProviders[i].MaxPowerOutputE < system.currentPowerOutput) continue;

            powerProvider = system.energyProviders[i];
        }

        return powerProvider;
    }

    EnergySystem FindSystem(string Id)
    {
        foreach (var s in systemList)
        {
            if (s.SystemId == Id) return s;
        }
        return null;
    }

    EnergySystem CrateEnergySystem(string Id)
    {
        EnergySystem newSystem = new EnergySystem();
        newSystem.SystemId = Id;
        newSystem.IsPowered = false;
        newSystem.maxPowerOutput = 0;
        return newSystem;
    }
}

public interface IEnergyConsumer
{
    string NameE { get; }
    bool IsPoweredE { get; }
    float PowerConsumptionE { get; }
    string SystemIdE { get; }
    void ChangePowerStatusE(bool status);
}

public interface IEnergyProvider
{
    string NameE { get; }
    bool IsEnabledE { get; }
    float MaxPowerOutputE { get; }
    float CurrentEnergyE { get; }
    int PriorityE { get; }
    string SystemIdE { get; }

    float SupplyPowerE(float totalRequestedPower);
    void ShutDownE();
    
}