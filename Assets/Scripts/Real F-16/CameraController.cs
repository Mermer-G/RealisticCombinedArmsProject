using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;


public class CameraController : MonoBehaviour
{
    public static CameraController instance;
    public CustomCamera[] customCameras;
    public int activeCameraIndex;
    public int lastPrimaryCameraIndex;

    public Action<Camera> cameraChanged;

    Vector2 cameraInput = Vector2.zero;

    [SerializeField] float zoomSensivity;

    [SerializeField] AnimationCurve depthOfFieldCurve;
    [SerializeField] float animationTime;
    [SerializeField] VolumeProfile globalVolumeProfile;
    DepthOfField depthOfField;

    [SerializeField] Transform target;

    bool control = true;

    private void Awake()
    {
        if (instance == null) instance = this;
    }

    private void Start()
    {
        if (instance == null) instance = this;
        for (int i = 0; i < customCameras.Length; i++)
        {
            if (i == activeCameraIndex)
            {
                customCameras[i].thisCamera.enabled = true;
            }
            else
            {
                customCameras[i].thisCamera.enabled = false;
            }

        }
        depthOfField = globalVolumeProfile.components[1] as DepthOfField;
        StartCoroutine(TransitionEffect(animationTime));
        cameraChanged?.Invoke(customCameras[activeCameraIndex].thisCamera);
    }

    bool secondaryCommandFlag = false;
    // Update is called once per frame
    void Update()
    {

        TextFieldManager.Instance.CreateOrUpdateScreenField("SecComFlag").Value(secondaryCommandFlag.ToString()).End();
        if (Input.GetKey(KeyCode.V) && !secondaryCommandFlag)
        {
            //Command for switching to free camera
            if (Input.GetKey(KeyCode.Alpha1))
            {
                if (customCameras[activeCameraIndex].cameraType == CustomCamera.CameraType.Free) return;
                
                secondaryCommandFlag = true;
                int index = activeCameraIndex;
                //We will find the free camera by linear search.
                for (int i = 0;i < customCameras.Length; i++)
                {
                    if (customCameras[i].cameraGroup == CustomCamera.CameraGroup.Secondary && customCameras[i].cameraType == CustomCamera.CameraType.Free) 
                    { 
                        index = i;
                        break;
                    }
                }

                if (index != activeCameraIndex)
                {
                    lastPrimaryCameraIndex = activeCameraIndex;
                    ChangeCamera(index);
                }
                else
                    Debug.LogError("Free camera index could not be found in the list!");
            }
        }

        if (Input.GetKeyUp(KeyCode.V))
        {
            if (secondaryCommandFlag)
            {
                secondaryCommandFlag = false;
                return;
            }

            
            //If the previous camera was primary
            //search the list and find the next primary index.
            if (customCameras[activeCameraIndex].cameraGroup == CustomCamera.CameraGroup.Primary)
            {
                int foundIndex = activeCameraIndex; // 1
                bool jumpToStart = false;
                int start;
                //If it is at the last index start from the first index. 
                if (activeCameraIndex + 1 >= customCameras.Length) start = 0;

                //Can go the first index once.
                else
                {
                    jumpToStart = true; //true
                    start = activeCameraIndex + 1; //2
                } 
                        

                for (int i = start; i < customCameras.Length; i++)
                {
                        
                    //If found break the loop
                    if (customCameras[i].cameraGroup == CustomCamera.CameraGroup.Primary)
                    {
                        foundIndex = i;
                        break;
                    }

                    //If the next camera index is lower than the last go to the beginning once. 
                    if (jumpToStart && i + 1 >= customCameras.Length)
                    {
                        i = -1;
                        jumpToStart = false;
                    }
                }

                if (foundIndex == activeCameraIndex)
                {
                    Debug.LogError("There is only one secondary camera in the list!");
                }
                else ChangeCamera(foundIndex);
            }

            //If the previous camera was secondary
            //return to last primary index
            else
            {
                ChangeCamera(lastPrimaryCameraIndex);
            }
            
            
        }

        //Axis freedom
        if (Input.GetKey(KeyCode.LeftControl) && customCameras[activeCameraIndex].cameraType == CustomCamera.CameraType.TP)
        {
            if (Input.GetKeyDown(KeyCode.F1))
            {
                customCameras[activeCameraIndex].axisFreedom = CustomCamera.AxisFreedom.A;
            }

            if (Input.GetKeyDown(KeyCode.F2))
            {
                customCameras[activeCameraIndex].axisFreedom = CustomCamera.AxisFreedom.B;
            }

            if (Input.GetKeyDown(KeyCode.F3))
            {
                customCameras[activeCameraIndex].axisFreedom = CustomCamera.AxisFreedom.C;
            }

            if (Input.GetKeyDown(KeyCode.F4))
            {
                customCameras[activeCameraIndex].axisFreedom = CustomCamera.AxisFreedom.None;
            }
        }

        //Control
        if (Input.GetKey(KeyCode.Mouse1)) control = false;
        else control = true;

        //Input
        if (control) cameraInput = new Vector2(InputManager.instance.GetInput("CameraHorizontal"), InputManager.instance.GetInput("CameraVertical"));
        else cameraInput = Vector2.zero;

        //Zoom
        customCameras[activeCameraIndex].thisCamera.fieldOfView -= InputManager.instance.GetInput("CameraZoom") * zoomSensivity;
        customCameras[activeCameraIndex].targetObjectSize += InputManager.instance.GetInput("CameraZoom") != 0 ? Mathf.Sign(InputManager.instance.GetInput("CameraZoom")) : 0;
        customCameras[activeCameraIndex].targetObjectSize = Math.Clamp(customCameras[activeCameraIndex].targetObjectSize, 0, 100);
        var a = customCameras[activeCameraIndex];
        customCameras[activeCameraIndex].thisCamera.fieldOfView = Mathf.Clamp(a.thisCamera.fieldOfView, a.minFov, a.maxFov);



        if (Input.GetKey(KeyCode.LeftAlt))
        {
            cameraInput = Vector2.zero;
        }
        

        //Send input
        customCameras[activeCameraIndex].ControlCamera(cameraInput, target);
    }

