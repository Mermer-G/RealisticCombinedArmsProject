using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
[CreateAssetMenu(menuName = "Aerodynamics/AerodynamicSurfaceObject")]
public class AerodynamicSurfaceObject : ScriptableObject
{
    public XFoilData xfoildata;

    public List<Vector3> localPoints = new List<Vector3>()
    {
        new Vector3(-1, 0, -1),
        new Vector3(1, 0, -1),
        new Vector3(0, 0, 1)
    };
    public Vector3 centerPoint = Vector3.zero;
    public float area;
    public float aspectRatio;
    public float sweepAngle;

    public Color cornerVertexColor = Color.green;
    public Color edgeVertexColor = Color.blue;
    public Color edgeColor = Color.yellow;
    public Color fillColor = new Color(0, 1, 0, 0.3f);
    public Color fontColor = new Color(0, 0, 0, 1);
    public Color fontBackgroundColor = new Color(1, 1, 1, 1);
    public int fontSize = 10;
    public float edgeThickness = 2f;
    public float cornerVertexSize = 0.1f;
    public float edgeVertexSize = 0.1f;
}
