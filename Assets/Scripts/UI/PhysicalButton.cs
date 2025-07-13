using UnityEngine;

public class PhysicalButton : MonoBehaviour, IClickable
{
    [SerializeField] protected Vector3 pressedPosition;
    [SerializeField] protected Vector3 normalPosition;
    [SerializeField] protected Transform objectToMove;
    [SerializeField] protected float returnSpeed;
    [SerializeField] protected bool animate;

    [SerializeField] protected string leftClickMethod;
    [SerializeField] protected string rightClickMethod;

    protected void SetPosition()
    {
        objectToMove.localPosition = pressedPosition;
    }

    protected void returnToNormal()
    {
        if (objectToMove.localPosition == normalPosition) return;
        objectToMove.localPosition = Vector3.Lerp(objectToMove.localPosition, normalPosition, returnSpeed);
    }

    private void Update()
    {
        if (animate) returnToNormal();
    }

    public virtual void LeftClick()
    {
        ClickableEventHandler.Invoke(leftClickMethod);
        if (animate) SetPosition();
    }

    public virtual void RightClick()
    {
        if (rightClickMethod != "")
        ClickableEventHandler.Invoke(rightClickMethod);
    }
}