using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections;

public class PlayerShot : MonoBehaviour
{
    [Header("Shot Settings")]
    [SerializeField] private float flatSpeed = 28f;
    [SerializeField] private float topspinSpeed = 22f;
    [SerializeField] private float sliceSpeed = 18f;
    [SerializeField] private float hitRadius = 2f;
    [SerializeField] private Transform targetCourtPosition;

    [Header("Arc Heights")]
    [SerializeField] private float flatArc = 1.2f;
    [SerializeField] private float topspinArc = 1.8f;
    [SerializeField] private float sliceArc = 1.0f;

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

    [Header("Input")]
    [SerializeField] private InputActionAsset inputActionAsset;
    [SerializeField] private string actionMapName = "Player";
    [SerializeField] private string moveActionName = "Move";
    [SerializeField] private string takebackActionName = "Takeback";
    [SerializeField] private string flatActionName = "FlatShot";
    [SerializeField] private string topspinActionName = "TopspinShot";
    [SerializeField] private string sliceActionName = "SliceShot";
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

    private InputAction moveAction;
    private InputAction takebackAction;
    private InputAction flatAction;
    private InputAction topspinAction;
    private InputAction sliceAction;
    private InputAction serveAction;

    private Rigidbody ballRb;
    private float takebackTimer = 0f;
    private bool? lockedForehand = null;

    private bool hasHit = false;
    private bool ballReleased = false;
    private bool isPreparingServe = false;

    void Awake()
    {
        ballRb = ball.GetComponent<Rigidbody>();

        var map = inputActionAsset.FindActionMap(actionMapName, true);
        moveAction = map.FindAction(moveActionName, true);
        takebackAction = map.FindAction(takebackActionName, true);
        flatAction = map.FindAction(flatActionName, true);
        topspinAction = map.FindAction(topspinActionName, true);
        sliceAction = map.FindAction(sliceActionName, true);
        serveAction = map.FindAction(serveActionName, true);
    }

    void OnEnable()
    {
        moveAction.Enable(); takebackAction.Enable();
        flatAction.Enable(); topspinAction.Enable();
        sliceAction.Enable(); serveAction.Enable();
    }

    void OnDisable()
    {
        moveAction.Disable(); takebackAction.Disable();
        flatAction.Disable(); topspinAction.Disable();
        sliceAction.Disable(); serveAction.Disable();
    }

    void Update()
    {
        HandleTakeback();
        HandleServePrepare();

        if (flatAction.WasPressedThisFrame()) TryHit(0f);
        if (topspinAction.WasPressedThisFrame()) TryHit(1f);
        if (sliceAction.WasPressedThisFrame()) TryHit(-1f);
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
            anim.SetBool("ForehandTakeback", false);
            anim.SetBool("BackhandTakeback", false);
        }
    }

    void TryHit(float shotInput)
    {
        if (playerMovement != null &&
            playerMovement.currentState == PlayerMovement.PlayerState.Serving) return;

        if (takebackTimer < takebackThreshold) return;

        anim.SetTrigger("Hit");
        playerMovement?.StartSwing();
        StartCoroutine(ResetHasHit());

        if (whooshAudioSource != null && racketWhoosh != null)
        {
            whooshAudioSource.pitch = Random.Range(whooshMinPitch, whooshMaxPitch);
            whooshAudioSource.PlayOneShot(racketWhoosh);
        }

        takebackTimer = 0f;

        if (Vector3.Distance(transform.position, ball.transform.position) > hitRadius) return;

        bool isForehand = lockedForehand ?? true;
        float hitDelay = isForehand ? forehandHitDelay : backhandHitDelay;
        StartCoroutine(DelayedHit(shotInput, hitDelay));
    }

    IEnumerator DelayedHit(float shotInput, float hitDelay)
    {
        yield return new WaitForSeconds(hitDelay);

        if (hitAudioSource != null && hitSound != null)
            hitAudioSource.PlayOneShot(hitSound);

        Vector2 input = moveAction.ReadValue<Vector2>();
        Vector3 worldMove = playerBody.TransformDirection(new Vector3(input.x, 0, input.y));
        float h = worldMove.x;

        float baseSpeed, spinAmount, arc;
        if (shotInput > 0.5f) { baseSpeed = topspinSpeed; spinAmount = 1.5f; arc = topspinArc; }
        else if (shotInput < -0.5f) { baseSpeed = sliceSpeed; spinAmount = -1.5f; arc = sliceArc; }
        else { baseSpeed = flatSpeed; spinAmount = 0f; arc = flatArc; }

        Vector3 dynamicTarget = new Vector3(
            targetCourtPosition.position.x + h * 4f,
            targetCourtPosition.position.y,
            targetCourtPosition.position.z
        );

        Vector3 velocity = CalculateArcVelocity(ball.transform.position, dynamicTarget, baseSpeed, arc);
        ballRb.linearVelocity = velocity;

        BallPhysics bp = ball.GetComponent<BallPhysics>();
        if (bp != null)
        {
            Vector3 travelDir = new Vector3(velocity.x, 0, velocity.z).normalized;
            bp.SetSpin(new Vector3(travelDir.z, 0, -travelDir.x), spinAmount);
        }

        hasHit = true;
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
        yield return new WaitForSeconds(0.5f);
        hasHit = false;
        playerMovement?.EndSwing();
    }
}