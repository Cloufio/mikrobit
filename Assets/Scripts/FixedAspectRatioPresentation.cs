using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Keeps screen-space UI and gameplay cameras in a 16:9 frame. Wider or taller
/// displays use letterboxing instead of stretching the pixel-art presentation.
/// </summary>
[DefaultExecutionOrder(-10000)]
public sealed class FixedAspectRatioPresentation : MonoBehaviour
{
    public const float TargetAspectRatio = 16f / 9f;

    private static FixedAspectRatioPresentation instance;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Install()
    {
        if (instance != null)
        {
            return;
        }

        GameObject bootstrap = new("16:9 Presentation");
        instance = bootstrap.AddComponent<FixedAspectRatioPresentation>();
        DontDestroyOnLoad(bootstrap);
    }

    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void Start()
    {
        ApplyPresentation();
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        StartCoroutine(ApplyOnNextFrame());
    }

    private IEnumerator ApplyOnNextFrame()
    {
        // Existing scene Awake calls may create UI. Waiting one frame places
        // those runtime controls in the 16:9 frame too.
        yield return null;
        ApplyPresentation();
    }

    private static void ApplyPresentation()
    {
        foreach (Camera camera in Camera.allCameras)
        {
            if (camera != null && camera.cameraType == CameraType.Game && camera.targetTexture == null &&
                camera.GetComponent<FixedAspectRatioCamera>() == null)
            {
                camera.gameObject.AddComponent<FixedAspectRatioCamera>();
            }
        }

        foreach (Canvas canvas in Object.FindObjectsByType<Canvas>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
        {
            if (canvas.isRootCanvas && canvas.renderMode != RenderMode.WorldSpace)
            {
                FixedAspectRatioCanvas.Ensure(canvas);
            }
        }
    }
}

[DisallowMultipleComponent]
public sealed class FixedAspectRatioCanvas : MonoBehaviour
{
    private const string LegacyViewportName = "16:9 Viewport";

    public static Transform GetContentRoot(Canvas canvas)
    {
        Ensure(canvas);
        return canvas.transform;
    }

    public static FixedAspectRatioCanvas Ensure(Canvas canvas)
    {
        FixedAspectRatioCanvas fitter = canvas.GetComponent<FixedAspectRatioCanvas>();
        if (fitter == null)
        {
            fitter = canvas.gameObject.AddComponent<FixedAspectRatioCanvas>();
        }

        fitter.ConfigureCanvas();
        return fitter;
    }

    private void Awake()
    {
        ConfigureCanvas();
    }

    private void ConfigureCanvas()
    {
        Canvas canvas = GetComponent<Canvas>();
        if (canvas == null || canvas.renderMode == RenderMode.WorldSpace)
        {
            return;
        }

        RestoreLegacyViewportIfNeeded();

        Camera uiCamera = FindGameCamera();
        if (uiCamera == null)
        {
            return;
        }

        // Screen Space - Camera uses the camera's letterboxed pixel rect. This
        // keeps the existing Canvas hierarchy and anchored layout intact.
        canvas.renderMode = RenderMode.ScreenSpaceCamera;
        canvas.worldCamera = uiCamera;
        canvas.planeDistance = Mathf.Max(uiCamera.nearClipPlane + 1f, 10f);
    }

    private void RestoreLegacyViewportIfNeeded()
    {
        Transform legacyViewport = transform.Find(LegacyViewportName);
        if (legacyViewport == null)
        {
            return;
        }

        // Earlier versions nested every direct Canvas child in this object.
        // Move them back before removing it so an already-running scene repairs
        // itself after recompiling this script.
        Transform[] children = new Transform[legacyViewport.childCount];
        for (int index = 0; index < legacyViewport.childCount; index++)
        {
            children[index] = legacyViewport.GetChild(index);
        }

        foreach (Transform child in children)
        {
            child.SetParent(transform, false);
        }

        Destroy(legacyViewport.gameObject);
    }

    private static Camera FindGameCamera()
    {
        if (Camera.main != null && Camera.main.cameraType == CameraType.Game)
        {
            return Camera.main;
        }

        foreach (Camera candidate in Camera.allCameras)
        {
            if (candidate != null && candidate.cameraType == CameraType.Game && candidate.targetTexture == null)
            {
                return candidate;
            }
        }

        return null;
    }
}

[DisallowMultipleComponent]
public sealed class FixedAspectRatioCamera : MonoBehaviour
{
    [SerializeField] private float targetAspectRatio = FixedAspectRatioPresentation.TargetAspectRatio;

    private Camera cachedCamera;
    private int lastWidth;
    private int lastHeight;

    private void Awake()
    {
        cachedCamera = GetComponent<Camera>();
        ApplyViewport();
    }

    private void LateUpdate()
    {
        if (Screen.width != lastWidth || Screen.height != lastHeight)
        {
            ApplyViewport();
        }
    }

    private void ApplyViewport()
    {
        if (cachedCamera == null || Screen.height <= 0)
        {
            return;
        }

        lastWidth = Screen.width;
        lastHeight = Screen.height;

        float screenAspect = Screen.width / (float)Screen.height;
        Rect viewport = new(0f, 0f, 1f, 1f);

        if (screenAspect > targetAspectRatio)
        {
            viewport.width = targetAspectRatio / screenAspect;
            viewport.x = (1f - viewport.width) * 0.5f;
        }
        else if (screenAspect < targetAspectRatio)
        {
            viewport.height = screenAspect / targetAspectRatio;
            viewport.y = (1f - viewport.height) * 0.5f;
        }

        cachedCamera.rect = viewport;
    }
}
