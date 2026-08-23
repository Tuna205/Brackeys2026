using TMPro;
using UnityEngine;

[RequireComponent(typeof(TMP_Text))]
public class InfoText : MonoBehaviour
{
    private TMP_Text infoText;
    private Info info;

    private void Awake()
    {
        infoText = GetComponent<TMP_Text>();
    }

    private void Start()
    {
        info = Info.instance;
        info.Changed += Display;
        Display(info.Value);
    }

    private void OnDestroy()
    {
        if (info != null)
        {
            info.Changed -= Display;
        }
    }

    private void Display(float value)
    {
        infoText.text = $"Info: {value:0}";
    }
}
