using UnityEngine;
using UnityEditor;
using System;
using Unity.VisualScripting;

public class Hinge : MonoBehaviour
{
    [SerializeField] GameObject hingeHead;
    [Range(-150, 180)] public int maxLimit;
    [Range(-180, 150)] public int minLimit;
    [Range(-30, 30)] public float speed;
    [Range(-90, 90)] public float rotation;
    [SerializeField] float offsetValue = 90;

    [SerializeField] bool drawArc;
    [SerializeField] float arcRadius;
    [SerializeField] Color arcColor;
    int segments;

    [SerializeField] bool drawFrontArrow;
    [SerializeField] int frontArrowLength;
    [SerializeField] Color frontArrowColor;
    float prevRotation;

    [SerializeField] bool drawSpeedIndicator;
    [SerializeField] float speedIndicatorDistance;
    [SerializeField] float speedIndicatorSize;
    [SerializeField] Color speedIndicatorColor;

    void Start()
    {
        rotation = Quaternion.Euler(hingeHead.transform.rotation.x - offsetValue, transform.rotation.y, transform.rotation.z).x;
        prevRotation = rotation;
    }

    void FixedUpdate()
    {
        rotation += speed;
        
        if (rotation > maxLimit) rotation = maxLimit;
        if (rotation < minLimit) rotation = minLimit;

        if (rotation != prevRotation)
        {
            hingeHead.transform.localRotation = Quaternion.Euler(rotation + offsetValue, 0, 0);
            prevRotation = rotation;
        }

    }

