using UnityEngine;

public class CameraShake : MonoBehaviour
{
    Vector3 initialLocalPosition;
    float shakeDuration = 0f;
    float shakeStrength = 0f;
    float shakeFrequency = 25f;
    bool isConstant = false;

    float elapsed = 0f;
    float timeSinceLastShake = 0f;

    void Start()
    {
        initialLocalPosition = transform.localPosition;
    }

    void Update()
    {
        if (shakeDuration > 0 || isConstant)
        {
            elapsed += Time.deltaTime;
            timeSinceLastShake += Time.deltaTime;

            float interval = 1f / shakeFrequency;
            if (timeSinceLastShake >= interval)
            {
                Vector3 randomShake = Random.insideUnitSphere * shakeStrength;
                transform.localPosition = initialLocalPosition + randomShake;
                timeSinceLastShake = 0f;
            }

            if (!isConstant)
            {
                shakeDuration -= Time.deltaTime;

                if (shakeDuration <= 0)
                {
                    StopShake();
                }
            }
        }
    }

    public void ShakeOnce(float strength, float duration, float frequency = 25f)
    {
        shakeStrength = strength;
        shakeDuration = duration;
        shakeFrequency = frequency;
        elapsed = 0f;
        isConstant = false;
    }

    public void StartConstantShake(float strength, float frequency = 25f)
    {
        shakeStrength = strength;
        shakeFrequency = frequency;
        isConstant = true;
    }

    public void StopShake()
    {
        shakeDuration = 0f;
        isConstant = false;
        transform.localPosition = initialLocalPosition;
    }
}