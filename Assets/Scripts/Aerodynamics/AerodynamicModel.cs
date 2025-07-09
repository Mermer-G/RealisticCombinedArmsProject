using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEditor;
using System;

[ExecuteAlways]
public class AerodynamicModel : MonoBehaviour
{
    [SerializeField] Transform cockpitCamera;
    [SerializeField] Rigidbody rb;
    [SerializeField] Mesh arrow;
    [SerializeField] Material arrowMat;

    public List<AerodynamicSurface> surfaces = new List<AerodynamicSurface>();
    [SerializeField] F110Engine engine;
    public static float engineRPMPercent;

    [SerializeField] Vector3 angularVelocity;

    [Header("Speed")]
    [SerializeField] Vector3 wind;
    [SerializeField] float speed;
    [SerializeField] float Mach;
    [SerializeField] float Alpha;
    public static float GForce;
    public static float alpha;
    Vector3 velocity;

    [Header("Thrust")]
    [SerializeField] float thrust;
    [SerializeField] float thrustMultiplier;
    [SerializeField] float realThrust;

    [Header("Lift")]
    [SerializeField] float lift;
    [SerializeField] float beneficialLift;
    [SerializeField] float benLiftRatio;
    [SerializeField] float liftMultiplier;
    [SerializeField] Vector3 centerOfLift;
    [SerializeField] string altitude;
    [HideInInspector] public static float alt;

    [Header("Drag")]
    [SerializeField] float drag;
    [SerializeField] float inducedDrag;
    [SerializeField] float totalDrag;
    [SerializeField] float dragMultiplier;

    [Header("Mass")]
    [SerializeField] float mass;
    [SerializeField] Vector3 centerOfMass;
    
    [Header("Conditions")]
    [SerializeField] float currentTemperature;
    [SerializeField] float temperatureAtSeaLevel;


    [Header("Debugging")]
    public bool drawSurfaces;
    [SerializeField] bool getAllSurfacesInChildren;
    [SerializeField] bool drawCenterOfMass;
    [SerializeField] bool drawCenterOfLift;
    [SerializeField] bool drawLiftVectors;
    [SerializeField] bool drawDragVectors;


    public void GetAllSurfacesInChildren()
    {
        if (!getAllSurfacesInChildren) return;
        surfaces.Clear();
        surfaces = GetComponentsInChildren<AerodynamicSurface>().ToList();
        getAllSurfacesInChildren = false;
    }

