using UnityEngine;

public class InputTrigger
{
    public string mapName;
    public bool requiresUpdate;
    public virtual float GetValue() { return 0; }
    public virtual void UpdateTrigger() { return; }
}

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

public class KeyHoldTrigger : InputTrigger
{
    public KeyCode keyCode;

    public override float GetValue()
    {
        return Input.GetKey(keyCode) ? 1 : 0;
    }

    public override void UpdateTrigger()
    {
        throw new System.NotImplementedException();
    }
}

public class AxisTrigger : InputTrigger
{
    public string axis;

    public override float GetValue()
    {
        return Input.GetAxis(axis);
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
    public string targetMapName; // Etkileyece�i map

    public override float GetValue()
    {
        // Bu trigger kendi map'inde input olarak kullan�lmaz, bu y�zden 0 d�ner.
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

public class ToggleToActivateMapTrigger : InputTrigger
{
    public KeyCode keyCode;
    public string targetMapName; // Etkileyece�i map
    bool toggle;
    public override float GetValue()
    {
        // Bu trigger kendi map'inde input olarak kullan�lmaz, bu y�zden 0 d�ner.
        return 0;
    }

    public override void UpdateTrigger()
    {
        if(Input.GetKeyDown(keyCode)) toggle = !toggle;

        if (InputManager.instance.maps.TryGetValue(targetMapName, out var map))
        {
            map.isActive = toggle;
        }
        else
        {
            Debug.LogWarning($"[HoldToActivateMapTrigger] Map not found: {targetMapName}");
        }
    }
}

public class ToggleMapWithActionTrigger : InputTrigger
{
    public string actionName;       // Dinlenecek input action
    public string targetMapName;    // Etkilenecek harita
    private bool toggle;

    public override float GetValue()
    {
        return 0; // Bu trigger direkt input olarak kullan�lmaz
    }

    public override void UpdateTrigger()
    {
        if (InputManager.instance.GetInput(actionName).ToBool())
        {
            toggle = !toggle;

            if (InputManager.instance.maps.TryGetValue(targetMapName, out var map))
            {
                map.isActive = toggle;
            }
            else
            {
                Debug.LogWarning($"[ToggleMapWithActionTrigger] Map not found: {targetMapName}");
            }
        }
    }
}

//Bunun i�inde tuttu�u trigger'lar hepsi true ise ya da float d�nd�r�yorsa d�nd�rece�i float 0'dan farkl�ysa input manager yard�m�yla d�nd�r�lebilir olmal�.
public class InputAction
{
    public string name;
    public bool essential;
    public InputTrigger trigger;
}