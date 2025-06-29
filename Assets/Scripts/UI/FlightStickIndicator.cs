using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

public class FlightStickIndicator : MonoBehaviour
{

    [SerializeField] Image flightStickIndicator;
    [SerializeField] Image zeroInputArea;
    [SerializeField] Image inputIndicator;
    [SerializeField] Image outputIndicator;

    float indicatorLimit;

    float inputIndicatorPitch = 0;
    float inputIndicatorRoll = 0;

    float outputIndicatorPitch = 0;
    float outputIndicatorRoll = 0;

    // Start is called before the first frame update
    void Start()
    {
        indicatorLimit = flightStickIndicator.rectTransform.sizeDelta.x * 0.4f;
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
        FCS.pitchInput += SetInputIndicatorPitch;
        FCS.rollInput += SetInputIndicatorRoll;
        FCS.pitchOutput += SetOutputIndicatorPitch;
        FCS.rollOutput += SetOutputIndicatorRoll;
    }

    private void OnDisable()
    {
        FlightControlSystem FCS = FindAnyObjectByType<FlightControlSystem>();
        if (FCS == null) return;
        FCS.setInputSize -= SetZeroInputSize;
        FCS.setZeroInputAreaSize -= SetZeroInputSize;
        FCS.pitchInput -= SetInputIndicatorPitch;
        FCS.rollInput -= SetInputIndicatorRoll;
        FCS.pitchOutput -= SetOutputIndicatorPitch;
        FCS.rollOutput -= SetOutputIndicatorRoll;
    }

    void SetZeroInputSize(int size)
    {
        zeroInputArea.rectTransform.sizeDelta = new Vector2(size * 2, size * 2);
    }

    void SetInputAreaSize(int size)
    {
        flightStickIndicator.rectTransform.sizeDelta = new Vector2(size * 1.5f, size * 1.5f);
    }
    
    void SetIndicatorPosition()
    {
        inputIndicator.rectTransform.localPosition = new Vector2(inputIndicatorRoll * indicatorLimit, inputIndicatorPitch * indicatorLimit);
        var a = outputIndicator.rectTransform.localPosition;
        outputIndicator.rectTransform.localPosition = new Vector2(Mathf.Lerp(a.x, outputIndicatorRoll * indicatorLimit, 0.02f), Mathf.Lerp(a.y, outputIndicatorPitch * indicatorLimit, 0.02f));
        ClampPointPosition();
    }

    void SetInputIndicatorPitch(float pitch)
    {
        inputIndicatorPitch = pitch;
    }

    void SetInputIndicatorRoll(float roll)
    {
        inputIndicatorRoll = roll;
    }

    void SetOutputIndicatorPitch(float pitch)
    {
        outputIndicatorPitch = pitch;
    }

    void SetOutputIndicatorRoll(float roll)
    {
        outputIndicatorRoll = roll;
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
