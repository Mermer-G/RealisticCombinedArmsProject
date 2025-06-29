using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FTITGauge : MonoBehaviour
{
    [SerializeField] AnimationCurve RPMAngle;
    [SerializeField] Transform arrow;
    float sentFTIT;
    float FTIT;

    private void OnEnable()
    {
        GenericEventManager.Subscribe<float>("1-FTIT", GetFTIT);
    }

    private void OnDisable()
    {
        GenericEventManager.Unsubscribe<float>("1-FTIT", GetFTIT);
    }

    void GetFTIT(float value)
    {
        sentFTIT = value;
    }

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        /*if (sentFTIT > FTIT) */FTIT = Mathf.Lerp(FTIT, sentFTIT, 0.005f);
        //else FTIT = Mathf.Lerp(FTIT, sentFTIT, 0.001f);
        var angle = RPMAngle.Evaluate(FTIT);
        arrow.localRotation = Quaternion.Euler(0, 0, angle);
    }
}
