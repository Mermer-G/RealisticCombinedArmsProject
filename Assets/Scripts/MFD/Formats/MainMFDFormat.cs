using System.Collections.Generic;
using System;
using UnityEngine;

class MainMFDFormat : MonoBehaviour, IMFDFormat, ISensorOfInterest
{
    
    public Dictionary<string, Action> osbCommands = new();

    public Stack<(int index, string label)> OSBUpdates => osbToUpdate;
    Stack<(int index, string label)> osbToUpdate = new();

    public void OnFormatEnter()
    {
        gameObject.SetActive(true);
        osbCommands.Clear();

        osbCommands["BLANK"] = () =>
        {
            //
        };

        //Set the Text for OSBs
        osbToUpdate.Push((0, "TGP"));
        osbToUpdate.Push((1, "HAD"));
        osbToUpdate.Push((3, "RCCE"));
        osbToUpdate.Push((4, "RESET\nMENU"));
        osbToUpdate.Push((5, "SMS"));
        osbToUpdate.Push((6, "HSD"));
        osbToUpdate.Push((7, "DTE"));
        osbToUpdate.Push((8, "TEST"));
        osbToUpdate.Push((9, "FLCS"));
        osbToUpdate.Push((10, "DCLT"));
        osbToUpdate.Push((11, "   "));
        osbToUpdate.Push((12, "   "));
        osbToUpdate.Push((13, "   "));
        osbToUpdate.Push((14, "SWAP"));
        osbToUpdate.Push((15, "FLIR"));
        osbToUpdate.Push((16, "TFR"));
        osbToUpdate.Push((17, "WPN"));
        osbToUpdate.Push((18, "TGP"));
        osbToUpdate.Push((19, "FCR"));
    }

    public void OnFormatExit()
    {
        osbToUpdate.Push((0, ""));
        osbToUpdate.Push((1, ""));
        osbToUpdate.Push((2, ""));
        osbToUpdate.Push((3, ""));
        osbToUpdate.Push((4, ""));
        osbToUpdate.Push((5, ""));
        osbToUpdate.Push((6, ""));
        osbToUpdate.Push((7, ""));
        osbToUpdate.Push((8, ""));
        osbToUpdate.Push((9, ""));
        osbToUpdate.Push((10, ""));
        osbToUpdate.Push((11, ""));
        osbToUpdate.Push((12, ""));
        osbToUpdate.Push((13, ""));
        osbToUpdate.Push((14, ""));
        osbToUpdate.Push((15, ""));
        osbToUpdate.Push((16, ""));
        osbToUpdate.Push((17, ""));
        osbToUpdate.Push((18, ""));
        osbToUpdate.Push((19, ""));
        gameObject.SetActive(false);
    }

    public void OnFormatStay()
    {
        
    }

    public void SetSOI()
    {
        throw new NotImplementedException();
    }

    public void UnSetSOI()
    {
        throw new NotImplementedException();
    }

    public bool HandleArg(string arg)
    {
        if (!osbCommands.TryGetValue(arg, out Action key)) return false;

        osbCommands[arg].Invoke();
        return true;
    }
}
