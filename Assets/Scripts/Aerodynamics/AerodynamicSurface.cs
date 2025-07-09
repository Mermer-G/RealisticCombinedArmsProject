using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;
using static AerodynamicSurface;
using System;

[ExecuteAlways]
public class AerodynamicSurface : MonoBehaviour
{
    public AerodynamicSurfaceObject aerodynamicSurfaceObject;

    public float surfaceLiftMultiplier = 1;

    public List<GameObject> childrenOfRoot = new List<GameObject>();
    public int selectedIndex;
    public enum MirroringAxis{ None, X, Y, Z }
    public MirroringAxis mirroringAxis;
    public enum TopWayIndicator { plusY, minusY, transformPlusY, transformMinusY, transformPlusX, transformMinusX }
    public TopWayIndicator topWayIndicator;
    public enum SurfaceType { Standart, ControlSurface, Slat, LERX }
    public SurfaceType surfaceType;
    public float LERXStart;
    public float LERXEnd;
    public float LERXcl;
    public List<AerodynamicSurface> subSurfaces;
    public AerodynamicSurface mirroredSurface;
    public bool drawEditingPolygon;
    public bool drawSubSurfaces;
    public bool pivotEditing;
    public bool showSurfaceObjectProperties;
    private void OnValidate()
    {
        if (!childrenOfRoot.Any()) 
        {
            AddAllChildren(transform.root, childrenOfRoot);
        }
    }
    
    void AddAllChildren(Transform parent, List<GameObject> list)
    {
        foreach (Transform child in parent)
        {
            if (child.gameObject.tag == tag)
            {
                list.Add(child.gameObject);
            }
            AddAllChildren(child, list);
        }
    }

    public void ResetChildrenOfRoot()
    {
        if (childrenOfRoot == null)
        {
            Debug.Log("childrenOfRoot was null, creating a new list.");
            childrenOfRoot = new List<GameObject>(); // E�er null ise, yeni bir liste olu�turun
        }
        childrenOfRoot.Clear();
        AddAllChildren(transform.root, childrenOfRoot);
    }
}



#if UNITY_EDITOR
[CustomEditor(typeof(AerodynamicSurface)), CanEditMultipleObjects]
public class AerodynamicSurfaceEditor : Editor
{
    SerializedProperty drawEditingPolygon;
    SerializedProperty drawSubSurfaces;
    SerializedProperty showSurfaceObjectProperties;
    SerializedProperty pivotEditing;
    SerializedProperty subSurfaces;
    bool mirroringFoldOut = false;
    Vector3 pivotLastPoint;
    bool resetPivotLastPoint;
    private void OnSceneGUI()
    {
        var surface = (AerodynamicSurface)target;
        if (drawEditingPolygon.boolValue)
        {
            if (!pivotEditing.boolValue)
            {
                HandleHandles(surface);
                resetPivotLastPoint = true;
            }
            else
            { 
                HandlePivotChange(surface);
            }
            DrawEdges(surface);
            DrawPolygon(surface);
            ShowArea(surface);
            if (surface.mirroredSurface != null && drawEditingPolygon.boolValue)
            {
                if (!pivotEditing.boolValue) HandleHandles(surface.mirroredSurface);
                DrawEdges(surface.mirroredSurface);
                DrawPolygon(surface.mirroredSurface);
                ShowArea(surface.mirroredSurface);
            }
        }
        if(surface.mirroringAxis != AerodynamicSurface.MirroringAxis.None)
        {
            CalculateMirroringSquare(surface.mirroringAxis, 20, surface.transform.root);
        }
        if (subSurfaces.arraySize > 0 && surface.surfaceType == SurfaceType.Standart && drawSubSurfaces.boolValue)
        {
            for (int i = 0; i < subSurfaces.arraySize; ++i)
            {
                SerializedProperty element = subSurfaces.GetArrayElementAtIndex(i);
                if (element.objectReferenceValue != null)
                {
                    DrawPolygon((AerodynamicSurface)element.objectReferenceValue);
                }
            }
        }
    }

