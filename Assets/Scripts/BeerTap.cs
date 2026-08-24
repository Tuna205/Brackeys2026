using UnityEngine;
using UnityEngine.InputSystem;

public class BeerTap : MonoBehaviour
{
    [SerializeField] private InputActionAsset inputActions = null;
    [SerializeField] private Player.BeerTypes beerType = Player.BeerTypes.White;

    private InputAction interactAction;
    private Collider playerColliderInside;

    private void Awake()
    {
        if (inputActions == null)
        {
            Debug.LogError("BeerTap needs an Input Action Asset.", this);
            enabled = false;
            return;
        }

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

        playerColliderInside = null;
    }

    private void OnDestroy()
    {
        interactAction?.Dispose();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (playerColliderInside != null || !IsPlayerCollider(other))
        {
            return;
        }

        playerColliderInside = other;
    }

    private void OnTriggerExit(Collider other)
    {
        if (other == playerColliderInside)
        {
            playerColliderInside = null;
        }
    }

    private void OnInteract(InputAction.CallbackContext context)
    {
        if (playerColliderInside != null)
        {
            Player.instance.AddBeer(beerType);
        }
    }

    private static bool IsPlayerCollider(Collider other)
    {
        return Player.instance != null &&
            (other.gameObject == Player.instance.gameObject ||
             other.transform.IsChildOf(Player.instance.transform));
    }
}
