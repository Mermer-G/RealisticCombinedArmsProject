using UnityEngine;
using UnityEngine.Audio;
using static UnityEngine.Rendering.DebugUI.Table;

public class Canopy : MonoBehaviour
{
    [SerializeField] float maxAngle;
    [SerializeField] float minAngle;
    [SerializeField] AudioMixer mixer;

    HydraulicConsumerComponent consumer;

    private void OnEnable()
    {
        ClickableEventHandler.Subscribe("RaiseCanopy", RaiseCanopy);
        ClickableEventHandler.Subscribe("LowerCanopy", LowerCanopy);
        ClickableEventHandler.Subscribe("StopCanopy", StopCanopy);
    }

    private void OnDisable()
    {
        ClickableEventHandler.Unsubscribe("RaiseCanopy", RaiseCanopy);
        ClickableEventHandler.Unsubscribe("LowerCanopy", LowerCanopy);
        ClickableEventHandler.Unsubscribe("StopCanopy", StopCanopy);
    }

    [System.Serializable]
    enum CanopyState
    {
        Stop,
        Lower,
        Raise
    }
    [SerializeField] CanopyState state;

    void LowerCanopy()
    {
        state = CanopyState.Lower;
    }

    void RaiseCanopy()
    {
        state = CanopyState.Raise;
    }

    void StopCanopy()
    {
        state = CanopyState.Stop;
    }

    // Start is called before the first frame update
    void Start()
    {
        consumer = GetComponent<HydraulicConsumerComponent>();
    }
    [SerializeField] float muffling;
    // Update is called once per frame
    void Update()
    {
        float currentAngle = NormalizeAngle(transform.localEulerAngles.x);
        muffling = Mathf.Clamp(ProjectUtilities.Map(currentAngle, 0, -5, 2000, 5000), 2000, 5000);
        mixer.SetFloat("LowPassFreq", muffling);

        switch (state)
        {
            case CanopyState.Stop:
                break;

            case CanopyState.Lower:
                if (consumer.IsActuatedH && consumer.SystemPressureH > consumer.MinPressureH)
                {
                    if (currentAngle >= maxAngle)
                    {
                        transform.localEulerAngles = new Vector3(maxAngle, 0, 0);
                        GenericEventManager.Invoke("CanopySet", 1);
                    }
                    else
                    {
                        float delta = (1f * Time.deltaTime) * 10f;
                        currentAngle = Mathf.MoveTowards(currentAngle, maxAngle, delta);
                        transform.localRotation = Quaternion.Euler(currentAngle, 0, 0);
                        consumer.AccumulatedPressureDrawH += delta;
                    }
                }
                break;

            case CanopyState.Raise:
                if (consumer.IsActuatedH && consumer.SystemPressureH > consumer.MinPressureH)
                {
                    if (currentAngle <= minAngle)
                    {
                        transform.localEulerAngles = new Vector3(minAngle, 0, 0);
                        GenericEventManager.Invoke("CanopySet", 1);
                    }
                    else
                    {
                        float delta = (1f * Time.deltaTime) * 10f;
                        currentAngle = Mathf.MoveTowards(currentAngle, minAngle, delta);
                        transform.localRotation = Quaternion.Euler(currentAngle, 0, 0);
                        consumer.AccumulatedPressureDrawH += delta;
                    }
                }
                break;
        }
    }

    float NormalizeAngle(float angle)
    {
        angle = angle % 360f;
        if (angle > 180f) angle -= 360f;
        return angle;
    }
}
