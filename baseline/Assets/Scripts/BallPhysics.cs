using UnityEngine;

public class BallPhysics : MonoBehaviour
{
    [Header("Spin")]
    public float magnusCoefficient = 0.05f;
    public Vector3 spinAxis = Vector3.zero;
    public float spinAmount = 0f;

    private Rigidbody rb;
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
        rb.angularVelocity = Vector3.zero;
    }

    void FixedUpdate()
    {
        if (spinActive)
            ApplyMagnusEffect();
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
        spinActive = true;
    }
}