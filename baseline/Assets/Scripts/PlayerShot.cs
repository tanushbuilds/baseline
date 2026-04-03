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
    [SerializeField] private float hitDelay = 0.2083f;

    [Header("Timing System")]
    [SerializeField] private float perfectWindowDuration = 0.15f;
    [SerializeField] private float totalTimingWindow = 0.5f;
    [SerializeField] private float minPowerMultiplier = 0.45f;
    [SerializeField] private float maxDirectionError = 12f;

    [Header("Input")]
    [SerializeField] private InputActionAsset inputActionAsset;
    [SerializeField] private string actionMapName = "Player";
    [SerializeField] private string moveActionName = "Move";
    [SerializeField] private string takebackActionName = "Takeback";
    [SerializeField] private string flatActionName = "FlatShot";
    [SerializeField] private string topspinActionName = "TopspinShot";
    [SerializeField] private string sliceActionName = "SliceShot";

    [Header("Audio")]
    [SerializeField] private AudioSource hitAudioSource;
    [SerializeField] private AudioSource whooshAudioSource;
    [SerializeField] private AudioClip flatHitSound;
    [SerializeField] private AudioClip topspinHitSound;
    [SerializeField] private AudioClip sliceHitSound;
    [SerializeField] private AudioClip racketWhoosh;
    [SerializeField] private float whooshMinPitch = 0.9f;
    [SerializeField] private float whooshMaxPitch = 1.1f;

    [Header("References")]
    [SerializeField] private GameObject ball;
    [SerializeField] private Animator anim;
    [SerializeField] private Transform playerBody;

    private InputAction moveAction;
    private InputAction takebackAction;
    private InputAction flatAction;
    private InputAction topspinAction;
    private InputAction sliceAction;

    private Rigidbody ballRb;
    private float takebackTimer = 0f;

    /*
    private bool timingWindowOpen = false;
    private float timingWindowTimer = 0f;
    private bool ballWasInRange = false;
    */

    void Awake()
    {
        ballRb = ball.GetComponent<Rigidbody>();

        var map = inputActionAsset.FindActionMap(actionMapName, true);
        moveAction = map.FindAction(moveActionName, true);
        takebackAction = map.FindAction(takebackActionName, true);
        flatAction = map.FindAction(flatActionName, true);
        topspinAction = map.FindAction(topspinActionName, true);
        sliceAction = map.FindAction(sliceActionName, true);
    }

    void OnEnable()
    {
        moveAction.Enable();
        takebackAction.Enable();
        flatAction.Enable();
        topspinAction.Enable();
        sliceAction.Enable();
    }

    void OnDisable()
    {
        moveAction.Disable();
        takebackAction.Disable();
        flatAction.Disable();
        topspinAction.Disable();
        sliceAction.Disable();
    }

    void Update()
    {
        HandleTakeback();

        /*
        bool ballInRange = Vector3.Distance(transform.position, ball.transform.position) <= hitRadius;
        if (ballInRange && !ballWasInRange) OpenTimingWindow();
        else if (!ballInRange && ballWasInRange) CloseTimingWindow();
        ballWasInRange = ballInRange;

        if (timingWindowOpen)
            timingWindowTimer += Time.deltaTime;
        */

        if (flatAction.WasPressedThisFrame()) TryHit(0f);
        if (topspinAction.WasPressedThisFrame()) TryHit(1f);
        if (sliceAction.WasPressedThisFrame()) TryHit(-1f);
    }

    void HandleTakeback()
    {
        bool held = takebackAction.IsPressed();
        anim.SetBool("Takeback", held);
        takebackTimer = held ? takebackTimer + Time.deltaTime : 0f;
    }

    /*

    void OpenTimingWindow() { timingWindowOpen = true; timingWindowTimer = 0f; }
    void CloseTimingWindow() { timingWindowOpen = false; timingWindowTimer = 0f; }

    float CalculateTimingScore()
    {
        if (!timingWindowOpen) return minPowerMultiplier;
        float t = timingWindowTimer;
        if (t <= perfectWindowDuration) return 1f;
        if (t <= totalTimingWindow)
        {
            float decay = 1f - ((t - perfectWindowDuration) / (totalTimingWindow - perfectWindowDuration));
            return Mathf.Lerp(minPowerMultiplier, 1f, decay);
        }
        return minPowerMultiplier;
    }
    */

    void TryHit(float shotInput)
    {
        if (takebackTimer < takebackThreshold) return;

        anim.SetTrigger("Hit");

        if (whooshAudioSource != null && racketWhoosh != null)
        {
            whooshAudioSource.pitch = Random.Range(whooshMinPitch, whooshMaxPitch);
            whooshAudioSource.PlayOneShot(racketWhoosh);
        }

        takebackTimer = 0f;

        if (Vector3.Distance(transform.position, ball.transform.position) > hitRadius) return;

        float timingScore = 1f;

        Vector3 toBall = ball.transform.position - transform.position;
        float side = Vector3.Dot(toBall, playerBody.right);
        Debug.Log($"{gameObject.name}: {(side >= 0 ? "Forehand" : "Backhand")}");

        StartCoroutine(DelayedHit(shotInput, timingScore));
    }

    IEnumerator DelayedHit(float shotInput, float timingScore)
    {
        yield return new WaitForSeconds(hitDelay);

        CameraShake.Instance.ShakeOnHit();

        AudioClip clipToPlay = shotInput > 0.5f ? topspinHitSound
                             : shotInput < -0.5f ? sliceHitSound
                             : flatHitSound;

        if (hitAudioSource != null && clipToPlay != null)
            hitAudioSource.PlayOneShot(clipToPlay);

        float h = moveAction.ReadValue<Vector2>().x;

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

        float speed = baseSpeed * timingScore;

        float errorAmount = (1f - timingScore) * maxDirectionError;
        float directionError = Random.Range(-errorAmount, errorAmount);

        Vector3 dynamicTarget = new Vector3(
            targetCourtPosition.position.x + h * 4f,
            targetCourtPosition.position.y,
            targetCourtPosition.position.z
        );

        Vector3 toTarget = dynamicTarget - ball.transform.position;
        toTarget = Quaternion.Euler(0, directionError, 0) * toTarget;

        Vector3 velocity = CalculateArcVelocity(
            ball.transform.position,
            ball.transform.position + toTarget,
            speed, arc
        );

        ballRb.linearVelocity = velocity;

        BallPhysics bp = ball.GetComponent<BallPhysics>();
        Vector3 travelDir = new Vector3(velocity.x, 0, velocity.z).normalized;
        Vector3 spinAxis = new Vector3(travelDir.z, 0, -travelDir.x);
        if (bp != null) bp.SetSpin(spinAxis, spinAmount);

        Debug.Log($"{gameObject.name} | Speed: {speed:F1} | Error: {directionError:F1}deg");
    }

    Vector3 CalculateArcVelocity(Vector3 origin, Vector3 target, float speed, float height)
    {
        Vector3 toTarget = target - origin;
        Vector3 toTargetXZ = new Vector3(toTarget.x, 0, toTarget.z);
        float distance = toTargetXZ.magnitude;
        float time = distance / speed;

        Debug.Log($"Origin: {origin}, Target: {target}, Distance: {distance}, Time: {time}");

        float vy = (2 * height) / time + 0.5f * Mathf.Abs(Physics.gravity.y) * time;
        Vector3 vel = toTargetXZ.normalized * speed;
        vel.y = vy;

        Debug.Log($"Velocity: {vel}");

        return vel;
    }
}