    public void OnSelectionChanged()
    {
        var surface = target as AerodynamicSurface;
        if (Selection.activeGameObject != surface.gameObject) // E�er hi�bir GameObject se�ili de�ilse
        {
            serializedObject.Update();
            pivotEditing.boolValue = false;
            serializedObject.ApplyModifiedProperties();
        }
    }

    private void HandlePivotChange(AerodynamicSurface surface)
    {
        
        if (resetPivotLastPoint)
        {
            pivotLastPoint = surface.transform.position;
            resetPivotLastPoint = false;
        }
        if (pivotLastPoint == surface.transform.position) return;

        Handles.CubeHandleCap(0,surface.transform.position, Quaternion.identity, 1, EventType.Repaint);
        var change = surface.transform.position - pivotLastPoint;
        for (int i = 0; i < surface.aerodynamicSurfaceObject.localPoints.Count; i++)
        {
            var worldPosition = surface.transform.TransformPoint(surface.aerodynamicSurfaceObject.localPoints[i]);
            worldPosition = worldPosition - change;
            surface.aerodynamicSurfaceObject.localPoints[i] = surface.transform.InverseTransformPoint(worldPosition);
        }
        pivotLastPoint = surface.transform.position;
        UpdateCenterPoint(surface);
    }

    private void OnEnable()
    {
        var surface = (AerodynamicSurface)target;
        drawEditingPolygon = serializedObject.FindProperty("drawEditingPolygon");
        drawSubSurfaces = serializedObject.FindProperty("drawSubSurfaces");
        showSurfaceObjectProperties = serializedObject.FindProperty("showSurfaceObjectProperties");
        pivotEditing = serializedObject.FindProperty("pivotEditing");
        subSurfaces = serializedObject.FindProperty("subSurfaces");
        EditorApplication.update += OnSelectionChanged;
    }
    private void OnDisable()
    {
        EditorApplication.update -= OnSelectionChanged;
    }

