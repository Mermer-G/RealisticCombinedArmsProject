using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//[ExecuteAlways]
public class ControlSurface : MonoBehaviour
{
    HydraulicConsumerComponent consumer;

    public float input;
    [SerializeField] float affectOnX;
    [SerializeField] bool symetrizeOnX;
    [SerializeField] float affectOnY;
    [SerializeField] bool symetrizeOnY;
    [SerializeField] float affectOnZ;
    [SerializeField] bool symetrizeOnZ;

    FlightControlSystem FCS;

    [SerializeField] bool pitch;
    [SerializeField] bool roll;
    [SerializeField] bool flap;
    [SerializeField] bool inverseFlap;
    [SerializeField] bool yaw;
    [SerializeField] bool slat;

    private void Awake()
    {
        consumer = GetComponent<HydraulicConsumerComponent>();
        if (consumer == null)
            Debug.LogError("Control Surfaces Require a Hydraulic Consumer Component!");
    }

    private void OnEnable()
    {
        if (FCS == null && transform.root.TryGetComponent(out FCS))
        {
            if (pitch) FCS.pitchOutput += SetInput;

            else if (flap && roll) FCS.flapAndRollOutput += SetInputFlapAndRoll;
            
            else if (roll) FCS.rollOutput += SetInput;

            else if (yaw) FCS.yawOutput += SetInput;

            else if (flap) FCS.flapOutput += SetInput;

            else if (slat) FCS.slatOutput += SetInput;
        }
    }

    private void OnDisable()
    {
        if (FCS == null && transform.root.TryGetComponent(out FCS))
        {
            if (pitch) FCS.pitchOutput -= SetInput;

            else if (flap && roll) FCS.flapAndRollOutput -= SetInputFlapAndRoll;
            
            else if (roll) FCS.rollOutput -= SetInput;

            else if (yaw) FCS.yawOutput -= SetInput;

            else if (flap) FCS.flapOutput -= SetInput;

            else if (slat) FCS.slatOutput -= SetInput;
        }
    }
    // Update is called once per frame
    void Update()
    {
        SetAngles();
    }

    void SetInput(float inputFromFCS)
    {
        input = inputFromFCS;
    }

    void SetInputFlapAndRoll(float flapInputFromFCS, float rollInputFromFCS)
    {
        if (!inverseFlap) input = Mathf.Clamp(rollInputFromFCS + flapInputFromFCS, -1, 1);
        else input = Mathf.Clamp(rollInputFromFCS - flapInputFromFCS, -1, 1);
    }

    void SetAngles()
    {
        if (!consumer.IsActuatedH) return;
        if (consumer.SystemPressureH < consumer.MinPressureH) return;

        var speed = Mathf.Clamp01(ProjectUtilities.Map(consumer.SystemPressureH, consumer.MinPressureH, consumer.OptimalPressureH, 0.2f, 1));
        Vector3 angles = new Vector3(input * affectOnX, input * affectOnY, input * affectOnZ);
        if (symetrizeOnX) angles.x = Mathf.Abs(angles.x);
        if (symetrizeOnY) angles.y = Mathf.Abs(angles.y);
        if (symetrizeOnZ) angles.z = Mathf.Abs(angles.z);
        var anglesBefore = transform.localEulerAngles.normalized;
        transform.localRotation = Quaternion.Lerp(transform.localRotation, Quaternion.Euler(angles), 0.01f * speed);
        var pressureDraw = Vector3.Distance(anglesBefore, transform.localEulerAngles.normalized) * 5;
        consumer.AccumulatedPressureDrawH += pressureDraw;
    }
}
