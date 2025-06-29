using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SeaLevelIndicator : MonoBehaviour
{
    public static Transform instance;
    private void OnEnable()
    {
        if (instance == null)
            instance = transform;
    }
}
