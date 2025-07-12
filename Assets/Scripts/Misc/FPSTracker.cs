using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public class FPSTracker : MonoBehaviour
{
    const int frameSampleCount = 300; // Son 300 frame
    List<float> frameTimes = new List<float>(frameSampleCount);

    float currentFPS;
    float averageFPS;
    float onePercentLowFPS;
    [SerializeField] float fpsUpdateRate;
    float lastFPSUpdate;
    void Update()
    {
        if (Time.time > lastFPSUpdate + fpsUpdateRate)
        {
            // Anlýk FPS
            currentFPS = 1f / Time.unscaledDeltaTime;
            lastFPSUpdate = Time.time;
        }
        

        // Frame time buffer
        frameTimes.Add(Time.unscaledDeltaTime);
        if (frameTimes.Count > frameSampleCount)
            frameTimes.RemoveAt(0);

        // Ortalama
        float total = 0f;
        foreach (var ft in frameTimes)
            total += ft;
        averageFPS = frameTimes.Count / total;

        // %1 Low FPS
        if (frameTimes.Count >= 100)
        {
            // En yavaþ %1 frame'leri al (frame time büyük)
            var sorted = frameTimes.OrderByDescending(x => x).ToArray();
            int onePercentCount = Mathf.Max(1, Mathf.FloorToInt(sorted.Length * 0.01f));
            float avgWorstFrameTime = 0f;
            for (int i = 0; i < onePercentCount; i++)
                avgWorstFrameTime += sorted[i];
            avgWorstFrameTime /= onePercentCount;
            onePercentLowFPS = 1f / avgWorstFrameTime;
        }

        // Ekrana bastýr
        TextFieldManager.Instance.CreateOrUpdateScreenField("FPS")
            .Value($"FPS: {currentFPS:0} | Avg: {averageFPS:0} | 1% Low: {onePercentLowFPS:0}")
            .End();
    }
}
