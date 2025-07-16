using System.Collections.Generic;

public interface IMFDFormat 
{
    string Context { get; set; }
    Stack<(int index, string label)> OSBUpdates { get; }
    public void OnFormatEnter();
    public void OnFormatStay();
    public void OnFormatExit();
    bool HandleArg(string arg);
}

public interface ISensorOfInterest
{
    void SetSOI();
    void UnSetSOI();
}



