using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections.Generic;

public class UIStylingHelper : MonoBehaviour
{
    private static UIStylingHelper instance;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void OnSceneLoaded()
    {
        if (instance == null)
        {
            GameObject go = new GameObject("UIStylingHelper");
            instance = go.AddComponent<UIStylingHelper>();
            DontDestroyOnLoad(go);
        }
        
        instance.StartCoroutine(instance.ApplyStylingDelayed());
    }

    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoadedHandler;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoadedHandler;
    }

    private void OnSceneLoadedHandler(Scene scene, LoadSceneMode mode)
    {
        StartCoroutine(ApplyStylingDelayed());
    }

    private System.Collections.IEnumerator ApplyStylingDelayed()
    {
        yield return new WaitForEndOfFrame();
        ApplyStylingForCurrentScene();
    }

    public void ApplyStylingForCurrentScene()
    {
        string sceneName = SceneManager.GetActiveScene().name;
        Debug.Log($"UIStylingHelper: Applying styles for scene {sceneName}");

        // 1. Always apply button styles to any Buttons in the active scene
        ApplyButtonStyles();

        // 2. Scene-specific styling
        if (sceneName == "MainScene2")
        {
            ApplyTimerSprites();
            ApplyHealthBarStyles();
            CreateGameManual();
        }
    }

    void ApplyButtonStyles()
    {
        var buttonsSprites = Resources.LoadAll<Sprite>("Buttons");
        if (buttonsSprites == null || buttonsSprites.Length == 0)
        {
            Debug.LogWarning("UIStylingHelper: Could not load Buttons sprite sheet from Resources.");
            return;
        }

        Sprite normalBtn = System.Array.Find(buttonsSprites, s => s.name == "Buttons_0");
        Sprite hoverBtn = System.Array.Find(buttonsSprites, s => s.name == "Buttons_3");

        if (normalBtn == null || hoverBtn == null) return;

        var allButtons = FindObjectsOfType<Button>();
        foreach (var btn in allButtons)
        {
            // Skip the close button of our manual to avoid double styling
            if (btn.gameObject.name == "ManualCloseButton") continue;

            var img = btn.GetComponent<Image>();
            if (img != null)
            {
                img.sprite = normalBtn;
                img.type = Image.Type.Sliced;

                btn.transition = Selectable.Transition.SpriteSwap;
                var spriteState = btn.spriteState;
                spriteState.highlightedSprite = hoverBtn;
                spriteState.pressedSprite = hoverBtn;
                btn.spriteState = spriteState;
            }
        }
    }

    void ApplyTimerSprites()
    {
        if (ScoreManager.Instance == null) return;

        var timerSprites = Resources.LoadAll<Sprite>("Timer");
        if (timerSprites == null || timerSprites.Length == 0) return;

        var green = new List<Sprite>();
        var yellow = new List<Sprite>();
        var red = new List<Sprite>();

        foreach (var sprite in timerSprites)
        {
            string name = sprite.name;
            int num;
            if (int.TryParse(name.Substring(name.LastIndexOf('_') + 1), out num))
            {
                if (num <= 12) green.Add(sprite);
                else if (num <= 25) yellow.Add(sprite);
                else red.Add(sprite);
            }
        }

        green.Sort((a, b) => GetSpriteNum(a.name).CompareTo(GetSpriteNum(b.name)));
        yellow.Sort((a, b) => GetSpriteNum(a.name).CompareTo(GetSpriteNum(b.name)));
        red.Sort((a, b) => GetSpriteNum(a.name).CompareTo(GetSpriteNum(b.name)));

        ScoreManager.Instance.timerGreenFrames = green.ToArray();
        ScoreManager.Instance.timerYellowFrames = yellow.ToArray();
        ScoreManager.Instance.timerRedFrames = red.ToArray();

        // Style the static Timer Icon if it exists
        if (ScoreManager.Instance.timerIconElement != null)
        {
            ScoreManager.Instance.timerIconElement.sprite = System.Array.Find(timerSprites, s => s.name == "Timer_0");
        }
    }

    int GetSpriteNum(string name)
    {
        int lastUnder = name.LastIndexOf('_');
        if (lastUnder >= 0 && int.TryParse(name.Substring(lastUnder + 1), out int num))
            return num;
        return 0;
    }

    void ApplyHealthBarStyles()
    {
        var playerHealth = FindObjectOfType<PlayerHealth>();
        if (playerHealth == null || playerHealth.hudHealthBar == null) return;

        var hudHealthBar = playerHealth.hudHealthBar;

        var healthSprites = Resources.LoadAll<Sprite>("Health bar");
        if (healthSprites == null || healthSprites.Length == 0) return;

        Sprite frameSprite = System.Array.Find(healthSprites, s => s.name == "Health bar_16");
        Sprite fillSprite = System.Array.Find(healthSprites, s => s.name == "Health bar_18");

        if (fillSprite == null) fillSprite = System.Array.Find(healthSprites, s => s.name == "Health bar_0");
        if (frameSprite == null) frameSprite = System.Array.Find(healthSprites, s => s.name == "Health bar_1");

        hudHealthBar.sprite = fillSprite;
        hudHealthBar.type = Image.Type.Filled;
        hudHealthBar.fillMethod = Image.FillMethod.Horizontal;

        var parent = hudHealthBar.transform.parent;
        if (parent != null && parent.Find("HealthBarFrame") == null)
        {
            GameObject frameGO = new GameObject("HealthBarFrame", typeof(RectTransform), typeof(Image));
            frameGO.transform.SetParent(parent, false);
            frameGO.transform.SetSiblingIndex(hudHealthBar.transform.GetSiblingIndex());

            var frameImg = frameGO.GetComponent<Image>();
            frameImg.sprite = frameSprite;
            frameImg.type = Image.Type.Sliced;

            var frameRect = frameGO.GetComponent<RectTransform>();
            var fillRect = hudHealthBar.GetComponent<RectTransform>();

            frameRect.anchorMin = fillRect.anchorMin;
            frameRect.anchorMax = fillRect.anchorMax;
            frameRect.anchoredPosition = fillRect.anchoredPosition;
            frameRect.sizeDelta = fillRect.sizeDelta + new Vector2(16, 16);
            frameRect.pivot = fillRect.pivot;
        }
    }

    void CreateGameManual()
    {
        // Avoid duplicate manual popups
        if (GameObject.Find("GameManualPanel") != null) return;

        Canvas canvas = FindObjectOfType<Canvas>();
        if (canvas == null)
        {
            Debug.LogError("UIStylingHelper: No Canvas found in MainScene2 to spawn the game manual!");
            return;
        }

        // Disable player movement temporarily while reading
        var playerMovement = FindObjectOfType<NewMonoBehaviourScript>();
        if (playerMovement != null)
        {
            playerMovement.enabled = false;
        }

        var buttonsSprites = Resources.LoadAll<Sprite>("Buttons");
        Sprite panelBg = System.Array.Find(buttonsSprites, s => s.name == "Buttons_12"); // Decorated panel border
        Sprite btnNormal = System.Array.Find(buttonsSprites, s => s.name == "Buttons_0");
        Sprite btnHover = System.Array.Find(buttonsSprites, s => s.name == "Buttons_3");

        if (panelBg == null) panelBg = btnNormal;

        // 1. Create Panel Container GameObject
        GameObject panelGO = new GameObject("GameManualPanel", typeof(RectTransform), typeof(Image));
        panelGO.transform.SetParent(canvas.transform, false);

        var rectTransform = panelGO.GetComponent<RectTransform>();
        rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
        rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
        rectTransform.pivot = new Vector2(0.5f, 0.5f);
        rectTransform.sizeDelta = new Vector2(460, 320);
        rectTransform.anchoredPosition = Vector2.zero;

        var img = panelGO.GetComponent<Image>();
        img.sprite = panelBg;
        img.type = Image.Type.Sliced;

        // 2. Title Text
        GameObject titleGO = new GameObject("TitleText", typeof(RectTransform), typeof(Text));
        titleGO.transform.SetParent(panelGO.transform, false);

        var titleRect = titleGO.GetComponent<RectTransform>();
        titleRect.anchorMin = new Vector2(0, 1);
        titleRect.anchorMax = new Vector2(1, 1);
        titleRect.pivot = new Vector2(0.5f, 1);
        titleRect.sizeDelta = new Vector2(-40, 40);
        titleRect.anchoredPosition = new Vector2(0, -25);

        var titleText = titleGO.GetComponent<Text>();
        titleText.text = "PANDUAN BERMAIN";
        titleText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        titleText.fontSize = 24;
        titleText.fontStyle = FontStyle.Bold;
        titleText.alignment = TextAnchor.MiddleCenter;
        titleText.color = new Color(0.25f, 0.15f, 0.05f); // Cozy Dark Brown

        // 3. Description/Body Text
        GameObject bodyGO = new GameObject("BodyText", typeof(RectTransform), typeof(Text));
        bodyGO.transform.SetParent(panelGO.transform, false);

        var bodyRect = bodyGO.GetComponent<RectTransform>();
        bodyRect.anchorMin = new Vector2(0, 0);
        bodyRect.anchorMax = new Vector2(1, 1);
        bodyRect.pivot = new Vector2(0.5f, 0.5f);
        bodyRect.sizeDelta = new Vector2(-50, -110);
        bodyRect.anchoredPosition = new Vector2(0, -10);

        var bodyText = bodyGO.GetComponent<Text>();
        bodyText.text = "Selamat datang! Ayahmu mewariskan tugas mulia untuk menyelamatkan laut ini.\n\n" +
                        "• <b>Misi</b>: Kumpulkan sampah plastik & limbah sebanyak-banyaknya di laut.\n" +
                        "• <b>Kontrol</b>: Gunakan tombol <b>W, A, S, D / Arah Panah</b> untuk bergerak.\n" +
                        "• <b>Kapal</b>: Dekati kapal dan tekan tombol <b>E</b> untuk naik/mengemudi.\n" +
                        "• <b>Waktu</b>: Waktu 1 Menit baru dimulai <b>setelah Anda naik ke kapal</b>!\n" +
                        "• <b>Rintangan</b>: Hindari menabrak rintangan laut agar kapal tidak rusak (darah habis).";
        bodyText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        bodyText.fontSize = 13;
        bodyText.alignment = TextAnchor.UpperLeft;
        bodyText.supportRichText = true;
        bodyText.lineSpacing = 1.2f;
        bodyText.color = new Color(0.3f, 0.2f, 0.1f); // Cozy Brown

        // 4. Button
        GameObject buttonGO = new GameObject("ManualCloseButton", typeof(RectTransform), typeof(Image), typeof(Button));
        buttonGO.transform.SetParent(panelGO.transform, false);

        var buttonRect = buttonGO.GetComponent<RectTransform>();
        buttonRect.anchorMin = new Vector2(0.5f, 0);
        buttonRect.anchorMax = new Vector2(0.5f, 0);
        buttonRect.pivot = new Vector2(0.5f, 0);
        buttonRect.sizeDelta = new Vector2(160, 40);
        buttonRect.anchoredPosition = new Vector2(0, 20);

        var buttonImg = buttonGO.GetComponent<Image>();
        buttonImg.sprite = btnNormal;
        buttonImg.type = Image.Type.Sliced;

        var button = buttonGO.GetComponent<Button>();
        button.transition = Selectable.Transition.SpriteSwap;
        var states = button.spriteState;
        states.highlightedSprite = btnHover;
        states.pressedSprite = btnHover;
        button.spriteState = states;

        // Button Text
        GameObject btnTextGO = new GameObject("ButtonText", typeof(RectTransform), typeof(Text));
        btnTextGO.transform.SetParent(buttonGO.transform, false);

        var btnTextRect = btnTextGO.GetComponent<RectTransform>();
        btnTextRect.anchorMin = Vector2.zero;
        btnTextRect.anchorMax = Vector2.one;
        btnTextRect.sizeDelta = Vector2.zero;

        var btnText = btnTextGO.GetComponent<Text>();
        btnText.text = "Mulai Bersihkan!";
        btnText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        btnText.fontSize = 14;
        btnText.fontStyle = FontStyle.Bold;
        btnText.alignment = TextAnchor.MiddleCenter;
        btnText.color = Color.white;

        // Button Click Event
        button.onClick.AddListener(() =>
        {
            // Re-enable player movement
            if (playerMovement != null)
            {
                playerMovement.enabled = true;
            }
            Destroy(panelGO);
        });
    }
}
