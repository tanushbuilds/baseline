using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerMovement : MonoBehaviour
{
    // ===== STATE =====
    public enum PlayerState
    {
        Normal,
        Serving,
        Swinging
    }

    public PlayerState currentState = PlayerState.Normal;

    [Header("Movement")]
    [SerializeField] private float moveSpeed = 10f;
    [SerializeField] private float sprintSpeed = 16f;
    [SerializeField] private float acceleration = 15f;
    [SerializeField] private Transform playerBody;

    [Header("Input")]
    [Tooltip("Drag in a different Input Action Asset for each player.")]
    [SerializeField] private InputActionAsset inputActionAsset;
    [SerializeField] private string actionMapName = "Player";
    [SerializeField] private string moveActionName = "Move";
    [SerializeField] private string sprintActionName = "Sprint";


    [Header("Animation")]
    [SerializeField] private Animator animator;

    private InputAction moveAction;
    private InputAction sprintAction;
    private CharacterController cc;
    private Vector3 velocity;

    void Awake()
    {
        cc = GetComponent<CharacterController>();

        var map = inputActionAsset.FindActionMap(actionMapName, true);
        moveAction = map.FindAction(moveActionName, true);
        sprintAction = map.FindAction(sprintActionName, true);
    }

    void OnEnable()
    {
        moveAction.Enable();
        sprintAction.Enable();
    }

    void OnDisable()
    {
        moveAction.Disable();
        sprintAction.Disable();
    }

    void Update()
    {
        // Disable movement during serve
        if (currentState == PlayerState.Serving | currentState == PlayerState.Swinging)
        {
            HandleGravity(); // still apply gravity
            return;
        }

        HandleMovement();
        HandleGravity();
    }

    void HandleMovement()
    {
        Vector2 input = moveAction.ReadValue<Vector2>();
        bool isSprinting = sprintAction.IsPressed();

        Vector3 forward = playerBody.forward;
        Vector3 right = playerBody.right;

        forward.y = 0f;
        right.y = 0f;

        forward.Normalize();
        right.Normalize();

        Vector3 moveDirection = (forward * input.y + right * input.x).normalized;

        float targetSpeed = isSprinting ? sprintSpeed : moveSpeed;
        Vector3 targetVelocity = moveDirection * targetSpeed;

        velocity = Vector3.Lerp(velocity, targetVelocity, acceleration * Time.deltaTime);

        cc.Move(new Vector3(velocity.x, 0f, velocity.z) * Time.deltaTime);

        bool isMoving = moveDirection.magnitude > 0.1f;
        animator.SetBool("isRunning", isMoving);

        float rightDot = Vector3.Dot(moveDirection, Vector3.right);
        bool isForehandRun = isMoving && rightDot > 0.3f;
        bool isBackhandRun = isMoving && rightDot < -0.3f;
        animator.SetBool("isForehandRun", isForehandRun);
        animator.SetBool("isBackhandRun", isBackhandRun);
    }

    void HandleGravity()
    {
        if (!cc.isGrounded)
        {
            cc.Move(Vector3.down * 9.81f * Time.deltaTime);
        }
    }

    // ===== SERVE CONTROL =====

    public void StartServe()
    {
        currentState = PlayerState.Serving;
        StopMovement();
    }


    public void EndServe()
    {
        currentState = PlayerState.Normal;
    }

    public void StartSwing()
    {
        currentState = PlayerState.Swinging;
    }

    public void EndSwing()
    {
        currentState = PlayerState.Normal;
    }

    public void StopMovement()
    {
        velocity = Vector3.zero;
    }
}