using UnityEngine;
using UnityEngine.InputSystem;

public class LookAround : MonoBehaviour
{
    [Header("Look Settings")]
    [SerializeField] private float sensitivity = 0.3f;
    [SerializeField] private float verticalClamp = 80f;
    [SerializeField] private Transform playerBody;
    [SerializeField] private Transform spineBone;

    private TennisControls playerInput;
    private float xRotation = 0f;

    void Awake() { playerInput = new TennisControls(); }
    void OnEnable() { playerInput.Enable(); }
    void OnDisable() { playerInput.Disable(); }

    void Update()
    {
        if (Mouse.current != null && Mouse.current.leftButton.isPressed) return;

        Vector2 lookInput = playerInput.Player.Look.ReadValue<Vector2>();
        float mouseX = lookInput.x * sensitivity;
        float mouseY = lookInput.y * sensitivity;

        xRotation -= mouseY;
        xRotation = Mathf.Clamp(xRotation, -verticalClamp, verticalClamp);

        playerBody.Rotate(Vector3.up * mouseX);
    }

    void LateUpdate()
    {
        spineBone.localRotation *= Quaternion.Euler(xRotation, 0f, 0f);
    }
}