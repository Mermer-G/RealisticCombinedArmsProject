using System;
#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;

public static class ProjectUtilities
{
    /// <summary>
    /// Temperature decrease rate 
    /// </summary>
    public static float L = 0.0065f;

    /// <summary>
    /// Gravitional accel
    /// </summary>
    public static float g = 9.80665f;

    /// <summary>
    /// air gas constant
    /// </summary>
    public static float R = 287.05f;

    public static float CalculateAltitude(Vector3 position)
    {
        float altitude = position.y - SeaLevelIndicator.instance.position.y;
        return altitude;
    }

    /// <summary>
    /// Returns the temp at given altitude in celcius;
    /// </summary>
    /// <param name="tempAtSeaLevel"></param>
    /// <param name="alt"></param>
    /// <returns></returns>
    public static float CalculateTemparatureAtAltitude(float tempAtSeaLevel, Vector3 position)
    {
        float altitude = position.y - SeaLevelIndicator.instance.position.y;
        return tempAtSeaLevel - (L * altitude);
    }

    /// <summary>
    /// Returns the sea level temp with the given temp and altitude.
    /// </summary>
    /// <param name="tempAtSeaLevel"></param>
    /// <param name="alt"></param>
    /// <returns></returns>
    public static float CalculateTemparatureAtSeaLevel(float tempCelcius, Vector3 position)
    {
        float altitude = position.y - SeaLevelIndicator.instance.position.y;
        return tempCelcius + (L * altitude);
    }

    /// <summary>
    /// Calculates a numbers equal from a range to another.
    /// </summary>
    /// <param name="x">The number</param>
    /// <param name="xMin">1st range's min</param>
    /// <param name="xMax">1st range's max</param>
    /// <param name="yMin">2nd range's min</param>
    /// <param name="yMax">2nd range's max</param>
    /// <returns></returns>
    public static float MapWithSign(float x, float xMin, float xMax, float yMin, float yMax)
    {
        float sign = Mathf.Sign(x);  // İşareti kaydet (-1 veya 1)
        float absX = Mathf.Abs(x);   // Mutlak değeri al

        float mapped = yMin + ((yMax - yMin) / (xMax - xMin)) * (absX - xMin);

        return mapped * sign;  // Orijinal işareti geri ekle
    }

    public static float Map(float x, float xMin, float xMax, float yMin, float yMax)
    {
        return yMin + ((yMax - yMin) / (xMax - xMin)) * (x - xMin);
    }

    public static float CalculateAirDensity(Vector3 position, float temperatureCelsius, float rho0 = 1.225f)
    {
        float altitude = position.y - SeaLevelIndicator.instance.position.y;
        var T0 = temperatureCelsius + 273.15f;

        // Troposferde sıcaklık değişimi
        float T = T0 - (L * altitude);

        // Hava yoğunluğu formülü
        float rho = rho0 * Mathf.Pow((T / T0), ((g / (R * L)) - 1));

        return rho;
    }

    public static float CalculateSeaLevelDensity(float temperatureCelsius)
    {
        // Sabitler
        float P0 = 101325; // Pa (Deniz seviyesi basıncı)
        float R = 287.05f; // J/(kg·K) (Havanın gaz sabiti)

        // Celsius'u Kelvin'e çevir
        float T0 = temperatureCelsius + 273.15f;

        // Yoğunluk hesaplama
        float rho0 = P0 / (R * T0);

        return rho0; // kg/m³ cinsinden yoğunluk
    }

    public static float PID(float pGain, float dGain, float iGain, ref float iStored, float iSaturation, float error, ref float lastError)
    {
        //calculate P
        float P = error * pGain;

        
        //Calculate D
        var errorRateOfChange = (error - lastError);
        float D = errorRateOfChange * dGain;
        lastError = error;

        //Calculate I
        iStored = Mathf.Clamp(iStored + error, -iSaturation, iSaturation);
        float I = iGain * iStored;

        return P + I + D;
    }

    public static float P(float pGain, float error)
    {
        //calculate P
        float P = error * pGain;

        return P;
    }

    public static float PD(float pGain, float dGain, float error, ref float lastError)
    {
        //calculate P
        float P = error * pGain;

        //Calculate D
        var errorRateOfChange = (error - lastError);
        float D = errorRateOfChange * dGain;
        lastError = error;

        return P + D;
    }

    /// <summary>
    /// Softens a value below a certain error. Or returns the same value
    /// </summary>
    /// <param name="value">The value to be softened</param>
    /// <param name="currentPosition"></param>
    /// <param name="targetPosition"></param>
    /// <param name="softeningRange">How close the softening should begin?</param>
    /// <returns></returns>
    public static float SoftenInRange(float value, float currentPosition, float targetPosition, float softeningRange)
    {
        float distance = targetPosition - currentPosition;

        if (distance < softeningRange)
        {
            float scale = distance / softeningRange; // 1 → 0 arasında yumuşak azalma
            return value * scale;
        }

        return value; // Normal durumda değişim yok
    }


    public static float RoundToZero(float value, float threshold = 0.001f)
    {
        return Mathf.Abs(value) < threshold ? 0f : value;
    }

