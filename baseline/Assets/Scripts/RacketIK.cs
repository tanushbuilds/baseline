using UnityEngine;
using UnityEngine.Animations.Rigging;

public class RacketIK : MonoBehaviour
{
    [Header("IK")]
    [SerializeField] private TwoBoneIKConstraint forehandIK;
    [SerializeField] private TwoBoneIKConstraint backhandIK;
    [SerializeField] private Transform forehandTarget;
    [SerializeField] private Transform backhandTarget;

    [Header("Blend")]
    [SerializeField] private float blendInSpeed = 20f;
    [SerializeField] private float blendOutSpeed = 4f;
    [SerializeField] private float holdDuration = 0.15f;

    [Header("Contact Offset")]
    [SerializeField] private Vector3 contactOffset = new Vector3(0f, 0f, 0.3f);

    private bool _active = false;
    private bool _isForehand = true;
    private float _currentWeight = 0f;
    private float _holdTimer = 0f;
    private bool _blendingOut = false;

    void Update()
    {
        if (_active && !_blendingOut)
        {
            _currentWeight = Mathf.MoveTowards(_currentWeight, 1f, blendInSpeed * Time.deltaTime);
            _holdTimer += Time.deltaTime;
            if (_holdTimer >= holdDuration) _blendingOut = true;
        }
        else if (_blendingOut)
        {
            _currentWeight = Mathf.MoveTowards(_currentWeight, 0f, blendOutSpeed * Time.deltaTime);
            if (_currentWeight <= 0f) { _active = false; _blendingOut = false; }
        }

        forehandIK.weight = _isForehand ? _currentWeight : 0f;
        backhandIK.weight = _isForehand ? 0f : _currentWeight;
    }

    public void TriggerIK(Vector3 ballWorldPosition, bool isForehand)
    {
        _isForehand = isForehand;
        _active = true;
        _blendingOut = false;
        _holdTimer = 0f;
        _currentWeight = 0f;

        Vector3 contactPoint = new Vector3(
            ballWorldPosition.x,
            ballWorldPosition.y,
            ballWorldPosition.z
        ) + transform.TransformDirection(contactOffset);

        if (isForehand)
            forehandTarget.position = contactPoint;
        else
            backhandTarget.position = contactPoint;
    }
}