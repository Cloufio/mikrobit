using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class FinalScoreController : MonoBehaviour
{
    [Header("Run Preview")]
    [SerializeField] private int previewScore = 130;
    [SerializeField] private int previewBestScore = 140;

    [Header("Tire Insight")]
    [SerializeField] private string trashName = "CAR TIRE";
    [SerializeField, Min(0)] private int tiresCleaned = 20;
    [SerializeField, TextArea(3, 5)] private string tireFunFact =
        "Car tires slowly wear away every time they touch the road. Rain can carry those tiny particles through drains and into rivers and seas.";
    [SerializeField] private TMP_FontAsset boldPixelsFont;

    [Header("Text Style")]
    [SerializeField] private Color titleColor = new Color(1f, 0.82f, 0.40f, 1f);
    [SerializeField] private Color bodyColor = new Color(0.94f, 0.97f, 0.93f, 1f);
    [SerializeField] private Color outlineColor = new Color(0.24f, 0.12f, 0.13f, 1f);
    [SerializeField] private Color shadowColor = new Color(0.72f, 0.29f, 0.18f, 1f);

    private readonly List<Material> runtimeMaterials = new List<Material>();

    private void Start()
    {
        ApplyExistingLayoutContent(GetRunScore(), GetBestScore());
    }

    private void ApplyExistingLayoutContent(int runScore, int bestScoreValue)
    {
        TMP_Text congrats = FindText("CongratsText");
        TMP_Text currentScore = FindText("CurrentScoreText");
        TMP_Text bestScore = FindText("BestScoreText");
        TMP_Text mostPicked = FindText("MostPickedTrashText");
        TMP_Text funFact = FindText("FunFactText");
        Image trashImage = FindImage("TrashImage");

        ConfigureText(congrats, "CONGRATS", 48f, bodyColor, TextAlignmentOptions.Center);
        ConfigureText(currentScore, runScore.ToString(), 96f, titleColor, TextAlignmentOptions.Center);
        ConfigureText(bestScore, "BEST SCORE : " + bestScoreValue, 32f, bodyColor, TextAlignmentOptions.Center);
        ConfigureText(mostPicked, "MOST PICKED TRASH\n" + trashName + " - " + tiresCleaned + " PIECES", 32f, titleColor, TextAlignmentOptions.Center);
        ConfigureText(funFact, "FUN FACT\n" + tireFunFact + "\n\nTIRE CLEANUP +" + tiresCleaned, 24f, bodyColor, TextAlignmentOptions.TopLeft);

        if (funFact != null)
            funFact.enableWordWrapping = true;

        // This intentionally remains an empty image slot until a dedicated tire illustration is added.
        if (trashImage != null)
        {
            trashImage.sprite = null;
            trashImage.color = Color.white;
        }
    }

    private int GetRunScore()
    {
        return PlayerPrefs.HasKey("Microbit_LastScore")
            ? Mathf.Max(0, PlayerPrefs.GetInt("Microbit_LastScore"))
            : Mathf.Max(0, previewScore);
    }

    private int GetBestScore()
    {
        int runScore = GetRunScore();
        if (PlayerPrefs.HasKey("Microbit_BestScore"))
            return Mathf.Max(runScore, PlayerPrefs.GetInt("Microbit_BestScore"));

        return Mathf.Max(runScore, previewBestScore);
    }

    private void ConfigureText(TMP_Text label, string value, float fontSize, Color color, TextAlignmentOptions alignment)
    {
        if (label == null)
            return;

        label.text = value;
        label.fontSize = fontSize;
        label.enableAutoSizing = false;
        label.alignment = alignment;
        label.color = color;
        label.raycastTarget = false;
        label.overflowMode = TextOverflowModes.Overflow;

        if (boldPixelsFont == null)
            return;

        label.font = boldPixelsFont;
        Material material = new Material(label.fontSharedMaterial);
        runtimeMaterials.Add(material);
        label.fontMaterial = material;
        SetMaterialColor(material, "_FaceColor", color);
        SetMaterialColor(material, "_OutlineColor", outlineColor);
        SetMaterialColor(material, "_UnderlayColor", shadowColor);
        SetMaterialFloat(material, "_OutlineWidth", 0.12f);
        SetMaterialFloat(material, "_UnderlayOffsetX", 0.18f);
        SetMaterialFloat(material, "_UnderlayOffsetY", -0.18f);
        SetMaterialFloat(material, "_UnderlayDilate", 0.04f);
        SetMaterialFloat(material, "_UnderlaySoftness", 0f);
    }

    private TMP_Text FindText(string objectName)
    {
        Transform target = FindDeep(transform, objectName);
        return target != null ? target.GetComponent<TMP_Text>() : null;
    }

    private Image FindImage(string objectName)
    {
        Transform target = FindDeep(transform, objectName);
        return target != null ? target.GetComponent<Image>() : null;
    }

    private static Transform FindDeep(Transform root, string objectName)
    {
        if (root == null)
            return null;
        if (root.name == objectName)
            return root;

        for (int i = 0; i < root.childCount; i++)
        {
            Transform result = FindDeep(root.GetChild(i), objectName);
            if (result != null)
                return result;
        }

        return null;
    }

    private static void SetMaterialColor(Material material, string property, Color color)
    {
        if (material != null && material.HasProperty(property))
            material.SetColor(property, color);
    }

    private static void SetMaterialFloat(Material material, string property, float value)
    {
        if (material != null && material.HasProperty(property))
            material.SetFloat(property, value);
    }

    private void OnDestroy()
    {
        foreach (Material material in runtimeMaterials)
        {
            if (material != null)
                Destroy(material);
        }
        runtimeMaterials.Clear();
    }
}
