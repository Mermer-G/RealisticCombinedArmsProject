using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

public class YawIndicator : MonoBehaviour
{
    [SerializeField] Image yawIndicator;
    [SerializeField] Image zeroInputArea;
    [SerializeField] Image inputIndicator;
    [SerializeField] Image outputIndicator;

    float indicatorLimit;

    float inputIndicatorYaw = 0;
    float outputIndicatorYaw = 0;

    // Start is called before the first frame update
    void Start()
    {
        indicatorLimit = yawIndicator.rectTransform.sizeDelta.x * 0.45f;
    }

    // Update is called once per frame
    void Update()
    {
        SetIndicatorPosition();
    }

    private void OnEnable()
    {
        FlightControlSystem FCS = FindAnyObjectByType<FlightControlSystem>();
        if (FCS == null) return;
        FCS.setInputSize += SetZeroInputSize;
        FCS.setZeroInputAreaSize += SetZeroInputSize;
        FCS.yawInput += SetInputIndicatorYaw;
        FCS.yawOutput += SetOutputIndicatorYaw;
    }

    private void OnDisable()
    {
        FlightControlSystem FCS = FindAnyObjectByType<FlightControlSystem>();
        if (FCS == null) return;
        FCS.setInputSize -= SetZeroInputSize;
        FCS.setZeroInputAreaSize -= SetZeroInputSize;
        FCS.yawInput -= SetInputIndicatorYaw;
        FCS.yawOutput -= SetOutputIndicatorYaw;
    }

    void SetZeroInputSize(int size)
    {
        zeroInputArea.rectTransform.sizeDelta = new Vector2(size * 2, 10);
    }

    void SetInputAreaSize(int size)
    {
        yawIndicator.rectTransform.sizeDelta = new Vector2(size * 1.5f, 10);
    }

    void SetIndicatorPosition()
    {
        inputIndicator.rectTransform.localPosition = new Vector2(inputIndicatorYaw * indicatorLimit, 0);
        outputIndicator.rectTransform.localPosition = new Vector2(Mathf.Lerp(outputIndicator.rectTransform.localPosition.x, outputIndicatorYaw * indicatorLimit, 0.01f), 0);
        ClampPointPosition();
    }

    void SetInputIndicatorYaw(float yaw)
    {
        inputIndicatorYaw = yaw;
    }

    void SetOutputIndicatorYaw(float yaw)
    {
        outputIndicatorYaw = yaw;
    }

    void ClampPointPosition()
    {
        Vector2 direction = inputIndicator.rectTransform.localPosition; // Þu anki pozisyonu al

        // Merkeze olan mesafeyi hesapla
        float distance = direction.magnitude;

        Vector2 newPosition = direction;
        // Eðer sýnýrý aþarsa, çemberin sýnýrýna it
        if (distance > indicatorLimit)
        {
            direction = direction.normalized * indicatorLimit; // Vektörü sýnýr noktasýna taþý
            newPosition = direction;
        }

        inputIndicator.rectTransform.localPosition = newPosition; // Güncellenmiþ pozisyonu uygula
    }
}
