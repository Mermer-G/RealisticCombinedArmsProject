using System;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEditor;
using UnityEngine;

//[ExecuteAlways]
public class AerodynamicSurfaceCalculator : MonoBehaviour
{
    [SerializeField] AerodynamicSurface surface;

    [SerializeField] Mesh arrow;
    [SerializeField] Material arrowMaterial;
    GameObject windArrow;
    GameObject liftArrow;
    GameObject dragArrow;


    [SerializeField] Vector3 incomingRelativeWind;
    [SerializeField] float alpha;
    [SerializeField] float AR;
    [SerializeField] float deltaTMax;
    [SerializeField] float e;
    [SerializeField] float c_l;
    [SerializeField] float C_L;
    [SerializeField] float windStartingDistance;
    [SerializeField] float windEndDistance;
    [SerializeField] float liftForce;
    [SerializeField] float dragForce;

    [SerializeField] AnimationCurve CLxAlpha;
    [SerializeField] AnimationCurve CDxAlpha;
    [Range(-0.08f, 1)] [SerializeField] float slatInput;
    [SerializeField] float slatMaxAngle;
    [Range(-0.1f, 1)] [SerializeField] float flapInput;
    [SerializeField] float flapMaxAngle;

    Vector3 lift;
    Vector3 drag;

    // Sabitler (ISA - Uluslararası Standart Atmosfer)
    [SerializeField] const float T0 = 25;   // K (Deniz seviyesi sıcaklığı)
    [SerializeField] float relativeWindSpeed;

    void CalculateLift()
    {
        alpha = CalculateAlpha(surface);
        C_L = Calculate3DLiftCoefficient(alpha);
        var rho = ProjectUtilities.CalculateAirDensity(surface.transform.position, T0);
        var wingArea = CalculatePolygonArea3D(surface.aerodynamicSurfaceObject.localPoints);
        var velocity = CalculateTotalVelocity();
        var L = C_L * rho * wingArea * (Math.Pow(velocity.magnitude, 2) / 2);
        var direction = CalculateLiftDirection(surface);
        lift = (float)L * direction;
        liftForce = (float)L;
    }

    float Calculate3DLiftCoefficient(float alpha)
    {
        c_l = CLxAlpha.Evaluate(alpha);
        AR = surface.aerodynamicSurfaceObject.aspectRatio;
        deltaTMax = surface.aerodynamicSurfaceObject.sweepAngle;
        e = 1 / (2 - AR + Mathf.Sqrt(4 + (AR * AR * (1 + Mathf.Pow(Mathf.Tan(deltaTMax * Mathf.Deg2Rad), 2)))));

        return c_l / (1 + (c_l / Mathf.PI * e * AR)); 
    }

    Vector3 CalculateLiftDirection(AerodynamicSurface surface)
    {
        Vector3 up;
        if (surface.topWayIndicator == AerodynamicSurface.TopWayIndicator.plusY) up = transform.up;
        else up = -transform.up;
        return up;
    }

    void CalculateDrag()
    {
        var CD = CDxAlpha.Evaluate(alpha); //surface.aerodynamicSurfaceObject.xfoildata.cdCurve.Evaluate(alpha);
        var rho = ProjectUtilities.CalculateAirDensity(surface.transform.position, T0);
        var wingArea = CalculatePolygonArea3D(surface.aerodynamicSurfaceObject.localPoints);
        var velocity = CalculateTotalVelocity();
        var D = CD * rho * wingArea * (Math.Pow(velocity.magnitude, 2) / 2);
        var direction = incomingRelativeWind.normalized;
        drag = (float)D * direction;
        dragForce = drag.magnitude;
    }
    
    #region Angle Testing
    private double lastTime;
    private float interval = 0.1f; // 0.1 saniye aralık
    private int maxRuns = 10; // Maksimum çalıştırma sayısı
    [SerializeField] private int currentRuns = 0;
    [SerializeField] bool testAngles = false;
    private void EditorUpdate()
    {

        if (currentRuns >= maxRuns)
        {
            EditorApplication.update -= EditorUpdate; // Çalışmayı durdur
            return;
        }

        double currentTime = EditorApplication.timeSinceStartup;
        if (currentTime - lastTime >= interval)
        {
            TestAngles();
            lastTime = currentTime;
            currentRuns++;
            // Editörün sahneyi yenilemesini sağla
            SceneView.RepaintAll();
        }
    }

