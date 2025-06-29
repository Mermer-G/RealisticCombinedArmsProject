using TMPro;
using UnityEngine;

public class MFDManager : MonoBehaviour
{
    private IMFDMode currentMode;




    GameObject navPanel;
    GameObject weapPanel;
    GameObject statPanel;

    private IMFDMode navMode;
    private IMFDMode weapMode;
    private IMFDMode statMode;

    void Start()
    {
        SwitchMode(navMode);
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Alpha1)) SwitchMode(navMode);
        if (Input.GetKeyDown(KeyCode.Alpha2)) SwitchMode(weapMode);
        if (Input.GetKeyDown(KeyCode.Alpha3)) SwitchMode(statMode);

        currentMode?.Update();
    }

    void SwitchMode(IMFDMode newMode)
    {
        if (currentMode != null) currentMode.Exit();
        currentMode = newMode;
        currentMode.Enter();
    }
}

public class MFDOSB : MonoBehaviour
{
    [SerializeField] int id;
    [SerializeField] TextMeshPro text;
}

public interface IMFDMode
{
    void Enter(); // Mod aktif olurken
    void Exit();  // Moddan çýkarken
    void Update(); // Her frame çaðrýlýr
}