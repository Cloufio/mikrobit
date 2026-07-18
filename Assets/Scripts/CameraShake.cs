using UnityEngine;

[DefaultExecutionOrder(10000)]
public class CameraShake : MonoBehaviour
{
    private float remainingDuration;
    private float totalDuration;
    private float strength;
    private Vector3 appliedOffset;

    public void Shake(float duration, float amount)
    {
        totalDuration = Mathf.Max(totalDuration, duration);
        remainingDuration = Mathf.Max(remainingDuration, duration);
        strength = Mathf.Max(strength, amount);
    }

    private void LateUpdate()
    {
        transform.localPosition -= appliedOffset;
        appliedOffset = Vector3.zero;

        if (remainingDuration <= 0f)
        {
            return;
        }

        float fade = remainingDuration / totalDuration;
        Vector2 offset = Random.insideUnitCircle * strength * fade;
        appliedOffset = new Vector3(offset.x, offset.y, 0f);
        transform.localPosition += appliedOffset;
        remainingDuration -= Time.unscaledDeltaTime;

        if (remainingDuration <= 0f)
        {
            strength = 0f;
            totalDuration = 0f;
        }
    }

    private void OnDisable()
    {
        transform.localPosition -= appliedOffset;
        appliedOffset = Vector3.zero;
    }
}
