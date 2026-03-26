using UnityEngine;
using System.Collections;

public class CameraShake : MonoBehaviour
{
    public static CameraShake Instance;

    [Header("Shake Settings")]
    [SerializeField] private float hitShakeDuration = 0.15f;
    [SerializeField] private float hitShakeMagnitude = 0.05f;
    [SerializeField] private float moveShakeMagnitude = 0.008f;
    [SerializeField] private float moveShakeSpeed = 12f;

    private Vector3 originalLocalPos;
    private bool isShaking = false;
    private TennisControls playerInput;
    private float moveShakeTimer = 0f;

    void Awake()
    {
        Instance = this;
        playerInput = new TennisControls();
    }

    void OnEnable() { playerInput.Enable(); }
    void OnDisable() { playerInput.Disable(); }

    void Start()
    {
        originalLocalPos = transform.localPosition;
    }

    void Update()
    {
        HandleMoveShake();
    }

    void HandleMoveShake()
    {
        Vector2 moveInput = playerInput.Player.Move.ReadValue<Vector2>();

        if (moveInput.magnitude > 0.1f && !isShaking)
        {
            moveShakeTimer += Time.deltaTime * moveShakeSpeed;
            float shakeY = Mathf.Sin(moveShakeTimer) * moveShakeMagnitude;
            float shakeX = Mathf.Sin(moveShakeTimer * 0.7f) * moveShakeMagnitude * 0.5f;
            transform.localPosition = originalLocalPos + new Vector3(shakeX, shakeY, 0f);
        }
        else if (!isShaking)
        {
            // Smoothly return to original position when not moving
            transform.localPosition = Vector3.Lerp(
                transform.localPosition,
                originalLocalPos,
                Time.deltaTime * 8f
            );
        }
    }

    public void ShakeOnHit()
    {
        if (!isShaking)
            StartCoroutine(HitShake());
    }

    IEnumerator HitShake()
    {
        isShaking = true;
        float elapsed = 0f;

        while (elapsed < hitShakeDuration)
        {
            float x = Random.Range(-1f, 1f) * hitShakeMagnitude;
            float y = Random.Range(-1f, 1f) * hitShakeMagnitude;

            transform.localPosition = originalLocalPos + new Vector3(x, y, 0f);

            elapsed += Time.deltaTime;
            yield return null;
        }

        transform.localPosition = originalLocalPos;
        isShaking = false;
    }
}