    void OnDrawGizmos()
    {
        //Arc'ý çizer
        if (drawArc)
        {
            Gizmos.color = arcColor;
            Vector3[] arcVertices = CalculateArc();
            for (int i = 0; i < arcVertices.Length - 1; i++)
            {
                Gizmos.DrawLine(arcVertices[i], arcVertices[i + 1]);
            }
        }

        if (drawFrontArrow)
        {    
            //Oku çizer
            Gizmos.color = frontArrowColor;
            Vector3[] arrowVertices = CalculateFront();
            for (int i = 0; i < arrowVertices.Length - 1; i++)
            {
                Gizmos.DrawLine(arrowVertices[i], arrowVertices[i + 1]);
            }
            Gizmos.DrawLine(arrowVertices[3], arrowVertices[1]);
        }

        if (drawSpeedIndicator)
        {
            //Hýzý çizer
            Gizmos.color = speedIndicatorColor;
            Vector3[] speedVerticies = CalculateSpeedIndicator();
            if (speed == 0)
            {
                Gizmos.DrawSphere(speedVerticies[0], 0.1f);
                Gizmos.DrawLine(hingeHead.transform.position, speedVerticies[0]);
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

    Vector3[] CalculateFront()
    {
        // Okun ucu noktasý
        Vector3 arrowHead = hingeHead.transform.up * frontArrowLength;

        // Okun tabaný için sað ve sol noktalar, obje yönüne göre ayarlanmalý
        Vector3 right = hingeHead.transform.rotation * (Quaternion.Euler(5, 0, 0) * Vector3.up);
        Vector3 left = hingeHead.transform.rotation * (Quaternion.Euler(-5, 0, 0) * Vector3.up);

        // Tabandaki köþe noktalarý
        Vector3 baseRight = arrowHead - right;
        Vector3 baseLeft = arrowHead - left;

        // Dünya uzayýna taþýrken pozisyonu ekle
        arrowHead = hingeHead.transform.position + arrowHead;
        baseLeft = hingeHead.transform.position + baseLeft;
        baseRight = hingeHead.transform.position + baseRight;

        // Okun tabaný ve ucu
        Vector3[] arrowPoints = new Vector3[]
        {
            hingeHead.transform.position,
            arrowHead, // Okun ucu
            baseLeft, // Tabanýn sol köþesi
            baseRight, // Tabanýn sað köþesi
        };


        return arrowPoints;
    } 

    Vector3[] CalculateArc()
    {
        var radian = -(maxLimit + offsetValue - 90) * Mathf.Deg2Rad;
        Vector3 pos = Vector3.zero;

        segments = maxLimit - minLimit;

        Vector3[] verticies = new Vector3[segments];
        verticies[0] = transform.position;
        verticies[segments-1] = transform.position;

        //Max Limit
        Vector3 localPoint = new Vector3(0, arcRadius * Mathf.Sin(radian), arcRadius * Mathf.Cos(radian));
        verticies[1] = transform.TransformPoint(localPoint);

        //Min Limit
        radian = -(minLimit + offsetValue - 90) * Mathf.Deg2Rad;
        localPoint = new Vector3(0, arcRadius * Mathf.Sin(radian), arcRadius * Mathf.Cos(radian));
        verticies[segments - 2] = transform.TransformPoint(localPoint);

        //Total Degrees
        int totalDegrees = maxLimit - minLimit;
        //Decrement ammount
        int decreement = totalDegrees / (segments - 4);


        totalDegrees = maxLimit;
        //In Between
        for (int i = 2; i < segments - 2; i++)
        {
            
            totalDegrees -= decreement;
            var radian2 = -(totalDegrees + offsetValue - 90) * Mathf.Deg2Rad;
            // Yalnýzca local pozisyonda hesapla, transform.position eklemeye gerek yok
            Vector3 localPoint2 = new Vector3(0, arcRadius * Mathf.Sin(radian2), arcRadius * Mathf.Cos(radian2));

            // Sonrasýnda bu noktayý dünya uzayýna çevir
            verticies[i] = transform.TransformPoint(localPoint2);
        }

        return verticies;
    }

    Vector3[] CalculateSpeedIndicator()
    {
        int segments = 10;
        Vector3[] verticies = new Vector3[segments + 1];

        // Ýlk noktayý setler (obje yönüyle uyumlu)
        Vector3 firstPoint = Vector3.up * speedIndicatorDistance;
        verticies[0] = hingeHead.transform.TransformPoint(firstPoint);
        float radian;
        Vector3 point;
        float degree = 0;
        for (int i = 1; i < segments; i++)
        {
            degree += speed * speedIndicatorSize;
            radian = -(degree + offsetValue - 90) * Mathf.Deg2Rad;
            point = new Vector3(0, -speedIndicatorDistance * Mathf.Sin(radian), -speedIndicatorDistance * Mathf.Cos(radian));
            verticies[i] = hingeHead.transform.TransformPoint(point);
        }
        verticies[segments] = hingeHead.transform.position;

        return verticies;
    }
}





#if UNITY_EDITOR
[CustomEditor(typeof(Hinge))]
public class HingeEditor : Editor
{

    public SerializedProperty _hingeHead;
    SerializedProperty _maxLimit;
    SerializedProperty _minLimit;
    SerializedProperty _speed;
    SerializedProperty _rotation;
    SerializedProperty _offsetValue;

    SerializedProperty _drawArc;
    SerializedProperty _arcRadius;
    SerializedProperty _arcColor;


    SerializedProperty _drawFrontArrow;
    SerializedProperty _frontArrowLength;
    SerializedProperty _frontArrowColor;

    SerializedProperty _drawSpeedIndicator;
    SerializedProperty _speedIndicatorDistance;
    SerializedProperty _speedIndicatorSize;
    SerializedProperty _speedIndicatorColor;

    private void OnEnable()
    {
        _hingeHead = serializedObject.FindProperty("hingeHead");
        _maxLimit = serializedObject.FindProperty("maxLimit");
        _minLimit = serializedObject.FindProperty("minLimit");
        _speed = serializedObject.FindProperty("speed");
        _rotation = serializedObject.FindProperty("rotation");
        _offsetValue = serializedObject.FindProperty("offsetValue");

        _drawArc = serializedObject.FindProperty("drawArc");
        _arcRadius = serializedObject.FindProperty("arcRadius");
        _arcColor = serializedObject.FindProperty("arcColor");


        _drawFrontArrow = serializedObject.FindProperty("drawFrontArrow");
        _frontArrowLength = serializedObject.FindProperty("frontArrowLength");
        _frontArrowColor = serializedObject.FindProperty("frontArrowColor");

        _drawSpeedIndicator = serializedObject.FindProperty("drawSpeedIndicator");
        _speedIndicatorDistance = serializedObject.FindProperty("speedIndicatorDistance");
        _speedIndicatorSize = serializedObject.FindProperty("speedIndicatorSize");
        _speedIndicatorColor = serializedObject.FindProperty("speedIndicatorColor");
    }


    public override void OnInspectorGUI()
    {
        

        serializedObject.Update();

        EditorGUILayout.PropertyField(_hingeHead);
        EditorGUILayout.PropertyField(_maxLimit);
        EditorGUILayout.PropertyField(_minLimit);

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
        _rotation.floatValue = EditorGUILayout.Slider(_rotation.floatValue, -155, 90);

        EditorGUILayout.Space(5);

        EditorGUILayout.PropertyField(_offsetValue);



        GUI.contentColor = Color.red;
        EditorGUILayout.Space(20);
        EditorGUILayout.LabelField("DEBUGGING", EditorStyles.boldLabel);

        EditorGUILayout.PropertyField(_drawArc, new GUIContent("Draw Range of Motion?"));
        if (_drawArc.boolValue)
        {
            EditorGUI.indentLevel++;
            EditorGUILayout.PropertyField(_arcColor);
            EditorGUILayout.PropertyField(_arcRadius);
            EditorGUI.indentLevel--;
            EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);
        }

        EditorGUILayout.PropertyField(_drawFrontArrow, new GUIContent("Draw front facing vector?"));
        if (_drawFrontArrow.boolValue)
        {
            EditorGUI.indentLevel++;
            EditorGUILayout.PropertyField(_frontArrowColor);
            EditorGUILayout.PropertyField(_frontArrowLength);
            EditorGUI.indentLevel--;
            EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);
        }
        
        EditorGUILayout.PropertyField(_drawSpeedIndicator, new GUIContent("Speed indicator?"));
        if (_drawSpeedIndicator.boolValue)
        {
            EditorGUI.indentLevel++;
            EditorGUILayout.PropertyField(_speedIndicatorColor);
            EditorGUILayout.PropertyField(_speedIndicatorDistance);
            EditorGUILayout.PropertyField(_speedIndicatorSize);
            EditorGUI.indentLevel--;
            EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);
        }

        // Kullanýcý tarafýndan yapýlan deðiþiklikleri kaydetmek için kullanýlýr
        serializedObject.ApplyModifiedProperties();

    }
}
#endif