    [SerializeField] Dictionary<Transform, Vector3> liftValues = new Dictionary<Transform, Vector3>();
    [SerializeField] Dictionary<Transform, Vector3> dragValues = new Dictionary<Transform, Vector3>();
    void CalculateTotalForces()
    {
        //Misc
        currentTemperature = ProjectUtilities.CalculateTemparatureAtAltitude(temperatureAtSeaLevel, transform.position);

        TextFieldManager.Instance.CreateOrUpdateScreenField("AOA").Value($"AOA: {alpha:F1}");
        TextFieldManager.Instance.CreateOrUpdateScreenField("Lift").Value($"Total Lift: {lift:F1}");
        TextFieldManager.Instance.CreateOrUpdateScreenField("BenLift").Value($"Beneficial Lift: {beneficialLift:F1}");
        TextFieldManager.Instance.CreateOrUpdateScreenField("BenLiftRatio").Value($"BL/L Ratio: {benLiftRatio:P0}"); // % format�nda

        TextFieldManager.Instance.CreateOrUpdateScreenField("Drag").Value($"Main Drag: {drag:F1}");
        TextFieldManager.Instance.CreateOrUpdateScreenField("Induced").Value($"Induced Drag: {inducedDrag:F1}");
        TextFieldManager.Instance.CreateOrUpdateScreenField("TotalDrag").Value($"Total Drag: {totalDrag:F1}");

        TextFieldManager.Instance.CreateOrUpdateScreenField("Thrust").Value($"Thrust: {thrust * thrustMultiplier:F1}");
        //Lift
        liftValues.Clear();
        Vector3 totalLiftVector = Vector3.zero;
        foreach (var surface in surfaces)
        {
            if (surface.surfaceType != AerodynamicSurface.SurfaceType.Standart) continue;
            var force = CalculateLift(surface);
            if (force == Vector3.negativeInfinity || force == Vector3.positiveInfinity)
            {
                force = Vector3.zero;
            }
            UpdateValues(surface, force, VType.lift);
            rb.AddForceAtPosition(force * liftMultiplier * surface.surfaceLiftMultiplier, surface.transform.TransformPoint(surface.aerodynamicSurfaceObject.centerPoint));
            totalLiftVector += force;
            TextFieldManager.Instance.CreateOrUpdateWorldField(surface.name).Value("Lift: " + (int)force.magnitude).FontSize(0.1f).WorldPosition(surface.transform.position + surface.transform.up);
        }
        lift = totalLiftVector.magnitude * liftMultiplier;
        beneficialLift = CalculateBeneficialLift(totalLiftVector * liftMultiplier, velocity);
        benLiftRatio = beneficialLift / lift;
        inducedDrag = CalculateInducedDrag(totalLiftVector * liftMultiplier, -velocity);
        totalDrag = inducedDrag + drag;
        Debug.DrawLine(transform.position, transform.position + velocity.normalized * 10, Color.red, 0.01f);
        //Drag
        dragValues.Clear();
        Vector3 totalDragVector = Vector3.zero;
        foreach (var surface in surfaces)
        {
            if (surface.surfaceType != AerodynamicSurface.SurfaceType.Standart) continue;
            var force = CalculateDrag(surface);
            if (force == Vector3.negativeInfinity || force == Vector3.positiveInfinity)
            {
                force = Vector3.zero;
            }
            UpdateValues(surface, force, VType.drag);
            rb.AddForceAtPosition(force * dragMultiplier, surface.transform.TransformPoint(surface.aerodynamicSurfaceObject.centerPoint));
            totalDragVector += force;
        }
        drag = totalDragVector.magnitude * dragMultiplier;
    }

    void InitializeValues()
    {
        foreach (var surface in surfaces)
        {
            liftValues.Add(surface.transform, Vector3.zero);
            dragValues.Add(surface.transform, Vector3.zero);
        }
    }
    enum VType
    {
        drag,
        lift
    }
    void UpdateValues(AerodynamicSurface surface, Vector3 value, VType type)
    {
        switch (type)
        {
            case VType.lift:
                liftValues[surface.transform] = value;
                break;
            


            case VType.drag:
                dragValues[surface.transform] = value;
                break;
        }
    }

    #region Lift
    Vector3 CalculateLift(AerodynamicSurface surface)
    {
        var alpha = CalculateAlpha(surface);
        float CL = Calculate3DLiftCoefficient(alpha, surface);
        var rho = ProjectUtilities.CalculateAirDensity(surface.transform.position, currentTemperature);
        var wingArea = CalculatePolygonArea3D(surface.aerodynamicSurfaceObject.localPoints);
        var inversedVelocity = CalculateRelativeAirFlow(surface.transform.position);
        var L = CL * rho * wingArea * (Mathf.Pow(inversedVelocity.magnitude, 2) / 2);
        var direction = CalculateLiftDirection(surface);
        if (surface.CompareTag("Respawn"))
        {
            //print(surface.gameObject.name + ": " + alpha);
        }
        return (L * direction);
    }