    bool resetChildrenOfRoot = true;
    public override void OnInspectorGUI()
    {
        //DrawDefaultInspector();
        var surface = (AerodynamicSurface)target;
        if (surface == null) return;
        serializedObject.Update();

        //Lift Multiplier
        surface.surfaceLiftMultiplier = EditorGUILayout.FloatField("Surface Lift Multiplier", surface.surfaceLiftMultiplier);

        //Show the surface object
        surface.aerodynamicSurfaceObject = (AerodynamicSurfaceObject)EditorGUILayout.ObjectField("Aerodynamic Surface Object", surface.aerodynamicSurfaceObject, typeof(AerodynamicSurfaceObject), true);

        //Top Way Indicator
        surface.topWayIndicator = (TopWayIndicator)EditorGUILayout.EnumPopup("Top Way Indicator", surface.topWayIndicator);

        //Surface Type
        surface.surfaceType = (SurfaceType)EditorGUILayout.EnumPopup("Surface Type", surface.surfaceType);

        //Show the subsurfaces screen if standart has been chosen.
        if (surface.surfaceType == SurfaceType.Standart)
        {
            EditorGUILayout.PropertyField(subSurfaces, new GUIContent("subSurfaces"), true);
        }

        //Show the subsurfaces screen if standart has been chosen.
        if (surface.surfaceType == SurfaceType.LERX)
        {
            if (surface.aerodynamicSurfaceObject.sweepAngle < 60)
            {
                EditorGUILayout.HelpBox("", MessageType.Warning);
                EditorUtility.DisplayDialog(
                    "Warning!",
                    "LERXs can't have their sweep angle lower than 60 degrees!",
                    "OK."
                );
                surface.surfaceType = SurfaceType.Standart;
            }
            surface.LERXStart = EditorGUILayout.FloatField("LERX Addition Start Angle", surface.LERXStart);
            surface.LERXEnd = EditorGUILayout.FloatField("LERX Addition End Angle", surface.LERXEnd);
            surface.LERXcl = EditorGUILayout.FloatField("LERX Addition Cl", surface.LERXcl);
        }

        //Draw editing polygon
        if (drawEditingPolygon.boolValue = EditorGUILayout.Toggle("Draw Editing Polygon", drawEditingPolygon.boolValue))
        {
            pivotEditing.boolValue = EditorGUILayout.Toggle("Enable Pivot Editing", pivotEditing.boolValue);
        }

        //Draw SubSurfaces
        drawSubSurfaces.boolValue = EditorGUILayout.Toggle("Draw SubSurfaces", drawSubSurfaces.boolValue);
        

        //Show Surface Object Properties
        showSurfaceObjectProperties.boolValue = EditorGUILayout.Toggle("Show Object Properties", showSurfaceObjectProperties.boolValue);
        if (showSurfaceObjectProperties.boolValue)
        ShowObjectDataOnInspector();

        GUILayout.Space(5);

        if (mirroringFoldOut = EditorGUILayout.Foldout(mirroringFoldOut,"Surface Mirroring Options"))
        {
            if (surface.childrenOfRoot == null || surface.childrenOfRoot.Contains(null))
            {
                surface.ResetChildrenOfRoot();
            }
            resetChildrenOfRoot = GUILayout.Button("Reset Children Of Root");
            if (resetChildrenOfRoot)
            {
                surface.ResetChildrenOfRoot();
                resetChildrenOfRoot = false;
            }

            GUILayout.Label("Select Mirroring Axis");
            surface.mirroringAxis = (MirroringAxis)EditorGUILayout.EnumPopup(surface.mirroringAxis);

            GUILayout.Label("Select Parent For Mirrored Object");
            
            List<string> namesOfChildren = new List<string>();
            namesOfChildren = surface.childrenOfRoot.Select(x => x.name).ToList();
            namesOfChildren.Insert(0, "None");
            surface.selectedIndex = EditorGUILayout.Popup(surface.selectedIndex, namesOfChildren.ToArray());

            if (surface.mirroredSurface != null)
            {
                if (GUILayout.Button("Remove Mirrored Surface"))
                {
                    DestroyImmediate(surface.mirroredSurface.gameObject);
                    surface.mirroredSurface = null;
                    DestroyImmediate(surface.GetComponent<MirrorObjects>());
                    surface.ResetChildrenOfRoot();
                }
            }
            else 
            {
                GUIContent buttonContent = new GUIContent("Add Mirrored Surface");
                if (surface.selectedIndex == 0)
                {
                    GUI.enabled = false;
                    buttonContent = new GUIContent("Add Mirrored Surface", "Select a parent for mirrored surface first!");
                }

                if (surface.mirroringAxis == MirroringAxis.None)
                {
                    GUI.enabled = false;
                    buttonContent = new GUIContent("Add Mirrored Surface", "Select a mirroring axis for mirrored surface first!");
                }

                if (GUILayout.Button(buttonContent))
                {
                    CreateMirror(surface);
                }
                GUI.enabled = true;
            }
        }
        else if (!resetChildrenOfRoot) resetChildrenOfRoot = true;



        

        serializedObject.ApplyModifiedProperties(); // De�i�iklikleri kaydet
    }
    
    void CalculateMirroringSquare(MirroringAxis axis, float size, Transform transform)
    {
        Vector3 right = Vector3.zero, up = Vector3.zero;

        // Ana eksene ba�l� olarak d�zlem belirleme
        switch (axis)
        {
            case MirroringAxis.None:
                return;
            case MirroringAxis.X:
                Handles.color = new Color(1, 0, 0, 0.3f);
                right = transform.forward;  // Z ekseni
                up = transform.up;          // Y ekseni
                break;
            case MirroringAxis.Y:
                Handles.color = new Color(0, 1, 0, 0.3f);
                right = transform.right;    // X ekseni
                up = transform.forward;     // Z ekseni
                break;
            case MirroringAxis.Z:
                Handles.color = new Color(0, 0, 1, 0.3f);
                right = transform.right;    // X ekseni
                up = transform.up;          // Y ekseni
                break;            
        }

        // Orta noktadan uzakla�arak 4 k��e noktay� hesapla
        Vector3 center = transform.position;
        float halfSize = size / 2f;

        Vector3[] points = new Vector3[4];
        points[0] = center + (right * halfSize) + (up * halfSize);  // Sa� �st
        points[1] = center - (right * halfSize) + (up * halfSize);  // Sol �st
        points[2] = center - (right * halfSize) - (up * halfSize);  // Sol alt
        points[3] = center + (right * halfSize) - (up * halfSize);  // Sa� alt

        
        Handles.DrawAAConvexPolygon(points);
    }

