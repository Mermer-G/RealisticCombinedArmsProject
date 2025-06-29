using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RPMGauge : MonoBehaviour
{
    [SerializeField] AnimationCurve RPMAngle;
    [SerializeField] Transform arrow;
    float sentRPM;
    float RPMPercent;
    
    private void OnEnable()
    {
        GenericEventManager.Subscribe<float>("F16_1RPM", GetRPM);
    }

    private void OnDisable()
    {
        GenericEventManager.Unsubscribe<float>("F16_1RPM", GetRPM);
    }

    void GetRPM(float RPM)
    {
        sentRPM = RPM;
    }

    // Update is called once per frame
    void Update()
    {
        var percent = (sentRPM / 12000) * 100;
        /*if (percent > RPMPercent)*/ RPMPercent = Mathf.Lerp(RPMPercent, percent, 0.01f);
        //else RPMPercent = Mathf.Lerp(RPMPercent, percent, 0.001f);

        var angle = RPMAngle.Evaluate(RPMPercent);
        arrow.localRotation = Quaternion.Euler(0, 0, angle);
    }
}
