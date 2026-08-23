using System;
using UnityEngine;

public class Suspition : MonoBehaviour
{
    public static Suspition instance { get; private set; }

    public event Action<float> Changed;

    [SerializeField, Range(0f, 100f)] private float value;
    [SerializeField, Min(0f)] private float decayAmount = 1f;
    [SerializeField, Min(0.01f)] private float decayInterval = 1f;

    public float Value => value;

    private void Awake()
    {
        if (instance != null)
        {
            Debug.LogError("More than one Suspition component exists in the scene.", this);
            Destroy(this);
            return;
        }

        instance = this;
    }

    private void OnEnable()
    {
        InvokeRepeating(nameof(Decay), decayInterval, decayInterval);
    }

    private void OnDisable()
    {
        CancelInvoke(nameof(Decay));
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
        Debug.LogWarning("Sus value: " + value);
        Changed?.Invoke(value);
    }

    public void Add(float amount)
    {
        SetValue(value + amount);
    }

    private void Decay()
    {
        Add(-decayAmount);
    }
}
