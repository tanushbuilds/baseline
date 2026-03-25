using UnityEngine;
using UnityEngine.InputSystem;

public class LookAround : MonoBehaviour
{
    [Header("Look Settings")]
    public float sensitivity = 0.3f;
    public float verticalClamp = 80f;

    private TennisControls playerInput;
    private InputAction lookAction;
    private float xRotation = 0f;

    public Transform playerBody;

    void Awake()
    {
        playerInput = new TennisControls();
    }

    void OnEnable() { playerInput.Enable(); }
    void OnDisable() { playerInput.Disable(); }

    void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    void Update()
    {
        Vector2 lookInput = playerInput.Player.Look.ReadValue<Vector2>();
        float mouseX = lookInput.x * sensitivity;
        float mouseY = lookInput.y * sensitivity;

        xRotation -= mouseY;
        xRotation = Mathf.Clamp(xRotation, -verticalClamp, verticalClamp);

        playerBody.rotation = Quaternion.Euler(xRotation, playerBody.eulerAngles.y + mouseX, 0f);
    }
}
