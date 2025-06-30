using TMPro;
using UnityEngine;

public class ResizeFieldBackground : MonoBehaviour
{
    [SerializeField] TextMeshProUGUI text;
    RectTransform rectTransform;
    // Start is called before the first frame update
    void Start()
    {
        rectTransform = GetComponent<RectTransform>();
    }

    // Update is called once per frame
    void Update()
    {
        ResizeBackgroundToFitText(text);
    }

    public void ResizeBackgroundToFitText(TextMeshProUGUI tmp, float paddingX = 10f, float paddingY = 5f)
    {
        tmp.ForceMeshUpdate(); // TMP'nin layoutunu g�ncelle

        float width = tmp.preferredWidth;// + paddingX;
        float height = tmp.preferredHeight;// + paddingY;

        RectTransform tmpRect = tmp.rectTransform;
        RectTransform bgRect = rectTransform;

        // 1. Boyutu ayarla
        bgRect.sizeDelta = new Vector2(width, height);

        // 2. Pivot ve pozisyonu ayarla
        TextAlignmentOptions alignment = tmp.alignment;

        Vector2 offset = Vector2.zero;
        Vector2 pivot = new Vector2(0.5f, 0.5f); // Ortada sabit kal�yor

        // X offset (yatay hizalama)
        if (alignment == TextAlignmentOptions.Left || alignment == TextAlignmentOptions.TopLeft || alignment == TextAlignmentOptions.BottomLeft || alignment == TextAlignmentOptions.BaselineLeft)
        {
            offset.x = width / 2f;
        }
        else if (alignment == TextAlignmentOptions.Right || alignment == TextAlignmentOptions.TopRight || alignment == TextAlignmentOptions.BottomRight || alignment == TextAlignmentOptions.BaselineRight)
        {
            offset.x = -width / 2f;
        }
        else
        {
            offset.x = 0f; // Ortalanm��
        }

        // Y offset (dikey hizalama)
        if (alignment == TextAlignmentOptions.TopLeft || alignment == TextAlignmentOptions.Top || alignment == TextAlignmentOptions.TopRight)
        {
            offset.y = -height / 2f;
        }
        else if (alignment == TextAlignmentOptions.BottomLeft || alignment == TextAlignmentOptions.Bottom || alignment == TextAlignmentOptions.BottomRight)
        {
            offset.y = height / 2f;
        }
        else if (alignment == TextAlignmentOptions.BaselineLeft || alignment == TextAlignmentOptions.Baseline || alignment == TextAlignmentOptions.BaselineRight)
        {
            offset.y = height / 4f; // Baseline �zel; tahmini offset verildi
        }
        else
        {
            offset.y = 0f; // Ortalanm��
        }

        bgRect.pivot = pivot;
        bgRect.anchoredPosition = tmpRect.anchoredPosition + offset;
    }
}
