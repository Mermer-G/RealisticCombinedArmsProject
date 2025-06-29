using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NOZPOS_Gauge : MonoBehaviour
{
    [SerializeField] AnimationCurve PosAngle;
    [SerializeField] Transform arrow;
    EnergyConsumerComponent consumer;
    [SerializeField] float sentPos;
    float currentPos;
    [SerializeField] float RPM;

    private void OnEnable()
    {
        GenericEventManager.Subscribe<float>("1-NOZPOS", GetNozPos);
        GenericEventManager.Subscribe<float>("F16_1RPM", getRPM);
    }

    private void OnDisable()
    {
        GenericEventManager.Unsubscribe<float>("1-NOZPOS", GetNozPos);
        GenericEventManager.Unsubscribe<float>("F16_1RPM", getRPM);
    }

    void GetNozPos(float value)
    {
        sentPos = 100 - value * 100;
    }

    void getRPM(float value)
    {
        RPM = value;
    }

    private void Awake()
    {
        consumer = GetComponent<EnergyConsumerComponent>();
    }

    // Update is called once per frame
    void Update()
    {
        if (RPM > 6000 && !consumer.IsPoweredE)
        {
            consumer.ChangePowerStatusE(true);
            EnergyBus.Instance.ApplyToBus(consumer.SystemIdE, 0, null, consumer);
        }

        if (consumer.IsPoweredE)
        {
            currentPos = Mathf.Lerp(currentPos, sentPos, 0.01f);
            var angle = PosAngle.Evaluate(currentPos);
            arrow.localRotation = Quaternion.Euler(0, 0, angle);
        }
        else
        {
            currentPos = Mathf.Lerp(currentPos, 0, 0.01f);
            var angle = PosAngle.Evaluate(currentPos);
            arrow.localRotation = Quaternion.Euler(0, 0, angle);
        }

    }
}