    IEnumerator TransitionEffect(float duration)
    {
        float time = 0f; // Başlangıç zamanı
        var max = depthOfFieldCurve.keys[depthOfFieldCurve.length - 1].time; // Eğriyi tamamlamak için gereken zaman

        while (time < max)
        {
            float value = depthOfFieldCurve.Evaluate(time); // `time`'a bağlı olarak değeri hesapla
            depthOfField.focalLength.value = value; // Depth of Field focal length'i değiştir
            time += Time.deltaTime / duration; // `time`'ı arttır (yavaşça geçiş yapmak için süreyi normalize et)

            yield return null;
        }

        // Geçiş tamamlandığında (max değeri ulaşıldığında) son durumu ayarla
        depthOfField.focalLength.value = depthOfFieldCurve.Evaluate(max);
    }

    void ChangeCamera(int index)
    {
        //Will set the current camera off and start a new transition effect.
        customCameras[activeCameraIndex].thisCamera.enabled = false;
        StopCoroutine(TransitionEffect(animationTime));
        StartCoroutine(TransitionEffect(animationTime));

        activeCameraIndex = index;

        customCameras[activeCameraIndex].thisCamera.enabled = true;
        cameraChanged.Invoke(customCameras[activeCameraIndex].thisCamera);
    }
}

[Serializable]
public class CustomCamera
{
    public Camera thisCamera;
    public float minFov;
    public float maxFov;
    public GameObject mainParentObject;
    public GameObject AzimuthAxisObject;
    public GameObject ElevationAxisObject;
    public Quaternion AzimuthAxisRelatedToObject;
    public Quaternion ElevationAxisRelatedToObject;
    public float sensivity;
    public CameraType cameraType;
    public AxisFreedom axisFreedom = AxisFreedom.None;
    public CameraGroup cameraGroup;
    public float targetObjectSize;

    //There are two groups primary for tp and fov cameras
    //And all the other ones will be as secondary
    public enum CameraGroup
    {
        Primary,
        Secondary
    }

