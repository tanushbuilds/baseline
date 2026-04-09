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

    [Header("Serve Ball")]
    [SerializeField] private ServeBall serveBall;


    [Header("Serve Settings")]
    [SerializeField] private float serveSpeed = 30f;
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
    //[SerializeField] private AudioClip flatHitSound;
    //[SerializeField] private AudioClip topspinHitSound;
    [SerializeField] private AudioClip hitSound;
    //[SerializeField] private AudioClip sliceHitSound;
    [SerializeField] private AudioClip racketWhoosh;
    [SerializeField] private float whooshMinPitch = 0.9f;
    [SerializeField] private float whooshMaxPitch = 1.1f;

    [Header("References")]
    [SerializeField] private GameObject ball;
    [SerializeField] private Animator anim;
    [SerializeField] private Transform playerBody;

    // movement reference
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



    private bool hasHit;

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
        moveAction.Enable();
        takebackAction.Enable();
        flatAction.Enable();
        topspinAction.Enable();
        sliceAction.Enable();
        serveAction.Enable();
    }

    void OnDisable()
    {
        moveAction.Disable();
        takebackAction.Disable();
        flatAction.Disable();
        topspinAction.Disable();
        sliceAction.Disable();
        serveAction.Disable();
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

        anim.SetBool("ServePrepare", held);
    }

    void FireServe()
    {
        if (playerMovement != null &&
            playerMovement.currentState != PlayerMovement.PlayerState.Serving)
            return;

        anim.SetTrigger("ServeHit");

        hasHit = true;

        // Allow slight horizontal aim via move input
        Vector2 input = moveAction.ReadValue<Vector2>();
        Vector3 worldMove = playerBody.TransformDirection(new Vector3(input.x, 0f, input.y));

        Vector3 target = serveTargetPosition.position + new Vector3(worldMove.x * 2f, 0f, 0f);

        // Serve arc: ball starts high (contact point), travels downward into the service box.
        // Negative arc height means the peak is already behind us — ball goes high-to-low,
        // mirroring how topspin goes low-to-high-to-low but inverted.
        float serveArcHeight = 0.5f;

        Vector3 velocity = CalculateArcVelocity(
            ball.transform.position,
            target,
            serveSpeed,
            serveArcHeight
        );

        ballRb.linearVelocity = velocity;

        // Topspin so the ball kicks down after bouncing
        BallPhysics bp = ball.GetComponent<BallPhysics>();
        if (bp != null)
        {
            Vector3 travelDir = new Vector3(velocity.x, 0f, velocity.z).normalized;
            Vector3 spinAxis = new Vector3(travelDir.z, 0f, -travelDir.x);
            bp.SetSpin(spinAxis, 2f);
        }
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
                lockedForehand = side >= 0f;
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
            playerMovement.currentState == PlayerMovement.PlayerState.Serving)
            return;

        if (takebackTimer < takebackThreshold) return;

        anim.SetTrigger("Hit");

        if (whooshAudioSource != null && racketWhoosh != null)
        {
            whooshAudioSource.pitch = Random.Range(whooshMinPitch, whooshMaxPitch);
            whooshAudioSource.PlayOneShot(racketWhoosh);
        }

        takebackTimer = 0f;

        if (Vector3.Distance(transform.position, ball.transform.position) > hitRadius)
            return;

        hasHit = true;

        bool isForehand = lockedForehand ?? true;

        Vector2 input = moveAction.ReadValue<Vector2>();
        Vector3 worldMove = playerBody.TransformDirection(new Vector3(input.x, 0, input.y));
        float h = worldMove.x;

        float baseSpeed, spinAmount, arc;

        if (shotInput > 0.5f)
        {
            baseSpeed = topspinSpeed; spinAmount = 1.5f; arc = topspinArc;
        }
        else if (shotInput < -0.5f)
        {
            baseSpeed = sliceSpeed; spinAmount = -1.5f; arc = sliceArc;
        }
        else
        {
            baseSpeed = flatSpeed; spinAmount = 0f; arc = flatArc;
        }

        Vector3 dynamicTarget = new Vector3(
            targetCourtPosition.position.x + h * 4f,
            targetCourtPosition.position.y,
            targetCourtPosition.position.z
        );

        Vector3 velocity = CalculateArcVelocity(
            ball.transform.position,
            dynamicTarget,
            baseSpeed,
            arc
        );

        ballRb.linearVelocity = velocity;

        BallPhysics bp = ball.GetComponent<BallPhysics>();
        if (bp != null)
        {
            Vector3 travelDir = new Vector3(velocity.x, 0, velocity.z).normalized;
            Vector3 spinAxis = new Vector3(travelDir.z, 0, -travelDir.x);
            bp.SetSpin(spinAxis, spinAmount);
        }
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

    private void PlayHitSound()
    {
        if (hasHit && hitAudioSource != null && hitSound != null)
            hitAudioSource.PlayOneShot(hitSound);
    }

    // ===== SERVE CONTROL =====

    public void StartServe()
    {
        anim.SetBool("ServeStance", true);
    }

    public void EndServe()
    {
        anim.SetBool("ServeStance", false);
    }

    public void ReleaseBall()
    {
        serveBall.ReleaseBall();
    }
}