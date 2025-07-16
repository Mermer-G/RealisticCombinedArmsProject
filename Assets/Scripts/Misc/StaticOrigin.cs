using UnityEngine;

public class StaticOrigin : MonoBehaviour
{
    public static Transform instance;
    private void OnEnable()
    {
        if (instance == null)
            instance = transform;
    }
}
