using TMPro;
using UnityEngine;

[System.Serializable]
public class MFDOSB : PhysicalButton
{
    public int id;
    public TextMeshProUGUI tmp;

    public override void LeftClick()
    {
        ClickableEventHandler.Invoke(leftClickMethod, this);
        if (animate) SetPosition();
    }

    public override void RightClick()
    {
        if (rightClickMethod != "")
            ClickableEventHandler.Invoke(rightClickMethod, this);
    }

    private void Update()
    {
        if (animate) returnToNormal();
    }
}
