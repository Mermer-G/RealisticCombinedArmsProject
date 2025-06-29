using UnityEngine;

public class ThrottleIndicator : MonoBehaviour
{

    [SerializeField] RectTransform ThrottleArm;

    FlightControlSystem FCS;

    private void Awake()
    {
        FCS = FindAnyObjectByType<FlightControlSystem>();
    }

    private void OnEnable()
    {
        FCS.throttleInput += UpdateArmPosition;
    }

    private void OnDisable()
    {
        FCS.throttleInput -= UpdateArmPosition;
    }

    void UpdateArmPosition(float input)
    {
        float mappedInput = ProjectUtilities.Map(input, 0, 1.25f, 5, 105);
        var position = ThrottleArm.localPosition;
        position.y = Mathf.Lerp(position.y, mappedInput, 0.05f);
        ThrottleArm.localPosition = position;
    }
}
