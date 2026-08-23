using System;
using UnityEngine;

public class Memory : MonoBehaviour
{
    public static Memory instance { get; private set; }

    public event Action<float> Changed;

    [SerializeField, Range(0f, 100f)] private float value;

    public float Value => value;

    private void Awake()
    {
        if (instance != null)
        {
            Debug.LogError("More than one Memory component exists in the scene.", this);
            Destroy(this);
            return;
        }

        instance = this;
    }

    private void OnDestroy()
    {
        if (instance == this)
        {
            instance = null;
        }
    }

    public void SetValue(float newValue)
    {
        float clampedValue = Mathf.Clamp(newValue, 0f, 100f);
        if (Mathf.Approximately(value, clampedValue))
        {
            return;
        }

        value = clampedValue;
        Debug.LogWarning("Mem value: " + value);
        Changed?.Invoke(value);
    }

    public void Add(float amount)
    {
        SetValue(value + amount);
    }
}
