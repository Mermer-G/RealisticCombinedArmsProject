using UnityEngine;
using UnityEngine.Events;

public class PhysicalSwitch : MonoBehaviour, IClickable
{
    [SerializeField] bool loopPositions;
    [SerializeField] PositionData[] positionDatas;
    [SerializeField] int currentIndex = 0;
    [SerializeField] Transform objectToRotate;
    [SerializeField] string selfSettingId;
    
    private void OnEnable()
    {
        if (selfSettingId != "")
            GenericEventManager.Subscribe<int>(selfSettingId, SetIndex);
    }

    private void OnDisable()
    {
        if (selfSettingId != "")
            GenericEventManager.Unsubscribe<int>(selfSettingId, SetIndex);
    }

    void SetRotation()
    {
        var rot = positionDatas[currentIndex].rotation;
        objectToRotate.localRotation = rot;
    }

    public void NextIndex()
    {
        if (!loopPositions && currentIndex + 1 != positionDatas.Length) currentIndex++;
        else return;
        if (currentIndex >= positionDatas.Length)
            currentIndex = 0; // Döngüsel gider
    }

    void SetIndex(int Id)
    {        
        currentIndex = Id;
        SetRotation();
        UseByID(currentIndex);
        
    }

    public void PreviousIndex()
    {
        if (!loopPositions && currentIndex - 1 != -1) currentIndex--;
        else return;
        if (currentIndex == -1)
            currentIndex = positionDatas.Length - 1; // Döngüsel gider
    }

    public void UseByID(int id)
    {
        if (positionDatas[id].methodId != "")
            ClickableEventHandler.Invoke(positionDatas[id].methodId);
    }

    public void LeftClick()
    {
        var indexBefore = currentIndex;
        NextIndex();
        if (indexBefore == currentIndex) return;
        UseByID(currentIndex);
        SetRotation();
    }

    public void RightClick()
    {
        var indexBefore = currentIndex;
        PreviousIndex();
        if (indexBefore == currentIndex) return;
        UseByID(currentIndex);
        SetRotation();
    }
}
[System.Serializable]
public struct PositionData
{
    public int id;
    public Quaternion rotation;
    public string methodId;
}