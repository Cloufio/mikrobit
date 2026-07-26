using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

/// <summary>
/// Shows a short world-space movement hint above the player after the opening camera pan.
/// The prompt is parented to the player, so it follows them until it fades away.
/// </summary>
[DisallowMultipleComponent]
public sealed class PlayerMovementTutorialPrompt : MonoBehaviour
{
    [Header("Artwork")]
    [SerializeField] private Sprite wasdKeysSprite;
    [SerializeField] private TMP_FontAsset promptFont;

    [Header("Copy")]
    [SerializeField] private string beforeKeysText = "Pencet";
    [SerializeField] private string afterKeysText = "untuk bergerak";

    [Header("Timing")]
    [SerializeField, Min(0.1f)] private float displayDuration = 3f;
    [SerializeField, Min(0.05f)] private float popDuration = 0.16f;
    [SerializeField, Min(0.05f)] private float fadeDuration = 0.55f;

    [Header("World Layout")]
    [SerializeField] private Vector3 localOffset = new(0f, 1.5f, 0f);
    [SerializeField, Min(0.1f)] private float keyGraphicWidth = 1.7f;
    [SerializeField, Range(1f, 6f)] private float textWorldScale = 3.2f;
    [SerializeField, Range(1f, 6f)] private float textFontSize = 3.4f;
    [SerializeField, Min(0)] private int sortingOrder = 50;

    private readonly List<SpriteRenderer> spriteRenderers = new();
    private readonly List<TextMeshPro> textMeshes = new();
    private readonly List<Material> textMaterials = new();
    private GameObject promptObject;
    private bool hasShown;

    public void Show()
    {
        if (hasShown || wasdKeysSprite == null)
        {
            return;
        }

        hasShown = true;
        StartCoroutine(ShowSequence());
    }

    private void OnDestroy()
    {
        DisposeTextMaterials();
    }

    private IEnumerator ShowSequence()
    {
        promptObject = new GameObject("WASD Movement Tutorial");
        promptObject.transform.SetParent(transform, false);
        promptObject.transform.localPosition = localOffset;
        promptObject.transform.localScale = Vector3.one;

        TextMeshPro beforeKeys = CreateText("Before Keys", beforeKeysText);
        SpriteRenderer keyGraphic = CreateKeyGraphic();
        TextMeshPro afterKeys = CreateText("After Keys", afterKeysText);
        LayoutPromptRow(beforeKeys, keyGraphic, afterKeys);
        promptObject.transform.localScale = Vector3.zero;

        float elapsed = 0f;
        while (elapsed < popDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float progress = Mathf.Clamp01(elapsed / popDuration);
            float eased = 1f - Mathf.Pow(1f - progress, 3f);
            promptObject.transform.localScale = Vector3.one * eased;
            yield return null;
        }

        promptObject.transform.localScale = Vector3.one;
        yield return new WaitForSecondsRealtime(Mathf.Max(0f, displayDuration - fadeDuration));

        elapsed = 0f;
        while (elapsed < fadeDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            SetAlpha(1f - Mathf.Clamp01(elapsed / fadeDuration));
            yield return null;
        }

        Destroy(promptObject);
        promptObject = null;
        DisposeTextMaterials();
    }

    private SpriteRenderer CreateKeyGraphic()
    {
        GameObject keyGraphic = new GameObject("WASD Keys", typeof(SpriteRenderer));
        keyGraphic.transform.SetParent(promptObject.transform, false);

        float spriteWidth = Mathf.Max(wasdKeysSprite.bounds.size.x, 0.01f);
        keyGraphic.transform.localScale = Vector3.one * (keyGraphicWidth / spriteWidth);

        SpriteRenderer renderer = keyGraphic.GetComponent<SpriteRenderer>();
        renderer.sprite = wasdKeysSprite;
        renderer.sortingOrder = sortingOrder;
        spriteRenderers.Add(renderer);
        return renderer;
    }

    private TextMeshPro CreateText(string objectName, string value)
    {
        GameObject textObject = new GameObject(objectName, typeof(RectTransform), typeof(TextMeshPro));
        textObject.transform.SetParent(promptObject.transform, false);
        textObject.transform.localScale = Vector3.one * textWorldScale;

        TextMeshPro text = textObject.GetComponent<TextMeshPro>();
        text.font = promptFont != null ? promptFont : TMP_Settings.defaultFontAsset;
        Material promptMaterial = new Material(text.font.material)
        {
            name = "Movement Tutorial Text (Runtime)"
        };
        promptMaterial.SetColor("_FaceColor", Color.white);
        promptMaterial.SetFloat("_OutlineWidth", 0f);
        promptMaterial.DisableKeyword("UNDERLAY_ON");
        text.fontMaterial = promptMaterial;
        textMaterials.Add(promptMaterial);
        text.text = value;
        text.fontSize = textFontSize;
        text.fontStyle = FontStyles.Bold;
        text.alignment = TextAlignmentOptions.Center;
        text.textWrappingMode = TextWrappingModes.NoWrap;
        text.overflowMode = TextOverflowModes.Overflow;
        text.color = Color.white;
        text.faceColor = Color.white;
        text.outlineWidth = 0f;
        text.rectTransform.sizeDelta = Vector2.zero;
        MeshRenderer renderer = text.GetComponent<MeshRenderer>();
        renderer.sortingOrder = sortingOrder + 1;
        textMeshes.Add(text);
        return text;
    }

    private void LayoutPromptRow(TextMeshPro beforeKeys, SpriteRenderer keyGraphic, TextMeshPro afterKeys)
    {
        const float labelGap = 0.22f;

        beforeKeys.ForceMeshUpdate();
        afterKeys.ForceMeshUpdate();

        float beforeWidth = beforeKeys.GetRenderedValues(false).x * beforeKeys.transform.localScale.x;
        float keyWidth = keyGraphic.bounds.size.x;
        float afterWidth = afterKeys.GetRenderedValues(false).x * afterKeys.transform.localScale.x;
        float totalWidth = beforeWidth + keyWidth + afterWidth + labelGap * 2f;
        float cursor = -totalWidth * 0.5f;

        beforeKeys.transform.localPosition = new Vector3(cursor + beforeWidth * 0.5f, 0f, -0.03f);
        cursor += beforeWidth + labelGap;

        keyGraphic.transform.localPosition = new Vector3(cursor + keyWidth * 0.5f, 0f, 0f);
        cursor += keyWidth + labelGap;

        afterKeys.transform.localPosition = new Vector3(cursor + afterWidth * 0.5f, 0f, -0.03f);
    }

    private void DisposeTextMaterials()
    {
        foreach (Material material in textMaterials)
        {
            if (material != null)
            {
                Destroy(material);
            }
        }

        textMaterials.Clear();
    }

    private void SetAlpha(float alpha)
    {
        foreach (SpriteRenderer renderer in spriteRenderers)
        {
            if (renderer != null)
            {
                Color color = renderer.color;
                color.a = alpha;
                renderer.color = color;
            }
        }

        foreach (TextMeshPro text in textMeshes)
        {
            if (text != null)
            {
                Color color = text.color;
                color.a = alpha;
                text.color = color;
            }
        }
    }
}
