using UnityEngine;
using UnityEngine.InputSystem;

public class InfoTrigger : MonoBehaviour
{
    [SerializeField] private InputActionAsset inputActions;
    [SerializeField, Min(0f)] private float transferAmount = 10f;
    [SerializeField, Min(0.01f)] private float transferInterval = 1f;

    private InputAction interactAction;
    private bool playerInside;
    private float nextTransferTime;

    private void Awake()
    {
        interactAction = inputActions.FindAction("Player/Jump", true);
    }

    private void OnEnable()
    {
        interactAction.performed += OnInteract;
        interactAction.Enable();
    }

    private void OnDisable()
    {
        interactAction.performed -= OnInteract;
        interactAction.Disable();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            playerInside = true;
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            playerInside = false;
        }
    }

    private void OnInteract(InputAction.CallbackContext context)
    {
        if (!playerInside)
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
}
