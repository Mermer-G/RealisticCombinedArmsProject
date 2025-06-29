using TMPro;
using UnityEngine;

[RequireComponent(typeof(TextMeshProUGUI))]
public class UpdateText : MonoBehaviour
{
    [Tooltip("Bu TMP'nin hangi string event'ine abone olacaðýný belirler.")]
    public string stringEventKey;

    private TextMeshProUGUI tmp;

    private void OnEnable()
    {
        tmp = GetComponent<TextMeshProUGUI>();
        StringEventManager.Subscribe(stringEventKey, UpdateTxt);
    }

    private void OnDisable()
    {
        StringEventManager.Unsubscribe(stringEventKey, UpdateTxt);
    }

    private void UpdateTxt(string newValue)
    {
        if (tmp != null)
            tmp.text = newValue;
    }
}