using UnityEngine;

public class F16HydAccumulator : MonoBehaviour, IEnergyConsumer, IHydraulicConsumer
{
    JetFuelStarter JFS;

    [SerializeField] float maxPressurePerUnit = 3000;  // 3000 PSI → Pascal cinsine çevrildi
    [SerializeField] float ACCU1;
    [SerializeField] float ACCU2;



    public string NameE => gameObject.name;
    public bool IsPoweredE => isPoweredE;
    public float PowerConsumptionE => powerConsumptionAtOnceE;
    public string SystemIdE => systemIdE;

    public string NameH => gameObject.name;
    public string SystemIdH => systemIdH;
    public bool IsActuatedH => isPoweredH;
    public float AccumulatedPressureDrawH => accumulatedPressureDrawH;
    public float MaxPressureH => maxPressureH;
    public float OptimalPressureH => optimalPressureH;
    public float MinPressureH => minPressureH;


    [Header("Electric Consumer Values")]
    [SerializeField] bool isPoweredE;
    [SerializeField] string systemIdE;
    [SerializeField] float powerConsumptionAtOnceE;

    [Header("hydraulic Consumer Values")]
    [SerializeField] bool isPoweredH;
    [SerializeField] string systemIdH;
    [SerializeField] float accumulatedPressureDrawH;
    [SerializeField] float maxPressureH;
    [SerializeField] float optimalPressureH;
    [SerializeField] float minPressureH;

    private float Charge(float pressure)
    {
        //if ACCU1 not full
        if (ACCU1 < maxPressurePerUnit)
        {
            //if ACCU1 will not get full
            if (ACCU1 + pressure < maxPressurePerUnit)
            {
                ACCU1 += pressure;
                return 0;
            }
            //if it will overflow
            else
            {
                pressure -= ACCU1 - maxPressurePerUnit;
                ACCU1 = maxPressurePerUnit;
            }
        }

        //if ACCU2 not full
        if (ACCU2 < maxPressurePerUnit)
        {
            //if ACCU2 will not get full
            if (ACCU2 + pressure < maxPressurePerUnit)
            {
                ACCU2 += pressure;
                return 0;
            }
            //if it will overflow
            else
            {
                ACCU2 = maxPressurePerUnit;
                pressure -= ACCU1 - maxPressurePerUnit;
                isPoweredH = false;
            }
        }
        return pressure;
    }


    private void FixedUpdate()
    {
        if (ACCU1 < 3000 || ACCU2 < 3000 && isPoweredH && systemPressure > 0)
        {
            var leftOverPressure = Charge(systemPressure);
            accumulatedPressureDrawH = systemPressure - leftOverPressure;
            systemPressure = leftOverPressure;
        }
    }

    private void OnEnable()
    {
        ClickableEventHandler.Subscribe("JFSStart1", Start1);
        ClickableEventHandler.Subscribe("JFSStart2", Start2);
    }

    private void OnDisable()
    {
        ClickableEventHandler.Unsubscribe("JFSStart1", Start1);
        ClickableEventHandler.Unsubscribe("JFSStart2", Start2);
    }

    void Start1()
    {
        if (EnergyBus.Instance.ApplyToBus(systemIdE, (int)powerConsumptionAtOnceE, null, this) != (int)powerConsumptionAtOnceE) return;
        HydraulicBus.Instance.ApplyToBus(systemIdH, this);
        isPoweredH = true;
        OneOrTwo = StartWith.OneAccumulator;
        DischargeAndStart(JFS);
    }

    void Start2()
    {
        if (EnergyBus.Instance.ApplyToBus(systemIdE, (int)powerConsumptionAtOnceE * 2, null, this) != (int)powerConsumptionAtOnceE * 2) return;
        HydraulicBus.Instance.ApplyToBus(systemIdH, this);
        isPoweredH = true;
        OneOrTwo = StartWith.TwoAccumulator;
        DischargeAndStart(JFS);
    }

    public enum StartWith
    {
        OneAccumulator,
        TwoAccumulator
    }
    [SerializeField] StartWith OneOrTwo;

    public void DischargeAndStart(JetFuelStarter JFS) //,StartWith OneOrTwo)
    {
        float totalPressure = ACCU1;
        ACCU1 = 0;
        if (OneOrTwo == StartWith.TwoAccumulator)
        {
            totalPressure += ACCU2;
            ACCU2 = 0;
        }
        JFS.StartEngine(totalPressure);
        //to keep it not use poweratonce constantly
        isPoweredE = false;
    }

    private void Awake()
    {
        JFS = FindAnyObjectByType<JetFuelStarter>();
    }

    public void ChangePowerStatusE(bool status)
    {
        isPoweredE = status;
    }

    [SerializeField] float systemPressure = 0;
    public void SendRemainingPressure(float pressure)
    {
        systemPressure = pressure;
    }

    public void ResetAccumulatedDraw()
    {
        accumulatedPressureDrawH = 0;
    }
}
