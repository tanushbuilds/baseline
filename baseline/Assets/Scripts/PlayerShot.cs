using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
using System.Collections;
using System;
using Random = UnityEngine.Random;

public class PlayerShot : MonoBehaviour
{
    [Header("Shot Settings")]
    [SerializeField] private float topspinSpeed = 22f;
    [SerializeField] private float hitRadius = 2f;
    [SerializeField] private Transform targetCourtPosition;

    [Header("Arc Heights")]
    [SerializeField] private float topspinArc = 1.8f;
    [SerializeField] private float serveArc = 1.8f;

    [Header("Takeback")]
    [SerializeField] private float takebackThreshold = 0.2f;

    [Header("Timing")]
    [SerializeField] private float forehandHitDelay = 0.2083f;
    [SerializeField] private float backhandHitDelay = 0.1667f;

    [Header("Serve Settings")]
    [SerializeField] private float serveSpeed = 30f;
    [SerializeField] private Transform serveTargetPosition;

    [Header("Shot Offsets")]
    [SerializeField] private float maxHorizontalOffset = 4f;
    [SerializeField] private float minDepthOffset = -3f;
    [SerializeField] private float maxDepthOffset = 3f;

    [Header("Audio")]
    [SerializeField] private AudioSource hitAudioSource;
    [SerializeField] private AudioSource whooshAudioSource;
    [SerializeField] private AudioClip hitSound;
    [SerializeField] private AudioClip racketWhoosh;
    [SerializeField] private float whooshMinPitch = 0.9f;
    [SerializeField] private float whooshMaxPitch = 1.1f;

    [Header("References")]
    [SerializeField] private GameObject ball;
    [SerializeField] private Animator anim;
    [SerializeField] private Transform playerBody;
    [SerializeField] private bool flipSide = false;
    [SerializeField] private PlayerMovement playerMovement;
    [SerializeField] private string NAME;

    [Header("IK")]
    [SerializeField] private RacketIK racketIK;

    // ── Internal ──────────────────────────────────────────────────────────────
    private Rigidbody ballRb;
    private float takebackTimer = 0f;
    private bool isServing = false;

    public event Action<string> OnServeHit;
    public event Action OnBallHit;

    // add this with your other state fields
    private bool takebackLocked = false;
    private bool lockedForehand = false;

    // ─────────────────────────────────────────────────────────────────────────

    void Awake()
    {
        ballRb = ball.GetComponent<Rigidbody>();
    }

    // ── Device helpers — reads from PlayerMovement's assigned device ──────────

    Gamepad GetGamepad()
    {
        if (playerMovement.useJoystick) return null;
        int idx = playerMovement.gamepadIndex;
        return idx < Gamepad.all.Count ? Gamepad.all[idx] : null;
    }

    Joystick GetJoystick()
    {
        if (!playerMovement.useJoystick) return null;
        return Joystick.all.Count > 0 ? Joystick.all[0] : null;
    }

    ButtonControl GetJoystickButton(Joystick joy, string name)
    {
        foreach (var control in joy.allControls)
            if (control.name == name && control is ButtonControl btn)
                return btn;
        return null;
    }

    Vector2 ReadMoveInput()
    {
        if (playerMovement.useJoystick)
        {
            var joy = GetJoystick();
            if (joy == null) return Vector2.zero;
            return new Vector2(joy.stick.x.ReadValue(), joy.stick.y.ReadValue());
        }
        else
        {
            var pad = GetGamepad();
            if (pad == null) return Vector2.zero;
            return pad.leftStick.ReadValue();
        }
    }

    bool WasButtonPressed(string button)
    {
        if (!playerMovement.deviceAssigned) return false;

        if (playerMovement.useJoystick)
        {
            var joy = GetJoystick();
            if (joy == null) return false;

            // SHANWAN Android Gamepad layout:
            // trigger = A, button2 = B, button4 = X, button5 = Y
            // button7 = LB (Serve), button9 = LT (Takeback)
            var lt = GetJoystickButton(joy, "button9");
            var lb = GetJoystickButton(joy, "button7");
            var a = GetJoystickButton(joy, "trigger");
            var b = GetJoystickButton(joy, "button2");
            var x = GetJoystickButton(joy, "button4");
            var y = GetJoystickButton(joy, "button5");

            return button switch
            {
                "Takeback" => lt != null && lt.wasPressedThisFrame,
                "Serve" => lb != null && lb.wasPressedThisFrame,
                "Hit" => (a != null && a.wasPressedThisFrame) ||
                              (b != null && b.wasPressedThisFrame) ||
                              (x != null && x.wasPressedThisFrame) ||
                              (y != null && y.wasPressedThisFrame),
                _ => false
            };
        }
        else
        {
            var pad = GetGamepad();
            if (pad == null) return false;

            return button switch
            {
                "Takeback" => pad.leftTrigger.wasPressedThisFrame,
                "Serve" => pad.leftShoulder.wasPressedThisFrame,
                "Hit" => pad.buttonSouth.wasPressedThisFrame ||
                              pad.buttonNorth.wasPressedThisFrame ||
                              pad.buttonEast.wasPressedThisFrame ||
                              pad.buttonWest.wasPressedThisFrame,
                _ => false
            };
        }
    }

    bool IsButtonHeld(string button)
    {
        if (!playerMovement.deviceAssigned) return false;

        if (playerMovement.useJoystick)
        {
            var joy = GetJoystick();
            if (joy == null) return false;
            var lt = GetJoystickButton(joy, "button9");
            return button == "Takeback" && lt != null && lt.isPressed;
        }
        else
        {
            var pad = GetGamepad();
            if (pad == null) return false;
            return button == "Takeback" && pad.leftTrigger.isPressed;
        }
    }

