using Unity.VisualScripting;
using UnityEditor;
using UnityEditor.TerrainTools;
using UnityEngine;
[ExecuteAlways]
public class Rotor : MonoBehaviour
{
    [SerializeField] GameObject rotorHead;
    [Range(-360, 360)][SerializeField] float rotation;
    [Range(-30, 30)] public float speed;

    public bool enableLimits;
    public float maxLimit;
    public float minLimit;

    [SerializeField] bool drawDownIndicator;
    [SerializeField] float downIndicatorDistance;
    [SerializeField] Color downIndicatorColor;

    [SerializeField] bool drawGravityIndicator;
    [SerializeField] float gravityIndicatorDistance;
    [SerializeField] Color gravityIndicatorColor;

    [SerializeField] bool drawSpeedIndicator;
    [SerializeField] float speedIndicatorDistance;
    [SerializeField] float speedIndicatorSize;
    [SerializeField] Color speedIndicatorColor;




    float prevRotation;
    void Start()
    {
        rotation = Quaternion.Euler(transform.rotation.x, rotation, transform.rotation.z).y;
        prevRotation = rotation;
    }

    
    void FixedUpdate()
    {
        rotation += speed;
        if (enableLimits)
        {
            if (rotation > maxLimit) rotation = maxLimit;
            if (rotation < minLimit) rotation = minLimit;
        }
        else
        {
            if (rotation > 360)
            {
                rotation -= 360;
            }
            if (rotation < -360)
            {
                rotation += 360;
            }
        }


        if (rotation != prevRotation)
        {
            
            rotorHead.transform.localRotation = Quaternion.Euler(0, rotation, 0);
            prevRotation = rotation;
        }
    }

    void OnDrawGizmos()
    {
        if (drawDownIndicator)
        {
            Gizmos.color = downIndicatorColor;
            Gizmos.DrawLine(rotorHead.transform.position, CalculateDown());
            Gizmos.DrawSphere(CalculateDown(), 0.1f);
        }

        if (drawGravityIndicator)
        {
            Gizmos.color = gravityIndicatorColor;
            Gizmos.DrawLine(rotorHead.transform.position, CalculateGravity());
            Gizmos.DrawCube(CalculateGravity(), new Vector3(0.1f, 0.1f, 0.1f));
        }

        if (drawSpeedIndicator)
        {
            Gizmos.color = speedIndicatorColor;
            Vector3[] speedVerticies = CalculateSpeed();

            if (speed == 0)
            {
                Gizmos.DrawSphere(speedVerticies[0], 0.1f);
                Gizmos.DrawLine(rotorHead.transform.position, speedVerticies[0]);
            }
            else
            {
                for (int i = 1; i < speedVerticies.Length; i++)
                {
                    Gizmos.DrawLine(speedVerticies[i], speedVerticies[i - 1]);
                }
            }            
        }

        UnityEditorInternal.InternalEditorUtility.RepaintAllViews();
    }

    Vector3 CalculateDown()
    {
        Vector3 down = rotorHead.transform.TransformPoint(new Vector3(0, 0, 1) * downIndicatorDistance);

        return down;
    }

    Vector3 CalculateGravity()
    {
        Vector3 down = new Vector3(0, -1, 0) * gravityIndicatorDistance + rotorHead.transform.position;
        return down;
    }

    Vector3[] CalculateSpeed()
    {
        Vector3[] verticies = new Vector3[11];
        // Ýlk noktayý setler (obje yönüyle uyumlu)
        Vector3 firstPoint = -Vector3.forward * speedIndicatorDistance;
        verticies[0] = rotorHead.transform.TransformPoint(firstPoint);
        float radian;
        Vector3 point;
        float degree = 0;
        for (int i = 1; i < 10; i++)
        {
            degree += speed * speedIndicatorSize;
            radian = -(degree) * Mathf.Deg2Rad;
            point = new Vector3(speedIndicatorDistance * Mathf.Sin(radian), 0, -speedIndicatorDistance * Mathf.Cos(radian));
            verticies[i] = rotorHead.transform.TransformPoint(point);
        }
        verticies[10] = rotorHead.transform.position;
        return verticies;
    }
}

[CustomEditor(typeof(Rotor))]
public class RotorEditor : Editor
{

    SerializedProperty _rotorHead;
    SerializedProperty _rotation;
    SerializedProperty _speed;

    SerializedProperty _enableLimits;
    SerializedProperty _maxLimit;
    SerializedProperty _minLimit;

