using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.ShaderGraph.Legacy;
using UnityEngine;

public class Nozzle : MonoBehaviour
{
    [SerializeField] Transform aircraft;
    [SerializeField] Transform[] nozzleParts;
    [Range(0,.5f)] [SerializeField] float t;
    [SerializeField] Transform centerObj;

    [SerializeField] float maxT;
    [SerializeField] AnimationCurve RPMtoNozzle;
    // Start is called before the first frame update
    void Awake()
    {

        t = .5f;
    }

    // Update is called once per frame
    void Update()
    {
        UpdateNozzle();
    }

    void UpdateNozzle()
    {
        var rpmPercent = AerodynamicModel.engineRPMPercent;

        var a = RPMtoNozzle.Evaluate(rpmPercent);
        t = Mathf.Lerp(t, a * maxT, 0.05f);

        //t = Mathf.Clamp(Mathf.Lerp(t, ProjectUtilities.Map(rpmPercent, 100, 110, maxT, minT), 0.05f), minT, maxT);

        GenericEventManager.Invoke("1-NOZPOS", a);

        foreach (var part in nozzleParts)
        {
            var firstVector = centerObj.position - part.position;
            var secondVector = Vector3.ProjectOnPlane(firstVector, centerObj.up);
            var thirdVector = secondVector.normalized * -2;
            var fourth = Vector3.Lerp(centerObj.position, centerObj.position + thirdVector, t);
            Vector3 dir = fourth - part.position;
            part.rotation = Quaternion.LookRotation(dir, aircraft.up);
        }
    }

    //private void OnDrawGizmos()
    //{
    //    if (!drawGizmos) return;

    //    Gizmos.color = Color.red;
    //    Gizmos.DrawSphere(centerObj.position, 0.1f);

    //    foreach (var part in nozzleParts)
    //    {
    //        var firstVector = centerObj.position - part.position;
    //        var secondVector = Vector3.ProjectOnPlane(firstVector, centerObj.up);
    //        var thirdVector = secondVector.normalized * -2;
    //        var fourth = Vector3.Lerp(centerObj.position, centerObj.position + thirdVector, t);
    //        Gizmos.color = Color.blue;
    //        Gizmos.DrawSphere(fourth, 0.02f);
    //        Gizmos.color = Color.red;
    //        Vector3 dir = fourth - part.position;
    //        Gizmos.DrawLine(part.position, dir);
    //    }
    //}

}