    Vector3 forwardVector = Vector3.zero;
    public enum CameraType
    {
        POV,
        TP,
        Free
    }

    public enum AxisFreedom
    {
        None,
        A,
        B,
        C
    }

    Vector3 lastForward = Vector3.zero;
    Vector3 lookVector = Vector3.zero;
    public void ControlCamera(Vector2 cameraInput, Transform target = null)
    {


        if (cameraType == CameraType.TP)
        {
            Quaternion b = Quaternion.identity;
            switch (axisFreedom)
            {
                
                case AxisFreedom.None:
                    if (forwardVector != Vector3.zero) forwardVector = Vector3.zero;
                    mainParentObject.transform.rotation = Quaternion.LookRotation(mainParentObject.transform.root.forward, mainParentObject.transform.root.up);
                    break;
                case AxisFreedom.A:
                    if (forwardVector != Vector3.zero) forwardVector = Vector3.zero;
                    if (lookVector == Vector3.zero) lookVector = mainParentObject.transform.root.forward;

                    float angle = -Vector3.Angle(lastForward, mainParentObject.transform.root.forward) / Time.deltaTime;
                    lastForward = mainParentObject.transform.root.forward;

                    Debug.Log("Angle:" + angle);

                    
                    Vector3 rotatedVector = Quaternion.AngleAxis(angle * 2, mainParentObject.transform.root.right) * mainParentObject.transform.root.forward;

                    lookVector = Vector3.Lerp(lookVector, rotatedVector, 0.01f);

                    mainParentObject.transform.rotation = Quaternion.LookRotation(mainParentObject.transform.root.forward, Vector3.up);
                    

                    cameraInput = Vector2.zero;
                    break;
                case AxisFreedom.B:
                    if (forwardVector == Vector3.zero) forwardVector = mainParentObject.transform.root.forward;

                    b = Quaternion.LookRotation(forwardVector, Vector3.up);
                    mainParentObject.transform.rotation = b;

                    break;
                case AxisFreedom.C:
                
                    break;
            }

            cameraInput.y = -cameraInput.y;
        }

        if (cameraType == CameraType.Free && target != null)
        {
            Vector3 t = target.position + target.up + target.forward * 4;
            Vector3 dir = t - thisCamera.transform.position;

            AzimuthAxisObject.transform.rotation = Quaternion.LookRotation(dir, Vector3.up);
            // --- ELEVATION (X ekseninde döner)
            // Elevation objesi azimuth'a bağlıysa (parent-child)
            Vector3 localDir = ElevationAxisObject.transform.parent.InverseTransformDirection(dir);
            float elevationAngle = Mathf.Atan2(localDir.y, new Vector2(localDir.x, localDir.z).magnitude) * Mathf.Rad2Deg;
            ElevationAxisObject.transform.localRotation = Quaternion.Euler(elevationAngle, 0, 0);

            // 2. Sensör yüksekliğini al (Unity'de default = 24mm film → genelde 24mm)
            float sensorHeight = thisCamera.sensorSize.y;

            // 3. Objeye olan mesafe
            float distance = dir.magnitude;

            // 4. Objeye ait yükseklik (örneğin kapsül, karakter vb.)
            float objectHeight = targetObjectSize;

            TextFieldManager.Instance.CreateOrUpdateScreenField("Object Height").Value("Object Height: " + objectHeight);

            // 5. Focal length hesapla (mm cinsinden)
            float focalLength = (sensorHeight * distance) / objectHeight;

            // 6. Clamp opsiyonel (örneğin aşırı zoom’dan kaçınmak için)
            focalLength = Mathf.Clamp(focalLength, 10f, 2000f);

            // 7. Uygula
            thisCamera.focalLength = focalLength;

            cameraInput = Vector2.zero;
        }

        // Azimuth ekseninde dönüşü hesapla
        var azimuthRotation = ApplyValueToAxis(cameraInput.x, AzimuthAxisRelatedToObject);
        AzimuthAxisObject.transform.rotation = AzimuthAxisObject.transform.rotation * azimuthRotation;

        // Elevation ekseninde dönüşü hesapla
        var elevationRotation = ApplyValueToAxis(cameraInput.y, ElevationAxisRelatedToObject);
        ElevationAxisObject.transform.rotation = ElevationAxisObject.transform.rotation * elevationRotation;


        //Constraints for POV
        if (cameraType == CameraType.POV)
        {
            //Azimuth Axis Lock
            var azimuth = AzimuthAxisObject.transform.localRotation.eulerAngles.y;
            if (azimuth > 180) azimuth = azimuth - 360;
            if (azimuth > 150) AzimuthAxisObject.transform.localRotation = Quaternion.Euler(0, 150, 0);
            if (azimuth < -150) AzimuthAxisObject.transform.localRotation = Quaternion.Euler(0, -150, 0);

            //Azimuth Back Looking Lean
            if (azimuth > 90) AzimuthAxisObject.transform.localPosition = Vector3.Lerp(AzimuthAxisObject.transform.localPosition, new Vector3(0, 0, ProjectUtilities.MapWithSign(azimuth, 75, 100, 0, 0.05f)), 0.05f);
            else if (azimuth < -90) AzimuthAxisObject.transform.localPosition = Vector3.Lerp(AzimuthAxisObject.transform.localPosition, new Vector3(0, 0, ProjectUtilities.Map(azimuth, -75, -100, 0, -0.05f)), 0.05f);
            else AzimuthAxisObject.transform.localPosition = Vector3.Lerp(AzimuthAxisObject.transform.localPosition, Vector3.zero, 0.05f);

            //Elevation Axis Lock
            var elevation = ElevationAxisObject.transform.localRotation.eulerAngles.z;
            if (elevation > 180) elevation = elevation - 360;
            if (elevation > 80) ElevationAxisObject.transform.localRotation = Quaternion.Euler(0, 0, 80);
            if (elevation < -80) ElevationAxisObject.transform.localRotation = Quaternion.Euler(0, 0, -80);

            //MFD Looking Lean
            if (elevation > 10)
            {
                ElevationAxisObject.transform.localPosition = Vector3.Lerp(ElevationAxisObject.transform.localPosition,
                new Vector3(Mathf.Clamp(ProjectUtilities.Map(elevation, 10, 20, 0, -0.1f), -0.1f, 0),
                            Mathf.Clamp(ProjectUtilities.Map(elevation, 10, 20, 0, -0.06f), -0.06f, 0), 0), 0.05f);

                if (azimuth > 3 && azimuth < 45) 
                    AzimuthAxisObject.transform.localPosition = Vector3.Lerp(AzimuthAxisObject.transform.localPosition, new Vector3(0, 0, Mathf.Clamp(ProjectUtilities.Map(azimuth, 5, 5.1f, 0, 0.12f), 0, 0.12f)), 0.05f);
                if (azimuth < -3 && azimuth > -45)
                    AzimuthAxisObject.transform.localPosition = Vector3.Lerp(AzimuthAxisObject.transform.localPosition, new Vector3(0, 0, Mathf.Clamp(ProjectUtilities.Map(azimuth, -5, -5.1f, 0, -0.12f), -0.12f, 0)), 0.05f);
            }
            else ElevationAxisObject.transform.localPosition = Vector3.Lerp(ElevationAxisObject.transform.localPosition, Vector3.zero, 0.05f);

        } 

        //Constraints for TP
        if (cameraType == CameraType.TP)
        {
            //Elevation Axis Lock
            var elevation = ElevationAxisObject.transform.localRotation.eulerAngles.x;
            if (elevation > 180) elevation = elevation - 360;
            if (elevation > 80) ElevationAxisObject.transform.localRotation = Quaternion.Euler(80, 0, 0);
            if (elevation < -80) ElevationAxisObject.transform.localRotation = Quaternion.Euler(-80, 0, 0);
        }

    }

    Quaternion ApplyValueToAxis(float baseValue, Quaternion multiplier)
    {
        // Burada dönüşüm için doğru çarpma işlemini yapıyoruz
        Quaternion rotation = Quaternion.Euler(baseValue * multiplier.x * sensivity, baseValue * multiplier.y * sensivity, baseValue * multiplier.z * sensivity);
        return rotation;
    }
}