using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class TextFieldManager : MonoBehaviour
{
    public static TextFieldManager Instance;

    //What this script will do?
    //it will create two types of field: Screen and world space
    //Screen fields will be created and positioned automaticaly. 
    //World space text fields will be positioned everytime they are being called.
    //Both systems will support to timed destruction or immediate destruction.
    //They will be colorable and resizable. But also support default values for it.

    [SerializeField] GameObject textFieldPrefab;
    [SerializeField] Canvas screenCanvas;
    [SerializeField] Canvas worldCanvas;
    [SerializeField] float paragraphSpacing;
    [SerializeField] float leftPadding;
    [SerializeField] float topPadding;


    Dictionary<string, TextField> screenTextFields = new Dictionary<string, TextField>();
    Dictionary<string, TextField> worldTextFields = new Dictionary<string, TextField>();


    // Start is called before the first frame update
    void Awake()
    {
        if (Instance == null) Instance = this;
    }

    private void Start()
    {
        CameraController.instance.cameraChanged += HandleCameraChange;
    }

    private void OnDisable()
    {
        CameraController.instance.cameraChanged -= HandleCameraChange;
    }

    private void Update()
    {
        RepositionScreenFields();
        RotateWorldFields();
    }

    void RepositionScreenFields()
    {
        float left = (-Screen.width / 2) + leftPadding;
        float top = (Screen.height / 2) - topPadding;
        

        foreach (var field in screenTextFields.Values)
        {
            field.parent.localPosition = new Vector3(left, top, 0);
            top -= field.textMP.preferredHeight + paragraphSpacing;
        }
    }

    void RotateWorldFields()
    {
        Vector3 cameraToField;
        foreach (var field in worldTextFields.Values)
        {
            cameraToField = field.parent.position - worldCanvas.worldCamera.transform.position;
            field.parent.rotation = Quaternion.LookRotation(cameraToField, worldCanvas.worldCamera.transform.up);
        }
    }

    //Ýf a new call comes this method should renew the destroy time.
    void RemoveInSeconds(Dictionary<string, TextField> fieldDictionary, string id, float inSeconds)
    {

    }

    public TextFieldBuilder CreateOrUpdateScreenField(string id)
    {
        //Check if the dictionary has the field.
        foreach (var item in screenTextFields)
        {
            if (item.Key == id)
            {
                return new TextFieldBuilder(item.Value);
            }
        }

        //Create a new one if not.
        screenTextFields.Add(id, new TextField());
        print("Created a new field with id: " + id);
        return new TextFieldBuilder(screenTextFields[id], Instantiate(textFieldPrefab, screenCanvas.transform));
    }

    public TextFieldBuilder CreateOrUpdateWorldField(string id)
    {
        //Check if the dictionary has the field.
        foreach (var item in worldTextFields)
        {
            if (item.Key == id)
            {
                return new TextFieldBuilder(item.Value);
            }
        }

        worldTextFields.Add(id, new TextField());
        return new TextFieldBuilder(worldTextFields[id], Instantiate(textFieldPrefab, worldCanvas.transform));
    }

    void HandleCameraChange(Camera camera)
    {
        worldCanvas.worldCamera = camera;
    }

    public void DestroyField(string id)
    {
        foreach (var item in screenTextFields)
        {
            if (item.Key == id)
            {
                screenTextFields.Remove(item.Key);
                Destroy(item.Value.backGround);
                return;
            } 
        }

        foreach (var item in worldTextFields)
        {
            if (item.Key == id)
            {
                worldTextFields.Remove(item.Key);
                Destroy(item.Value.backGround);
                return;
            }
        }

        Debug.LogError("The field with id: " + id + " has not been found in the list and destroyed!");
    }
}

//If some stupid error happens in the future, try making this a class. 
public class TextField
{
    public Transform parent;
    public Image backGround;
    public TextMeshProUGUI textMP;
}

public class TextFieldBuilder
{
    TextField field;

    public TextFieldBuilder(TextField field, GameObject prefab)
    {
        field.parent = prefab.transform;
        field.textMP = prefab.GetComponentInChildren<TextMeshProUGUI>();
        field.backGround = prefab.GetComponentInChildren<Image>();
        this.field = field;
    }

    public TextFieldBuilder(TextField field)
    {
        this.field = field;
    }

    public TextField End()
    {
        return field;
    }

    public TextFieldBuilder Value(string value) { field.textMP.text = value; return this; }

    public TextFieldBuilder WorldPosition(Vector3 position) { field.parent.position = position; return this; }

    public TextFieldBuilder ScreenPosition(Vector2 position) { field.backGround.rectTransform.position = position; return this; }

    public TextFieldBuilder FontColor(Color color) { field.textMP.color = color; return this; }

    public TextFieldBuilder BackgroundColor(Color color) { field.backGround.color = color; return this; }

    public TextFieldBuilder FontSize(float size) { field.textMP.fontSize = size; return this; }

    public TextFieldBuilder Alignment(TextAlignmentOptions options) { field.textMP.alignment = options; return this; }
}