    // ─────────────────────────────────────────────────────────────────────────

    void Update()
    {
        // Don't do anything until device is assigned
        if (!playerMovement.deviceAssigned) return;

        if (WasButtonPressed("Serve") &&
            playerMovement.currentState == PlayerMovement.PlayerState.Serving)
            FireServe();

        if (isServing) return;

        HandleTakeback();
        HandleShots();
    }

    void HandleTakeback()
    {
        bool held = IsButtonHeld("Takeback");
        takebackTimer = held ? takebackTimer + Time.deltaTime : 0f;

        if (held)
        {
            if (!takebackLocked)
            {
                // First frame of holding — decide forehand or backhand and lock it
                Vector3 toBall = ball.transform.position - playerBody.position;
                float side = Vector3.Dot(toBall, Vector3.right);
                lockedForehand = flipSide ? side < 0f : side >= 0f;
                takebackLocked = true;
            }

            anim.SetBool("ForehandTakeback", lockedForehand);
            anim.SetBool("BackhandTakeback", !lockedForehand);
        }
        else
        {
            if (takebackLocked)
            {
                takebackLocked = false;
                StartCoroutine(ClearTakebackBools());
            }
        }
    }

    void HandleShots()
    {
        if (takebackTimer < takebackThreshold) return;
        if (playerMovement.currentState == PlayerMovement.PlayerState.Serving) return;

        if (WasButtonPressed("Hit"))
        {
            Vector2 moveInput = ReadMoveInput();

            float horizontalOffset = moveInput.x * maxHorizontalOffset;
            if (flipSide) horizontalOffset = -horizontalOffset;

            float depthOffset = moveInput.y > 0.1f ? maxDepthOffset :
                                moveInput.y < -0.1f ? minDepthOffset : 0f;

            TryHit(horizontalOffset, depthOffset);
        }
    }

    // ─────────────────────────────────────────────────────────────────────────

    public void FireServe()
    {
        if (playerMovement.currentState != PlayerMovement.PlayerState.Serving) return;
        Serve();
    }

    private void Serve()
    {
        Vector3 target = serveTargetPosition.position;
        Vector3 velocity = CalculateArcVelocity(ball.transform.position, target, serveSpeed, serveArc);
        ballRb.linearVelocity = velocity;

        BallPhysics bp = ball.GetComponent<BallPhysics>();
        if (bp != null)
        {
            Vector3 travelDir = new Vector3(velocity.x, 0f, velocity.z).normalized;
            bp.SetSpin(new Vector3(travelDir.z, 0f, -travelDir.x), 2f);
        }

        OnServeHit?.Invoke(NAME);
        isServing = false;
        StartCoroutine(ResetHasHit());
    }

    void TryHit(float horizontalOffset, float depthOffset)
    {
        anim.ResetTrigger("Hit");
        anim.SetTrigger("Hit");
        playerMovement?.StartSwing();

        if (whooshAudioSource != null && racketWhoosh != null)
        {
            whooshAudioSource.pitch = Random.Range(whooshMinPitch, whooshMaxPitch);
            whooshAudioSource.PlayOneShot(racketWhoosh);
        }

        takebackTimer = 0f;

        if (Vector3.Distance(transform.position, ball.transform.position) > hitRadius)
        {
            StartCoroutine(ResetHasHit());
            return;
        }

        Vector3 toBall = ball.transform.position - playerBody.position;
        float side = Vector3.Dot(toBall, Vector3.right);
        bool isForehand = flipSide ? side < 0f : side >= 0f;
        racketIK?.TriggerIK(ball.transform.position, isForehand);

        float hitDelay = isForehand ? forehandHitDelay : backhandHitDelay;
        StartCoroutine(DelayedHit(hitDelay, horizontalOffset, depthOffset));
    }

    IEnumerator DelayedHit(float hitDelay, float horizontalOffset, float depthOffset)
    {
        yield return new WaitForSeconds(hitDelay);

        OnBallHit?.Invoke();

        if (hitAudioSource != null && hitSound != null)
            hitAudioSource.PlayOneShot(hitSound);

        Vector3 dynamicTarget = new Vector3(
            targetCourtPosition.position.x + horizontalOffset,
            targetCourtPosition.position.y,
            targetCourtPosition.position.z + depthOffset
        );

        Vector3 velocity = CalculateArcVelocity(ball.transform.position, dynamicTarget,
                                                topspinSpeed, topspinArc);
        ballRb.linearVelocity = velocity;

        BallPhysics bp = ball.GetComponent<BallPhysics>();
        if (bp != null)
        {
            Vector3 travelDir = new Vector3(velocity.x, 0, velocity.z).normalized;
            bp.SetSpin(new Vector3(travelDir.z, 0, -travelDir.x), 1.5f);
        }

        StartCoroutine(ResetHasHit());
    }

    Vector3 CalculateArcVelocity(Vector3 origin, Vector3 target, float speed, float height)
    {
        Vector3 toTarget = target - origin;
        Vector3 toTargetXZ = new Vector3(toTarget.x, 0, toTarget.z);
        float distance = toTargetXZ.magnitude;
        float time = distance / speed;
        float vy = (2 * height) / time + 0.5f * Mathf.Abs(Physics.gravity.y) * time;
        Vector3 vel = toTargetXZ.normalized * speed;
        vel.y = vy;
        return vel;
    }

    IEnumerator ClearTakebackBools()
    {
        yield return null;
        anim.SetBool("ForehandTakeback", false);
        anim.SetBool("BackhandTakeback", false);
    }

    IEnumerator ResetHasHit()
    {
        yield return new WaitForSeconds(0.7f);
        playerMovement?.EndSwing();
    }

    public void SetIsServing(bool serving) => isServing = serving;
}