    void TestAngles()
    {
        incomingRelativeWind.y += 0.01f;
    }
    #endregion

    //Calculates the attack angle
    float CalculateAlpha(AerodynamicSurface surface)
    {
        var projectedWind = Vector3.ProjectOnPlane(incomingRelativeWind, transform.right);
        var alphaa = -Vector3.SignedAngle(projectedWind, transform.forward, transform.right);
        return surface.topWayIndicator == AerodynamicSurface.TopWayIndicator.plusY ? alphaa : -alphaa;
    }

    Vector3 CalculateTotalVelocity()
    {
        var velocity = surface.transform.root.GetComponent<Rigidbody>().velocity;
        var wind = incomingRelativeWind.normalized * relativeWindSpeed;
        return wind - velocity;
    }

    float CalculatePolygonArea3D(List<Vector3> points)
    {
        int n = points.Count;
        if (n < 3) return 0; // En az 3 nokta olmalı

        // Çokgenin normalini hesapla (ilk üç nokta ile)
        Vector3 normal = Vector3.Cross(points[1] - points[0], points[2] - points[0]).normalized;

        // En baskın ekseni bul (XY, XZ veya YZ düzlemine projekte etmek için)
        Vector3 absNormal = new Vector3(Mathf.Abs(normal.x), Mathf.Abs(normal.y), Mathf.Abs(normal.z));
        int dominantAxis = absNormal.x > absNormal.y
            ? (absNormal.x > absNormal.z ? 0 : 2) // X mi Z mi?
            : (absNormal.y > absNormal.z ? 1 : 2); // Y mi Z mi?

        // 3D noktaları 2D'ye dönüştür (Shoelace için)
        List<Vector2> projectedPoints = new List<Vector2>();
        foreach (Vector3 p in points)
        {
            if (dominantAxis == 0) projectedPoints.Add(new Vector2(p.y, p.z)); // X baskın -> (Y, Z) kullan
            else if (dominantAxis == 1) projectedPoints.Add(new Vector2(p.x, p.z)); // Y baskın -> (X, Z) kullan
            else projectedPoints.Add(new Vector2(p.x, p.y)); // Z baskın -> (X, Y) kullan
        }

        // 2D Shoelace Teoremi ile alan hesapla
        float area = 0;
        for (int i = 0; i < n; i++)
        {
            Vector2 p1 = projectedPoints[i];
            Vector2 p2 = projectedPoints[(i + 1) % n]; // Son noktadan ilk noktaya dön
            area += (p1.x * p2.y) - (p2.x * p1.y);
        }

        return Mathf.Abs(area) / 2f;
    }


    void DrawIncomingWind()
    {
        // Doğrudan gelen rüzgar yönünü normalize et
        var windDirection = incomingRelativeWind.normalized;

        // Rüzgar başlangıç ve bitiş noktaları
        var windStartPosition = surface.transform.position + surface.aerodynamicSurfaceObject.centerPoint + (windDirection * windStartingDistance);
        var windEndPosition = surface.transform.position + surface.aerodynamicSurfaceObject.centerPoint + (windDirection * windEndDistance);
        var distance = windStartPosition - windEndPosition;

        // Görselleştirme için çizimler
        Vector3 scale = Vector3.one / 3 * distance.magnitude;
        Vector3 position = windStartPosition;
        Quaternion rotation = Quaternion.LookRotation(-windDirection);
        Matrix4x4 matrix4X4 = Matrix4x4.TRS(position, rotation, scale);
        MaterialPropertyBlock props = new MaterialPropertyBlock();
        props.SetColor("_BaseColor", Color.white);

        Graphics.DrawMesh(arrow, matrix4X4, arrowMaterial, 0, null, 0, props);

        //Gizmos.DrawSphere(windStartPosition, 0.1f);
        //Gizmos.DrawSphere(windEndPosition, 0.1f);
        //Gizmos.DrawLine(windStartPosition, windEndPosition);
    }

