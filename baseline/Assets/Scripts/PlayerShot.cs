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

    private TennisControls playerInput;
    private Rigidbody ballRb;
    private float takebackTimer = 0f;

    void Awake()
    {
        playerInput = new TennisControls();
        ballRb = ball.GetComponent<Rigidbody>();
    }

    void OnEnable() { playerInput.Enable(); }
    void OnDisable() { playerInput.Disable(); }

    void Update()
    {
        if (playerInput.Player.Takeback.IsPressed())
        {
            anim.SetBool("Takeback", true);
            takebackTimer += Time.deltaTime;
        }
        else
        {
            anim.SetBool("Takeback", false);
            takebackTimer = 0f;
        }

        if (playerInput.Player.FlatShot.WasPressedThisFrame())
            TryHit(0f);
        if (playerInput.Player.TopspinShot.WasPressedThisFrame())
            TryHit(1f);
        if (playerInput.Player.SliceShot.WasPressedThisFrame())
            TryHit(-1f);
    }

    void TryHit(float shotInput)
    {
        if (takebackTimer < takebackThreshold) return;

        anim.SetTrigger("Hit");
        if (whooshAudioSource != null && racketWhoosh != null)
            whooshAudioSource.pitch = Random.Range(whooshMinPitch, whooshMaxPitch);
            whooshAudioSource.PlayOneShot(racketWhoosh);
        takebackTimer = 0f;

        float distanceToBall = Vector3.Distance(transform.position, ball.transform.position);
        if (distanceToBall > hitRadius) return;

        Vector3 toBall = ball.transform.position - transform.position;
        float side = Vector3.Dot(toBall, playerBody.right);

        if (side >= 0)
            Debug.Log("Forehand");
        else
            Debug.Log("Backhand");

        StartCoroutine(DelayedHit(shotInput));
    }

    IEnumerator DelayedHit(float shotInput)
    {
        yield return new WaitForSeconds(hitDelay);

        CameraShake.Instance.ShakeOnHit();

        AudioClip clipToPlay = shotInput > 0.5f ? topspinHitSound
                             : shotInput < -0.5f ? sliceHitSound
                             : flatHitSound;

        if (hitAudioSource != null && clipToPlay != null)
            hitAudioSource.PlayOneShot(clipToPlay);

        Vector2 moveInput = playerInput.Player.Move.ReadValue<Vector2>();
        float speed;
        float spinAmount;
        float arc;

        if (shotInput > 0.5f)
        {
            speed = topspinSpeed;
            spinAmount = 1.5f;
            arc = topspinArc;
        }
        else if (shotInput < -0.5f)
        {
            speed = sliceSpeed;
            spinAmount = -1.5f;
            arc = sliceArc;
        }
        else
        {
            speed = flatSpeed;
            spinAmount = 0f;
            arc = flatArc;
        }

        float directionOffset = moveInput.x * 4f;
        Vector3 dynamicTarget = new Vector3(
            targetCourtPosition.position.x + directionOffset,
            targetCourtPosition.position.y,
            targetCourtPosition.position.z
        );

        Vector3 velocity = CalculateArcVelocity(
            ball.transform.position,
            dynamicTarget,
            speed,
            arc
        );

        ballRb.linearVelocity = velocity;

        BallPhysics bp = ball.GetComponent<BallPhysics>();
        if (bp != null)
        {
            bp.SetSpin(Vector3.right, spinAmount);
        }
    }

    Vector3 CalculateArcVelocity(Vector3 origin, Vector3 target, float speed, float height)
    {
        Vector3 toTarget = target - origin;
        Vector3 toTargetXZ = new Vector3(toTarget.x, 0, toTarget.z);
        float distance = toTargetXZ.magnitude;
        float time = distance / speed;
        float vy = (2 * height) / time + 0.5f * Mathf.Abs(Physics.gravity.y) * time;
        Vector3 velocity = toTargetXZ.normalized * speed;
        velocity.y = vy;
        return velocity;
    }
}