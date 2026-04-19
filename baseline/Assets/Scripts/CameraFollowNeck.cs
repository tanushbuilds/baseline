using UnityEngine;

/// <summary>
/// Attach to the Camera. Do NOT parent the camera to the neck.
/// Only smooths rotation. Position is untouched.
/// </summary>
public class CameraFollowNeck : MonoBehaviour
{
    [Header("References")]
    public Transform neckBone;

    [Header("Rotation Smoothing  (higher = snappier  |  lower = more lag)")]
    public float rotationSmoothSpeed = 6f;

    private Quaternion _neckRestRot;
    private Quaternion _camRestRot;
    private Quaternion _smoothRot;

    void Start()
    {
        if (neckBone == null)
        {
            Debug.LogError("[CameraFollowNeck] neckBone is not assigned!", this);
            enabled = false;
            return;
        }

        _neckRestRot = neckBone.rotation;
        _camRestRot = transform.rotation;
        _smoothRot = _camRestRot;
    }

    void LateUpdate()
    {
        Quaternion neckRotDelta = neckBone.rotation * Quaternion.Inverse(_neckRestRot);
        Quaternion desiredRot = neckRotDelta * _camRestRot;

        _smoothRot = Quaternion.Slerp(_smoothRot, desiredRot, Time.deltaTime * rotationSmoothSpeed);
        transform.rotation = _smoothRot;
    }

#if UNITY_EDITOR
    [ContextMenu("Re-capture Rest Pose")]
    void RecaptureRestPose()
    {
        if (neckBone == null) return;
        _neckRestRot = neckBone.rotation;
        _camRestRot = transform.rotation;
        _smoothRot = _camRestRot;
        Debug.Log("[CameraFollowNeck] Rest pose re-captured.");
    }
#endif
}