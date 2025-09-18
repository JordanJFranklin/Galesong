using UnityEngine;

public class CameraShake : MonoBehaviour
{
    public static CameraShake Instance;

    private Vector3 shakeOffset = Vector3.zero;
    private float shakeDuration = 0f;
    private float shakeMagnitude = 0.1f;

    void Awake()
    {
        if (Instance == null)
            Instance = this;
    }

    void LateUpdate()
    {
        if (shakeDuration > 0)
        {
            shakeOffset = Random.insideUnitSphere * shakeMagnitude;
            shakeDuration -= Time.deltaTime;
        }
        else
        {
            shakeOffset = Vector3.zero;
        }
    }

    public void Shake(float duration, float magnitude)
    {
        shakeDuration = duration;
        shakeMagnitude = magnitude;
    }

    public Vector3 GetShakeOffset()
    {
        return shakeOffset;
    }
}
