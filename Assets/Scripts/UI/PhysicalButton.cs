using UnityEngine;

public class PhysicalButton : MonoBehaviour, IClickable
{
    [SerializeField] Vector3 pressedPosition;
    [SerializeField] Vector3 normalPosition;
    [SerializeField] Transform objectToMove;
    [SerializeField] float returnSpeed;
    [SerializeField] bool animate;

    [SerializeField] string leftClickMethod;
    [SerializeField] string rightClickMethod;

    void SetPosition()
    {
        objectToMove.localPosition = pressedPosition;
    }

    void returnToNormal()
    {
        if (objectToMove.localPosition == normalPosition) return;
        objectToMove.localPosition = Vector3.Lerp(objectToMove.localPosition, normalPosition, returnSpeed);
    }

    private void Update()
    {
        if (animate) returnToNormal();
    }

    public void LeftClick()
    {
        ClickableEventHandler.Invoke(leftClickMethod);
        if (animate) SetPosition();
    }

    public void RightClick()
    {
        if (rightClickMethod != "")
        ClickableEventHandler.Invoke(rightClickMethod);
    }
}