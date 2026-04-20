using UnityEngine;

public class ForehandMaskBlend : MonoBehaviour
{
    [Header("Layer Indices")]
    [SerializeField] private int runFullBodyLayer = 1;      // no mask
    [SerializeField] private int runLowerBodyLayer = 2;     // lower body mask
    [SerializeField] private int forehandFullBodyLayer = 3; // no mask
    [SerializeField] private int forehandUpperBodyLayer = 4;// upper body mask

    [Header("Blend Speed")]
    [SerializeField] private float blendSpeed = 8f;

    private Animator _anim;
    private float _upperBodyWeight = 0f;
    private float _runLowerWeight = 0f;
    private float _forehandFullWeight = 0f;

    void Awake()
    {
        _anim = GetComponent<Animator>();
        _anim.SetLayerWeight(runFullBodyLayer, 1f);
        _anim.SetLayerWeight(runLowerBodyLayer, 0f);
        _anim.SetLayerWeight(forehandFullBodyLayer, 0f);
        _anim.SetLayerWeight(forehandUpperBodyLayer, 0f);
    }

    void Update()
    {
        bool isMoving = _anim.GetBool("isRunning");
        bool isTakeback = _anim.GetBool("ForehandTakeback");

        if (!isTakeback)
        {
            // Full body run, no forehand
            _runLowerWeight = Mathf.Lerp(_runLowerWeight, 0f, Time.deltaTime * blendSpeed);
            _upperBodyWeight = Mathf.Lerp(_upperBodyWeight, 0f, Time.deltaTime * blendSpeed);
            _forehandFullWeight = Mathf.Lerp(_forehandFullWeight, 0f, Time.deltaTime * blendSpeed);

            _anim.SetLayerWeight(runFullBodyLayer, 1f);
            _anim.SetLayerWeight(runLowerBodyLayer, _runLowerWeight);
            _anim.SetLayerWeight(forehandFullBodyLayer, _forehandFullWeight);
            _anim.SetLayerWeight(forehandUpperBodyLayer, _upperBodyWeight);
            return;
        }

        if (isMoving)
        {
            // Run (lower only) + forehand upper body
            _runLowerWeight = Mathf.Lerp(_runLowerWeight, 1f, Time.deltaTime * blendSpeed);
            _upperBodyWeight = Mathf.Lerp(_upperBodyWeight, 1f, Time.deltaTime * blendSpeed);
            _forehandFullWeight = Mathf.Lerp(_forehandFullWeight, 0f, Time.deltaTime * blendSpeed);

            _anim.SetLayerWeight(runFullBodyLayer, 0f);
            _anim.SetLayerWeight(runLowerBodyLayer, _runLowerWeight);
            _anim.SetLayerWeight(forehandFullBodyLayer, _forehandFullWeight);
            _anim.SetLayerWeight(forehandUpperBodyLayer, _upperBodyWeight);
        }
        else
        {
            // Standing takeback — full forehand, no run
            _runLowerWeight = Mathf.Lerp(_runLowerWeight, 0f, Time.deltaTime * blendSpeed);
            _upperBodyWeight = Mathf.Lerp(_upperBodyWeight, 0f, Time.deltaTime * blendSpeed);
            _forehandFullWeight = Mathf.Lerp(_forehandFullWeight, 1f, Time.deltaTime * blendSpeed);

            _anim.SetLayerWeight(runFullBodyLayer, 0f);
            _anim.SetLayerWeight(runLowerBodyLayer, _runLowerWeight);
            _anim.SetLayerWeight(forehandFullBodyLayer, _forehandFullWeight);
            _anim.SetLayerWeight(forehandUpperBodyLayer, _upperBodyWeight);
        }
    }
}