using UnityEngine;
using UnityEngine.VFX;

[RequireComponent(typeof(VisualEffect))]
public class VFXGraphEventCatcher : MonoBehaviour
{
    bool _unsetAttribute;
    bool _unsetAttributeOnNextUpdate;

    VisualEffect _vfx;

    /**
    * This is completely fucking bonkers but it's the ONLY way I've found to set a value in the VFX graph for
    * only one frame. What this does is allow the frame to play out and, if we just set the attribute, set a
    * flag to check on the frame after that in order to unset it again.
    * Yes this is shit.
    * Yes I hate it.
    * It works. LEAVE IT ALONE.
    */
    void Update()
    {
        if (_unsetAttributeOnNextUpdate)
        {
            _vfx.SetVector3("_shiftingVector", Vector3.zero);
            _unsetAttribute = false;
            _unsetAttributeOnNextUpdate = false;
        }

        if (_unsetAttribute) _unsetAttributeOnNextUpdate = true;
    }

    void OnEnable()
    {
        _vfx = GetComponent<VisualEffect>();
        FloatingOrigin.originShifted += UpdatePosition;
    }

    void OnDisable()
    {
        FloatingOrigin.originShifted -= UpdatePosition;
    }

    void UpdatePosition(Vector3 position)
    {
        _vfx.SetVector3("_shiftingVector", position);
        _unsetAttribute = true;
    }
}
