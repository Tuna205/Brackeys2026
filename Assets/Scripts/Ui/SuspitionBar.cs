using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Slider))]
public class SuspitionBar : MonoBehaviour
{
    private Slider slider;
    private Suspition suspition;

    private void Awake()
    {
        slider = GetComponent<Slider>();
        suspition = FindAnyObjectByType<Suspition>();
    }

    private void OnEnable()
    {
        if (suspition == null)
        {
            Debug.LogError("SuspitionBar could not find a Suspition component in the scene.", this);
            return;
        }

        suspition.Changed += Display;
        Display(suspition.Value);
    }

    private void OnDisable()
    {
        if (suspition != null)
        {
            suspition.Changed -= Display;
        }
    }

    private void Display(float value)
    {
        slider.value = value / 100f;
    }
}
