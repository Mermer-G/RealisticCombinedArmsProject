using UnityEditor.ShaderKeywordFilter;
using UnityEngine;

public class QuickStarter : MonoBehaviour
{
    [SerializeField] bool quickStart;
    [SerializeField] int speed;
    [SerializeField] bool retractLandingGears;
    F110Engine F110Engine;
    FlightControlSystem FLCS;



    // Start is called before the first frame update
    void Start()
    {
        FLCS = FindAnyObjectByType<FlightControlSystem>();
    }
    bool one;
    // Update is called once per frame
    void Update()
    {
        if (quickStart)
        {
            GetComponent<Rigidbody>().constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationY | RigidbodyConstraints.FreezeRotationZ;
            F110Engine = FindAnyObjectByType<F110Engine>();
            F110Engine.quickStart = true;
            GenericEventManager.Invoke("JFSStartSet", 2);
            FLCS.F16Input.t = 0.1f;
            GenericEventManager.Invoke("MainPowerSet", 0);
            GenericEventManager.Invoke("CanopySet", 2);
            GenericEventManager.Invoke<float>("1-HMCSAlphaSet", 1);
            GenericEventManager.Invoke<float>("1-HUDAlphaSet", 1);
            GenericEventManager.Invoke<int>("MMCSet", 0);
            if(retractLandingGears) GenericEventManager.Invoke<int>("SetLandingGears", 0);
            GetComponent<Rigidbody>().velocity = transform.forward * speed;
            quickStart = false;
        }
        
        if (Time.time > 3 && !one)
        {
            one = true;
            //GetComponent<Rigidbody>().constraints = RigidbodyConstraints.None;

        }
    }
}
