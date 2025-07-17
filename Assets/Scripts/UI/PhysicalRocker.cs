using UnityEngine;

public class PhysicalRocker : MonoBehaviour, IClickable
{
    [SerializeField] Vector3 rightClickRotation;
    [SerializeField] Vector3 normalRotation;
    [SerializeField] Vector3 leftClickRotation;
    [SerializeField] Transform objectToRotate;

    [SerializeField] string leftClickMethod;
    [SerializeField] string rightClickMethod;

    [SerializeField] float returnSpeed;
    public void LeftClick()
    {
        ClickableEventHandler.Invoke(leftClickMethod);
        objectToRotate.localEulerAngles = leftClickRotation;
    }

    public void RightClick()
    {
        ClickableEventHandler.Invoke(rightClickMethod);
        objectToRotate.localEulerAngles = rightClickRotation;
    }

    void returnToNormal()
    {
        if (objectToRotate.localEulerAngles == normalRotation) return;
        objectToRotate.localEulerAngles = Vector3.Lerp(objectToRotate.localPosition, normalRotation, returnSpeed);
    }

    // Update is called once per frame
    void Update()
    {
        returnToNormal();
    }
}
