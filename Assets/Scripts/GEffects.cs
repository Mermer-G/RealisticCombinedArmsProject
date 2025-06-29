using System.Collections;
using System.Collections.Generic;
using UnityEditor.SearchService;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.Rendering;
using UnityEngine.UI;

public class GEffects : MonoBehaviour
{
    GEffects instance;

    [SerializeField] Image dark;
    [SerializeField] Image red;
    [SerializeField] AudioClip[] bInSoft;
    [SerializeField] AudioClip[] bOutSoft;
    [SerializeField] AudioClip[] bInHard;
    [SerializeField] AudioClip[] bOutHard;
    [SerializeField] Camera povCamera;
    [SerializeField] AudioMixerGroup mixerGroup;
    CameraShake shaker;

    [SerializeField] bool A;
    [SerializeField] bool B;
    [SerializeField] bool C;
    [SerializeField] bool D;



    float negativeFadeOut;



    private void Awake()
    {
        if (instance == null) instance = this;
    }

    // Start is called before the first frame update
    void Start()
    {
        shaker = povCamera.GetComponent<CameraShake>();

    }

    // Update is called once per frame
    void Update()
    {

    }

    private void FixedUpdate()
    {
        // Yorgunluða göre nefes alma seslerini çalma
        ManageBreathingSounds();
        ManageFadeOut();
    }
    [SerializeField] bool breathIn;
    [SerializeField] AudioClip currentClip;
    [SerializeField] float nextBreathTime = 0f;
    [SerializeField] float G;
    [SerializeField] float positiveFadeOut;
    [SerializeField] float unconciousUntil;
    [SerializeField] bool unconcious;
    void ManageFadeOut()
    {
        if (unconcious && unconciousUntil > Time.time)
        {
            return;
        }
        else if (unconcious && unconciousUntil < Time.time)
        {
            unconcious = false;
        }

        G = AerodynamicModel.GForce;

        if (G < 4)
        {
            shaker.StopShake();
        }
        else shaker.StartConstantShake(ProjectUtilities.Map(G, 4f, 9.5f, 0.0005f, 0.0015f));

        if (G < 8.5f && G > 3.5f && positiveFadeOut < 10)
        {
            positiveFadeOut = ProjectUtilities.Map(G, 3.5f, 8.5f, 0, 10);
        }
        else if (G > 8.5f && G < 9.5f)
        {
            positiveFadeOut += ProjectUtilities.Map(G, 8.5f, 9.5f, 0.016f, 0.032f);
        }
        else if (G > 9.5f)
        {
            positiveFadeOut += 0.15f;
        }
        else if (positiveFadeOut >= 20)
        {
            unconciousUntil = Time.time + Random.Range(2, 10);
            unconcious = true;
            positiveFadeOut = 19.99f;
        }
        else if (positiveFadeOut < 20 && positiveFadeOut > 10)
        {
            positiveFadeOut -= 0.016f;
        }
        else if (positiveFadeOut > 0 && positiveFadeOut < 10)
            positiveFadeOut -= 0.008f;

        if (positiveFadeOut > 10)
        {
            var color = dark.color;
            color.a = Mathf.Clamp(ProjectUtilities.Map(positiveFadeOut, 12.5f, 20, 0, 1), 0, 1);
            dark.color = color;
        }
        else dark.color = new Color(0, 0, 0, 0);
    }

