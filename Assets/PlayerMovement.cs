using UnityEngine;
using UnityEngine.InputSystem;

public sealed class PlayerMovement : MonoBehaviour
{
    [SerializeField] private InputActionAsset inputActions;
    [SerializeField, Min(0f)] private float moveSpeed = 5f;

    private InputAction moveAction;
    private Vector2 moveInput;

    private void Awake()
    {
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
        Vector3 direction = new Vector3(moveInput.x, 0f, moveInput.y);
        transform.Translate(direction * (moveSpeed * Time.deltaTime), Space.World);
    }

    private void OnMove(InputAction.CallbackContext context)
    {
        moveInput = context.ReadValue<Vector2>();
    }
}
