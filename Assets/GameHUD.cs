using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class GameHUD : MonoBehaviour
{
    private TMP_Text objectiveText;
    private TMP_Text countText;
    private TMP_Text healthText;
    private TMP_Text statusText;
    private TMP_Text controlsText;
    private TMP_Text monsterText;
    private GameObject guardianPanel;
    private Image healthFill;
    private PlayerHealth playerHealth;
    private GameManager gameManager;
    private GameObject hudContent;
    private GameObject titleScreen;
    private GameObject victoryScreen;

    private void Awake()
    {
        gameManager = GetComponent<GameManager>();
        BuildInterface();
    }

    private void Update()
    {
        if (gameManager == null) gameManager = GameManager.Instance;
        if (playerHealth == null)
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null) playerHealth = player.GetComponent<PlayerHealth>();
        }

        if (gameManager == null) return;
        if (Keyboard.current != null && !gameManager.HasStarted &&
            (Keyboard.current.enterKey.wasPressedThisFrame || Keyboard.current.numpadEnterKey.wasPressedThisFrame))
            gameManager.StartGame();

        if (titleScreen != null) titleScreen.SetActive(!gameManager.HasStarted);
        if (hudContent != null) hudContent.SetActive(gameManager.HasStarted);
        if (victoryScreen != null) victoryScreen.SetActive(gameManager.IsComplete);

        if (countText != null) countText.text = "RELICS     " + gameManager.CollectedPieces + " / " + gameManager.totalPieces;
        if (objectiveText != null)
        {
            objectiveText.text = gameManager.IsComplete
                ? "JOURNEY COMPLETE\nThe Hollowwood is at peace."
                : gameManager.IsBridgeRestored
                    ? "CROSS THE BRIDGE\nReach the far end to leave the forest."
                : gameManager.MonsterAwake
                    ? "DEFEAT THE GUARDIAN\nAim for the creature and cast your magic."
                    : "FIND THE RELICS\nSearch the clearing for three ancient pieces.";
        }
        if (healthText != null)
        {
            int current = playerHealth != null ? playerHealth.CurrentHealth : 5;
            int max = playerHealth != null ? playerHealth.MaxHealth : 5;
            healthText.text = "VITALITY";
            healthText.color = current <= 2 ? new Color(1f, 0.55f, 0.4f) : new Color(0.85f, 0.94f, 0.82f);
            if (healthFill != null)
            {
                healthFill.fillAmount = max > 0 ? Mathf.Clamp01((float)current / max) : 0f;
                healthFill.color = current <= 2 ? new Color(0.95f, 0.25f, 0.18f) : new Color(0.28f, 0.82f, 0.48f);
            }
        }
        if (statusText != null)
        {
            statusText.text = gameManager.StatusMessage;
            statusText.enabled = gameManager.IsComplete || gameManager.IsGameOver;
            if (gameManager.IsGameOver) statusText.color = new Color(1f, 0.55f, 0.42f);
            else statusText.color = new Color(0.84f, 0.95f, 0.77f);
        }
        if (monsterText != null)
        {
            MonsterHealth monster = gameManager.forestMonster != null ? gameManager.forestMonster.GetComponent<MonsterHealth>() : null;
            monsterText.enabled = gameManager.MonsterAwake && monster != null;
            if (monster != null) monsterText.text = "GUARDIAN   " + new string('*', Mathf.Max(0, monster.CurrentHealth)) + new string('o', Mathf.Max(0, monster.MaxHealth - monster.CurrentHealth));
        }
        if (guardianPanel != null)
            guardianPanel.SetActive(gameManager.MonsterAwake && !gameManager.IsComplete && !gameManager.IsGameOver);
        if (controlsText != null) controlsText.text = "WASD MOVE     SHIFT SPRINT     SPACE JUMP\nRMB LOOK     LMB / F CAST";
    }

    private void BuildInterface()
    {
        GameObject canvasObject = new GameObject("Hollowwood HUD", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        Canvas canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 20;
        CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        hudContent = new GameObject("In-Game HUD", typeof(RectTransform));
        hudContent.transform.SetParent(canvas.transform, false);
        RectTransform hudRect = hudContent.GetComponent<RectTransform>();
        hudRect.anchorMin = Vector2.zero;
        hudRect.anchorMax = Vector2.one;
        hudRect.offsetMin = Vector2.zero;
        hudRect.offsetMax = Vector2.zero;

        CreatePanel(hudContent.transform, "Quest Panel", new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(24f, -24f), new Vector2(520f, 188f), new Color(0.035f, 0.075f, 0.062f, 0.86f));
        CreatePanel(hudContent.transform, "Vitality Panel", new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-24f, -24f), new Vector2(390f, 72f), new Color(0.035f, 0.075f, 0.062f, 0.86f));
        CreatePanel(hudContent.transform, "Controls Panel", new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 22f), new Vector2(900f, 62f), new Color(0.035f, 0.075f, 0.062f, 0.82f));
        guardianPanel = CreatePanel(hudContent.transform, "Guardian Panel", new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -24f), new Vector2(500f, 72f), new Color(0.075f, 0.035f, 0.03f, 0.86f));
        guardianPanel.SetActive(false);

        CreateText(hudContent.transform, "Hollowwood Title", "H O L L O W W O O D", 24f, new Color(0.86f, 0.72f, 0.44f), TextAlignmentOptions.Left, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(46f, -38f), new Vector2(480f, 36f));
        objectiveText = CreateText(hudContent.transform, "Objective", "", 22f, new Color(0.9f, 0.94f, 0.86f), TextAlignmentOptions.Left, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(46f, -82f), new Vector2(480f, 62f));
        countText = CreateText(hudContent.transform, "Relic Counter", "RELICS  0 / 3", 22f, new Color(0.83f, 0.9f, 0.72f), TextAlignmentOptions.Left, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(46f, -152f), new Vector2(480f, 30f));
        healthText = CreateText(hudContent.transform, "Vitality", "VITALITY", 18f, new Color(0.85f, 0.94f, 0.82f), TextAlignmentOptions.Left, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-24f, -12f), new Vector2(342f, 24f));
        healthFill = CreateHealthBar(hudContent.transform);
        monsterText = CreateText(hudContent.transform, "Guardian Vitality", "", 22f, new Color(1f, 0.68f, 0.47f), TextAlignmentOptions.Center, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -24f), new Vector2(500f, 72f));
        monsterText.enabled = false;
        statusText = CreateText(hudContent.transform, "Ending Message", "", 30f, Color.white, TextAlignmentOptions.Center, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, -130f), new Vector2(980f, 120f));
        statusText.enabled = false;
        controlsText = CreateText(hudContent.transform, "Controls", "", 18f, new Color(0.79f, 0.85f, 0.76f), TextAlignmentOptions.Center, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 22f), new Vector2(880f, 58f));

        BuildTitleScreen(canvas.transform);
        BuildVictoryScreen(canvas.transform);
    }

    private void BuildTitleScreen(Transform parent)
    {
        titleScreen = CreatePanel(parent, "Title Screen", Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, new Color(0.015f, 0.035f, 0.03f, 0.82f));
        CreatePanel(titleScreen.transform, "Title Card", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(820f, 590f), new Color(0.025f, 0.075f, 0.06f, 0.96f));
        CreateText(titleScreen.transform, "Title Eyebrow", "A FOREST FOLK TALE", 22f, new Color(0.72f, 0.83f, 0.66f), TextAlignmentOptions.Center, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 190f), new Vector2(700f, 40f));
        CreateText(titleScreen.transform, "Title Logo", "HOLLOWWOOD", 66f, new Color(0.91f, 0.75f, 0.42f), TextAlignmentOptions.Center, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 105f), new Vector2(760f, 100f));
        CreateText(titleScreen.transform, "Story Introduction", "A sleeping guardian watches over the old forest.\nFind three relics, face the golem, and restore the bridge.", 25f, new Color(0.88f, 0.92f, 0.84f), TextAlignmentOptions.Center, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, -5f), new Vector2(700f, 110f));
        CreateButton(titleScreen.transform, "Begin Journey", "BEGIN JOURNEY", new Vector2(0f, -145f), () => { if (gameManager != null) gameManager.StartGame(); });
        CreateText(titleScreen.transform, "Title Controls", "WASD to move   •   Shift to sprint   •   Space to jump\nAim and cast with the mouse or F", 19f, new Color(0.68f, 0.76f, 0.67f), TextAlignmentOptions.Center, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, -235f), new Vector2(700f, 75f));
        CreateText(titleScreen.transform, "Start Hint", "Click Begin Journey or press Enter", 17f, new Color(0.79f, 0.82f, 0.73f), TextAlignmentOptions.Center, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, -275f), new Vector2(600f, 32f));
    }

    private void BuildVictoryScreen(Transform parent)
    {
        victoryScreen = CreatePanel(parent, "Victory Screen", Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, new Color(0.012f, 0.04f, 0.03f, 0.86f));
        CreatePanel(victoryScreen.transform, "Victory Card", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(820f, 500f), new Color(0.025f, 0.075f, 0.06f, 0.97f));
        CreateText(victoryScreen.transform, "Victory Eyebrow", "THE OLD PATH IS OPEN", 22f, new Color(0.72f, 0.83f, 0.66f), TextAlignmentOptions.Center, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 140f), new Vector2(700f, 40f));
        CreateText(victoryScreen.transform, "Victory Title", "HOLLOWWOOD RESTORED", 48f, new Color(0.91f, 0.75f, 0.42f), TextAlignmentOptions.Center, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 65f), new Vector2(760f, 80f));
        CreateText(victoryScreen.transform, "Victory Message", "You crossed the bridge and brought peace to the forest.\nThe guardian can finally rest.", 25f, new Color(0.88f, 0.92f, 0.84f), TextAlignmentOptions.Center, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, -35f), new Vector2(700f, 95f));
        CreateButton(victoryScreen.transform, "Play Again", "PLAY AGAIN", new Vector2(0f, -145f), () => { if (gameManager != null) gameManager.RestartGame(); });
        CreateText(victoryScreen.transform, "Victory Restart Hint", "Press R to return to the beginning", 18f, new Color(0.72f, 0.79f, 0.7f), TextAlignmentOptions.Center, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, -205f), new Vector2(700f, 36f));
        victoryScreen.SetActive(false);
    }

    private static void CreateButton(Transform parent, string name, string label, Vector2 position, UnityEngine.Events.UnityAction onClick)
    {
        GameObject buttonObject = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
        buttonObject.transform.SetParent(parent, false);
        RectTransform rect = buttonObject.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = position;
        rect.sizeDelta = new Vector2(330f, 66f);
        Image image = buttonObject.GetComponent<Image>();
        image.color = new Color(0.48f, 0.57f, 0.31f, 1f);
        Button button = buttonObject.GetComponent<Button>();
        button.targetGraphic = image;
        button.onClick.AddListener(onClick);
        ColorBlock colors = button.colors;
        colors.highlightedColor = new Color(0.65f, 0.73f, 0.43f, 1f);
        colors.pressedColor = new Color(0.34f, 0.43f, 0.24f, 1f);
        button.colors = colors;
        CreateText(buttonObject.transform, name + " Label", label, 23f, new Color(0.98f, 0.96f, 0.86f), TextAlignmentOptions.Center, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
    }

    private static GameObject CreatePanel(Transform parent, string name, Vector2 anchorMin, Vector2 anchorMax, Vector2 position, Vector2 size, Color color)
    {
        GameObject panel = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        panel.transform.SetParent(parent, false);
        RectTransform rect = panel.GetComponent<RectTransform>();
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.pivot = anchorMin == Vector2.zero ? Vector2.zero : anchorMin;
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
        panel.GetComponent<Image>().color = color;
        return panel;
    }

    private static Image CreateHealthBar(Transform parent)
    {
        GameObject bar = new GameObject("Vitality Bar", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        bar.transform.SetParent(parent, false);
        RectTransform rect = bar.GetComponent<RectTransform>();
        rect.anchorMin = Vector2.one;
        rect.anchorMax = Vector2.one;
        rect.pivot = Vector2.one;
        rect.anchoredPosition = new Vector2(-24f, -43f);
        rect.sizeDelta = new Vector2(342f, 14f);
        Image image = bar.GetComponent<Image>();
        image.type = Image.Type.Filled;
        image.fillMethod = Image.FillMethod.Horizontal;
        image.fillOrigin = (int)Image.OriginHorizontal.Left;
        image.fillAmount = 1f;
        image.color = new Color(0.28f, 0.82f, 0.48f);
        return image;
    }

    private static TMP_Text CreateText(Transform parent, string name, string initialText, float size, Color color, TextAlignmentOptions alignment, Vector2 anchorMin, Vector2 anchorMax, Vector2 position, Vector2 dimensions)
    {
        GameObject textObject = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        textObject.transform.SetParent(parent, false);
        RectTransform rect = textObject.GetComponent<RectTransform>();
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.pivot = anchorMin == Vector2.zero ? Vector2.zero : anchorMin;
        rect.anchoredPosition = position;
        rect.sizeDelta = dimensions;
        TMP_Text text = textObject.GetComponent<TMP_Text>();
        text.text = initialText;
        text.fontSize = size;
        text.color = color;
        text.alignment = alignment;
        text.font = TMP_Settings.defaultFontAsset;
        text.textWrappingMode = TextWrappingModes.Normal;
        text.raycastTarget = false;
        return text;
    }
}
