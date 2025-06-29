using UnityEngine;

public class JFSRUNIndicator : MonoBehaviour
{
    [SerializeField] string MethodId;
    [SerializeField] Light IndicatorLight;

    private void OnEnable()
    {
        GenericEventManager.Subscribe<bool>(MethodId, SetIndicatorLight);
    }

    private void OnDisable()
    {
        GenericEventManager.Unsubscribe<bool>(MethodId, SetIndicatorLight);
    }

    void SetIndicatorLight(bool value)
    {
        IndicatorLight.enabled = value;
    }
}
