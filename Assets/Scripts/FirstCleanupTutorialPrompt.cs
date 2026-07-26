using System.Collections.Generic;
using TMPro;
using UnityEngine;

/// <summary>
/// Shows the first-cleanup hint above the nearest reachable water-trash item.
/// It is only active at zero score and permanently ends after the first item
/// is collected.
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(ToolController))]
public sealed class FirstCleanupTutorialPrompt : MonoBehaviour
{
    [Header("Artwork")]
    [SerializeField] private Sprite leftClickSprite;
    [SerializeField] private TMP_FontAsset promptFont;

    [Header("Copy")]
    [SerializeField] private string promptText = "untuk hancurin sampah";

    [Header("Layout")]
    [SerializeField] private Vector3 localOffset = new(0f, 1.6f, 0f);
    [SerializeField, Min(0.1f)] private float iconWorldWidth = 0.72f;
    [SerializeField, Range(0.5f, 4f)] private float textWorldScale = 1.7f;
    [SerializeField, Range(0.5f, 6f)] private float textFontSize = 2.1f;
    [SerializeField, Min(0f)] private float iconTextGap = 0.16f;
    [SerializeField, Min(0)] private int sortingOrder = 50;

    [Header("Detection")]
    [SerializeField, Min(0.02f)] private float scanInterval = 0.12f;

    private readonly List<Material> textMaterials = new();
    private ToolController toolController;
    private BoatController boatController;
    private TreeCut currentTarget;
    private GameObject promptObject;
    private float nextScanTime;
    private bool tutorialComplete;

    private void Awake()
    {
        toolController = GetComponent<ToolController>();
        boatController = FindFirstObjectByType<BoatController>();
    }

    private void Update()
    {
        if (tutorialComplete)
        {
            return;
        }

        if (boatController == null || !boatController.HasBoardedBoat)
        {
            HidePrompt();
            return;
        }

        if (ScoreManager.Instance != null && ScoreManager.Instance.currentScore > 0)
        {
            tutorialComplete = true;
            HidePrompt();
            return;
        }

        if (Time.unscaledTime < nextScanTime)
        {
            return;
        }

        nextScanTime = Time.unscaledTime + scanInterval;
        TreeCut nearestTrash = FindNearestReachableTrash();
        if (nearestTrash == currentTarget)
        {
            return;
        }

        currentTarget = nearestTrash;
        if (currentTarget == null)
        {
            HidePrompt();
            return;
        }

        ShowPrompt(currentTarget.transform);
    }

    private void OnDestroy()
    {
        HidePrompt();
    }

    private TreeCut FindNearestReachableTrash()
    {
        if (toolController == null)
        {
            return null;
        }

        TreeCut closest = null;
        float closestDistance = float.MaxValue;
        foreach (TreeCut candidate in FindObjectsByType<TreeCut>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
        {
            if (!IsWaterTrash(candidate) || !toolController.IsWithinInteractionRange(candidate.transform))
            {
                continue;
            }

            float distance = ((Vector2)(candidate.transform.position - transform.position)).sqrMagnitude;
            if (distance < closestDistance)
            {
                closest = candidate;
                closestDistance = distance;
            }
        }

        return closest;
    }

    private static bool IsWaterTrash(TreeCut candidate)
    {
        if (candidate == null)
        {
            return false;
        }

        SpriteRenderer renderer = candidate.GetComponent<SpriteRenderer>();
        return renderer != null && renderer.sortingOrder <= 2;
    }

    private void ShowPrompt(Transform target)
    {
        HidePrompt();
        if (target == null || leftClickSprite == null)
        {
            return;
        }

        // The trash uses animated transforms. Keep the prompt independent at
        // the initial world position so the lettering never bobs or wiggles.
        promptObject = new GameObject("First Cleanup Tutorial");
        promptObject.transform.position = target.position + localOffset;

        GameObject iconObject = new("Left Click Icon", typeof(SpriteRenderer));
        iconObject.transform.SetParent(promptObject.transform, false);
        SpriteRenderer icon = iconObject.GetComponent<SpriteRenderer>();
        icon.sprite = leftClickSprite;
        icon.sortingOrder = sortingOrder;
        iconObject.transform.localScale = Vector3.one * (iconWorldWidth / Mathf.Max(leftClickSprite.bounds.size.x, 0.01f));

        GameObject textObject = new("Hint Text", typeof(RectTransform), typeof(TextMeshPro));
        textObject.transform.SetParent(promptObject.transform, false);
        textObject.transform.localScale = Vector3.one * textWorldScale;
        TextMeshPro text = textObject.GetComponent<TextMeshPro>();
        text.font = promptFont != null ? promptFont : TMP_Settings.defaultFontAsset;
        Material material = new(text.font.material) { name = "Cleanup Tutorial Text (Runtime)" };
        material.SetColor("_FaceColor", Color.white);
        material.SetFloat("_OutlineWidth", 0f);
        material.DisableKeyword("UNDERLAY_ON");
        text.fontMaterial = material;
        textMaterials.Add(material);
        text.text = promptText;
        text.fontSize = textFontSize;
        text.fontStyle = FontStyles.Bold;
        text.alignment = TextAlignmentOptions.Center;
        text.textWrappingMode = TextWrappingModes.NoWrap;
        text.overflowMode = TextOverflowModes.Overflow;
        text.color = Color.white;
        text.faceColor = Color.white;
        text.outlineWidth = 0f;
        text.rectTransform.sizeDelta = Vector2.zero;
        text.GetComponent<MeshRenderer>().sortingOrder = sortingOrder + 1;

        text.ForceMeshUpdate();
        float iconWidth = icon.bounds.size.x;
        float textWidth = text.GetRenderedValues(false).x * text.transform.localScale.x;
        float fullWidth = iconWidth + iconTextGap + textWidth;
        iconObject.transform.localPosition = new Vector3(-fullWidth * 0.5f + iconWidth * 0.5f, 0f, 0f);
        textObject.transform.localPosition = new Vector3(fullWidth * 0.5f - textWidth * 0.5f, 0f, -0.03f);
    }

    private void HidePrompt()
    {
        if (promptObject != null)
        {
            Destroy(promptObject);
            promptObject = null;
        }

        foreach (Material material in textMaterials)
        {
            if (material != null)
            {
                Destroy(material);
            }
        }

        textMaterials.Clear();
    }
}
