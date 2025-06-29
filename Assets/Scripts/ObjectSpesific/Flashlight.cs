using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Flashlight : MonoBehaviour
{
    [SerializeField] Camera povCamera;
    [SerializeField] RectTransform crosshair;
    [SerializeField] Light handlight;
    bool flashlight;
    void HandleLight()
    {
        if (Input.GetKeyDown(KeyCode.K))
        {
            flashlight = !flashlight;
        }

        if (flashlight)
        {
            handlight.enabled = true;
            Ray ray = povCamera.ScreenPointToRay(crosshair.position);
            transform.LookAt(povCamera.transform.position + ray.direction, Vector3.up);
        }
        else
        {
            handlight.enabled = false;
        }
    }
    // Start is called before the first frame update
    void Start()
    {
        handlight = GetComponent<Light>();
    }

    // Update is called once per frame
    void Update()
    {
        HandleLight();
    }
}
