using UnityEngine;

[RequireComponent(typeof(RectTransform))]
public class ImagePositionListener : MonoBehaviour
{
    [Tooltip("Bu image hangi pozisyon event'ine tepki verecek?")]
    public string positionEventKey;

    [Tooltip("Canvas referans� (World Space olmal�!)")]
    public Canvas canvas;

    [Tooltip("D�nya pozisyonunu ekran pozisyonuna �evirmek i�in kullan�lan kamera")]
    public Camera mainCamera;

    RectTransform rectTransform;

    private void OnEnable()
    {
        rectTransform = GetComponent<RectTransform>();
        if (mainCamera == null)
        {
            Debug.LogWarning("Main Camera was null.");
            mainCamera = Camera.main;
        }

        Vector3EventManager.Subscribe(positionEventKey, UpdatePosition);
    }

    private void OnDisable()
    {
        Vector3EventManager.Unsubscribe(positionEventKey, UpdatePosition);
    }

    private void UpdatePosition(Vector3 worldPos)
    {
        Vector3 screenPos = mainCamera.WorldToScreenPoint(worldPos);

        // UI elementi canvas �zerindeki do�ru yerle yerle�tir
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            canvas.GetComponent<RectTransform>(),
            screenPos,
            canvas.worldCamera,
            out Vector2 localPoint);

        rectTransform.localPosition = Vector3.Lerp(rectTransform.localPosition, localPoint, 0.05f);
    }
}