    //This will just return the up rotation wihch we are going to apply the lift.
    Vector3 CalculateLiftDirection(AerodynamicSurface surface)
    {
        Vector3 up;
        if (surface.topWayIndicator == AerodynamicSurface.TopWayIndicator.plusY) up = surface.transform.up;
        else if (surface.topWayIndicator == AerodynamicSurface.TopWayIndicator.minusY) up = -surface.transform.up;
        else if (surface.topWayIndicator == AerodynamicSurface.TopWayIndicator.transformPlusY) up = surface.transform.root.up;
        else if (surface.topWayIndicator == AerodynamicSurface.TopWayIndicator.transformPlusX) up = surface.transform.root.right;
        else if (surface.topWayIndicator == AerodynamicSurface.TopWayIndicator.transformMinusX) up = -surface.transform.root.right;
        else up = -surface.transform.root.up;
        return up;
    }

    //AnimationCurve tempLiftCurve = new AnimationCurve();
    float Calculate3DLiftCoefficient(float alpha, AerodynamicSurface surface)
    {
        var cl_alpha_graph = surface.aerodynamicSurfaceObject.xfoildata.clCurve;
        Vector2 scale = CalculateScales(surface, VType.lift);
        //ProjectUtilities.ManipulateGraph(cl_alpha_graph, ref tempLiftCurve, scale.x, scale.y, true);
        cl_alpha_graph = ProjectUtilities.ManipulateGraphReturn(cl_alpha_graph, scale.x, scale.y, true);
        var maincl = cl_alpha_graph.Evaluate(alpha);
        var cl = maincl + CalculateAdditionalCl(surface, 1f) + CalculateLERXAddition(surface, alpha, maincl);
        //var cl = cl_alpha_graph.Evaluate(alpha) + CalculateAdditionalCl(surface, 1f);
        var AR = surface.aerodynamicSurfaceObject.aspectRatio;
        var deltaTMax = surface.aerodynamicSurfaceObject.sweepAngle;
        var e = 1 / (2 - AR + Mathf.Sqrt(4 + (AR * AR * (1 + Mathf.Pow(Mathf.Tan(deltaTMax * Mathf.Deg2Rad), 2)))));
        
        return cl / (1 + (cl / Mathf.PI * e * AR));
    }

    private float CalculateLERXAddition(AerodynamicSurface surface, float alpha, float maincl)
    {
        foreach (var item in surface.subSurfaces)
        {
            if (item.surfaceType == AerodynamicSurface.SurfaceType.LERX)
            {
                return alpha > item.LERXStart && alpha < item.LERXEnd ? item.LERXcl - maincl : 0;
            }
        }
        return 0;
    }

    float CalculateBeneficialLift(Vector3 lift, Vector3 velocity)
    {
        var perpendicular = Vector3.Cross(velocity, transform.right);
        var beneficialFactor = Vector3.Dot(perpendicular.normalized, lift.normalized);

        return beneficialFactor * lift.magnitude;
    }

    float CalculateInducedDrag(Vector3 lift, Vector3 relativeVelocity)
    {
        var inducedFactor = Vector3.Dot(lift.normalized, relativeVelocity.normalized);

        return inducedFactor * lift.magnitude;
    }
    #endregion

    #region Drag
    //AnimationCurve tempDragCurve = new AnimationCurve();
    //float cdXS
    Vector3 CalculateDrag(AerodynamicSurface surface)
    {
        var cd_alpha_graph = surface.aerodynamicSurfaceObject.xfoildata.cdCurve;
        Vector2 scale = CalculateScales(surface, VType.drag);
        //ProjectUtilities.ManipulateGraph(cd_alpha_graph, ref tempDragCurve, scale.x, scale.y, true);
        cd_alpha_graph = ProjectUtilities.ManipulateGraphReturn(cd_alpha_graph, scale.x, scale.y, true);
        var alpha = CalculateAlpha(surface);
        var CD = cd_alpha_graph.Evaluate(alpha);
        var rho = ProjectUtilities.CalculateAirDensity(surface.transform.position, currentTemperature);
        var wingArea = CalculatePolygonArea3D(surface.aerodynamicSurfaceObject.localPoints);
        var inversedVelocity = -velocity;//CalculateRelativeAirFlow(surface.transform.position);
        var D = CD * rho * wingArea * (Mathf.Pow(inversedVelocity.magnitude, 2) / 2);
        var direction = inversedVelocity.normalized;
        return D * direction;
    }
    #endregion

