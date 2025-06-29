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
        tmp.ForceMeshUpdate(); // TMP'nin layoutunu güncelle

        float width = tmp.preferredWidth + paddingX;
        float height = tmp.preferredHeight + paddingY;

        RectTransform tmpRect = tmp.rectTransform;
        RectTransform bgRect = rectTransform;

        // 1. Boyutu ayarla
        bgRect.sizeDelta = new Vector2(width, height);

        // 2. Pivot ve pozisyonu ayarla
        TextAlignmentOptions alignment = tmp.alignment;

        Vector2 pivot = new Vector2(0.5f, 0.5f); // default ortalanmýþ
        Vector2 offset = Vector2.zero;

        if (alignment == TextAlignmentOptions.Left || alignment == TextAlignmentOptions.TopLeft || alignment == TextAlignmentOptions.BottomLeft || alignment == TextAlignmentOptions.BaselineLeft)
        {
            pivot = new Vector2(0f, 0.5f); // sola yaslý
            offset = new Vector2(width / 2f, 0f);
        }
        else if (alignment == TextAlignmentOptions.Right || alignment == TextAlignmentOptions.TopRight || alignment == TextAlignmentOptions.BottomRight || alignment == TextAlignmentOptions.BaselineRight)
        {
            pivot = new Vector2(1f, 0.5f); // saða yaslý
            offset = new Vector2(-width / 2f, 0f);
        }
        else
        {
            pivot = new Vector2(0.5f, 0.5f); // ortalanmýþ
            offset = Vector2.zero;
        }

        bgRect.pivot = pivot;
        bgRect.anchoredPosition = tmpRect.anchoredPosition + offset;
    }
}
