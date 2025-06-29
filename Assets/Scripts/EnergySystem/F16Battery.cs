using UnityEngine;

public class F16Battery : MonoBehaviour, IEnergyProvider
{
    public string NameE => gameObject.name;
    public bool IsEnabledE => isEnabled;
    public float MaxPowerOutputE => maxPowerOutput;
    public float CurrentEnergyE => currentEnergy;
    public int PriorityE => priority;
    public string SystemIdE => systemId;

    [SerializeField] bool isEnabled;
    [SerializeField] float maxPowerOutput;

    [SerializeField] float maxPowerStorage;
    [SerializeField] float currentEnergy;

    [SerializeField] int priority;
    [SerializeField] string systemId;

    [SerializeField] float lastSupplyTime = -1f;  // En son SupplyPower çağrılma zamanı
    [SerializeField] bool isSoundPlaying = false;
    void Enable()
    {
        if (currentEnergy <= 0) return;
        isEnabled = true;
        if (EnergyBus.Instance == null) print("That damn instance is null!");
        EnergyBus.Instance.ApplyToBus(systemId, 0, (IEnergyProvider)this);
    }

    private void OnEnable()
    {
        ClickableEventHandler.Subscribe("PowerModeBattery", PowerModeBattery);
        ClickableEventHandler.Subscribe("PowerModeMainPower", PowerModeMainPower);
        ClickableEventHandler.Subscribe("PowerModeOff", PowerModeOff);
    }

    private void OnDisable()
    {
        ClickableEventHandler.Unsubscribe("PowerModeBattery", PowerModeBattery);
        ClickableEventHandler.Unsubscribe("PowerModeMainPower", PowerModeMainPower);
        ClickableEventHandler.Unsubscribe("PowerModeOff", PowerModeOff);
    }

    private void Update()
    {
        // Eğer batarya sesi çalıyorsa ama 2 saniyedir SupplyPower çağrılmadıysa sesi durdur
        if (isSoundPlaying && (Time.time - lastSupplyTime > 2f))
        {
            StopBatterySound();
        }
    }

    void PowerModeOff()
    {
        ShutDownE();
        SoundManager.instance.StopInPool("Battery Loop");
        StopBatterySound();
    }

    void PowerModeBattery()
    {
        Enable();
    }

    void PowerModeMainPower()
    {
        Enable();
    }

    public void ShutDownE()
    {
        isEnabled = false;
    }

    

    private void StartBatterySound()
    {
        SoundManager.instance.PlayInPool("Battery Loop", transform, 0.2f, 1, 0, true);  // Looplu çal
        isSoundPlaying = true;
    }

    private void StopBatterySound()
    {
        SoundManager.instance.StopInPool("Battery Loop");
        isSoundPlaying = false;
    }

    public float SupplyPowerE(float totalRequestedPower)
    {
        currentEnergy -= totalRequestedPower;
        lastSupplyTime = Time.time;  // Batarya şu anda aktif, zamanını güncelle

        // Ses henüz çalmıyorsa ➔ başlat
        if (!isSoundPlaying)
        {
            StartBatterySound();
        }

        return currentEnergy;
    }

    // Start is called before the first frame update
    void Start()
    {
        currentEnergy = maxPowerStorage;
    }

    
}
