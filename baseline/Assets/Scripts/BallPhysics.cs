using UnityEngine;

public class BallPhysics : MonoBehaviour
{
    [Header("Spin")]
    public float magnusCoefficient = 0.05f;
    public Vector3 spinAxis = Vector3.zero;
    public float spinAmount = 0f;

    private Rigidbody rb;
    private float spinDelay = 0.4f;
    private float spinTimer = 0f;
    private bool spinActive = false;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
    }

    void OnCollisionEnter(Collision collision)
    {
        spinActive = false;
        spinAmount = 0f;
        spinAxis = Vector3.zero;
        spinTimer = 0f;
        rb.angularVelocity = Vector3.zero;
    }

    void FixedUpdate()
    {
        if (spinActive)
        {
            Debug.Log("Magnus active, spinAmount: " + spinAmount + " velocity: " + rb.linearVelocity);
            ApplyMagnusEffect();
        }

        if (spinAmount != 0f && !spinActive)
        {
            spinTimer += Time.fixedDeltaTime;
            if (spinTimer >= spinDelay)
                spinActive = true;
        }
    }

    void ApplyMagnusEffect()
    {
        Vector3 magnusForce = Vector3.Cross(spinAxis * spinAmount, rb.linearVelocity);
        rb.AddForce(magnusForce * magnusCoefficient);
    }

    public void SetSpin(Vector3 axis, float amount)
    {
        spinAxis = axis;
        spinAmount = amount;
        spinActive = false;
        spinTimer = 0f;
    }
}