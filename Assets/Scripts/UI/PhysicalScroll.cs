using Microsoft.Unity.VisualStudio.Editor;
using UnityEngine;

public class PhysicalScroll : MonoBehaviour, ISlidable
{
    [SerializeField] float scale;
    [SerializeField] Transform objectToRotate;
    
    [SerializeField] bool animate;

    [SerializeField] float maxValue;
    [SerializeField] float minValue;
    [SerializeField] float value;
    [SerializeField] float currentValue;

    [SerializeField] AnimationCurve valueToAnlge;

    [SerializeField] string slideUpMethod;
    [SerializeField] string slideDownMethod;
    [SerializeField] string selfSettingId;

    private void OnEnable()
    {
        GenericEventManager.Subscribe<float>(selfSettingId, SelfSet);
    }

    private void OnDisable()
    {
        GenericEventManager.Unsubscribe<float>(selfSettingId, SelfSet);
    }

    void SetRotation(float x)
    {
        var rot = valueToAnlge.Evaluate(x);
        objectToRotate.localRotation = Quaternion.Euler(0, 0, rot);
    }

    void SelfSet(float incomingValue)
    {
        value = incomingValue;
        Slide(value);
    }

    private void Update()
    {
        if (currentValue != value)
        {
            currentValue = Mathf.Lerp(currentValue, value, 0.05f);
        }
        if (animate) SetRotation(currentValue);
    }

    public void Slide(float incomingInput)
    {
        value = Mathf.Clamp(value + incomingInput * scale, minValue, maxValue);
        if (incomingInput > 0) SlidableEventHandler.Invoke(slideUpMethod, value);
        else SlidableEventHandler.Invoke(slideDownMethod, value);        
    }
}
