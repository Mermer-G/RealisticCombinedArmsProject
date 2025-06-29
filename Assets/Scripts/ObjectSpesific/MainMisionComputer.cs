using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MainMisionComputer : MonoBehaviour
{

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
        
        ClickableEventHandler.Invoke("EnableFCS");
    }

    void MMCOff()
    {
        
        ClickableEventHandler.Invoke("DisableFCS");
    }

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
