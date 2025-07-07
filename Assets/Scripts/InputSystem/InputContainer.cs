public class InputContainer
{
    //Input Map list
    public string containerName;
    public InputMap[] inputMaps;
}


public class InputMap
{
    //Input Action List
    public string mapName;
    public bool isActive;
    public string[] RequiredMaps;
    public string[] BlacklistedMaps;
    public InputAction[] inputActions;
}