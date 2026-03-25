using UnityEngine;

public class BallPhysics : MonoBehaviour
{
    [Header("Spin")]
    [SerializeField] private float magnusCoefficient = 0.05f;

    private Rigidbody rb;
    private Vector3 spinAxis = Vector3.zero;
    private float spinAmount = 0f;
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