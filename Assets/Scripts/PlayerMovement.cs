using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CharacterController))]
public sealed class PlayerMovement : MonoBehaviour
{
    private static readonly int WalkingParameter = Animator.StringToHash("walking");

    [SerializeField] private InputActionAsset inputActions;
    private float moveSpeed = 6f;
    private float rotationSpeed = 720f;

    private InputAction moveAction;
    private CharacterController characterController;
    private Animator animator;
    private Vector2 moveInput;
    private float fixedY;

    private void Awake()
    {
        characterController = GetComponent<CharacterController>();
        fixedY = transform.position.y;
        animator = GetComponentInChildren<Animator>(true);

        if (animator == null)
        {
            Debug.LogWarning("PlayerMovement could not find a child Animator.", this);
        }

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
        animator?.SetBool(WalkingParameter, false);

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
        RestoreFixedY();
        animator?.SetBool(WalkingParameter, moveInput.sqrMagnitude > 0.01f);

        if (animator != null && direction.sqrMagnitude > 0f)
        {
            Quaternion targetRotation = Quaternion.LookRotation(direction, Vector3.up);
            animator.transform.rotation = Quaternion.RotateTowards(
                animator.transform.rotation,
                targetRotation,
                rotationSpeed * Time.deltaTime);
        }
    }

    private void OnMove(InputAction.CallbackContext context)
    {
        moveInput = context.ReadValue<Vector2>();
    }

    private void RestoreFixedY()
    {
        Vector3 position = transform.position;
        if (Mathf.Approximately(position.y, fixedY))
        {
            return;
        }

        characterController.enabled = false;
        position.y = fixedY;
        transform.position = position;
        characterController.enabled = true;
    }
}
