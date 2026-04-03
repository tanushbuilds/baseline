using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerMovement : MonoBehaviour
{
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

    private InputAction moveAction;
    private InputAction sprintAction;
    private CharacterController cc;
    private Vector3 velocity;

    void Awake()
    {
        cc = GetComponent<CharacterController>();

        var map = inputActionAsset.FindActionMap(actionMapName, throwIfNotFound: true);
        moveAction = map.FindAction(moveActionName, throwIfNotFound: true);
        sprintAction = map.FindAction(sprintActionName, throwIfNotFound: true);
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
        cc.Move(new Vector3(velocity.x, 0, velocity.z) * Time.deltaTime);
    }

    void HandleGravity()
    {
        if (!cc.isGrounded)
            cc.Move(Vector3.down * 9.81f * Time.deltaTime);
    }
}