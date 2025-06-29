using System.Drawing;
using System.Linq;
using UnityEditor;
using UnityEngine;

[ExecuteAlways]
public class MirrorObjects : MonoBehaviour
{
    void OnEnable()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.update += UpdateMirror;
#endif
    }

    void OnDisable()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.update -= UpdateMirror;
#endif
    }

    void UpdateMirror()
    {
        if (!Application.isPlaying && enabled && Selection.objects.Contains(gameObject))
        {
            Mirror();
        }
    }

    public Transform mainTransform;
    public Transform mirroredTransform;
    public Transform mirrorTransform;
    public Vector3 axisOfReflection;
    void Mirror()
    {
        if (mainTransform == null) return;
        if (mirroredTransform == null) return;
        if (mirrorTransform == null) return;
        //Position

        mirrorTransform.position = mirrorTransform.position;
        var mirrorToT1 = mainTransform.position - mirrorTransform.position;

        mirrorToT1 = new Vector3(mirrorToT1.x * axisOfReflection.x, mirrorToT1.y * axisOfReflection.y, mirrorToT1.z * axisOfReflection.z);
        mirroredTransform.position = mainTransform.position + 2 * -mirrorToT1;

        Vector3 f1 = mainTransform.forward;
        Vector3 u1 = mainTransform.up;

        f1 = Vector3.Reflect(f1, axisOfReflection);
        u1 = -Vector3.Reflect(u1, axisOfReflection);

        mirroredTransform.rotation = Quaternion.LookRotation(f1, u1);
    }
}