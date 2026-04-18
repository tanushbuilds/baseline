using UnityEngine;

public class ForehandMaskBlend : MonoBehaviour
{
    [Header("Layer Indices (match your Animator)")]
    [SerializeField] private int forehandFullBodyLayer = 1;
    [SerializeField] private int forehandUpperBodyLayer = 2;

    [Header("Blend Speed")]
    [SerializeField] private float blendSpeed = 8f;

    private Animator _anim;
    private float _upperBodyWeight = 0f;

    void Awake()
    {
        _anim = GetComponent<Animator>();
        _anim.SetLayerWeight(forehandFullBodyLayer, 0f);
        _anim.SetLayerWeight(forehandUpperBodyLayer, 0f);
    }

    void Update()
    {
        bool isMoving = _anim.GetBool("isRunning");
        bool isTakeback = _anim.GetBool("ForehandTakeback");

        var stateInfo = _anim.GetCurrentAnimatorStateInfo(forehandUpperBodyLayer);
        Debug.Log($"upperLayerWeight:{_anim.GetLayerWeight(forehandUpperBodyLayer)} " +
                  $"stateHash:{stateInfo.shortNameHash} " +
                  $"normalizedTime:{stateInfo.normalizedTime} " +
                  $"clipName:{stateInfo.IsName("ForehandTakeBack")}");

        if (!isTakeback)
        {
            _upperBodyWeight = Mathf.Lerp(_upperBodyWeight, 0f, Time.deltaTime * blendSpeed);
            _anim.SetLayerWeight(forehandFullBodyLayer, 0f);
            _anim.SetLayerWeight(forehandUpperBodyLayer, 0f);
            return;
        }

        float target = isMoving ? 1f : 0f;
        _upperBodyWeight = Mathf.Lerp(_upperBodyWeight, target, Time.deltaTime * blendSpeed);

        _anim.SetLayerWeight(forehandFullBodyLayer, 1f - _upperBodyWeight);
        _anim.SetLayerWeight(forehandUpperBodyLayer, _upperBodyWeight);
    }
}