    #region Alpha
    float CalculateAlpha(AerodynamicSurface surface)
    {
        var incomingRelativeWind = wind + velocity;
        var projectedWind = Vector3.ProjectOnPlane(incomingRelativeWind, surface.transform.right);
        var relativeAlpha = -Vector3.SignedAngle(projectedWind, surface.transform.forward, surface.transform.right);
        if (surface.topWayIndicator == AerodynamicSurface.TopWayIndicator.plusY) return relativeAlpha;
        else return -relativeAlpha;
    }

    float CalculateAlpha(Transform tf)
    {
        var incomingRelativeWind = -velocity;
        var projectedWind = Vector3.ProjectOnPlane(incomingRelativeWind, tf.right);
        var relativeAlpha = -Vector3.SignedAngle(projectedWind, -tf.forward, tf.right);
        return relativeAlpha;
    }
    #endregion

    #region Misc
    Vector3 CalculateRelativeAirFlow(Vector3 position)
    {
        Vector3 r = transform.position - position;  // Y�zeyin g�vdeye uzakl���
        Vector3 additionalVelocity = Vector3.Cross(rb.angularVelocity, r * 10); // A��sal h�zdan gelen ek hava ak���
        return additionalVelocity - rb.linearVelocity;
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
    /// <summary>
    /// This method calculates the additional Cl of control surfaces.
    /// </summary>
    /// <param name="surface"></param>
    /// <param name="multiplier">How much additonal cl it should crate for max deflection</param>
    /// <returns></returns>
    float CalculateAdditionalCl(AerodynamicSurface surface, float multiplier)
    {
        float cladd = 0;
        foreach (var subSurface in surface.subSurfaces)
        {
            if (subSurface.surfaceType == AerodynamicSurface.SurfaceType.ControlSurface)
            {
                var projectedFVector = Vector3.ProjectOnPlane(subSurface.transform.forward, surface.transform.right);
                var angle = -Vector3.SignedAngle(surface.transform.forward, projectedFVector, transform.right);

                //�stedi�im a��ya ne kadar yak�nsa o kadar 1 'e yakla�acak.
                //Flap max angle = 30
                var effect = angle / 30;

                cladd += effect * multiplier;
            }
        }
        return cladd;
    }

    Vector2 CalculateScales(AerodynamicSurface surface, VType type)
    {
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
                var projectedFVector = Vector3.ProjectOnPlane(subSurface.transform.forward, surface.transform.right);
                var angle = Vector3.SignedAngle(surface.transform.forward, projectedFVector, transform.right);
                //angle = Mathf.Abs(angle);

                //�stedi�im a��ya ne kadar yak�nsa o kadar 1 'e yakla�acak.
                //Slat max angle = 25
                var effect = angle / 25;

                switch (type)
                {
                    case VType.lift:
                        //Affect on CL
                        clsclX += 1 * effect;
                        clsclY += 1 * effect;
                        break;
                    case VType.drag:
                        //Affect on CD
                        cdsclY += 1 * effect;
                        break;
                }
            }

            //Flaps increase the amount of lift cl.ScaleY++
            //Flaps increase the amount of drag cd.ScaleY++
            if (subSurface.surfaceType == AerodynamicSurface.SurfaceType.ControlSurface)
            {
                var projectedFVector = Vector3.ProjectOnPlane(subSurface.transform.forward, surface.transform.right);
                var angle = -Vector3.SignedAngle(surface.transform.forward, projectedFVector, transform.right);
                //angle = Mathf.Abs(angle);

                //�stedi�im a��ya ne kadar yak�nsa o kadar 1 'e yakla�acak.
                //Flap max angle = 30
                var effect = angle / 30;
                switch (type)
                {
                    case VType.lift:
                        //Affect on CL
                        //clsclX -= 0.2f * effect;
                        //clsclY += 1 * effect;
                        break;
                    case VType.drag:
                        //Affet on CD
                        cdsclY += 1 * effect;
                        break;
                }
            }

            
        }