    void HandleHandles(AerodynamicSurface surface)
    {
        var surfaceObject = surface.aerodynamicSurfaceObject;
        Event e = Event.current;

        //Drawing freemove handles
        //Corner Vertex
        var pointList = surfaceObject.localPoints;
        

        
        if (!e.control)
        {
            Handles.color = surfaceObject.cornerVertexColor;

            for (int i = 0; i < pointList.Count; i++)
            {
                var point = pointList[i];

                EditorGUI.BeginChangeCheck();
                var fmh_355_21_638876702397911659 = Quaternion.identity; var newPos = surface.transform.InverseTransformPoint(
                    Handles.FreeMoveHandle(surface.transform.TransformPoint(point),
                    surfaceObject.cornerVertexSize,
                    Vector3.zero,
                    Handles.SphereHandleCap)
                );

                if (EditorGUI.EndChangeCheck())
                {
                    Undo.RecordObject(surfaceObject, "Corner Position");
                    pointList[i] = Vector3.ProjectOnPlane(newPos, Vector3.up);
                    UpdateCenterPoint(surface);
                    UpdateAR(surface);
                    UpdateSweepAngle(surface);
                }
            }
        }
        //silme
        else 
        {
            Handles.color = Color.red;

            for (int i = 0; i < pointList.Count; i++)
            {
                var point = pointList[i];

                EditorGUI.BeginChangeCheck();
                if (Handles.Button(surface.transform.TransformPoint(point), Quaternion.identity, surfaceObject.cornerVertexSize, surfaceObject.cornerVertexSize, Handles.SphereHandleCap) &&
                    pointList.Count > 3)
                {
                    Undo.RecordObject(surfaceObject, "Remove Corner");
                    surfaceObject.localPoints.RemoveAt(i);
                    surfaceObject.localPoints = surfaceObject.localPoints.ToList();
                    UpdateCenterPoint(surface);
                    UpdateAR(surface);
                    UpdateSweepAngle(surface);
                }
                EditorGUI.EndChangeCheck();
                
            }

        }
        
        

        //Edge Vertex
        Handles.color = surfaceObject.edgeVertexColor;
        EditorGUI.BeginChangeCheck();
        //Put edge verticies between corners.
        for (int i = 0;i < pointList.Count - 1; i++)
        {
            var middlePoint = surface.transform.TransformPoint((pointList[i] + pointList[i + 1]) / 2);
            if(Handles.Button(middlePoint, Quaternion.identity, surfaceObject.edgeVertexSize, surfaceObject.edgeVertexSize, Handles.SphereHandleCap))
            {
                Undo.RecordObject(surfaceObject, "Add Corner"); 
                surfaceObject.localPoints.Insert(i+1, surface.transform.InverseTransformPoint(middlePoint));
                UpdateCenterPoint(surface);
                UpdateAR(surface);
                UpdateSweepAngle(surface);
            }
        }
        //Add the last vertex between the last and the first.
        var lastMiddlePoint = surface.transform.TransformPoint((pointList[pointList.Count - 1] + pointList[0]) / 2);
        if(Handles.Button(lastMiddlePoint, Quaternion.identity, surfaceObject.edgeVertexSize, surfaceObject.edgeVertexSize, Handles.SphereHandleCap))
        {
            Undo.RecordObject(surfaceObject, "Add Corner"); 
            surfaceObject.localPoints.Insert(pointList.Count, surface.transform.InverseTransformPoint(lastMiddlePoint));
            UpdateCenterPoint(surface);
            UpdateAR(surface);
            UpdateSweepAngle(surface);
        }
        EditorGUI.EndChangeCheck();
        
    }

