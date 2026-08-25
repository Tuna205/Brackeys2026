using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody))]
public sealed class PlayerMovement : MonoBehaviour
{
    [SerializeField] private InputActionAsset inputActions;
    private float moveSpeed = 5f;

    private InputAction moveAction;
    private Rigidbody playerRigidbody;
    private Vector2 moveInput;

    private void Awake()
    {
        playerRigidbody = GetComponent<Rigidbody>();
        playerRigidbody.isKinematic = false;
        playerRigidbody.useGravity = false;
        playerRigidbody.constraints |= RigidbodyConstraints.FreezePositionY
            | RigidbodyConstraints.FreezeRotation;
        playerRigidbody.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;

        moveAction = inputActions.FindAction("Player/Move", true);

        if (moveAction == null)
        {
            Debug.LogWarning("Move Actions Are NULL");
        }

    }

    private void OnEnable()
    {
        moveAction.performed += OnMove;
        moveAction.canceled += OnMove;
        moveAction.Enable();
    }

    private void OnDisable()
    {
        moveInput = Vector2.zero;
        playerRigidbody.linearVelocity = Vector3.zero;
        playerRigidbody.angularVelocity = Vector3.zero;

        if (moveAction == null)
        {
            return;
        }

        moveAction.performed -= OnMove;
        moveAction.canceled -= OnMove;
        moveAction.Disable();
    }

    private void FixedUpdate()
    {
        Vector3 direction = new Vector3(moveInput.x, 0f, moveInput.y).normalized;
        playerRigidbody.linearVelocity = direction * moveSpeed;
        playerRigidbody.angularVelocity = Vector3.zero;
    }

    private void OnMove(InputAction.CallbackContext context)
    {
        moveInput = context.ReadValue<Vector2>();
    }
}
