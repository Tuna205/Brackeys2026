using System;
using UnityEngine;

public class Info : MonoBehaviour
{
    public static Info instance { get; private set; }

    public event Action<float> Changed;

    [SerializeField] private float value;

    public float Value => value;

    private void Awake()
    {
        if (instance != null)
        {
            Debug.LogError("More than one Info component exists in the scene.", this);
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
        value = newValue;
        Changed?.Invoke(value);
    }

    public void Add(float amount)
    {
        SetValue(value + amount);
    }
}
