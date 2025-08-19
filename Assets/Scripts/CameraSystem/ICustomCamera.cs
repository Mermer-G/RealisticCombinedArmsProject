using UnityEngine;

public interface ICustomCamera
{
    public void ControlCamara();
}

public class CameraInput
{
    public Vector2 axisInput;
    public float zoomInput;
}