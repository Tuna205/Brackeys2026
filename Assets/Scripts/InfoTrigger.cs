using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class InfoTrigger : MonoBehaviour
{
    [SerializeField] private InputActionAsset inputActions = null;
    [SerializeField, Min(0f)] private float transferAmount = 10f;
    [SerializeField, Min(0.01f)] private float transferInterval = 1f;

    private InputAction interactAction;
    private readonly HashSet<Collider> playerCollidersInside = new();
    private float nextTransferTime;

    private bool PlayerInside => playerCollidersInside.Count > 0;

    private void Awake()
    {
        interactAction = inputActions.FindAction("Player/Jump", true).Clone();
    }

    private void OnEnable()
    {
        interactAction.performed += OnInteract;
        interactAction.Enable();
    }

    private void OnDisable()
    {
        if (interactAction != null)
        {
            interactAction.performed -= OnInteract;
            interactAction.Disable();
        }

        playerCollidersInside.Clear();
    }

    private void OnDestroy()
    {
        interactAction?.Dispose();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (IsPlayerCollider(other))
        {
            playerCollidersInside.Add(other);
        }
    }

    private void OnTriggerExit(Collider other)
    {
        playerCollidersInside.Remove(other);
    }

    private void OnInteract(InputAction.CallbackContext context)
    {
        if (!PlayerInside)
        {
            return;
        }

        if (Time.time < nextTransferTime)
        {
            return;
        }

        float availableMemory = Memory.instance.Value;
        float amount = Mathf.Min(transferAmount, availableMemory);

        if (amount <= 0f)
        {
            return;
        }

        Memory.instance.Add(-amount);
        Info.instance.Add(amount);
        nextTransferTime = Time.time + transferInterval;
    }

    private static bool IsPlayerCollider(Collider other)
    {
        return Player.instance != null &&
            (other.gameObject == Player.instance.gameObject ||
             other.transform.IsChildOf(Player.instance.transform));
    }
}
