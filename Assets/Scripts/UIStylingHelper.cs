using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class UIStylingHelper : MonoBehaviour
{
    private static UIStylingHelper instance;

    // --- Resource Paths & Constants ---
    private const string RESOURCE_BUTTONS = "Buttons";
    private const string RESOURCE_TIMER = "Timer";
    private const string RESOURCE_HEALTH_BAR = "Health bar";
    private const string RESOURCE_DEFAULT_FONT = "LegacyRuntime.ttf";

    // --- Sprite Names ---
    private const string SPRITE_BUTTON_NORMAL = "Buttons_0";
    private const string SPRITE_BUTTON_HOVER = "Buttons_3";
    private const string SPRITE_PANEL_BG = "Buttons_12";
    private const string SPRITE_HEALTH_FRAME = "Health bar_16";
    private const string SPRITE_HEALTH_FILL = "Health bar_18";
    private const string SPRITE_HEALTH_FRAME_ALT = "Health bar_1";
    private const string SPRITE_HEALTH_FILL_ALT = "Health bar_0";
    private const string SPRITE_TIMER_ICON = "Timer_0";

    // --- UI Colors & Names ---
    private const string MANUAL_PANEL_NAME = "GameManualPanel";
    private const string MANUAL_CLOSE_BTN_NAME = "ManualCloseButton";
    private const string TARGET_SCENE_MAIN = "MainScene2";

    private static readonly Color ColorTitle = new Color(0.25f, 0.15f, 0.05f); // Cozy Dark Brown
    private static readonly Color ColorBody = new Color(0.3f, 0.2f, 0.1f);   // Cozy Brown

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

    private IEnumerator ApplyStylingDelayed()
    {
        yield return new WaitForEndOfFrame();
        ApplyStylingForCurrentScene();
    }

    public void ApplyStylingForCurrentScene()
    {
        string sceneName = SceneManager.GetActiveScene().name;
        Debug.Log($"UIStylingHelper: Applying styles for scene {sceneName}");

        // 1. Always apply button styles to any Buttons in active scene
        ApplyButtonStyles();

        // 2. Scene-specific styling
        if (sceneName == TARGET_SCENE_MAIN)
        {
            ApplyTimerSprites();
            ApplyHealthBarStyles();
            CreateGameManual();
        }
    }

    private void ApplyButtonStyles()
    {
        Sprite[] buttonsSprites = Resources.LoadAll<Sprite>(RESOURCE_BUTTONS);
        if (buttonsSprites == null || buttonsSprites.Length == 0)
        {
            Debug.LogWarning("UIStylingHelper: Could not load Buttons sprite sheet from Resources.");
            return;
        }

        Sprite normalBtn = System.Array.Find(buttonsSprites, s => s.name == SPRITE_BUTTON_NORMAL);
        Sprite hoverBtn = System.Array.Find(buttonsSprites, s => s.name == SPRITE_BUTTON_HOVER);

        if (normalBtn == null || hoverBtn == null) return;

        Button[] allButtons = FindObjectsOfType<Button>();
        foreach (Button btn in allButtons)
        {
            // Skip the close button of our manual to avoid double styling
            if (btn.gameObject.name == MANUAL_CLOSE_BTN_NAME) continue;

            Image img = btn.GetComponent<Image>();
            if (img != null)
            {
                img.sprite = normalBtn;
                img.type = Image.Type.Sliced;

                btn.transition = Selectable.Transition.SpriteSwap;
                SpriteState spriteState = btn.spriteState;
                spriteState.highlightedSprite = hoverBtn;
                spriteState.pressedSprite = hoverBtn;
                btn.spriteState = spriteState;
            }
        }
    }

    private void ApplyTimerSprites()
    {
        if (ScoreManager.Instance == null) return;

        Sprite[] timerSprites = Resources.LoadAll<Sprite>(RESOURCE_TIMER);
        if (timerSprites == null || timerSprites.Length == 0) return;

        List<Sprite> green = new List<Sprite>();
        List<Sprite> yellow = new List<Sprite>();
        List<Sprite> red = new List<Sprite>();

        foreach (Sprite sprite in timerSprites)
        {
            int num = GetSpriteNum(sprite.name);
            if (num <= 12) green.Add(sprite);
            else if (num <= 25) yellow.Add(sprite);
            else red.Add(sprite);
        }

        green.Sort((a, b) => GetSpriteNum(a.name).CompareTo(GetSpriteNum(b.name)));
        yellow.Sort((a, b) => GetSpriteNum(a.name).CompareTo(GetSpriteNum(b.name)));
        red.Sort((a, b) => GetSpriteNum(a.name).CompareTo(GetSpriteNum(b.name)));

        ScoreManager.Instance.timerGreenFrames = green.ToArray();
        ScoreManager.Instance.timerYellowFrames = yellow.ToArray();
        ScoreManager.Instance.timerRedFrames = red.ToArray();

        // Style static Timer Icon if configured
        if (ScoreManager.Instance.timerIconElement != null)
        {
            ScoreManager.Instance.timerIconElement.sprite = System.Array.Find(timerSprites, s => s.name == SPRITE_TIMER_ICON);
        }
    }

    private int GetSpriteNum(string name)
    {
        int lastUnder = name.LastIndexOf('_');
        if (lastUnder >= 0 && int.TryParse(name.Substring(lastUnder + 1), out int num))
            return num;
        return 0;
    }

    private void ApplyHealthBarStyles()
    {
        PlayerHealth playerHealth = FindObjectOfType<PlayerHealth>();
        if (playerHealth == null || playerHealth.hudHealthBar == null) return;

        Image hudHealthBar = playerHealth.hudHealthBar;

        Sprite[] healthSprites = Resources.LoadAll<Sprite>(RESOURCE_HEALTH_BAR);
        if (healthSprites == null || healthSprites.Length == 0) return;

        Sprite frameSprite = System.Array.Find(healthSprites, s => s.name == SPRITE_HEALTH_FRAME);
        Sprite fillSprite = System.Array.Find(healthSprites, s => s.name == SPRITE_HEALTH_FILL);

        if (fillSprite == null) fillSprite = System.Array.Find(healthSprites, s => s.name == SPRITE_HEALTH_FILL_ALT);
        if (frameSprite == null) frameSprite = System.Array.Find(healthSprites, s => s.name == SPRITE_HEALTH_FRAME_ALT);

        hudHealthBar.sprite = fillSprite;
        hudHealthBar.type = Image.Type.Filled;
        hudHealthBar.fillMethod = Image.FillMethod.Horizontal;

        Transform parent = hudHealthBar.transform.parent;
        if (parent != null && parent.Find("HealthBarFrame") == null)
        {
            GameObject frameGO = new GameObject("HealthBarFrame", typeof(RectTransform), typeof(Image));
            frameGO.transform.SetParent(parent, false);
            frameGO.transform.SetSiblingIndex(hudHealthBar.transform.GetSiblingIndex());

            Image frameImg = frameGO.GetComponent<Image>();
            frameImg.sprite = frameSprite;
            frameImg.type = Image.Type.Sliced;

            RectTransform frameRect = frameGO.GetComponent<RectTransform>();
            RectTransform fillRect = hudHealthBar.GetComponent<RectTransform>();

            frameRect.anchorMin = fillRect.anchorMin;
            frameRect.anchorMax = fillRect.anchorMax;
            frameRect.anchoredPosition = fillRect.anchoredPosition;
            frameRect.sizeDelta = fillRect.sizeDelta + new Vector2(16, 16);
            frameRect.pivot = fillRect.pivot;
        }
    }

    private void CreateGameManual()
    {
        // Avoid duplicate manual popups
        if (GameObject.Find(MANUAL_PANEL_NAME) != null) return;

        Canvas canvas = FindObjectOfType<Canvas>();
        if (canvas == null)
        {
            Debug.LogError("UIStylingHelper: No Canvas found in MainScene2 to spawn the game manual!");
            return;
        }

        // Disable player movement temporarily while reading
        NewMonoBehaviourScript playerMovement = FindObjectOfType<NewMonoBehaviourScript>();
        if (playerMovement != null)
        {
            playerMovement.enabled = false;
        }

        Sprite[] buttonsSprites = Resources.LoadAll<Sprite>(RESOURCE_BUTTONS);
        Sprite panelBg = System.Array.Find(buttonsSprites, s => s.name == SPRITE_PANEL_BG);
        Sprite btnNormal = System.Array.Find(buttonsSprites, s => s.name == SPRITE_BUTTON_NORMAL);
        Sprite btnHover = System.Array.Find(buttonsSprites, s => s.name == SPRITE_BUTTON_HOVER);

        if (panelBg == null) panelBg = btnNormal;

        // Build Modular UI Components
        GameObject panelGO = BuildPanelContainer(canvas.transform, panelBg);
        BuildTitleText(panelGO.transform);
        BuildBodyText(panelGO.transform);
        BuildCloseButton(panelGO.transform, btnNormal, btnHover, panelGO, playerMovement);
    }

    private GameObject BuildPanelContainer(Transform parent, Sprite panelBg)
    {
        GameObject panelGO = new GameObject(MANUAL_PANEL_NAME, typeof(RectTransform), typeof(Image));
        panelGO.transform.SetParent(parent, false);

        RectTransform rectTransform = panelGO.GetComponent<RectTransform>();
        rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
        rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
        rectTransform.pivot = new Vector2(0.5f, 0.5f);
        rectTransform.sizeDelta = new Vector2(460, 320);
        rectTransform.anchoredPosition = Vector2.zero;

        Image img = panelGO.GetComponent<Image>();
        img.sprite = panelBg;
        img.type = Image.Type.Sliced;

        return panelGO;
    }

    private void BuildTitleText(Transform parent)
    {
        GameObject titleGO = new GameObject("TitleText", typeof(RectTransform), typeof(Text));
        titleGO.transform.SetParent(parent, false);

        RectTransform titleRect = titleGO.GetComponent<RectTransform>();
        titleRect.anchorMin = new Vector2(0, 1);
        titleRect.anchorMax = new Vector2(1, 1);
        titleRect.pivot = new Vector2(0.5f, 1);
        titleRect.sizeDelta = new Vector2(-40, 40);
        titleRect.anchoredPosition = new Vector2(0, -25);

        Text titleText = titleGO.GetComponent<Text>();
        titleText.text = "PANDUAN BERMAIN";
        titleText.font = Resources.GetBuiltinResource<Font>(RESOURCE_DEFAULT_FONT);
        titleText.fontSize = 24;
        titleText.fontStyle = FontStyle.Bold;
        titleText.alignment = TextAnchor.MiddleCenter;
        titleText.color = ColorTitle;
    }

    private void BuildBodyText(Transform parent)
    {
        GameObject bodyGO = new GameObject("BodyText", typeof(RectTransform), typeof(Text));
        bodyGO.transform.SetParent(parent, false);

        RectTransform bodyRect = bodyGO.GetComponent<RectTransform>();
        bodyRect.anchorMin = new Vector2(0, 0);
        bodyRect.anchorMax = new Vector2(1, 1);
        bodyRect.pivot = new Vector2(0.5f, 0.5f);
        bodyRect.sizeDelta = new Vector2(-50, -110);
        bodyRect.anchoredPosition = new Vector2(0, -10);

        Text bodyText = bodyGO.GetComponent<Text>();
        bodyText.text = "Selamat datang! Ayahmu mewariskan tugas mulia untuk menyelamatkan laut ini.\n\n" +
                        "• <b>Misi</b>: Kumpulkan sampah plastik & limbah sebanyak-banyaknya di laut.\n" +
                        "• <b>Kontrol</b>: Gunakan tombol <b>W, A, S, D / Arah Panah</b> untuk bergerak.\n" +
                        "• <b>Kapal</b>: Dekati kapal dan tekan tombol <b>E</b> untuk naik/mengemudi.\n" +
                        "• <b>Waktu</b>: Waktu 1 Menit baru dimulai <b>setelah Anda naik ke kapal</b>!\n" +
                        "• <b>Rintangan</b>: Hindari menabrak rintangan laut agar kapal tidak rusak (darah habis).";
        bodyText.font = Resources.GetBuiltinResource<Font>(RESOURCE_DEFAULT_FONT);
        bodyText.fontSize = 13;
        bodyText.alignment = TextAnchor.UpperLeft;
        bodyText.supportRichText = true;
        bodyText.lineSpacing = 1.2f;
        bodyText.color = ColorBody;
    }

    private void BuildCloseButton(Transform parent, Sprite btnNormal, Sprite btnHover, GameObject panelGO, NewMonoBehaviourScript playerMovement)
    {
        GameObject buttonGO = new GameObject(MANUAL_CLOSE_BTN_NAME, typeof(RectTransform), typeof(Image), typeof(Button));
        buttonGO.transform.SetParent(parent, false);

        RectTransform buttonRect = buttonGO.GetComponent<RectTransform>();
        buttonRect.anchorMin = new Vector2(0.5f, 0);
        buttonRect.anchorMax = new Vector2(0.5f, 0);
        buttonRect.pivot = new Vector2(0.5f, 0);
        buttonRect.sizeDelta = new Vector2(160, 40);
        buttonRect.anchoredPosition = new Vector2(0, 20);

        Image buttonImg = buttonGO.GetComponent<Image>();
        buttonImg.sprite = btnNormal;
        buttonImg.type = Image.Type.Sliced;

        Button button = buttonGO.GetComponent<Button>();
        button.transition = Selectable.Transition.SpriteSwap;
        SpriteState states = button.spriteState;
        states.highlightedSprite = btnHover;
        states.pressedSprite = btnHover;
        button.spriteState = states;

        // Button Text
        GameObject btnTextGO = new GameObject("ButtonText", typeof(RectTransform), typeof(Text));
        btnTextGO.transform.SetParent(buttonGO.transform, false);

        RectTransform btnTextRect = btnTextGO.GetComponent<RectTransform>();
        btnTextRect.anchorMin = Vector2.zero;
        btnTextRect.anchorMax = Vector2.one;
        btnTextRect.sizeDelta = Vector2.zero;

        Text btnText = btnTextGO.GetComponent<Text>();
        btnText.text = "Mulai Bersihkan!";
        btnText.font = Resources.GetBuiltinResource<Font>(RESOURCE_DEFAULT_FONT);
        btnText.fontSize = 14;
        btnText.fontStyle = FontStyle.Bold;
        btnText.alignment = TextAnchor.MiddleCenter;
        btnText.color = Color.white;

        // Button Click Event Listener
        button.onClick.AddListener(() =>
        {
            if (playerMovement != null)
            {
                playerMovement.enabled = true;
            }
            Destroy(panelGO);
        });
    }
}

