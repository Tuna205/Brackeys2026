using System;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Slider))]
public class MemoryBar : MonoBehaviour
{
    private Slider slider;
    private Memory memory;

    private void Awake()
    {
        slider = GetComponent<Slider>();
    }

    private void Start()
    {
        memory = Memory.instance;
        memory.Changed += Display;
        Display(memory.Value);
    }

    private void OnDestroy()
    {
        if (memory != null)
        {
            memory.Changed -= Display;
        }
    }

    private void Display(float value)
    {
        slider.value = value;
    }
}
