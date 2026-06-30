using UnityEngine;

public class TreeSway : MonoBehaviour
{
    public float swaySpeed = 1f;
    public float maxSwayAngle = 5f;
    
    private Quaternion startRotation;
    private float randomOffset;

    void Start()
    {
        startRotation = transform.rotation;
        randomOffset = Random.Range(0f, 100f);
    }

    void Update()
    {
        float angle = maxSwayAngle * Mathf.Sin(Time.time * swaySpeed + randomOffset);
        transform.rotation = startRotation * Quaternion.Euler(0, 0, angle);
    }
}