    void UpdateAR(AerodynamicSurface surface)
    {
        var surfaceObject = surface.aerodynamicSurfaceObject;
        var min = float.MaxValue;
        var max = float.MinValue;
        foreach (var point in surfaceObject.localPoints)
        {
            if (point.x > max)
            {
                max = point.x;
            }

            if (point.x < min)
            {
                min = point.x;
            }
        }
        var b = max - min;
        surfaceObject.aspectRatio = b * b / surfaceObject.area;
    }

    void UpdateSweepAngle(AerodynamicSurface surface)
    {
        var sO = surface.aerodynamicSurfaceObject;
        var A = sO.localPoints[0];
        var B = sO.localPoints[1];

        var z = A.z - B.z;
        var x = B.x - A.x;
        var sweep = 180 - (float)Math.Atan2(z, x) * Mathf.Rad2Deg;
        sO.sweepAngle = sweep > 90 ? 180 - sweep : sweep;
    }

    void UpdateCenterPoint(AerodynamicSurface surface)
    {
        var surfaceObject = surface.aerodynamicSurfaceObject;
        var pointList = surfaceObject.localPoints;

        var midPoint = Vector3.zero;
        for(int i = 0; i < pointList.Count; i++)
        {
            midPoint += pointList[i];
        }
        midPoint /= pointList.Count;
        surface.aerodynamicSurfaceObject.centerPoint = midPoint;
    }

    private Editor surfaceEditor;
    void ShowObjectDataOnInspector()
    {
        AerodynamicSurface script = (AerodynamicSurface)target;

        if (script.aerodynamicSurfaceObject != null)
        {
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Aerodynamic Surface Object", EditorStyles.boldLabel);

            // E�er �nceden olu�turulmad�ysa, CreateEditor ile olu�tur
            if (surfaceEditor == null)
            {
                surfaceEditor = CreateEditor(script.aerodynamicSurfaceObject);
            }

            // ScriptableObject'in Inspector i�eri�ini �iz
            surfaceEditor.OnInspectorGUI();
            // **Inspector'� g�ncelle** (yeniden �izdir)
            Repaint();
        }
    }

    void DrawEdges(AerodynamicSurface surface)
    {
        var surfaceObject = surface.aerodynamicSurfaceObject;

        Handles.color = surfaceObject.edgeColor;
        var points = surfaceObject.localPoints;
        for (int i = 0; i < points.Count - 1; i++)
        {
            Handles.DrawLine(surface.transform.TransformPoint(points[i]), surface.transform.TransformPoint(points[i + 1]), surfaceObject.edgeThickness);
        }
        Handles.DrawLine(surface.transform.TransformPoint(points[points.Count - 1]), surface.transform.TransformPoint(points[0]), surfaceObject.edgeThickness);
    }

    void DrawPolygon(AerodynamicSurface surface) 
    {
        var surfaceObject = surface.aerodynamicSurfaceObject;
        Handles.color = surfaceObject.fillColor;
        Vector3[] polygonCorners = new Vector3[surfaceObject.localPoints.Count];
        for (int i = 0; i < surfaceObject.localPoints.Count; i++)
        {
            polygonCorners[i] = surface.transform.TransformPoint(surfaceObject.localPoints[i]);
        }
        Handles.DrawAAConvexPolygon(polygonCorners);
    }

