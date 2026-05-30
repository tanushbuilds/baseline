using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;

public class PlayerMovement : MonoBehaviour
{
    public enum PlayerState { Normal, Serving, Swinging }
    public PlayerState currentState = PlayerState.Normal;

    [Header("Movement")]
    [SerializeField] private float moveSpeed = 10f;
    [SerializeField] private float sprintSpeed = 16f;
    [SerializeField] private float acceleration = 20f;
    [SerializeField] private float deceleration = 25f;
    [SerializeField] private Transform playerBody;

    [Header("Animation")]
    [SerializeField] private Animator animator;

    [Header("Input")]
    [SerializeField] private int playerNumber = 1; // 1 or 2 — just for display

    // Set at runtime by button press
    [HideInInspector] public bool deviceAssigned = false;
    [HideInInspector] public bool useJoystick = false;
    [HideInInspector] public int gamepadIndex = 0;

    private CharacterController cc;
    private Vector3 velocity;

    // Shared static tracker so Player 1 and Player 2 don't grab the same device
    private static int _assignedGamepadIndex = -1;
    private static bool _joystickAssigned = false;

    // Call this at the start of a new match to reset device assignments
    public static void ResetAssignments()
    {
        _assignedGamepadIndex = -1;
        _joystickAssigned = false;
    }

    void Awake()
    {
        cc = GetComponent<CharacterController>();

        _assignedGamepadIndex = -1;
        _joystickAssigned = false;
        deviceAssigned = false;
    }

    void Update()
    {
        if (!deviceAssigned)
        {
            TryAssignDevice();
            return;
        }

        if (currentState == PlayerState.Serving)
        {
            HandleGravity();
            return;
        }

        HandleMovement();
        HandleGravity();
    }

    void TryAssignDevice()
    {
        // Try gamepads
        for (int i = 0; i < Gamepad.all.Count; i++)
        {
            // Skip if this gamepad is already taken by the other player
            if (i == _assignedGamepadIndex) continue;

            var pad = Gamepad.all[i];
            if (pad.buttonSouth.wasPressedThisFrame || pad.buttonNorth.wasPressedThisFrame ||
                pad.buttonEast.wasPressedThisFrame || pad.buttonWest.wasPressedThisFrame ||
                pad.leftTrigger.wasPressedThisFrame || pad.rightTrigger.wasPressedThisFrame ||
                pad.leftShoulder.wasPressedThisFrame || pad.rightShoulder.wasPressedThisFrame ||
                pad.startButton.wasPressedThisFrame)
            {
                gamepadIndex = i;
                useJoystick = false;
                deviceAssigned = true;
                _assignedGamepadIndex = i;
                Debug.Log($"Player {playerNumber} assigned to Gamepad {i}: {Gamepad.all[i].name}");
                return;
            }
        }

        // Try joystick
        if (!_joystickAssigned && Joystick.all.Count > 0)
        {
            var joy = Joystick.all[0];
            foreach (var control in joy.allControls)
            {
                if (control is ButtonControl btn && btn.wasPressedThisFrame)
                {
                    useJoystick = true;
                    deviceAssigned = true;
                    _joystickAssigned = true;
                    Debug.Log($"Player {playerNumber} assigned to Joystick: {joy.name}");
                    return;
                }
            }
        }
    }

    Vector2 ReadMoveInput()
    {
        if (useJoystick)
        {
            if (Joystick.all.Count == 0) return Vector2.zero;
            var joy = Joystick.all[0];
            return new Vector2(joy.stick.x.ReadValue(), joy.stick.y.ReadValue());
        }
        else
        {
            if (gamepadIndex >= Gamepad.all.Count) return Vector2.zero;
            return Gamepad.all[gamepadIndex].leftStick.ReadValue();
        }
    }

    bool ReadSprint()
    {
        if (useJoystick)
        {
            if (Joystick.all.Count == 0) return false;
            return Joystick.all[0].trigger.isPressed;
        }
        else
        {
            if (gamepadIndex >= Gamepad.all.Count) return false;
            return Gamepad.all[gamepadIndex].leftTrigger.isPressed;
        }
    }

    void HandleMovement()
    {
        Vector2 input = ReadMoveInput();
        bool isSprinting = ReadSprint();

        Vector3 forward = playerBody.forward; forward.y = 0f; forward.Normalize();
        Vector3 right = playerBody.right; right.y = 0f; right.Normalize();

        Vector3 moveDirection = (forward * input.y + right * input.x).normalized;
        bool hasInput = moveDirection.magnitude > 0.1f;

        float targetSpeed = isSprinting ? sprintSpeed : moveSpeed;

        if (hasInput)
            velocity = Vector3.MoveTowards(velocity, moveDirection * targetSpeed, acceleration * Time.deltaTime);
        else
            velocity = Vector3.MoveTowards(velocity, Vector3.zero, deceleration * Time.deltaTime);

        cc.Move(new Vector3(velocity.x, 0f, velocity.z) * Time.deltaTime);

        animator.SetBool("isRunning", hasInput);

        float rightDot = Vector3.Dot(moveDirection, playerBody.right);
        float forwardDot = Vector3.Dot(moveDirection, playerBody.forward);
        animator.SetFloat("moveX", hasInput ? rightDot : 0f, 0.1f, Time.deltaTime);
        animator.SetFloat("moveZ", hasInput ? forwardDot : 0f, 0.1f, Time.deltaTime);
    }

    void HandleGravity()
    {
        if (!cc.isGrounded)
            cc.Move(Vector3.down * 9.81f * Time.deltaTime);
    }

    public void StartServe() { currentState = PlayerState.Serving; StopMovement(); }
    public void EndServe() { currentState = PlayerState.Normal; }
    public void StartSwing() { currentState = PlayerState.Swinging; }
    public void EndSwing() { currentState = PlayerState.Normal; }
    public void StopMovement() { velocity = Vector3.zero; }
}