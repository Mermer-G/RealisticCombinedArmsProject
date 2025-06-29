using UnityEngine;

public class CheatCamera : MonoBehaviour
{
    [SerializeField] GameObject target;

    Camera cam;

    // Start is called before the first frame update
    void Start()
    {
        cam = GetComponent<Camera>();
    }

    // Update is called once per frame
    void Update()
    {
        transform.LookAt(target.transform);

        Zoom();
    }

    int currentZoom = 0;
    void Zoom()
    {
        float zoom = (Input.mouseScrollDelta.y) * cam.fieldOfView;
        if (zoom > 0 && currentZoom + 1 < 6) currentZoom++;
        if (zoom < 0 && currentZoom - 1 > -1) currentZoom--;



        switch (currentZoom)
        {
            case 0:
                cam.focalLength = 60;
                break;
            case 1:
                cam.focalLength = 60 * 5;
                break;
            case 2:
                cam.focalLength = 60 * 10;
                break;
            case 3:
                cam.focalLength = 60 * 25;
                break;
            case 4:
                cam.focalLength = 60 * 50;
                break;
            case 5:
                cam.focalLength = 60 * 100;
                break;
        }

    }
}
