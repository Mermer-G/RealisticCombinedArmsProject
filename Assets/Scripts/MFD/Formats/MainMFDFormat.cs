using System.Collections.Generic;
using System;
using UnityEngine;

class MainMFDFormat : MonoBehaviour, IMFDFormat
{
    
    public Dictionary<string, Action> osbCommands = new();

    public Stack<(int index, string label)> OSBUpdates => osbToUpdate;
    Stack<(int index, string label)> osbToUpdate = new();

    public string Context { get; set; }

    public void OnFormatEnter()
    {
        gameObject.SetActive(true);
        osbCommands.Clear();

        osbCommands["BLANK"] = () =>
        {
            //
        };

        //Set the Text for OSBs
        osbToUpdate.Push((1, "TGP"));
        osbToUpdate.Push((2, "HAD"));
        osbToUpdate.Push((4, "RCCE"));
        osbToUpdate.Push((5, "RESET\nMENU"));
        osbToUpdate.Push((6, "SMS"));
        osbToUpdate.Push((7, "HSD"));
        osbToUpdate.Push((8, "DTE"));
        osbToUpdate.Push((9, "TEST"));
        osbToUpdate.Push((10, "FLCS"));
        osbToUpdate.Push((11, "DCLT"));
        osbToUpdate.Push((12, "   "));
        osbToUpdate.Push((13, "   "));
        osbToUpdate.Push((14, "   "));
        osbToUpdate.Push((15, "SWAP"));
        osbToUpdate.Push((16, "FLIR"));
        osbToUpdate.Push((17, "TFR"));
        osbToUpdate.Push((18, "WPN"));
        osbToUpdate.Push((19, "TGP"));
        osbToUpdate.Push((20, "FCR"));
    }

    public void OnFormatExit()
    {
        for (int i = 1; i < 20; i++)
        {
            osbToUpdate.Push((i, ""));
        }
        gameObject.SetActive(false);
    }

    public void OnFormatStay()
    {
        
    }

    public bool HandleArg(string arg)
    {
        if (!osbCommands.TryGetValue(arg, out Action key)) return false;

        osbCommands[arg].Invoke();
        return true;
    }
}