        if (type == VType.lift) return new Vector2(clsclX, clsclY);
        else return new Vector2(cdsclX, cdsclY);

    }
    bool OriginShifted;
    void DetectOriginShifting(Vector3 shiftingAmount)
    {
        OriginShifted = true;
    }
    #endregion

    private void OnEnable()
    {
        FloatingOrigin.originShifted += DetectOriginShifting;
    }

    private void OnDisable()
    {
        FloatingOrigin.originShifted -= DetectOriginShifting;
    }

    private void Update()
    {
        if (OriginShifted)
        {
            return;
        }
        GetAllSurfacesInChildren();
        velocity = rb.linearVelocity;
        speed = rb.linearVelocity.magnitude;
        var newAlpha = CalculateAlpha(transform);
        alpha = Mathf.Lerp(alpha, newAlpha, 0.01f);
        engineRPMPercent = engine.RPMPercent;
        var newG = Vector3.Dot(Vector3.up, transform.up) - (Quaternion.Inverse(transform.rotation) * rb.angularVelocity).x * speed / ProjectUtilities.g;
        GForce = Mathf.Lerp(GForce, newG, 0.01f);
        
        //if (GForce > 3) print("Gforce Plus is out of bound!");
        //if (GForce < -3) print("Gforce Plus is out of bound!");

        if (Application.isPlaying)
            alt = ProjectUtilities.CalculateAltitude(transform.position) * 3.2808f;
        Mach = speed / ProjectUtilities.CalculateSpeedOfSound(alt);
        altitude = alt.ToString("0");

        StringEventManager.Invoke("1-Speed", (speed * 1.943).ToString("0"));
        StringEventManager.Invoke("1-Altitude", altitude);
        StringEventManager.Invoke("1-Gforce", GForce.ToString("0.0"));
        StringEventManager.Invoke("1-Mach", Mach.ToString("0.00"));
        Vector3EventManager.Invoke("1-FlightPathIndicator", (cockpitCamera.position) + velocity.normalized * 100);
        Vector3EventManager.Invoke("1-BoreSight", (cockpitCamera.position) + transform.forward * 100);

        altitude = (alt * 3.2808f).ToString("0 Feet");
        if (drawLiftVectors) DrawLiftVectors();

        if (drawDragVectors) DrawDragVectors();
    }



    void ApplyThrust(float th, float multiplier)
    {
        rb.AddForce(transform.forward * th * multiplier, ForceMode.Force);
    }
    
    private void FixedUpdate()
    {

        if (OriginShifted)
        {
            OriginShifted = false;
            return;
        }
        CalculateTotalForces();

        thrust = engine.thrust;
        realThrust = thrust * thrustMultiplier;
        ApplyThrust(thrust, thrustMultiplier);
        ShowAcceleration();
    }

    

    private void Awake()
    {
        lastVelocity = rb.linearVelocity;
        rb = GetComponent<Rigidbody>();
        if (Application.isPlaying)
            temperatureAtSeaLevel = ProjectUtilities.CalculateTemparatureAtSeaLevel(currentTemperature, transform.position);

        rb.mass = mass;
        rb.centerOfMass = centerOfMass;

        InitializeValues();
    }

    private void OnDrawGizmos()
    {
        if (drawCenterOfMass) DrawCenterOfMass();
    }

    private Vector3 lastVelocity;
    public Vector3 acceleration;

    void ShowAcceleration()
    {
        Vector3 currentVelocity = rb.linearVelocity;
        float deltaTime = Time.fixedDeltaTime;

        acceleration = (currentVelocity - lastVelocity) / deltaTime;
        lastVelocity = currentVelocity;

        //Debug.Log("Acceleration: " + acceleration + " | Magnitude: " + acceleration.magnitude.ToString("0.0"));
    }

    #region Draw
    void DrawCenterOfMass()
    {
        Gizmos.color = Color.magenta;
        Gizmos.DrawWireSphere(transform.position + centerOfMass, 0.3f);
    }

    void DrawLiftVectors()
    {
        Vector3 scale;
        Vector3 position;
        Quaternion rotation;
        Matrix4x4 matrix4X4;
        MaterialPropertyBlock props = new MaterialPropertyBlock();
        props.SetColor("_BaseColor", Color.green);

        float force;
        foreach (var item in liftValues)
        {
            if (item.Value == Vector3.zero) continue;
            force = item.Value.magnitude;
            var surface = item.Key.GetComponent<AerodynamicSurface>();

            scale = Vector3.one / 3 * Mathf.Clamp(force / 2000, 0.1f, 6);
            position = surface.transform.position + surface.transform.rotation * surface.aerodynamicSurfaceObject.centerPoint;

            //var a = surface.transform.position + surface.transform.rotation * surface.aerodynamicSurfaceObject.centerPoint;
            //Debug.DrawLine(a, a + (item.Value.normalized) * 2, Color.magenta, 0.1f);

            rotation = Quaternion.LookRotation(item.Value.normalized);
            matrix4X4 = Matrix4x4.TRS(position, rotation, scale);
            Graphics.DrawMesh(arrow, matrix4X4, arrowMat, 0, null, 0, props);
        }
    }

    void DrawDragVectors()
    {
        Vector3 scale;
        Vector3 position;
        Quaternion rotation;
        Matrix4x4 matrix4X4;
        MaterialPropertyBlock props = new MaterialPropertyBlock();
        props.SetColor("_BaseColor", Color.red);

        float force;
        foreach (var item in dragValues)
        {
            if (item.Value == Vector3.zero) continue;
            force = item.Value.magnitude;
            var surface = item.Key.GetComponent<AerodynamicSurface>();

            scale = Vector3.one / 3 * Mathf.Clamp(force / 2000, 0.1f, 4);
            position = surface.transform.position + surface.transform.rotation * surface.aerodynamicSurfaceObject.centerPoint;

            //var a = surface.transform.position + surface.transform.rotation * surface.aerodynamicSurfaceObject.centerPoint;
            //Debug.DrawLine(a, a + (item.Value.normalized) * 2, Color.magenta, 0.1f);

            rotation = Quaternion.LookRotation(item.Value.normalized);
            matrix4X4 = Matrix4x4.TRS(position, rotation, scale);
            Graphics.DrawMesh(arrow, matrix4X4, arrowMat, 0, null, 0, props);
        }
    }

    #endregion

}

[CustomEditor(typeof(AerodynamicModel)), CanEditMultipleObjects]
public class AerodynamicModelEditor : Editor
{
    private void OnSceneGUI()
    {
        var model = (AerodynamicModel)target;
        if (!model.enabled) return;
        DrawSurfaces(model);
    }

    void DrawSurfaces(AerodynamicModel model)
    {
        if (!model.drawSurfaces) return;
        foreach (var surface in model.surfaces)
        {
            if (surface == null)
            {
                ((AerodynamicModel)target).GetAllSurfacesInChildren();
                break;
            }
            var sO = surface.aerodynamicSurfaceObject;
            Handles.color = sO.fillColor;
            Vector3[] polygonCorners = new Vector3[sO.localPoints.Count];
            for (int i = 0; i < sO.localPoints.Count; i++)
            {
                polygonCorners[i] = surface.transform.TransformPoint(sO.localPoints[i]);
            }
            Handles.DrawAAConvexPolygon(polygonCorners);
        }
    }
}