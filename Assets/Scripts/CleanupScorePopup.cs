using UnityEngine;
using UnityEngine.UI;

public class CleanupScorePopup : MonoBehaviour
{
    private const float Lifetime = 0.7f;

    private Text popupText;
    private float elapsed;

    public static void Spawn(Vector2 position, int score)
    {
        if (score <= 0 || ScoreManager.Instance == null || ScoreManager.Instance.scoreTextElement == null)
        {
            return;
        }

        Font font = ScoreManager.Instance.scoreTextElement.font;
        if (font == null)
        {
            return;
        }

        GameObject popupObject = new GameObject("Cleanup Score Popup", typeof(RectTransform), typeof(Canvas));
        popupObject.transform.position = position + Vector2.up * 0.55f;
        popupObject.transform.localScale = Vector3.one * 0.01f;

        Canvas canvas = popupObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        canvas.worldCamera = Camera.main;
        canvas.overrideSorting = true;
        canvas.sortingOrder = 50;

        RectTransform rectTransform = popupObject.GetComponent<RectTransform>();
        rectTransform.sizeDelta = new Vector2(240f, 80f);

        Text text = popupObject.AddComponent<Text>();
        text.font = font;
        text.text = $"+{score}";
        text.fontSize = 60;
        text.alignment = TextAnchor.MiddleCenter;
        text.horizontalOverflow = HorizontalWrapMode.Overflow;
        text.verticalOverflow = VerticalWrapMode.Overflow;
        text.color = new Color(1f, 0.88f, 0.6f, 1f);

        Outline outline = popupObject.AddComponent<Outline>();
        outline.effectColor = new Color(0.35f, 0.19f, 0.2f, 1f);
        outline.effectDistance = new Vector2(2f, -2f);

        CleanupScorePopup popup = popupObject.AddComponent<CleanupScorePopup>();
        popup.popupText = text;
    }

    private void Update()
    {
        elapsed += Time.unscaledDeltaTime;
        float progress = Mathf.Clamp01(elapsed / Lifetime);
        transform.position += Vector3.up * 0.7f * Time.unscaledDeltaTime;

        if (popupText != null)
        {
            Color color = popupText.color;
            color.a = 1f - progress;
            popupText.color = color;
        }

        if (progress >= 1f)
        {
            Destroy(gameObject);
        }
    }
}
