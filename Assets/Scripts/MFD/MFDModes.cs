using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MFDModes : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
public class MainMenuMode : IMFDMode
{
    private GameObject panel;

    public MainMenuMode(GameObject panel)
    {
        this.panel = panel;
    }

    public void Enter() => panel.SetActive(true);
    public void Exit() => panel.SetActive(false);
    public void Update() { }
}