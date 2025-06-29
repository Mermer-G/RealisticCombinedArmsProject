using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CockpitAnimations : MonoBehaviour
{
    float pitchInput;
    float rollInput;
    float yawInput;

    [SerializeField] Transform flightStick;
    [SerializeField] Transform pedalRight;
    [SerializeField] Transform pedalLeft;

    //Pedal Right Position
    Vector3 pRP;
    //Pedal Left Position
    Vector3 pLP;

    private void Start()
    {
        pLP = pedalLeft.localPosition;
        pRP = pedalRight.localPosition;
    }

    private void Update()
    {
        AnimateCockpitControls();
    }

    private void OnEnable()
    {
        FlightControlSystem FCS = FindAnyObjectByType<FlightControlSystem>();
        FCS.pitchInput += UpdatePitch;
        FCS.rollInput += UpdateRoll;
        FCS.yawInput += UpdateYaw;
    }

    private void OnDisable()
    {
        FlightControlSystem FCS = FindAnyObjectByType<FlightControlSystem>();
        if (FCS == null) return; 
        FCS.pitchInput -= UpdatePitch;
        FCS.rollInput -= UpdateRoll;
        FCS.yawInput -= UpdateYaw;
    }

    void UpdatePitch(float pitch)
    {
        pitchInput = pitch;
    }

    void UpdateRoll(float roll)
    {
        rollInput = roll;
    }

    void UpdateYaw(float yaw)
    {
        yawInput = yaw;
    }

    void AnimateCockpitControls()
    {
        //Stick
        Vector3 flightStickAngles = new Vector3(pitchInput * 8, 0, -rollInput * 8);
        flightStick.localRotation = Quaternion.Euler(flightStickAngles);

        //Pedals
        float pedalMoveLimit = 0.050f;
        
        pedalRight.localPosition = Vector3.Lerp(pedalRight.localPosition, new Vector3(pRP.x, pRP.y, pRP.z + pedalMoveLimit * yawInput), 1);
        pedalLeft.localPosition = Vector3.Lerp(pedalLeft.localPosition, new Vector3(pLP.x, pLP.y, pLP.z - pedalMoveLimit * yawInput), 1);
                
    }
}
