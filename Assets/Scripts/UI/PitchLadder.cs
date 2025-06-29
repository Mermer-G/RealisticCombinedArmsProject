using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

public class PitchLadder : MonoBehaviour
{
    [Header("References")]
    [SerializeField] GameObject aircraft;
    [SerializeField] Camera theCamera;
    [SerializeField] GameObject parentObject;
    [SerializeField] GameObject horizonLine;
    [SerializeField] GameObject PositivePicthStick;
    [SerializeField] GameObject NegativePitchStick;

    [SerializeField] GameObject[] horizonLines = new GameObject[2];
    [SerializeField] GameObject[] positiveSticks;
    [SerializeField] GameObject[] negativeSticks;

    [Header("Listener Properties")]
    [SerializeField] Canvas canvas;
    [SerializeField] Camera mainCamera;


    //find an alternative
    Rigidbody rb;
    void Awake()
    {
        rb = aircraft.GetComponent<Rigidbody>();
        pointer = Vector3.ProjectOnPlane(aircraft.transform.forward, Vector3.up);
    }
    //bool flipped = false;
    Vector3 lastfwd = Vector3.zero;
    Vector3 pointer = Vector3.zero;
    void Update()
    {
        SetPoints();
        CalculateRoll();

        var fwd = Vector3.ProjectOnPlane(aircraft.transform.forward, Vector3.up);
        var angle = Vector3.SignedAngle(lastfwd, fwd, Vector3.up);
        lastfwd = fwd;
        //print("Angle: " + angle);

        pointer = Quaternion.AngleAxis(angle, Vector3.up) * pointer;


        // Uçağın forward vektörü
        Vector3 forward = aircraft.transform.forward;

        // Forward'ı world up eksenine projekte et (yani world Y ekseniyle açısını ölçüyoruz)
        Vector3 projected = Vector3.ProjectOnPlane(forward, Vector3.right); // X eksenine göre düzleştiriyoruz (sadece YZ düzleminde ölçüyoruz)

        // Şimdi açıyı atan2 ile bulalım
        float angleRad = Mathf.Atan2(projected.y, projected.z);
        float angleDeg = angleRad * Mathf.Rad2Deg;

        // Şu an açı -180 ile 180 arası olur, biz 0-180 arası istiyoruz
        if (angleDeg < 0)
            angleDeg += 360f;

        //Debug.Log("Gerçek pitch açısı: " + angleDeg);

        

        //Debug.DrawLine(aircraft.transform.position, aircraft.transform.position + pointer * 10, Color.magenta, 0.001f);
    }

    void SetCircleOfPitch(Vector3 horizon)
    {

    }
    bool axisBoreSight = true;
    void SetPoints()
    {
        //MainAxis
        Vector3 axis;
        if (!axisBoreSight)
            axis = Vector3.ProjectOnPlane(rb.velocity.normalized * 100, Vector3.up).normalized;
        else
            axis= Vector3.ProjectOnPlane(aircraft.transform.forward * 100, Vector3.up).normalized;

        Debug.DrawLine(aircraft.transform.position, aircraft.transform.position + axis, Color.green, 0.001f);

        // Horizon Lines
        var mainPoint = theCamera.transform.position + axis * 10;
        Vector3EventManager.Invoke("1-HorizonLine", mainPoint);



        Vector3 start = axis; // Y eksenine paralel (x eksenine bakıyor)
        Vector3 target = Vector3.up;   // Y eksenine dik (yukarı bakıyor)

        float angleStep = 5f * Mathf.Deg2Rad; // 5 dereceyi radyana çeviriyoruz
        int steps = Mathf.CeilToInt(90f / 5f); // 90 dereceyi 5 derece adımlarla böleceğiz

        for (int i = 1; i < positiveSticks.Length; i++)
        {
            float stepAngle = i * angleStep;
            Vector3 rotated = Vector3.RotateTowards(start, target, stepAngle, 0f);

            // rotated vektör burada yeni noktan
            //Debug.DrawLine(theCamera.transform.position, theCamera.transform.position + rotated * 100, Color.red, 0.001f);

            var point = theCamera.transform.position + rotated.normalized * 2;

            //if (positiveSticks.Length > i)
            if (positiveSticks[i] != null )
            {
                Vector3EventManager.Invoke("1-Positive" + ((i) * 5).ToString(), point);
            }
        }
        angleStep = -5f * Mathf.Deg2Rad; // -5 dereceyi radyana çeviriyoruz
        target = Vector3.up;   // Y eksenine dik (yukarı bakıyor)
        for (int i = 1; i < negativeSticks.Length; i++)
        {
            float stepAngle = i * angleStep;
            Vector3 rotated = Vector3.RotateTowards(start, target, stepAngle, 0f);

            // rotated vektör burada yeni noktan
            //Debug.DrawLine(theCamera.transform.position, theCamera.transform.position + rotated * 100, Color.red, 0.001f);

            var point = theCamera.transform.position + rotated.normalized * 2;

            //if (positiveSticks.Length > i)
            if (negativeSticks[i] != null)
            {
                Vector3EventManager.Invoke("1-Negative" + ((i) * 5).ToString(), point);
            }
        }
    }



    void CalculateRoll()
    {
        // Get pitch & roll
        float pitch = aircraft.transform.eulerAngles.x;
        float roll = aircraft.transform.eulerAngles.z;

        if (pitch > 180f) pitch -= 360f;
        pitch = -pitch;
        if (roll > 180f) roll -= 360f;
        roll = -roll;
        //print("Pitch: " + pitch + " Roll: " + roll);

        foreach (var line in horizonLines) 
        {
            line.GetComponent<RectTransform>().rotation = Quaternion.LookRotation(aircraft.transform.forward, Vector3.up);
        }

        foreach (var line in positiveSticks)
        {
            if (line != null)
            line.GetComponent<RectTransform>().rotation = Quaternion.LookRotation(aircraft.transform.forward, Vector3.up);
        }

        foreach (var line in negativeSticks)
        {
            if (line != null)
                line.GetComponent<RectTransform>().rotation = Quaternion.LookRotation(aircraft.transform.forward, Vector3.up);
        }

    }


    
}
