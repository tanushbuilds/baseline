using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections;

public class PlayerShot : MonoBehaviour
{
    [Header("Shot Settings")]
    [SerializeField] private float topspinSpeed = 22f;
    [SerializeField] private float hitRadius = 2f;
    [SerializeField] private Transform targetCourtPosition;

    [Header("Arc Heights")]
    [SerializeField] private float topspinArc = 1.8f;

    [Header("Takeback")]
    [SerializeField] private float takebackThreshold = 0.2f;

    [Header("Timing")]
    [SerializeField] private float forehandHitDelay = 0.2083f;
    [SerializeField] private float backhandHitDelay = 0.1667f;

    [Header("Serve Ball")]
    [SerializeField] private ServeBall serveBall;

    [Header("Serve Settings")]
    [SerializeField] private float serveSpeed = 30f;
    [SerializeField] private float serveHitDelay = 0.21f;
    [SerializeField] private Transform serveTargetPosition;

    [Header("Swipe")]
    [SerializeField] private float minSwipeDistance = 50f;      // pixels — below this ignored
    [SerializeField] private float maxSwipeDistance = 400f;     // pixels — maps to max depth
    [SerializeField] private float maxHorizontalOffset = 4f;    // world units left/right aim
    [SerializeField] private float minDepthOffset = -3f;        // world units — short swipe (short ball)
    [SerializeField] private float maxDepthOffset = 3f;         // world units — long swipe (deep ball)
    [SerializeField] private float overshootMultiplier = 1.5f;  // how much fast swipes overshoot
    [SerializeField] private float fastSwipeThreshold = 800f;   // pixels/sec — above this = fast swipe
    [SerializeField] private Camera cam;

    [Header("Input")]
    [SerializeField] private InputActionAsset inputActionAsset;
    [SerializeField] private string actionMapName = "Player";
    [SerializeField] private string moveActionName = "Move";
    [SerializeField] private string takebackActionName = "Takeback";
    [SerializeField] private string serveActionName = "Serve";

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

    [Header("IK")]
    [SerializeField] private RacketIK racketIK;

    private InputAction moveAction;
    private InputAction takebackAction;
    private InputAction serveAction;

    private InputAction _pointerPosition;
    private InputAction _pointerPress;

    private Rigidbody ballRb;
    private float takebackTimer = 0f;
    private bool? lockedForehand = null;

    private bool hasHit = false;
    private bool ballReleased = false;
    private bool isPreparingServe = false;

    // Swipe state
    private Vector2 _swipeStart;
    private float _swipeStartTime;
    private bool _isSwiping;

    void Awake()
    {
        if (cam == null) cam = Camera.main;
        ballRb = ball.GetComponent<Rigidbody>();

        var map = inputActionAsset.FindActionMap(actionMapName, true);
        moveAction = map.FindAction(moveActionName, true);
        takebackAction = map.FindAction(takebackActionName, true);
        serveAction = map.FindAction(serveActionName, true);

        _pointerPosition = new InputAction("SwipePosition", binding: "<Pointer>/position");
        _pointerPress = new InputAction("SwipePress", binding: "<Mouse>/leftButton");

        _pointerPress.started += _ => { _swipeStart = _pointerPosition.ReadValue<Vector2>(); _swipeStartTime = Time.unscaledTime; _isSwiping = true; };
        _pointerPress.canceled += _ => OnSwipeReleased();
    }

    void OnEnable()
    {
        moveAction.Enable(); takebackAction.Enable(); serveAction.Enable();
        _pointerPosition.Enable(); _pointerPress.Enable();
    }

    void OnDisable()
    {
        moveAction.Disable(); takebackAction.Disable(); serveAction.Disable();
        _pointerPosition.Disable(); _pointerPress.Disable();
    }

    void OnSwipeReleased()
    {
        if (!_isSwiping) return;
        _isSwiping = false;

        // Must have takeback held
        if (takebackTimer < takebackThreshold) return;

        // Must not be serving
        if (playerMovement != null &&
            playerMovement.currentState == PlayerMovement.PlayerState.Serving) return;

        Vector2 swipeEnd = _pointerPosition.ReadValue<Vector2>();
        Vector2 swipeDelta = swipeEnd - _swipeStart;
        float swipeLen = swipeDelta.magnitude;

        if (swipeLen < minSwipeDistance) return;

        // Swipe speed (pixels/sec) — drives overshoot
        float swipeDuration = Mathf.Max(Time.unscaledTime - _swipeStartTime, 0.01f);
        float swipeSpeed = swipeLen / swipeDuration;

        // 0 = slow/controlled, 1 = max fast
        float speedFactor = Mathf.Clamp01(swipeSpeed / fastSwipeThreshold);

        // Overshoot: fast swipe pushes target further in swipe direction
        float overshoot = Mathf.Lerp(1f, overshootMultiplier, speedFactor);

        // X → horizontal aim, Y → depth (independent axes)
        float normalizedX = (swipeDelta.x / maxSwipeDistance) * overshoot;
        float horizontalOffset = normalizedX * maxHorizontalOffset;

        float normalizedY = (swipeDelta.y / maxSwipeDistance) * overshoot;
        float depthOffset = Mathf.Lerp(minDepthOffset, maxDepthOffset, Mathf.Clamp01(normalizedY));

        TryHit(horizontalOffset, depthOffset);
    }

