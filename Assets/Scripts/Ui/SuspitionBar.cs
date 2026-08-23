using System;
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
    }

    private void Start()
    {
        suspition = Suspition.instance;
        suspition.Changed += Display;
        Display(suspition.Value);
    }

    private void OnDestroy()
    {
        if (suspition != null)
        {
            suspition.Changed -= Display;
        }
    }

    private void Display(float value)
    {
        slider.value = value;
    }
}
