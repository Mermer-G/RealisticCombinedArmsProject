using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.UIElements;
using static UnityEditor.Progress;

public class SoundManager : MonoBehaviour
{
    public static SoundManager instance;
    [SerializeField] AudioListener audioListener;
    [SerializeField] GameObject audioSourcePrefab;
    [SerializeField] AudioMixer audioMixer;

    [SerializeField] AudioMixerGroup[] mixerGroups;
    [SerializeField] AdvencedAudioClip[] advencedAudioClips;

    private List<AudioSource> audioSourcePool;
    [HideInInspector] public Camera activeCamera;

    Dictionary<string, Coroutine> audioCoroutines = new Dictionary<string, Coroutine>();

    [System.Serializable]
    public struct AdvencedAudioClip
    {
        public AudioClip clip;

        public ClipCameraData[] cameraData;
    }
    [System.Serializable]
    public struct ClipCameraData
    {
        public Camera camera;
        public AudioMixerGroup mixerGroup;
        public float dopplerLevel;
    }

    // Start is called before the first frame update
    void Awake()
    {
        if (instance == null) instance = this;
        
        InitializePool();
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    private void InitializePool()
    {
        audioSourcePool = new List<AudioSource>();
        for (int i = 0; i < 20; i++) // 10 kaynaklýk havuz
        {
            var obj = Instantiate(audioSourcePrefab, transform);
            var src = obj.GetComponent<AudioSource>();
            src.playOnAwake = false;
            audioSourcePool.Add(src);
        }
    }

    public void HandleCameraChange(Camera camera)
    {
        activeCamera = camera;
        audioListener.transform.SetParent(camera.transform);
        audioListener.transform.localPosition = Vector3.zero;
        audioListener.transform.localRotation = Quaternion.identity;

        foreach (var src in audioSourcePool)
        {
            if (!src.isPlaying) continue;

            foreach (var AdvClip in advencedAudioClips)
            {
                //If the source's clip name and advenced clip's clip name matches
                if (AdvClip.clip.name != src.clip.name) continue;
                //If the adv clip has a clipCameraData for active camera
                foreach (var cameraData in AdvClip.cameraData)
                {
                    if (cameraData.camera != camera) continue;
                    //If cameraData has a mixer for this camera
                    if (cameraData.mixerGroup != null) src.outputAudioMixerGroup = cameraData.mixerGroup;
                    src.dopplerLevel = cameraData.dopplerLevel;
                }
            }
        }
    }

    public bool FindClipByName(string clipName, out AdvencedAudioClip clip)
    {
        foreach (var item in advencedAudioClips)
        {
            if (item.clip.name == clipName)
            {
                clip = item;
                return true;
            }
        }
        clip = new AdvencedAudioClip();
        return false;
    }

    public bool StopInPool(string clipName)
    {
        bool stoppedPlaying = false;
        
        foreach (var item in audioCoroutines)
        {
            if (item.Key == clipName)
            {
                StopCoroutine(item.Value);
            }
        }
        audioCoroutines.Remove(clipName);
        foreach (var src in audioSourcePool)
        {
            if (src.clip == null) continue;
            if (src.clip.name != clipName) continue;
            if (!src.isPlaying) continue;
            
            src.Stop();
            stoppedPlaying = true;
        }
        return stoppedPlaying;
    }

    public bool StopInPoolExternal(AudioClip clip)
    {
        bool stoppedPlaying = false;

        foreach (var src in audioSourcePool)
        {
            if (src.clip == null) continue;
            if (src.clip != clip) continue;
            if (!src.isPlaying) continue;

            src.Stop();
            stoppedPlaying = true;
        }
        return stoppedPlaying;
    }

    public bool AdjustInPool(string clipName, float volume, float pitch)
    {
        foreach (var item in audioSourcePool)
        {
            if (item.clip == null) continue;
            if (item.clip.name != clipName) continue; 
            if (!item.isPlaying) continue;
            
            item.volume = volume;
            item.pitch = pitch;
            return true;
            
        }
        return false;
    }
    
    public bool IsClipPlaying(string clipName)
    {
        foreach (var item in audioSourcePool)
        {
            if (item.clip == null) continue;
            if (item.clip.name != clipName) continue;
            if (!item.isPlaying) continue;

            return true;
        }
        return false;
    }

    public void PlayAfterClip(string delayClipName, string clipName, Vector3 position, float volume, float pitch, float dopplerLevel = 0, bool loop = false)
    {
        audioCoroutines.Add(clipName, StartCoroutine(PlayAfter(delayClipName, clipName, position, volume, pitch, dopplerLevel, loop)));
    }

    public void PlayAfterClip(string delayClipName, string clipName, Transform parent, float volume, float pitch, float dopplerLevel = 0, bool loop = false)
    {
        audioCoroutines.Add(clipName, StartCoroutine(PlayAfter(delayClipName, clipName, parent, volume, pitch, dopplerLevel, loop)));
    }

    IEnumerator PlayAfter(string delayClipName, string clipName, Vector3 position, float volume, float pitch, float dopplerLevel = 0, bool loop = false)
    {
        FindClipByName(delayClipName, out AdvencedAudioClip delayClip);
        yield return new WaitForSeconds(delayClip.clip.length);
        PlayInPool(clipName, position, volume, pitch, dopplerLevel, loop);
        audioCoroutines.Remove(clipName);
    }

    IEnumerator PlayAfter(string delayClipName ,string clipName, Transform parent, float volume, float pitch, float dopplerLevel = 0, bool loop = false)
    {
        FindClipByName(delayClipName, out AdvencedAudioClip delayClip);
        yield return new WaitForSeconds(delayClip.clip.length);
        PlayInPool(clipName, parent, volume, pitch, dopplerLevel, loop);
        audioCoroutines.Remove(clipName);
    }

    public bool PlayInPool(string clipName, Vector3 position, float volume, float pitch, float dopplerLevel = 0, bool loop = false)
    {
        for (int i = 0;i < audioSourcePool.Count; i++)
        {
            if (audioSourcePool[i].isPlaying) continue;

            if (FindClipByName(clipName, out AdvencedAudioClip AdvClip))
            {
                audioSourcePool[i].clip = AdvClip.clip;
                audioSourcePool[i].volume = volume;
                audioSourcePool[i].pitch = pitch;
                audioSourcePool[i].transform.SetParent(null, true);
                audioSourcePool[i].transform.position = position;
                audioSourcePool[i].dopplerLevel = dopplerLevel;
                audioSourcePool[i].spatialBlend = 1;
                if (loop) audioSourcePool[i].loop = loop;

                //Assign the mixer group
                foreach (var cData in AdvClip.cameraData)
                {
                    if (cData.camera != activeCamera) continue;
                    //If cameraData has a mixer for this camera
                    if (cData.mixerGroup != null) audioSourcePool[i].outputAudioMixerGroup = cData.mixerGroup;
                    audioSourcePool[i].dopplerLevel = cData.dopplerLevel;
                }

                audioSourcePool[i].Play();
                return true;
            }
            else Debug.LogError("Audio clip with name has not been found: " + clipName);
        }
        return false;
    }

    public bool PlayInPool(string clipName, Transform parent, float volume, float pitch, float dopplerLevel = 0, bool loop = false)
    {
        for (int i = 0; i < audioSourcePool.Count; i++)
        {
            if (audioSourcePool[i].isPlaying) continue;

            if (FindClipByName(clipName, out AdvencedAudioClip AdvClip))
            {
                audioSourcePool[i].clip = AdvClip.clip;
                audioSourcePool[i].volume = volume;
                audioSourcePool[i].pitch = pitch;
                audioSourcePool[i].transform.SetParent(parent, false);
                audioSourcePool[i].transform.position = parent.position;
                audioSourcePool[i].dopplerLevel = dopplerLevel;
                audioSourcePool[i].spatialBlend = 1;
                audioSourcePool[i].loop = loop;

                //Assign the mixer group
                foreach (var cData in AdvClip.cameraData)
                {
                    if (cData.camera != activeCamera) continue;
                    //If cameraData has a mixer for this camera
                    if (cData.mixerGroup != null) audioSourcePool[i].outputAudioMixerGroup = cData.mixerGroup;
                    audioSourcePool[i].dopplerLevel = cData.dopplerLevel;
                }

                audioSourcePool[i].Play();
                return true;
            }
            else Debug.LogError("Audio clip with name has not been found: " + clipName);
        }
        return false;
    }

    public bool PlayInPoolExternal(AudioClip clip, AudioMixerGroup mixerGroup, Vector3 position, float volume, float pitch, float dopplerLevel = 0, bool loop = false)
    {
        for (int i = 0; i < audioSourcePool.Count; i++)
        {
            if (audioSourcePool[i].isPlaying) continue;

            audioSourcePool[i].clip = clip;
            audioSourcePool[i].outputAudioMixerGroup = mixerGroup;
            audioSourcePool[i].transform.position = position;
            audioSourcePool[i].volume = volume;
            audioSourcePool[i].pitch = pitch;
            audioSourcePool[i].dopplerLevel = dopplerLevel;
            audioSourcePool[i].loop = loop;
            audioSourcePool[i].Play();
            return true;
        }
        return false;
    }

    public bool PlayInPoolExternal(AudioClip clip, AudioMixerGroup mixerGroup, Transform parent, float volume, float pitch, float dopplerLevel = 0, bool loop = false)
    {
        for (int i = 0; i < audioSourcePool.Count; i++)
        {
            if (audioSourcePool[i].isPlaying) continue;

            audioSourcePool[i].clip = clip;
 
            audioSourcePool[i].outputAudioMixerGroup = mixerGroup;
            audioSourcePool[i].transform.SetParent(parent, false);
            audioSourcePool[i].volume = volume;
            audioSourcePool[i].pitch = pitch;
            audioSourcePool[i].dopplerLevel = dopplerLevel;
            audioSourcePool[i].loop = loop;
            audioSourcePool[i].Play();
            return true;
        }
        return false;
    }
}
