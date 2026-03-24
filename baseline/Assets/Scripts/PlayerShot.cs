using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections;

public class PlayerShot : MonoBehaviour
{
    [Header("Shot Settings")]
    public float flatSpeed = 28f;
    public float topspinSpeed = 22f;
    public float sliceSpeed = 18f;
    public float hitRadius = 2f;
    public Transform targetCourtPosition;

    [Header("Arc Heights")]
    public float flatArc = 1.2f;
    public float topspinArc = 1.8f;
    public float sliceArc = 1.0f;

    [Header("References")]
    public GameObject ball;
    [SerializeField] private Animator anim;

    private TennisControls playerInput;
    private Rigidbody ballRb;
    private bool isInTakeback = false;

    void Awake()
    {
        playerInput = new TennisControls();
        ballRb = ball.GetComponent<Rigidbody>();
    }

    void OnEnable() { playerInput.Enable(); }
    void OnDisable() { playerInput.Disable(); }

    void Update()
    {
        // Takeback — hold T
        if (playerInput.Player.Takeback.IsPressed())
        {
            if (!isInTakeback)
            {
                isInTakeback = true;
                anim.SetBool("Takeback", true);
            }
        }
        else
        {
            if (isInTakeback)
            {
                isInTakeback = false;
                anim.SetBool("Takeback", false);
            }
        }

        // Only allow hitting if takeback is active
        if (isInTakeback)
        {
            if (playerInput.Player.FlatShot.WasPressedThisFrame())
                TryHit(0f);
            if (playerInput.Player.TopspinShot.WasPressedThisFrame())
                TryHit(1f);
            if (playerInput.Player.SliceShot.WasPressedThisFrame())
                TryHit(-1f);
        }
    }

    void TryHit(float shotInput)
    {
        float distanceToBall = Vector3.Distance(transform.position, ball.transform.position);
        if (distanceToBall > hitRadius) return;

        anim.SetTrigger("Hit");
        isInTakeback = false;

        StartCoroutine(DelayedHit(shotInput));
    }

    IEnumerator DelayedHit(float shotInput)
    {
        // Wait until animation reaches contact point
        yield return new WaitForSeconds(0.29f);

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