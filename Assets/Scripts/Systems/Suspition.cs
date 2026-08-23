using System;
using UnityEngine;

public class Suspition : MonoBehaviour
{
    public static Suspition instance { get; private set; }

    public event Action<float> Changed;

    [SerializeField, Range(0f, 100f)] private float value;

    public float Value => value;

    private void Awake()
    {
        Debug.LogWarning("Sus awake");
        if (instance != null)
        {
            Debug.LogError("More than one Suspition component exists in the scene.", this);
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
        Changed?.Invoke(value);
    }

    public void Add(float amount)
    {
        SetValue(value + amount);
    }
}