    void ManageBreathingSounds()
    {
        //fadeOut derecesine bak. 0 - 10 arasýnda
        //3.5G - 6G -> 2.5'a kadar. (Soft/offset 2.5-0.5/volume: 0.2-1)

        //6G - 7.5G -> 5'e kadar. (Soft/offset 0.5-1/volume: 1)
        //7.5G - 9.5G -> 7.5'a kadar (Hard/offset 0.5-1/volume: 1-1.5)
        //9.5G ve üzeri 9.8 sonrasý exponential -> 10 Kadar, 9'da kararma baþlar. 10'da bilinç kaybedilir 2-10 saniye arasýnda. (Hard/offset 0-(-0.2)/volume: 1.5)

        //In out mu ona bak.
        //Gruba bak.
        //Rastgele gruptan seç.
        //Rastgele pitch seç.
        //Rastgele volume seç
        //pitch ve clip uzunluðuyla sonraki çalma zamanýný hesapla.
        //Sesi belirtilen parametlerele çal.
        //bir sonraki çalma zamanýný bekle.

        if (Time.time >= nextBreathTime) // Zaman dolduysa, bir ses çal
        {
            if (positiveFadeOut <= 0 && !A && !B && !C && !D)
            {
                return;
            }

            if ((positiveFadeOut < 2.5f || A) && !B && !C && !D)
            {
                var clip = breathIn ? GetRandomClipFromGroup(bInSoft) : GetRandomClipFromGroup(bOutSoft);
                var pitch = 1;//Random.Range(0.9f, 1.1f);
                var volume = 1.5f;
                var offset = 1;//ProjectUtilities.Map(positiveFadeOut, 0, 2.5f, 2.5f, 0.5f);
                breathIn = !breathIn;

                SoundManager.instance.PlayInPoolExternal(clip, mixerGroup, povCamera.transform, volume, pitch);

                var waitForNext = (clip.length / pitch) + offset;
                nextBreathTime = Time.time + waitForNext;
                currentClip = clip;
            }
            
            else if (positiveFadeOut > 2.5f && positiveFadeOut < 5 || B)
            {
                var clip = breathIn ? GetRandomClipFromGroup(bInSoft) : GetRandomClipFromGroup(bOutSoft);
                var pitch = Random.Range(0.9f, 1.1f);
                var volume = 1.5f;
                var offset = 0.1f; //ProjectUtilities.Map(positiveFadeOut, 2.5f, 5, 1, 0.5f);
                breathIn = !breathIn;

                SoundManager.instance.PlayInPoolExternal(clip, mixerGroup, povCamera.transform, volume, pitch);

                var waitForNext = clip.length / pitch + offset;
                nextBreathTime = Time.time + waitForNext;
                currentClip = clip;
            }

            else if (positiveFadeOut > 5 && positiveFadeOut < 7.5f || C)
            {
                var clip = breathIn ? GetRandomClipFromGroup(bInHard) : GetRandomClipFromGroup(bOutHard);
                var pitch = Random.Range(0.9f, 1.1f);
                var volume = 1;// ProjectUtilities.Map(positiveFadeOut, 5, 7.5f, 1, 1.5f);
                var offset = 0.5f; //ProjectUtilities.Map(positiveFadeOut, 5, 7.5f, 1, 0.5f);
                breathIn = !breathIn;

                SoundManager.instance.PlayInPoolExternal(clip, mixerGroup, povCamera.transform, volume, pitch);

                var waitForNext = clip.length / pitch + offset;
                nextBreathTime = Time.time + waitForNext;
                currentClip = clip;
            }

            else if (positiveFadeOut > 7.5f || D)
            {
                var clip = breathIn ? GetRandomClipFromGroup(bInHard) : GetRandomClipFromGroup(bOutHard);
                var pitch = Random.Range(0.9f, 1.1f);
                var volume = 1.5f;
                var offset = -0.1f; //ProjectUtilities.Map(positiveFadeOut, 7.5f, 10, 0, -0.2f);
                breathIn = !breathIn;

                SoundManager.instance.PlayInPoolExternal(clip, mixerGroup, povCamera.transform, volume, pitch);

                var waitForNext = clip.length / pitch + offset;
                nextBreathTime = Time.time + waitForNext;
                currentClip = clip;
            }
        }
    }
    


    AudioClip GetRandomClipFromGroup(AudioClip[] clipGroup)
    {
        var index = Random.Range(0, clipGroup.Length);
        return clipGroup[index];
    }
}
