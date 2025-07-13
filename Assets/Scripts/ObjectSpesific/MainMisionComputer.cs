using System.Collections.Generic;
using UnityEngine;

public class MainMisionComputer : MonoBehaviour
{
    [SerializeField] FlightControlSystem FCS;


    [System.Serializable]
    class SensorOfInterest
    {
        public string name;
        public MonoBehaviour Sensor;
    }
    [SerializeField] SensorOfInterest[] sensorOfInterests;
    Dictionary<string, ISensorOfInterest> SOIDic = new();
    SensorOfInterest SOI;
    void ChangeSOI()
    {
        if (InputManager.instance.GetInput("DMSUp").ToBool())
        {

            ((ISensorOfInterest)SOI?.Sensor).UnSetSOI();
            SOI.name = "RightMFD";
            SOI.Sensor = (MonoBehaviour)SOIDic["HUD"];
            ((ISensorOfInterest)SOI.Sensor).SetSOI();
            print("SOI is: " + SOI.name);
        }
        if (InputManager.instance.GetInput("DMSDown").ToBool())
        {
            if (SOI is null)
            {
                SOI = new SensorOfInterest();
                SOI.name = "RightMFD";
                SOI.Sensor = (MonoBehaviour)SOIDic["RightMFD"];
                ((ISensorOfInterest)SOI.Sensor).SetSOI();
                print("SOI is: " + SOI.name);
                return;
            }

            // Unset current
            ((ISensorOfInterest)SOI.Sensor).UnSetSOI();

            if (SOI.name == "RightMFD")
            {
                SOI.name = "LeftMFD";
                SOI.Sensor = (MonoBehaviour)SOIDic["LeftMFD"];
            }
            else
            {
                SOI.name = "RightMFD";
                SOI.Sensor = (MonoBehaviour)SOIDic["RightMFD"];
            }

            ((ISensorOfInterest)SOI.Sensor).SetSOI();
                    print("SOI is now: " + SOI.name);
        }


    }

    private void OnEnable()
    {
        ClickableEventHandler.Subscribe("MMCOn", MMCOn);
        ClickableEventHandler.Subscribe("MMCOff", MMCOff);
    }

    private void OnDisable()
    {
        ClickableEventHandler.Unsubscribe("MMCOn", MMCOn);
        ClickableEventHandler.Unsubscribe("MMCOff", MMCOff);
    }

    void MMCOn()
    {
        //THIS IS TEMPORARAY AND MUST BE CHANGED!
        ClickableEventHandler.Invoke("EnableFCS");
    }

    void MMCOff()
    {
        
        ClickableEventHandler.Invoke("DisableFCS");
    }

    // Start is called before the first frame update
    void Awake()
    {
        foreach (var item in sensorOfInterests)
        {
            SOIDic[item.name] = ((ISensorOfInterest)item.Sensor);
        }
    }

    // Update is called once per frame
    void Update()
    {
        ChangeSOI();
    }
}
