using UnityEngine;

public class Crosshair : MonoBehaviour
{
    RectTransform crosshair;
    [SerializeField] LayerMask layerMask;

    // Start is called before the first frame update
    void Awake()
    {
        crosshair = GetComponent<RectTransform>();
        
    }


    // Update is called once per frame
    void Update()
    {
        HandleClick();
        HandleMovement();
    }

    

    ISlidable slider = null;
    void HandleClick()
    {
        if (!Input.GetKey(KeyCode.LeftAlt)) return;

        var a = CameraController.instance;
        var camera = a.customCameras[a.activeCameraIndex].thisCamera;

        if (Input.GetMouseButtonDown(0))
        {
            // Crosshair pozisyonundan ray at
            Ray ray = camera.ScreenPointToRay(crosshair.position);

           
            if (Physics.Raycast(ray, out RaycastHit hit, 1f, layerMask))
            {
                var button = hit.collider.GetComponent<IClickable>();
                if (button != null)
                {
                    button.LeftClick();
                }
            }
        }

        if (Input.GetMouseButtonDown(1))
        {
            // Crosshair pozisyonundan ray at
            Ray ray = camera.ScreenPointToRay(crosshair.position);

            if (Physics.Raycast(ray, out RaycastHit hit, 1f, layerMask))
            {
                var button = hit.collider.GetComponent<IClickable>();
                if (button != null)
                {
                    button.RightClick();
                }
            }
        }


        if (Input.GetMouseButtonDown(0))
        {
            // Crosshair pozisyonundan ray at
            Ray ray = camera.ScreenPointToRay(crosshair.position);

            if (Physics.Raycast(ray, out RaycastHit hit, 1f, layerMask))
            {
                slider = hit.collider.GetComponent<ISlidable>();
                
            }
        }
        if (Input.GetMouseButtonUp(0)) slider = null;

        if (slider != null)
        {
            float mouseX = Input.GetAxis("Mouse X");
            float mouseY = Input.GetAxis("Mouse Y");
            var delta = mouseX + mouseY;
            slider.Slide(delta);
        }

    }

    void HandleMovement()
    {
        if (Input.GetKey(KeyCode.LeftAlt))
        {

            // Mouse delta'yı al
            float mouseX = Input.GetAxis("Mouse X") * 10f; // Hız çarpanı
            float mouseY = Input.GetAxis("Mouse Y") * 10f;

            var crosshairPos = crosshair.position;

            crosshairPos += new Vector3(mouseX, mouseY, 0);

            // Clamp (ekrandan çıkmasın)
            crosshairPos.x = Mathf.Clamp(crosshairPos.x, 0, Screen.width);
            crosshairPos.y = Mathf.Clamp(crosshairPos.y, 0, Screen.height);

            // Crosshair UI pozisyonunu güncelle
            crosshair.position = crosshairPos;
        }
        else
        {
            Cursor.lockState = CursorLockMode.Confined;
            crosshair.position = new Vector2(Screen.width / 2, Screen.height / 2);
            Cursor.visible = false;
            // 2️⃣ Kamera hareket kodunu aktif et
            // cameraLookEnabled = true;
        }
    }

}

public interface IClickable
{
    public void LeftClick();
    public void RightClick();
}

public interface ISlidable
{
    public void Slide(float x);
}
