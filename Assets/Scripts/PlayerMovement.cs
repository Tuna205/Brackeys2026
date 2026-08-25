using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CharacterController))]
public sealed class PlayerMovement : MonoBehaviour
{
    [SerializeField] private InputActionAsset inputActions;
    private float moveSpeed = 5f;

    private InputAction moveAction;
    private CharacterController characterController;
    private Vector2 moveInput;

    private void Awake()
    {
        characterController = GetComponent<CharacterController>();

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

        if (moveAction == null)
        {
            return;
        }

        moveAction.performed -= OnMove;
        moveAction.canceled -= OnMove;
        moveAction.Disable();
    }

    private void Update()
    {
        Vector3 direction = new Vector3(moveInput.x, 0f, moveInput.y).normalized;
        characterController.Move(direction * (moveSpeed * Time.deltaTime));
    }

    private void OnMove(InputAction.CallbackContext context)
    {
        moveInput = context.ReadValue<Vector2>();
    }
}