    Vector3 liftTextPosition;
    void DrawLift()
    {
        var direction = lift.normalized;
        var force = liftForce;

        Vector3 scale = Vector3.one / 3 * Mathf.Clamp(force / 200000, 1, 4);
        Vector3 position = surface.aerodynamicSurfaceObject.centerPoint + transform.position;
        Quaternion rotation = Quaternion.LookRotation(direction);
        Matrix4x4 matrix4X4 = Matrix4x4.TRS(position, rotation, scale);
        MaterialPropertyBlock props = new MaterialPropertyBlock();
        props.SetColor("_BaseColor", Color.green);

        Graphics.DrawMesh(arrow, matrix4X4, arrowMaterial, 0, null, 0, props);

        liftTextPosition = surface.aerodynamicSurfaceObject.centerPoint + transform.position - (-direction * scale.magnitude);
    }

    Vector3 dragTextPosition;
    void DrawDrag()
    {
        var direction = drag.normalized;
        var force = drag.magnitude;

        Vector3 scale = Vector3.one / 3 * Mathf.Clamp(force / 10000, 1 , 4);
        Vector3 position = surface.aerodynamicSurfaceObject.centerPoint + transform.position;
        Quaternion rotation = Quaternion.LookRotation(-direction);
        Matrix4x4 matrix4X4 = Matrix4x4.TRS(position, rotation, scale);
        MaterialPropertyBlock props = new MaterialPropertyBlock();
        props.SetColor("_BaseColor", Color.red);

        Graphics.DrawMesh(arrow, matrix4X4, arrowMaterial, 0, null, 0, props);

        dragTextPosition = surface.aerodynamicSurfaceObject.centerPoint + transform.position - (direction * scale.magnitude);
    }

    void CalculateSubSurfaceEffects()
    {
        if (surface.surfaceType != AerodynamicSurface.SurfaceType.Standart) return;

        float clsclX = 1;
        float clsclY = 1;
        float cdsclX = 1;
        float cdsclY = 1;

        foreach (var subSurface in surface.subSurfaces)
        {
            //Slats increase the stall angle and amount of lift cl.scaleX++ cl.scaleY++
            //Slats increase drag cd.ScaleY++
            if (subSurface.surfaceType == AerodynamicSurface.SurfaceType.Slat)
            {
                var ratio = subSurface.aerodynamicSurfaceObject.area / surface.aerodynamicSurfaceObject.area;
                var angle = slatInput * slatMaxAngle;
                //Affect on CL
                clsclX += 1 * slatInput;
                clsclY += 1 * slatInput;
                //Affect on CD
                //cdsclX += 1 * slatInput;
                cdsclY += 1 * slatInput;
            }

            //Flaps increase the amount of lift cl.ScaleY++
            //Flaps increase the amount of drag cd.ScaleY++
            if (subSurface.surfaceType == AerodynamicSurface.SurfaceType.ControlSurface)
            {
                var ratio = subSurface.aerodynamicSurfaceObject.area / surface.aerodynamicSurfaceObject.area;
                var angle = flapInput * flapMaxAngle;
                //Affect on CL
                clsclX -= 0.2f * flapInput;
                clsclY += 1 * flapInput;
                //Affet on CD
                //cdsclX += 1 * flapInput;
                cdsclY += 1 * flapInput;
            }
        }
        
        ProjectUtilities.ManipulateGraph(surface.aerodynamicSurfaceObject.xfoildata.clCurve, ref CLxAlpha, clsclX, clsclY, false);
        ProjectUtilities.ManipulateGraph(surface.aerodynamicSurfaceObject.xfoildata.cdCurve, ref CDxAlpha, cdsclX, cdsclY, true);
    }

    

    private void Update()
    {
        CalculateLift();
        CalculateDrag();
        DrawIncomingWind();
        DrawLift();
        DrawDrag();
        CalculateSubSurfaceEffects();

        if (testAngles)
        {
            currentRuns = 0;
            lastTime = EditorApplication.timeSinceStartup;
            EditorApplication.update += EditorUpdate;
            testAngles = false;
        }

    }

    private void OnDrawGizmos()
    {
        if (!enabled) return;
        Handles.Label(liftTextPosition,"      Lift: " + liftForce.ToString("0.0N"));
        Handles.Label(dragTextPosition, "      Drag: " + dragForce.ToString("0.0N"));
    }
}