    void Update()
    {
        HandleTakeback();
        HandleServePrepare();

        if (serveAction.WasPressedThisFrame()) FireServe();
    }

    void HandleServePrepare()
    {
        bool isServeState = playerMovement != null &&
                            playerMovement.currentState == PlayerMovement.PlayerState.Serving;

        bool held = isServeState && takebackAction.IsPressed();
        isPreparingServe = held;
        anim.SetBool("ServePrepare", held);
    }

    void FireServe()
    {
        if (!ballReleased) return;
        if (playerMovement != null &&
            playerMovement.currentState != PlayerMovement.PlayerState.Serving) return;

        anim.SetTrigger("ServeHit");
        StartCoroutine(DelayedServe());
    }

    IEnumerator DelayedServe()
    {
        yield return new WaitForSeconds(serveHitDelay);

        if (hitAudioSource != null && hitSound != null)
            hitAudioSource.PlayOneShot(hitSound);

        Vector2 input = moveAction.ReadValue<Vector2>();
        Vector3 worldMove = playerBody.TransformDirection(new Vector3(input.x, 0f, input.y));
        Vector3 target = serveTargetPosition.position + new Vector3(worldMove.x * 2f, 0f, 0f);

        Vector3 velocity = CalculateArcVelocity(ball.transform.position, target, serveSpeed, 0.5f);
        ballRb.linearVelocity = velocity;

        BallPhysics bp = ball.GetComponent<BallPhysics>();
        if (bp != null)
        {
            Vector3 travelDir = new Vector3(velocity.x, 0f, velocity.z).normalized;
            bp.SetSpin(new Vector3(travelDir.z, 0f, -travelDir.x), 2f);
        }

        hasHit = true;
        StartCoroutine(ResetHasHit());
    }

    void HandleTakeback()
    {
        bool held = takebackAction.IsPressed();
        takebackTimer = held ? takebackTimer + Time.deltaTime : 0f;

        if (held)
        {
            if (lockedForehand == null)
            {
                Vector3 toBall = ball.transform.position - playerBody.position;
                float side = Vector3.Dot(toBall, Vector3.right);
                lockedForehand = flipSide ? side < 0f : side >= 0f;
            }

            anim.SetBool("ForehandTakeback", lockedForehand.Value);
            anim.SetBool("BackhandTakeback", !lockedForehand.Value);
        }
        else
        {
            lockedForehand = null;
            StartCoroutine(ClearTakebackBools());
        }
    }

    IEnumerator ClearTakebackBools()
    {
        yield return null;
        anim.SetBool("ForehandTakeback", false);
        anim.SetBool("BackhandTakeback", false);
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
            // No hit — still need to end swing after animation
            StartCoroutine(ResetHasHit());
            return;
        }

        bool isForehand = lockedForehand ?? true;
        racketIK?.TriggerIK(ball.transform.position, isForehand);
        float hitDelay = isForehand ? forehandHitDelay : backhandHitDelay;

        StartCoroutine(DelayedHit(hitDelay, horizontalOffset, depthOffset));
    }

    IEnumerator DelayedHit(float hitDelay, float horizontalOffset, float depthOffset)
    {
        yield return new WaitForSeconds(hitDelay);

        if (hitAudioSource != null && hitSound != null)
            hitAudioSource.PlayOneShot(hitSound);

        Vector3 dynamicTarget = new Vector3(
            targetCourtPosition.position.x + horizontalOffset,
            targetCourtPosition.position.y,
            targetCourtPosition.position.z + depthOffset
        );

        Vector3 velocity = CalculateArcVelocity(ball.transform.position, dynamicTarget, topspinSpeed, topspinArc);
        ballRb.linearVelocity = velocity;

        BallPhysics bp = ball.GetComponent<BallPhysics>();
        if (bp != null)
        {
            Vector3 travelDir = new Vector3(velocity.x, 0, velocity.z).normalized;
            bp.SetSpin(new Vector3(travelDir.z, 0, -travelDir.x), 1.5f);
        }

        hasHit = true;
        StartCoroutine(ResetHasHit()); // only called after confirmed hit
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

    // ===== SERVE CONTROL =====
    public void StartServe() { anim.SetBool("ServeStance", true); }
    public void EndServe() { anim.SetBool("ServeStance", false); }

    public void ReleaseBall()
    {
        if (!isPreparingServe || ballReleased) return;
        ballReleased = true;
        serveBall.ReleaseBall();
    }

    private IEnumerator ResetHasHit()
    {
        yield return new WaitForSeconds(0.7f);
        hasHit = false;
        playerMovement?.EndSwing();
    }
}