using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Aerodynamics/FLCSPIDData")]
public class FLCSPIDData : ScriptableObject
{
    public AnimationCurve P_Pitch;
    public AnimationCurve P_Roll;
    public AnimationCurve P_Yaw;

    public AnimationCurve I_Pitch;
    public AnimationCurve I_Roll;
    public AnimationCurve I_Yaw;

    public AnimationCurve ISat_Pitch;
    public AnimationCurve ISat_Roll;
    public AnimationCurve ISat_Yaw;

    public AnimationCurve D_Pitch;
    public AnimationCurve D_Roll;
    public AnimationCurve D_Yaw;
}
