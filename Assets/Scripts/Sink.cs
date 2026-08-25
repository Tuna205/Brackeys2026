using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class Sink : MonoBehaviour
{
    private const float SuspitionPenalty = 5f;

    [SerializeField] private InputActionAsset inputActions = null;

    private readonly HashSet<Collider> playerCollidersInside = new();
    private InputAction interactAction;

    private bool PlayerInside => playerCollidersInside.Count > 0;

    private void Awake()
    {
        if (inputActions == null)
        {
            Debug.LogError("Sink needs an Input Action Asset.", this);
            enabled = false;
            return;
        }

        interactAction = inputActions.FindAction("Player/Jump", true).Clone();
    }

    private void OnEnable()
    {
        if (interactAction == null)
        {
            return;
        }

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
        if (PlayerInside
            && Player.instance != null
            && Player.instance.RemoveLastBeer())
        {
            Suspition.instance?.Add(SuspitionPenalty);
        }
    }

    private static bool IsPlayerCollider(Collider other)
    {
        return Player.instance != null
            && (other.gameObject == Player.instance.gameObject
                || other.transform.IsChildOf(Player.instance.transform));
    }
}
