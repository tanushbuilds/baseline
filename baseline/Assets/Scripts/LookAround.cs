using UnityEngine;
using UnityEngine.InputSystem;

public class LookAround : MonoBehaviour
{
    [Header("Look Settings")]
    [SerializeField] private float sensitivity = 0.3f;
    [SerializeField] private Transform playerBody;

    private TennisControls playerInput;

    void Awake() { playerInput = new TennisControls(); }
    void OnEnable() { playerInput.Enable(); }
    void OnDisable() { playerInput.Disable(); }

    void Update()
    {
        if (Mouse.current != null && Mouse.current.leftButton.isPressed) return;

        Vector2 lookInput = playerInput.Player.Look.ReadValue<Vector2>();
        float mouseX = lookInput.x * sensitivity;
        float mouseY = lookInput.y * sensitivity;

        playerBody.Rotate(Vector3.up * mouseX);
    }
}