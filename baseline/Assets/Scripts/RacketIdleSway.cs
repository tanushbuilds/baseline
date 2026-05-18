using UnityEngine;

public class ArmIdleSway : MonoBehaviour
{
    [SerializeField] private float swayAmountX = 0.8f;
    [SerializeField] private float swayAmountZ = 0.5f;
    [SerializeField] private float noiseSpeedX = 0.3f;
    [SerializeField] private float noiseSpeedZ = 0.2f;

    private Transform spine2;
    private Quaternion spine2Initial;

    void Start()
    {
        spine2 = transform.FindChildRecursive("mixamorig12:Spine2");
        if (spine2 != null) spine2Initial = spine2.localRotation;
    }

    void LateUpdate()
    {
        if (spine2 == null) return;

        float x = Mathf.PerlinNoise(Time.time * noiseSpeedX, 0f) * 2f - 1f;
        float z = Mathf.PerlinNoise(0f, Time.time * noiseSpeedZ) * 2f - 1f;

        spine2.localRotation = spine2Initial * Quaternion.Euler(x * swayAmountX, 0f, z * swayAmountZ);
    }
}

public static class TransformExtensions
{
    public static Transform FindChildRecursive(this Transform parent, string name)
    {
        foreach (Transform child in parent.GetComponentsInChildren<Transform>())
            if (child.name == name) return child;
        return null;
    }
}