    /// <summary>
    /// multiplies a graph by given scale and symetry parameters.
    /// Use symetrical: false for cl and symetrical: true for cd
    /// </summary>
    /// <param name="baseGraph"></param>
    /// <param name="output"></param>
    /// <param name="scaleX"></param>
    /// <param name="scaleY"></param>
    /// <param name="symetrical"></param>
    public static void ManipulateGraph(AnimationCurve baseGraph, ref AnimationCurve output, float scaleX, float scaleY, bool symetrical)
    {
        Keyframe[] keyframes = new Keyframe[baseGraph.length];
        for (int i = 0; i < baseGraph.length; i++)
        {
            Keyframe key = baseGraph.keys[i]; // Keyframe struct olduğu için kopya al
            if (symetrical)
            {
                if (key.time >= 0)
                {
                    key.time *= scaleX;
                    key.value *= scaleY;
                }
                else
                {
                    key.time *= scaleX;
                    key.value *= scaleY;
                }
            }
            else
            {
                if (key.time >= 0)
                {
                    key.time *= scaleX;
                    key.value *= scaleY;
                }
                else
                {
                    key.time /= scaleX;
                    key.value /= scaleY;
                }
            }

            keyframes[i] = key;
        }
        output.keys = keyframes;


    }

    public static AnimationCurve ManipulateGraphReturn(AnimationCurve baseGraph, float scaleX, float scaleY, bool symetrical)
    {
        Keyframe[] keyframes = new Keyframe[baseGraph.length];
        for (int i = 0; i < baseGraph.length; i++)
        {
            Keyframe key = baseGraph.keys[i]; // Keyframe struct olduğu için kopya al
            if (symetrical)
            {
                if (key.time >= 0)
                {
                    key.time *= scaleX;
                    key.value *= scaleY;
                }
                else
                {
                    key.time *= scaleX;
                    key.value *= scaleY;
                }
            }
            else
            {
                if (key.time >= 0)
                {
                    key.time *= scaleX;
                    key.value *= scaleY;
                }
                else
                {
                    key.time /= scaleX;
                    key.value /= scaleY;
                }
            }

            keyframes[i] = key;
        }
        return new AnimationCurve(keyframes);
    }

    public static float CalculateSpeedOfSound(float altitudeMeters)
    {
        // Constants
        float gamma = 1.4f; // Isentropic expansion factor for air
        float R = 287.05f;  // Specific gas constant for dry air (J/kg·K)

        // Temperature lapse rate and sea-level temperature
        float seaLevelTempC = 15f; // °C
        float lapseRate = 0.0065f; // °C/m
        float tempAtAltitudeC = seaLevelTempC - lapseRate * altitudeMeters;
        float tempAtAltitudeK = tempAtAltitudeC + 273.15f;

        // Speed of sound formula
        float speedOfSound = Mathf.Sqrt(gamma * R * tempAtAltitudeK);
        return speedOfSound; // in m/s
    }

    /// <summary>
    /// Invokes an action in a wanted frequency regardless of framerate.
    /// </summary>
    /// <param name="freq"></param>
    /// <param name="action">Which action should be invoked?</param>
    /// <param name="accumulator">This parameter should be zero at start and will be used as a container.</param>
    public static void InvokeFixedFreq(float freq, Action action, ref float accumulator)
    {
        float targetDeltaTime = 1f / freq;

        accumulator += Time.deltaTime;

        while (accumulator >= targetDeltaTime)
        {
            action.Invoke();
            accumulator -= targetDeltaTime;
        }
    }

    public static Quaternion MultiplyDelta(Quaternion delta, float multiplier)
    {
        delta.ToAngleAxis(out float angle, out Vector3 axis);
        return Quaternion.AngleAxis(angle * multiplier, axis);
    }

}

public static class InputExtensions
{
    public static bool ToBool(this float value, float threshold = 1f)
    {
        return Mathf.Approximately(value, threshold);
    }

    public static float ToFloat(this bool value, float trueValue = 1f, float falseValue = 1f)
    {
        return value ? trueValue : falseValue;
    }
}

public static class AnimationCurveExtensions
{
    /// <summary>
    /// Gives the X value for the given Y value. Returns the first value.
    /// </summary>
    /// <param name="curve"></param>
    /// <param name="yTarget"></param>
    /// <param name="skipCount">If there are x values for the same value, skip</param>
    /// <returns></returns>
    public static float EvaluateX(this AnimationCurve curve, float yTarget, int skipCount)
    {
        int keyCount = curve.length;

        for (int i = 0; i < keyCount - 1; i++)
        {
            Keyframe k1 = curve[i];
            Keyframe k2 = curve[i + 1];

            if ((k1.value <= yTarget && k2.value >= yTarget) || (k1.value >= yTarget && k2.value <= yTarget))
            {
                if (skipCount > 0)
                {
                    skipCount--;
                    continue;
                }
                // Doğrusal interpolasyon
                float t = (yTarget - k1.value) / (k2.value - k1.value);
                return Mathf.Lerp(k1.time, k2.time, t);
            }
        }

        // Eğer değer aralıkta değilse baş veya son noktayı döndür
        return curve.keys[0].time;
    }



}

#if UNITY_EDITOR
public static class SaveScriptableObjectTool
{
    [MenuItem("Tools/Save Selected ScriptableObject")]
    public static void SaveSelectedScriptableObject()
    {
        var selected = Selection.activeObject;

        if (selected == null)
        {
            Debug.LogWarning("No object selected.");
            return;
        }

        if (!(selected is ScriptableObject))
        {
            Debug.LogWarning("Selected object is not a ScriptableObject.");
            return;
        }

        EditorUtility.SetDirty(selected);
        AssetDatabase.SaveAssets();
        Debug.Log($"Saved changes to ScriptableObject: {selected.name}");
    }
}
#endif