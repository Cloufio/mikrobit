using System.Collections;
using UnityEngine;

[DefaultExecutionOrder(-1000)]
public class SceneOpeningCamera : MonoBehaviour
{
    [Header("Focus Targets")]
    [SerializeField] private Transform boatFocus;
    [SerializeField] private Transform playerFocus;

    [Header("Timing")]
    [SerializeField, Min(0f)] private float fadeInDelay = 1.5f;
    [SerializeField, Min(0f)] private float boatFocusDuration = 2f;
    [SerializeField, Min(0.01f)] private float panDuration = 1f;
    [SerializeField] private AnimationCurve panCurve = null;

    [Header("Framing")]
    [SerializeField] private Vector3 focusOffset = new Vector3(0f, 0f, -10f);

    private Camera sceneCamera;
    private Behaviour cinemachineBrain;

    private void Awake()
    {
        sceneCamera = GetComponent<Camera>();
        cinemachineBrain = FindCinemachineBrain();

        if (sceneCamera == null || boatFocus == null || playerFocus == null)
        {
            Debug.LogWarning("SceneOpeningCamera needs a Camera, boat focus, and player focus.", this);
            enabled = false;
            return;
        }

        // Pause Cinemachine only for the opening shot. It resumes once the pan
        // reaches the player, returning control to the existing follow camera.
        if (cinemachineBrain != null)
        {
            cinemachineBrain.enabled = false;
        }

        sceneCamera.transform.position = GetFocusPosition(boatFocus);
    }

    private IEnumerator Start()
    {
        // The scene already has a fade-in. Wait for it before the visible boat hold.
        yield return new WaitForSecondsRealtime(fadeInDelay + boatFocusDuration);

        Vector3 startPosition = sceneCamera.transform.position;
        Vector3 endPosition = GetFocusPosition(playerFocus);
        float elapsed = 0f;

        while (elapsed < panDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float progress = Mathf.Clamp01(elapsed / panDuration);
            float easedProgress = panCurve != null && panCurve.length > 0
                ? panCurve.Evaluate(progress)
                : Mathf.SmoothStep(0f, 1f, progress);

            sceneCamera.transform.position = Vector3.LerpUnclamped(startPosition, endPosition, easedProgress);
            yield return null;
        }

        sceneCamera.transform.position = endPosition;

        if (cinemachineBrain != null)
        {
            cinemachineBrain.enabled = true;
        }
    }

    private Vector3 GetFocusPosition(Transform focusTarget)
    {
        return focusTarget.position + focusOffset;
    }

    private Behaviour FindCinemachineBrain()
    {
        foreach (Behaviour behaviour in GetComponents<Behaviour>())
        {
            if (behaviour.GetType().Name == "CinemachineBrain")
            {
                return behaviour;
            }
        }

        return null;
    }

    private void OnDisable()
    {
        if (cinemachineBrain != null)
        {
            cinemachineBrain.enabled = true;
        }
    }
}
