using UnityEngine;

public class InputTrigger
{
    public string mapName;
    public bool requiresUpdate;
    public virtual float GetValue() { return 0; }
    public virtual void UpdateTrigger() { return; }
}

//Bu yapýlan triggerlardan bir tanesi ve bu input action içinde saklanabilir olmalý.
public class KeyDownTrigger : InputTrigger
{
    public KeyCode keyCode;   

    public override float GetValue()
    {
        return Input.GetKeyDown(keyCode) ? 1 : 0;
    }

    public override void UpdateTrigger()
    {
        throw new System.NotImplementedException();
    }
}

public class HoldForSecondsTrigger : InputTrigger
{
    public KeyCode keyCode;
    public float secondsForHolding;
    float currentSeconds;

    public override float GetValue()
    {
        return currentSeconds >= secondsForHolding ? 1 : 0;
    }

    public override void UpdateTrigger()
    {
        if (Input.GetKey(keyCode))
        {
            currentSeconds += Time.deltaTime;
        }

        else currentSeconds = 0;
    }
}

public class HoldToActivateMapTrigger : InputTrigger
{
    public KeyCode keyCode;
    public string targetMapName; // Etkileyeceði map

    public override float GetValue()
    {
        // Bu trigger kendi map'inde input olarak kullanýlmaz, bu yüzden 0 döner.
        return 0;
    }

    public override void UpdateTrigger()
    {
        bool isHeld = Input.GetKey(keyCode);

        if (InputManager.instance.maps.TryGetValue(targetMapName, out var map))
        {
            map.isActive = isHeld;
        }
        else
        {
            Debug.LogWarning($"[HoldToActivateMapTrigger] Map not found: {targetMapName}");
        }
    }
}

//Bunun içinde tuttuðu trigger'lar hepsi true ise ya da float döndürüyorsa döndüreceði float 0'dan farklýysa input manager yardýmýyla döndürülebilir olmalý.
public class InputAction
{
    public string name;
    public bool essential;
    public InputTrigger trigger;
}