using UnityEngine;

public class SmoothFollow : MonoBehaviour
{
    public Transform target;
    public float smoothSpeed = 10f;
    public Vector3 offset = new Vector3(0f, 0.1f, 0.1f);
    public Vector3 rotationOffset = new Vector3(0f, 0f, 0f);

    void LateUpdate()
    {
        transform.position = target.position + target.TransformDirection(offset);

        Quaternion targetRotation = target.rotation * Quaternion.Euler(rotationOffset);
        transform.rotation = Quaternion.Lerp(transform.rotation, targetRotation, Time.deltaTime * smoothSpeed);
    }
}