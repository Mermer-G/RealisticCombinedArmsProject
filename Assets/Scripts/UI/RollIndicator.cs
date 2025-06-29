using UnityEngine;

public class RollIndicator : MonoBehaviour
{
    [SerializeField] GameObject aircraft;
    [SerializeField] RectTransform pivot;

    

    // Update is called once per frame
    void Update()
    {
        CalculateRoll();
    }

    void CalculateRoll()
    {
        float roll = aircraft.transform.eulerAngles.z;
        if (roll > 180f) roll -= 360f;
        roll = -roll;

        var rot = Quaternion.LookRotation(aircraft.transform.forward, Vector3.up);
        //rot.z = roll;
        if (roll > 45) 
        {
            var a = Vector3.RotateTowards(aircraft.transform.up, -aircraft.transform.right, 45 * Mathf.Deg2Rad, 0);
            rot = Quaternion.LookRotation(aircraft.transform.forward, a);
        }
        if (roll < -45)
        {
            var a = Vector3.RotateTowards(aircraft.transform.up, aircraft.transform.right, 45 * Mathf.Deg2Rad, 0);
            rot = Quaternion.LookRotation(aircraft.transform.forward, a);
        }
        pivot.rotation = rot;
    }
}
