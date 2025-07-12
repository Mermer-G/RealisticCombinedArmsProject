using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ExtLightController : MonoBehaviour
{
    EnergyConsumerComponent consumer;

    [SerializeField] Light[] wingTailLights;
    [SerializeField] Light[] fuselageLights;
    [SerializeField] Light anti_CollisionLight;
    [SerializeField] Light[] formationLights;

    [SerializeField] float brightLevel;
    [SerializeField] float dimLevel;

    float[] steady = new float[] { .5f };
    float[] flash = new float[] { .5f, 1 };


    float[] sequence1 = new float[] { .5f, 1f };
    float[] sequence2 = new float[] { .5f, .5f, 1 };
    float[] sequence3 = new float[] { .5f, .5f, .5f, 1 };
    float[] sequence4 = new float[] { .5f, .5f, .5f, .5f, 1 };
    float[] sequenceA = new float[] { .5f, 1, 1, .5f, 1 };
    float[] sequenceB = new float[] { .5f, 1, .5f, .5f, .5f };
    float[] sequenceC = new float[] { .5f, .5f, .5f, .5f, .5f, .5f, 1 };

    bool Steady;

    #region WING/TAIL
    void SetWingTailOff()
    {
        StopSequence("WingTail");
    }

    void SetWingTailBRT()
    {
        if (!consumer.IsPoweredE) EnergyBus.Instance.ApplyToBus(consumer.SystemIdE, 0, null, consumer);
        consumer.ChangePowerStatusE(true);
        foreach (var light in wingTailLights)
        {
            light.intensity = brightLevel;
        }
        if (Steady)
            HandleSequence("WingTail", steady);
        else
            HandleSequence("WingTail", flash);
    }

    void SetWingTailDIM()
    {
        if (!consumer.IsPoweredE) EnergyBus.Instance.ApplyToBus(consumer.SystemIdE, 0, null, consumer);
        consumer.ChangePowerStatusE(true);
        foreach (var light in wingTailLights)
        {
            light.intensity = dimLevel;
        }
        if (Steady)
            HandleSequence("WingTail", steady);
        else
            HandleSequence("WingTail", flash);
    }

    void SetSteady()
    {
        Steady = true;
    }

    void SetFlash()
    {
        Steady = false;
    }

    #endregion

    #region Fuselage
    void SetFuselageOff()
    {
        StopSequence("FuselageLights");
    }

    void SetFuselageBRT()
    {
        if (!consumer.IsPoweredE) EnergyBus.Instance.ApplyToBus(consumer.SystemIdE, 0, null, consumer);
        consumer.ChangePowerStatusE(true);
        foreach (var light in fuselageLights)
        {
            light.intensity = brightLevel;
        }
        if (Steady)
            HandleSequence("FuselageLights", steady);
        else
            HandleSequence("FuselageLights", flash);
    }

    void SetFuselageDIM()
    {
        if (!consumer.IsPoweredE) EnergyBus.Instance.ApplyToBus(consumer.SystemIdE, 0, null, consumer);
        consumer.ChangePowerStatusE(true);
        foreach (var light in fuselageLights)
        {
            light.intensity = dimLevel;
        }
        if (Steady)
            HandleSequence("FuselageLights", steady);
        else
            HandleSequence("FuselageLights", flash);
    }
    #endregion

    #region ANTI-COLLISION
    void SetSeqOff()
    {
        StopSequence("AntiCollision");
    }

    void SetSeq1()
    {
        if (!consumer.IsPoweredE) EnergyBus.Instance.ApplyToBus(consumer.SystemIdE, 0, null, consumer);
        consumer.ChangePowerStatusE(true);
        anti_CollisionLight.intensity = brightLevel;
        HandleSequence("AntiCollision", sequence1);
    }

    void SetSeq2()
    {
        if (!consumer.IsPoweredE) EnergyBus.Instance.ApplyToBus(consumer.SystemIdE, 0, null, consumer);
        consumer.ChangePowerStatusE(true);
        anti_CollisionLight.intensity = brightLevel;
        HandleSequence("AntiCollision", sequence2);
    }

    void SetSeq3()
    {
        if (!consumer.IsPoweredE) EnergyBus.Instance.ApplyToBus(consumer.SystemIdE, 0, null, consumer);
        consumer.ChangePowerStatusE(true);
        anti_CollisionLight.intensity = brightLevel;
        HandleSequence("AntiCollision", sequence3);
    }

    void SetSeq4()
    {
        if (!consumer.IsPoweredE) EnergyBus.Instance.ApplyToBus(consumer.SystemIdE, 0, null, consumer);
        consumer.ChangePowerStatusE(true);
        anti_CollisionLight.intensity = brightLevel;
        HandleSequence("AntiCollision", sequence4);
    }

    void SetSeqA()
    {
        if (!consumer.IsPoweredE) EnergyBus.Instance.ApplyToBus(consumer.SystemIdE, 0, null, consumer);
        consumer.ChangePowerStatusE(true);
        anti_CollisionLight.intensity = brightLevel;
        HandleSequence("AntiCollision", sequenceA);
    }

    void SetSeqB()
    {
        if (!consumer.IsPoweredE) EnergyBus.Instance.ApplyToBus(consumer.SystemIdE, 0, null, consumer);
        consumer.ChangePowerStatusE(true);
        anti_CollisionLight.intensity = brightLevel;
        HandleSequence("AntiCollision", sequenceB);
    }
    void SetSeqC()
    {
        if (!consumer.IsPoweredE) EnergyBus.Instance.ApplyToBus(consumer.SystemIdE, 0, null, consumer);
        consumer.ChangePowerStatusE(true);
        anti_CollisionLight.intensity = brightLevel;
        HandleSequence("AntiCollision", sequenceC);
    }
    #endregion

    #region Formation
    void HandleFormationLights(float brightness)
    {
        if (!consumer.IsPoweredE) EnergyBus.Instance.ApplyToBus(consumer.SystemIdE, 0, null, consumer);
        consumer.ChangePowerStatusE(true);
        foreach (var light in formationLights)
        {
            light.intensity = brightLevel * brightness * 10;
            if (brightness == 0) light.enabled = false;
            else light.enabled = true;
        }
    }
    #endregion

    #region Seq Controller
    private Dictionary<string, Coroutine> activeSequences = new();
    private Dictionary<string, Light[]> lightGroups = new();

    public void RegisterGroup(string groupName, params Light[] lights)
    {
        lightGroups[groupName] = lights;
    }

    public void HandleSequence(string groupName, float[] sequence)
    {
        if (!lightGroups.ContainsKey(groupName))
        {
            Debug.LogWarning($"Light group {groupName} not found.");
            return;
        }

        if (activeSequences.TryGetValue(groupName, out Coroutine current))
        {
            StopCoroutine(current);
        }

        Coroutine routine = StartCoroutine(SequenceCoroutine(groupName, sequence));
        activeSequences[groupName] = routine;
    }

    private IEnumerator SequenceCoroutine(string groupName, float[] sequence)
    {
        var lights = lightGroups[groupName];
        int index = 0;

        while (true)
        {
            if (!consumer.IsPoweredE)
            {
                foreach (var light in lights)
                    light.enabled = false;

                yield return null;
                continue;
            }

            float duration = sequence[index];
            bool isOn = duration < 1f;

            foreach (var light in lights)
                light.enabled = isOn;

            yield return new WaitForSeconds(duration);

            // Eðer bu bir açýk süreyse ve sonraki de yine açýk süreyse, araya kýsa bir kapanma ekle
            int nextIndex = (index + 1) % sequence.Length;
            bool nextIsOn = sequence[nextIndex] < 1f;

            if (isOn && nextIsOn)
            {
                // Kýsa bir söndürme efekti
                foreach (var light in lights)
                    light.enabled = false;

                yield return new WaitForSeconds(0.2f);
            }

            index = nextIndex;
        }
    }

    public void StopSequence(string groupName)
    {
        if (activeSequences.TryGetValue(groupName, out Coroutine coroutine))
        {
            StopCoroutine(coroutine);
            activeSequences.Remove(groupName);
            foreach (var light in lightGroups[groupName])
                light.enabled = false;
        }
    }

    #endregion

    private void OnEnable()
    {
        ClickableEventHandler.Subscribe("SetSteady", SetSteady);
        ClickableEventHandler.Subscribe("SetFlash", SetFlash);


        ClickableEventHandler.Subscribe("SetWingTailOff", SetWingTailOff);
        ClickableEventHandler.Subscribe("SetWingTailBRT", SetWingTailBRT);
        ClickableEventHandler.Subscribe("SetWingTailDIM", SetWingTailDIM);


        ClickableEventHandler.Subscribe("SetFuselageOff", SetFuselageOff);
        ClickableEventHandler.Subscribe("SetFuselageBRT", SetFuselageBRT);
        ClickableEventHandler.Subscribe("SetFuselageDIM", SetFuselageDIM);


        ClickableEventHandler.Subscribe("SetSeqOff", SetSeqOff);
        ClickableEventHandler.Subscribe("SetSeq1", SetSeq1);
        ClickableEventHandler.Subscribe("SetSeq2", SetSeq2);
        ClickableEventHandler.Subscribe("SetSeq3", SetSeq3);
        ClickableEventHandler.Subscribe("SetSeq4", SetSeq4);
        ClickableEventHandler.Subscribe("SetSeqA", SetSeqA);
        ClickableEventHandler.Subscribe("SetSeqB", SetSeqB);
        ClickableEventHandler.Subscribe("SetSeqC", SetSeqC);


        SlidableEventHandler.Subscribe("HandleFormationLights", HandleFormationLights);
    }

    private void OnDisable()
    {
        ClickableEventHandler.Unsubscribe("SetSteady", SetSteady);
        ClickableEventHandler.Unsubscribe("SetFlash", SetFlash);


        ClickableEventHandler.Unsubscribe("SetWingTailOff", SetWingTailOff);
        ClickableEventHandler.Unsubscribe("SetWingTailBRT", SetWingTailBRT);
        ClickableEventHandler.Unsubscribe("SetWingTailDIM", SetWingTailDIM);


        ClickableEventHandler.Unsubscribe("SetFuselageOff", SetFuselageOff);
        ClickableEventHandler.Unsubscribe("SetFuselageBRT", SetFuselageBRT);
        ClickableEventHandler.Unsubscribe("SetFuselageDIM", SetFuselageDIM);


        ClickableEventHandler.Unsubscribe("SetSeqOff", SetSeqOff);
        ClickableEventHandler.Unsubscribe("SetSeq1", SetSeq1);
        ClickableEventHandler.Unsubscribe("SetSeq2", SetSeq2);
        ClickableEventHandler.Unsubscribe("SetSeq3", SetSeq3);
        ClickableEventHandler.Unsubscribe("SetSeq4", SetSeq4);
        ClickableEventHandler.Unsubscribe("SetSeqA", SetSeqA);
        ClickableEventHandler.Unsubscribe("SetSeqB", SetSeqB);
        ClickableEventHandler.Unsubscribe("SetSeqC", SetSeqC);


        SlidableEventHandler.Unsubscribe("HandleFormationLights", HandleFormationLights);
    }

    // Start is called before the first frame update
    void Start()
    {
        consumer = GetComponent<EnergyConsumerComponent>();
        RegisterGroup("WingTail", wingTailLights);
        RegisterGroup("FuselageLights", fuselageLights);
        RegisterGroup("AntiCollision", anti_CollisionLight);
    }
}