    void ShowArea(AerodynamicSurface surface)
    {
        var surfaceObject = surface.aerodynamicSurfaceObject;
        var points = surfaceObject.localPoints;
        Vector3 middlePoint = Vector3.zero;
        foreach (var point in points) 
        {
            middlePoint += point;
        }
        
        middlePoint /= points.Count;
        //centerPoint.vector3Value = middlePoint;

        Handles.color = Color.black;


        // Yeni bir GUIStyle olu�tur
        GUIStyle labelStyle = new GUIStyle();
        labelStyle.fontSize = surfaceObject.fontSize;
        labelStyle.normal.textColor = surfaceObject.fontColor;
        labelStyle.alignment = TextAnchor.MiddleCenter;
        Texture2D bgTexture = new Texture2D(1, 1);
        bgTexture.SetPixel(0, 0, surfaceObject.fontBackgroundColor);
        bgTexture.Apply();
        labelStyle.normal.background = bgTexture;
        Handles.Label(surface.transform.TransformPoint(middlePoint), CalculatePolygonArea3D(points).ToString("0.00m^2"), labelStyle);
        surface.aerodynamicSurfaceObject.area = CalculatePolygonArea3D(points);
    }

    float CalculatePolygonArea3D(List<Vector3> points)
    {
        int n = points.Count;
        if (n < 3) return 0; // En az 3 nokta olmal�

        // �okgenin normalini hesapla (ilk �� nokta ile)
        Vector3 normal = Vector3.Cross(points[1] - points[0], points[2] - points[0]).normalized;

        // En bask�n ekseni bul (XY, XZ veya YZ d�zlemine projekte etmek i�in)
        Vector3 absNormal = new Vector3(Mathf.Abs(normal.x), Mathf.Abs(normal.y), Mathf.Abs(normal.z));
        int dominantAxis = absNormal.x > absNormal.y
            ? (absNormal.x > absNormal.z ? 0 : 2) // X mi Z mi?
            : (absNormal.y > absNormal.z ? 1 : 2); // Y mi Z mi?

        // 3D noktalar� 2D'ye d�n��t�r (Shoelace i�in)
        List<Vector2> projectedPoints = new List<Vector2>();
        foreach (Vector3 p in points)
        {
            if (dominantAxis == 0) projectedPoints.Add(new Vector2(p.y, p.z)); // X bask�n -> (Y, Z) kullan
            else if (dominantAxis == 1) projectedPoints.Add(new Vector2(p.x, p.z)); // Y bask�n -> (X, Z) kullan
            else projectedPoints.Add(new Vector2(p.x, p.y)); // Z bask�n -> (X, Y) kullan
        }

        // 2D Shoelace Teoremi ile alan hesapla
        float area = 0;
        for (int i = 0; i < n; i++)
        {
            Vector2 p1 = projectedPoints[i];
            Vector2 p2 = projectedPoints[(i + 1) % n]; // Son noktadan ilk noktaya d�n
            area += (p1.x * p2.y) - (p2.x * p1.y);
        }

        return Mathf.Abs(area) / 2f;
    }

    void CreateMirror(AerodynamicSurface surface)
    {
        //Creating new GameObject
        GameObject newSurfaceGameObject = Instantiate(surface.gameObject, surface.transform.position, surface.transform.rotation, surface.transform.parent);
        newSurfaceGameObject.name = surface.transform.gameObject.name + "-mirrored";
        var mirrorScript = surface.gameObject.AddComponent<MirrorObjects>();
        mirrorScript.mirrorTransform = surface.transform.root;
        switch (surface.mirroringAxis)
        {
            case MirroringAxis.None:
                break;
            case MirroringAxis.X:
                mirrorScript.axisOfReflection = surface.transform.root.right;
                break;
            case MirroringAxis.Y:
                mirrorScript.axisOfReflection = surface.transform.root.up;
                break;
            case MirroringAxis.Z:
                mirrorScript.axisOfReflection = surface.transform.root.forward;
                break;
        }
        mirrorScript.mirroredTransform = newSurfaceGameObject.transform;
        mirrorScript.mainTransform = surface.transform;
        surface.mirroredSurface = newSurfaceGameObject.GetComponent<AerodynamicSurface>();
        surface.mirroredSurface.transform.SetParent(surface.childrenOfRoot[surface.selectedIndex - 1].transform);
        surface.mirroringAxis = MirroringAxis.None;
        surface.mirroredSurface.mirroringAxis = MirroringAxis.None;
    }
}
#endif
