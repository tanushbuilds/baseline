using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerMovement : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] private float moveSpeed = 10f;
    [SerializeField] private float sprintSpeed = 16f;
    [SerializeField] private float acceleration = 15f;
    [SerializeField] private Transform playerBody;

    private CharacterController cc;
    private Vector3 velocity;
    private Vector2 moveInput;
    private bool isSprinting;
    private TennisControls playerInput;

    void Awake()
    {
        cc = GetComponent<CharacterController>();
        playerInput = new TennisControls();
    }

    void OnEnable() { playerInput.Enable(); }
    void OnDisable() { playerInput.Disable(); }

    void Update()
    {
        moveInput = playerInput.Player.Move.ReadValue<Vector2>();
        isSprinting = playerInput.Player.Sprint.IsPressed();
        HandleMovement();
        HandleGravity();
    }

    void HandleMovement()
    {
        Vector3 forward = playerBody.forward;
        Vector3 right = playerBody.right;

        forward.y = 0f;
        right.y = 0f;
        forward.Normalize();
        right.Normalize();

        Vector3 moveDirection = (forward * moveInput.y + right * moveInput.x).normalized;
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