    SerializedProperty _drawDownIndicator;
    SerializedProperty _downIndicatorDistance;
    SerializedProperty _downIndicatorColor;

    SerializedProperty _drawGravityIndicator;
    SerializedProperty _gravityIndicatorDistance;
    SerializedProperty _gravityIndicatorColor;

    SerializedProperty _drawSpeedIndicator;
    SerializedProperty _speedIndicatorDistance;
    SerializedProperty _speedIndicatorSize;
    SerializedProperty _speedIndicatorColor;

    private void OnEnable()
    {
        _rotorHead = serializedObject.FindProperty("rotorHead");
        _rotation = serializedObject.FindProperty("rotation");
        _speed = serializedObject.FindProperty("speed");

        _enableLimits = serializedObject.FindProperty("enableLimits");
        _maxLimit = serializedObject.FindProperty("maxLimit");
        _minLimit = serializedObject.FindProperty("minLimit");

        _drawDownIndicator = serializedObject.FindProperty("drawDownIndicator");
        _downIndicatorDistance = serializedObject.FindProperty("downIndicatorDistance");
        _downIndicatorColor = serializedObject.FindProperty("downIndicatorColor");

        _drawGravityIndicator = serializedObject.FindProperty("drawGravityIndicator");
        _gravityIndicatorDistance = serializedObject.FindProperty("gravityIndicatorDistance");
        _gravityIndicatorColor = serializedObject.FindProperty("gravityIndicatorColor");

        _drawSpeedIndicator = serializedObject.FindProperty("drawSpeedIndicator");
        _speedIndicatorDistance = serializedObject.FindProperty("speedIndicatorDistance");
        _speedIndicatorSize = serializedObject.FindProperty("speedIndicatorSize");
        _speedIndicatorColor = serializedObject.FindProperty("speedIndicatorColor");
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        EditorGUILayout.PropertyField(_rotorHead);

        EditorGUILayout.PropertyField(_enableLimits);
        if (_enableLimits.boolValue)
        {
            EditorGUILayout.PropertyField(_maxLimit);
            EditorGUILayout.PropertyField(_minLimit);
        }

        EditorGUILayout.Space(5);

        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("Speed", EditorStyles.boldLabel);
        if (GUILayout.Button("Reset Speed"))
        {
            _speed.floatValue = 0;
        }
        EditorGUILayout.EndHorizontal();
        _speed.floatValue = EditorGUILayout.Slider(_speed.floatValue, -30, 30);

        EditorGUILayout.Space(5);

        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("Rotation", EditorStyles.boldLabel);
        if (GUILayout.Button("Reset Rotation"))
        {
            _rotation.floatValue = 0;
        }
        EditorGUILayout.EndHorizontal();
        _rotation.floatValue = EditorGUILayout.Slider(_rotation.floatValue, -360, 360);

        EditorGUILayout.Space(5);

        GUI.contentColor = Color.red;
        EditorGUILayout.Space(20);
        EditorGUILayout.LabelField("DEBUGGING", EditorStyles.boldLabel);

        EditorGUILayout.PropertyField(_drawDownIndicator, new GUIContent("Draw Down Indicator?"));
        if (_drawDownIndicator.boolValue)
        {
            EditorGUI.indentLevel++;
            EditorGUILayout.PropertyField(_downIndicatorColor);
            EditorGUILayout.PropertyField(_downIndicatorDistance);
            EditorGUI.indentLevel--;
            EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);
        }

        EditorGUILayout.PropertyField(_drawGravityIndicator, new GUIContent("Draw gravity vector?"));
        if (_drawGravityIndicator.boolValue)
        {
            EditorGUI.indentLevel++;
            EditorGUILayout.PropertyField(_gravityIndicatorColor);
            EditorGUILayout.PropertyField(_gravityIndicatorDistance);
            EditorGUI.indentLevel--;
            EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);
        }

        EditorGUILayout.PropertyField(_drawSpeedIndicator, new GUIContent("Draw speed indicator?"));
        if (_drawSpeedIndicator.boolValue)
        {
            EditorGUI.indentLevel++;
            EditorGUILayout.PropertyField(_speedIndicatorColor);
            EditorGUILayout.PropertyField(_speedIndicatorDistance);
            EditorGUILayout.PropertyField(_speedIndicatorSize);
            EditorGUI.indentLevel--;
            EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);
        }


        serializedObject.ApplyModifiedProperties();
    }
}