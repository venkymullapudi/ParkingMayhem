using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

public class UIManager : MonoBehaviour
{
    public static UIManager Instance { get; private set; }

    //   Panel references  
    [Header("Panels")]
    public GameObject mainMenuPanel;
    public GameObject gamePanel;
    public GameObject levelCompletePanel;
    public GameObject gameOverPanel;
    public GameObject pauseMenuPanel;
    public GameObject achievementToastPanel;

    //   Canvas Groups (add one to each panel root)  
    [Header("Canvas Groups")]
    public CanvasGroup mainMenuCG;
    public CanvasGroup levelCompleteCG;
    public CanvasGroup gameOverCG;
    public CanvasGroup pauseCG;
    public CanvasGroup toastCG;

    //   HUD  
    [Header("HUD")]
    public TextMeshProUGUI levelText;
    public TextMeshProUGUI vehicleTypeText;
    public TextMeshProUGUI terrainTypeText;
    public Slider progressSlider;
    public Image progressFill;
    public TextMeshProUGUI timerText;
    public Button restartButton;
    public Button nextLevelButton;
    public Button pauseButton;

    //   Level Complete  
    [Header("Level Complete")]
    public TextMeshProUGUI levelCompleteText;
    public TextMeshProUGUI scoreText;
    public TextMeshProUGUI timeText;
    public TextMeshProUGUI bestText;
    public StarRating starRating;

    //   Achievement Toast  
    [Header("Achievement Toast")]
    public TextMeshProUGUI achievementToastText;
    public RectTransform toastRect;

    //   State  
    private float levelStartTime;
    private bool timerRunning;
    private float currentTime;
    private Coroutine timerRoutine;
    private Coroutine toastRoutine;
    private int _lastWarningSecond = -1;

    // Gradient colors for progress bar
    private static readonly Color progressLow  = new Color(0.2f, 0.9f, 0.3f);
    private static readonly Color progressMid  = new Color(1f,   0.8f, 0.1f);
    private static readonly Color progressHigh = new Color(1f,   0.3f, 0.2f);

    //   Lifecycle  

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else { Destroy(gameObject); return; }
    }

    private void Start()
    {
        EnsureCanvasGroups();
        HideAllImmediate();
        SetupButtons();
        PolishPanels();
        // Timer canvas is created lazily in ShowGameUI (not here) so it never bleeds
        // through the terrain-select overlay before the game has started.
        // OnSceneLoaded in GameManager calls ShowTerrainSelect() before BootstrapGameScene.
    }

    // Refine the pause / level-complete / game-over panels: a rounded card backdrop + rounded,
    // accent-coloured buttons with press feedback - a cohesive, polished look.
    private void PolishPanels()
    {
        PolishPanel(pauseMenuPanel);
        PolishPanel(levelCompletePanel);
        PolishPanel(gameOverPanel);
    }

    private void PolishPanel(GameObject panel)
    {
        if (panel == null) return;
        var round = SpriteGenerator.CreateRoundedRect(260, 96, 0.5f);

        foreach (Transform child in panel.transform)
        {
            var btn = child.GetComponent<Button>();
            if (btn == null) continue;
            var tmp = btn.GetComponentInChildren<TMPro.TextMeshProUGUI>(true);
            string label = tmp != null ? tmp.text.Trim().ToUpperInvariant() : "";
            Color col;
            if (label.Contains("RESUME") || label.Contains("NEXT"))      col = new Color(0.20f, 0.62f, 0.26f);
            else if (label.Contains("RESTART") || label.Contains("RETRY")) col = new Color(0.16f, 0.45f, 0.82f);
            else if (label.Contains("MENU"))                              col = new Color(0.42f, 0.30f, 0.55f);
            else                                                          col = new Color(0.30f, 0.34f, 0.45f);

            var img = btn.GetComponent<UnityEngine.UI.Image>();
            if (img != null) { img.sprite = round; img.type = UnityEngine.UI.Image.Type.Simple; img.color = col; }
            if (btn.GetComponent<ButtonAnimator>() == null) btn.gameObject.AddComponent<ButtonAnimator>();
        }
    }

    private void FindTimerText()
    {
        if (timerText != null) return;

        // Check if timer canvas persists from a previous Game scene visit
        var existingCanvas = GameObject.Find("_TimerCanvas");
        if (existingCanvas != null)
        {
            timerText = existingCanvas.GetComponentInChildren<TextMeshProUGUI>(true);
            if (timerText != null) { existingCanvas.SetActive(true); return; }
            Destroy(existingCanvas); // stale canvas, recreate
        }

        // We need a pre-configured TMP source to clone from (levelText has correct font asset).
        // Without it we can't create a visible TMP component at runtime.
        if (levelText == null) return;

        // Create an isolated ScreenSpaceOverlay canvas that lives in DontDestroyOnLoad.
        var canvasGO = new GameObject("_TimerCanvas");
        DontDestroyOnLoad(canvasGO);
        var c = canvasGO.AddComponent<Canvas>();
        c.renderMode = RenderMode.ScreenSpaceOverlay;
        c.sortingOrder = 100;
        var scaler = canvasGO.AddComponent<UnityEngine.UI.CanvasScaler>();
        scaler.uiScaleMode = UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1280f, 720f);
        scaler.matchWidthOrHeight = 0.5f;
        canvasGO.AddComponent<UnityEngine.UI.GraphicRaycaster>();

        // Polished rounded "pill" behind the timer (drop shadow + translucent panel)
        var shadowGO = new GameObject("TimerShadow", typeof(RectTransform));
        shadowGO.transform.SetParent(canvasGO.transform, false);
        var shadowImg = shadowGO.AddComponent<UnityEngine.UI.Image>();
        shadowImg.sprite = SpriteGenerator.CreateRoundedRect(220, 90, 0.5f);
        shadowImg.color = new Color(0f, 0f, 0f, 0.30f);
        shadowImg.raycastTarget = false;
        var srt = shadowGO.GetComponent<RectTransform>();
        srt.anchorMin = srt.anchorMax = new Vector2(0.5f, 1f); srt.pivot = new Vector2(0.5f, 1f);
        srt.anchoredPosition = new Vector2(3f, -11f); srt.sizeDelta = new Vector2(150f, 64f);

        var pillGO = new GameObject("TimerPill", typeof(RectTransform));
        pillGO.transform.SetParent(canvasGO.transform, false);
        var pillImg = pillGO.AddComponent<UnityEngine.UI.Image>();
        pillImg.sprite = SpriteGenerator.CreateRoundedRect(220, 90, 0.5f);
        pillImg.color = new Color(0.10f, 0.12f, 0.20f, 0.78f);
        pillImg.raycastTarget = false;
        var prt = pillGO.GetComponent<RectTransform>();
        prt.anchorMin = prt.anchorMax = new Vector2(0.5f, 1f); prt.pivot = new Vector2(0.5f, 1f);
        prt.anchoredPosition = new Vector2(0f, -8f); prt.sizeDelta = new Vector2(150f, 64f);

        // Clone levelText to inherit its font asset and TMP settings
        var clone = Instantiate(levelText.gameObject, canvasGO.transform, false);
        clone.name = "TimerLabel";
        var rt = clone.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 1f);
        rt.anchorMax = new Vector2(0.5f, 1f);
        rt.pivot     = new Vector2(0.5f, 1f);
        rt.anchoredPosition = new Vector2(0f, -10f);
        rt.sizeDelta = new Vector2(300f, 80f);

        timerText = clone.GetComponent<TextMeshProUGUI>();
        timerText.text = "1:00";
        timerText.fontSize = 56;
        timerText.fontStyle = TMPro.FontStyles.Bold;
        timerText.alignment = TMPro.TextAlignmentOptions.Center;
        timerText.color = Color.white;
        clone.SetActive(true);
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (pauseMenuPanel != null && pauseMenuPanel.activeSelf)
                HidePauseMenu();
            else
                ShowPauseMenu();
        }
    }

    private void EnsureCanvasGroups()
    {
        mainMenuCG      = EnsureCG(mainMenuPanel,      mainMenuCG);
        levelCompleteCG = EnsureCG(levelCompletePanel, levelCompleteCG);
        gameOverCG      = EnsureCG(gameOverPanel,      gameOverCG);
        pauseCG         = EnsureCG(pauseMenuPanel,     pauseCG);
        toastCG         = EnsureCG(achievementToastPanel, toastCG);
    }

    private CanvasGroup EnsureCG(GameObject panel, CanvasGroup existing)
    {
        if (panel == null) return existing;
        if (existing != null) return existing;
        var cg = panel.GetComponent<CanvasGroup>() ?? panel.AddComponent<CanvasGroup>();
        return cg;
    }

    private void HideAllImmediate()
    {
        SetPanelImmediate(mainMenuPanel,      mainMenuCG,      false);
        SetPanelImmediate(levelCompletePanel, levelCompleteCG, false);
        SetPanelImmediate(gameOverPanel,      gameOverCG,      false);
        SetPanelImmediate(pauseMenuPanel,     pauseCG,         false);
        SetPanelImmediate(achievementToastPanel, toastCG,      false);
    }

    private void SetPanelImmediate(GameObject panel, CanvasGroup cg, bool visible)
    {
        if (panel == null) return;
        panel.SetActive(visible);
        if (cg != null) { cg.alpha = visible ? 1f : 0f; cg.interactable = visible; cg.blocksRaycasts = visible; }
    }

    private void SetupButtons()
    {
        restartButton?.onClick.AddListener(RestartLevel);
        nextLevelButton?.onClick.AddListener(NextLevel);

        // The pauseButton reference is unassigned in the scene, so find it by name and wire it.
        if (pauseButton == null)
        {
            var pb = GameObject.Find("PauseButton");
            if (pb != null) pauseButton = pb.GetComponent<Button>();
        }
        if (pauseButton != null)
        {
            pauseButton.onClick.RemoveAllListeners();
            pauseButton.onClick.AddListener(ShowPauseMenu);
        }

        // Wire pause menu buttons directly by child name.
        // IMPORTANT: GameCanvas is a child of PauseMenuPanel in the scene hierarchy,
        // so GetComponentsInChildren would scan the ENTIRE scene. We only look at
        // direct children of each panel to avoid cross-wiring every scene button.
        WirePauseMenuButtons();
        WireButtonsInPanel(gameOverPanel);
        WireButtonsInPanel(levelCompletePanel);

        foreach (var btn in FindObjectsByType<Button>(FindObjectsSortMode.None))
            if (btn.GetComponent<ButtonAnimator>() == null)
                btn.gameObject.AddComponent<ButtonAnimator>();
    }

    private void WirePauseMenuButtons()
    {
        if (pauseMenuPanel == null) return;
        // Wire only direct children - avoids GameCanvas subtree which contains all scene buttons
        foreach (Transform child in pauseMenuPanel.transform)
        {
            var btn = child.GetComponent<Button>();
            if (btn == null) continue;
            btn.onClick.RemoveAllListeners();
            string n = child.name;
            if (n == "ResumeButton")    btn.onClick.AddListener(HidePauseMenu);
            else if (n == "RestartButton" || n == "RestartButton2") btn.onClick.AddListener(RestartLevel);
            else if (n == "MainMenuButton") btn.onClick.AddListener(GoToMainMenu);
        }
    }

    private void WireButtonsInPanel(GameObject panel)
    {
        if (panel == null) return;
        // Only scan direct children of this panel (not GameCanvas descendants)
        foreach (Transform child in panel.transform)
        {
            var btn = child.GetComponent<Button>();
            if (btn == null) continue;
            var tmp = btn.GetComponentInChildren<TMPro.TextMeshProUGUI>(true);
            if (tmp == null) continue;
            string label = tmp.text.Trim().ToUpperInvariant();
            btn.onClick.RemoveAllListeners();
            switch (label)
            {
                case "RESUME":     btn.onClick.AddListener(HidePauseMenu);  break;
                case "RESTART":
                case "RETRY":      btn.onClick.AddListener(RestartLevel);   break;
                case "MAIN MENU":
                case "MENU":       btn.onClick.AddListener(GoToMainMenu);   break;
                case "NEXT LEVEL": btn.onClick.AddListener(NextLevel);      break;
            }
        }
    }

    private void GoToMainMenu()
    {
        Time.timeScale = 1f;
        AudioManager.Instance?.SetGamePaused(false);   // don't carry a frozen AudioListener to the menu
        CleanupPersistentGameUI();
        UnityEngine.SceneManagement.SceneManager.LoadScene("MainMenu");
    }

    // The game scene creates several DontDestroyOnLoad UI canvases (HUD, on-screen controls,
    // confetti, timer, countdown, coin-fx). If they survive into the MainMenu scene they pollute
    // MainMenuManager's FindAnyObjectByType<Canvas>() lookups (its background / best-times get
    // parented to a leftover game canvas) and their overlays render on top of the menu -- the menu
    // ends up invisible-but-clickable. Tearing them down here makes returning to the menu behave
    // exactly like a fresh launch. Each is lazily recreated when the next game starts.
    private void CleanupPersistentGameUI()
    {
        OnScreenControls.InputBlocked = false;   // never carry an input lock out of the game
        if (GameHUD.Instance != null)          Destroy(GameHUD.Instance.gameObject);
        if (OnScreenControls.Instance != null) Destroy(OnScreenControls.Instance.gameObject);
        if (ParticleCanvas.Instance != null)   Destroy(ParticleCanvas.Instance.gameObject);
        foreach (var n in new[] { "_TimerCanvas", "_CountdownCanvas", "_CoinFxCanvas" })
        {
            var go = GameObject.Find(n);
            if (go != null) Destroy(go);
        }
    }

    //   Public Show Methods  

    public void ShowGameUI()
    {
        levelEnded = false;   // fresh level — re-arm win/lose resolution
        // Defensive: the 3-2-1 countdown sets the static OnScreenControls.InputBlocked=true and
        // clears it after its realtime yields. If that coroutine was aborted (e.g. Main Menu /
        // scene swap mid-countdown), the static would leak true and freeze the next level's
        // controls. Clearing it at every level start guarantees a responsive car.
        OnScreenControls.InputBlocked = false;
        AudioManager.Instance?.SetGamePaused(false);   // never carry a frozen AudioListener into a level
        SetPanelImmediate(mainMenuPanel,      mainMenuCG,      false);
        SetPanelImmediate(levelCompletePanel, levelCompleteCG, false);
        SetPanelImmediate(gameOverPanel,      gameOverCG,      false);
        SetPanelImmediate(pauseMenuPanel,     pauseCG,         false);
        if (gamePanel != null) gamePanel.SetActive(true);
        WirePauseButton();   // HUD is active now, so the pause button is findable
        SetPauseButtonVisible(true);   // ensure it's shown again after a game-over/level-complete hid it
        FindTimerText();
        // Show THIS level's real time limit (white) during the count-in — on canvas reuse the pill
        // otherwise kept the previous level's last value (possibly a red "0:03" or "PARK!").
        if (timerText != null)
        {
            float limit = 90f;
            if (GameManager.Instance != null && LevelDataManager.Instance != null)
            {
                var ld = LevelDataManager.Instance.GetLevelData(GameManager.Instance.currentLevel);
                if (ld != null) limit = ld.timeLimit;
            }
            int s = Mathf.CeilToInt(limit);
            timerText.text  = $"{s / 60}:{(s % 60):00}";
            timerText.color = Color.white;
        }
        UpdateGameUI();
        ShowShapeHint();
        StartCoroutine(ShowVehicleStatsCard());
        StartCoroutine(CountdownAndStartTimer());
        ShowOrRefreshOnScreenControls();
        ShowOrRefreshGameHUD();
    }

    // The pause button lives on the HUD panel, which is hidden during terrain-select (before
    // SetupButtons runs), so wire it here once the HUD is active.
    private void WirePauseButton()
    {
        if (pauseButton == null)
        {
            var pb = GameObject.Find("PauseButton");
            if (pb != null) pauseButton = pb.GetComponent<Button>();
        }
        if (pauseButton != null)
        {
            pauseButton.onClick.RemoveAllListeners();
            pauseButton.onClick.AddListener(ShowPauseMenu);
        }
    }

    // Show/hide the on-screen pause ("II") button — hidden on the game-over/level-complete
    // overlays so it doesn't linger over those panels. (Keeps the cached ref so it can re-show.)
    private void SetPauseButtonVisible(bool visible)
    {
        if (pauseButton == null)
        {
            var pb = GameObject.Find("PauseButton");
            if (pb != null) pauseButton = pb.GetComponent<Button>();
        }
        if (pauseButton != null) pauseButton.gameObject.SetActive(visible);
    }

    private void ShowShapeHint()
    {
        if (GameManager.Instance == null) return;
        int level = GameManager.Instance.currentLevel;
        var terrain = GameManager.Instance.currentTerrain;
        var kind = GameManager.KindForLevel(terrain, level);

        bool moving = level >= 12;
        bool busy = level >= 7;
        string hint = $"{kind}  -  {terrain}"
                    + (moving ? "  -  Spot MOVES!" : "")
                    + (busy ? "  -  Busy Lot" : "");

        // Show as a temporary floating banner at bottom
        StartCoroutine(ShowFloatingBanner(hint));
    }

    private IEnumerator ShowFloatingBanner(string text)
    {
        // Create or reuse banner canvas
        var existing = GameObject.Find("_BannerCanvas");
        if (existing != null) Destroy(existing);

        var canvasGO = new GameObject("_BannerCanvas");
        var c = canvasGO.AddComponent<Canvas>();
        c.renderMode = RenderMode.ScreenSpaceOverlay;
        c.sortingOrder = 95;
        var scaler = canvasGO.AddComponent<UnityEngine.UI.CanvasScaler>();
        scaler.uiScaleMode = UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1280f, 720f);
        scaler.matchWidthOrHeight = 0.5f;
        canvasGO.AddComponent<UnityEngine.UI.GraphicRaycaster>();

        var bgGO = new GameObject("Banner");
        bgGO.transform.SetParent(canvasGO.transform, false);
        var img = bgGO.AddComponent<UnityEngine.UI.Image>();
        img.color = new Color(0f, 0f, 0f, 0.6f);
        img.raycastTarget = false;
        var rt = bgGO.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.1f, 0.04f); rt.anchorMax = new Vector2(0.9f, 0.14f);
        rt.offsetMin = rt.offsetMax = Vector2.zero;

        if (levelText == null) { Destroy(canvasGO); yield break; }   // clone source gone (scene swap)
        var lblGO = Instantiate(levelText.gameObject, bgGO.transform, false);
        var lbl = lblGO.GetComponent<TextMeshProUGUI>();
        lbl.text = text;
        lbl.raycastTarget = false;
        lbl.fontSize = 26;
        lbl.color = new Color(1f, 0.9f, 0.4f, 1f);
        lbl.alignment = TMPro.TextAlignmentOptions.Center;
        lbl.fontStyle = TMPro.FontStyles.Normal;
        var lrt = lblGO.GetComponent<RectTransform>();
        lrt.anchorMin = Vector2.zero; lrt.anchorMax = Vector2.one;
        lrt.offsetMin = lrt.offsetMax = Vector2.zero;

        var cg = bgGO.AddComponent<CanvasGroup>();
        // Fade in (guard against this banner being destroyed by the next level's banner)
        float t = 0f;
        while (t < 0.4f) { if (cg == null) yield break; t += Time.deltaTime; cg.alpha = t / 0.4f; yield return null; }
        if (cg == null) yield break;
        cg.alpha = 1f;
        yield return new WaitForSeconds(2.5f);
        // Fade out
        t = 0f;
        while (t < 0.5f) { if (cg == null) yield break; t += Time.deltaTime; cg.alpha = 1f - t / 0.5f; yield return null; }
        if (canvasGO != null) Destroy(canvasGO);
    }

    // Celebratory top banner for hitting a win-streak milestone (every 5th consecutive park).
    public void ShowStreakMilestone(int streak, int bonus)
    {
        StartCoroutine(StreakMilestoneRoutine(streak, bonus));
    }

    // Quick upper-center toast when the player grazes an obstacle without hitting it.
    public void ShowNearMiss(int bonus) => ShowNearMiss(1, bonus);
    public void ShowNearMiss(int combo, int bonus)
    {
        StartCoroutine(NearMissRoutine(combo, bonus));
    }

    private IEnumerator NearMissRoutine(int combo, int bonus)
    {
        var existing = GameObject.Find("_NearMissCanvas");
        if (existing != null) Destroy(existing);

        var canvasGO = new GameObject("_NearMissCanvas");
        var c = canvasGO.AddComponent<Canvas>();
        c.renderMode = RenderMode.ScreenSpaceOverlay;
        c.sortingOrder = 120;
        var scaler = canvasGO.AddComponent<UnityEngine.UI.CanvasScaler>();
        scaler.uiScaleMode = UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1280f, 720f);
        scaler.matchWidthOrHeight = 0.5f;
        canvasGO.AddComponent<UnityEngine.UI.GraphicRaycaster>();

        var rtGO = new GameObject("Label", typeof(RectTransform));
        rtGO.transform.SetParent(canvasGO.transform, false);
        if (levelText == null) { Destroy(canvasGO); yield break; }   // clone source gone (scene swap)
        var lbl = Instantiate(levelText.gameObject, rtGO.transform, false).GetComponent<TextMeshProUGUI>();
        lbl.text = combo > 1 ? $"NEAR MISS x{combo}!  +{bonus}" : $"NEAR MISS!  +{bonus}";
        lbl.fontSize = 30; lbl.raycastTarget = false;
        lbl.color = new Color(1f, 0.55f, 0.15f, 1f);
        lbl.alignment = TMPro.TextAlignmentOptions.Center;
        lbl.fontStyle = TMPro.FontStyles.Bold;
        var lrt = lbl.rectTransform;
        lrt.anchorMin = new Vector2(0.3f, 0.66f); lrt.anchorMax = new Vector2(0.7f, 0.74f);
        lrt.offsetMin = lrt.offsetMax = Vector2.zero;

        float t = 0f;
        while (t < 1f)
        {
            if (lbl == null || lrt == null) yield break;   // canvas destroyed (level reload) - bail safely
            t += Time.deltaTime / 0.9f;
            lrt.anchoredPosition = new Vector2(0f, 30f * t);
            lbl.color = new Color(1f, 0.55f, 0.15f, 1f - t);
            yield return null;
        }
        if (canvasGO != null) Destroy(canvasGO);
    }

    private IEnumerator StreakMilestoneRoutine(int streak, int bonus)
    {
        var existing = GameObject.Find("_StreakCanvas");
        if (existing != null) Destroy(existing);

        var canvasGO = new GameObject("_StreakCanvas");
        var c = canvasGO.AddComponent<Canvas>();
        c.renderMode = RenderMode.ScreenSpaceOverlay;
        c.sortingOrder = 130;
        var scaler = canvasGO.AddComponent<UnityEngine.UI.CanvasScaler>();
        scaler.uiScaleMode = UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1280f, 720f);
        scaler.matchWidthOrHeight = 0.5f;
        canvasGO.AddComponent<UnityEngine.UI.GraphicRaycaster>();

        var bgGO = new GameObject("Banner", typeof(RectTransform));
        bgGO.transform.SetParent(canvasGO.transform, false);
        var img = bgGO.AddComponent<UnityEngine.UI.Image>();
        img.sprite = SpriteGenerator.CreateRoundedRect(360, 100, 0.5f);
        img.color = new Color(0.85f, 0.45f, 0.05f, 0.96f);
        img.raycastTarget = false;
        var rt = bgGO.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.28f, 0.72f); rt.anchorMax = new Vector2(0.72f, 0.82f);
        rt.offsetMin = rt.offsetMax = Vector2.zero;

        if (levelText == null) { Destroy(canvasGO); yield break; }   // clone source gone (scene swap)
        var lblGO = Instantiate(levelText.gameObject, bgGO.transform, false);
        var lbl = lblGO.GetComponent<TextMeshProUGUI>();
        lbl.text = $"{streak} STREAK!   +{bonus} BONUS COINS";
        lbl.raycastTarget = false;
        lbl.fontSize = 30;
        lbl.color = new Color(1f, 0.95f, 0.55f, 1f);
        lbl.alignment = TMPro.TextAlignmentOptions.Center;
        lbl.fontStyle = TMPro.FontStyles.Bold;
        var lrt = lblGO.GetComponent<RectTransform>();
        lrt.anchorMin = Vector2.zero; lrt.anchorMax = Vector2.one;
        lrt.offsetMin = lrt.offsetMax = Vector2.zero;

        AudioManager.Instance?.PlayStarChime(4);

        float t = 0f; rt.localScale = Vector3.zero;
        while (t < 1f) { t += Time.unscaledDeltaTime / 0.3f; rt.localScale = Vector3.one * UIAnimator.EaseOutBack(Mathf.Clamp01(t)); yield return null; }
        rt.localScale = Vector3.one;
        yield return new WaitForSecondsRealtime(2.3f);
        var cg = bgGO.AddComponent<CanvasGroup>();
        t = 0f; while (t < 1f) { t += Time.unscaledDeltaTime / 0.5f; cg.alpha = 1f - Mathf.Clamp01(t); yield return null; }
        if (canvasGO != null) Destroy(canvasGO);
    }

    //   Vehicle stats card  
    // Shows a brief panel (right side of screen) with terrain-appropriate
    // stats derived from the vehicle's actual physics configuration.

    private static readonly Color[] TerrainAccent =
    {
        new Color(0.28f, 0.82f, 0.42f), // Road  - green
        new Color(0.22f, 0.60f, 0.98f), // Water - blue
        new Color(0.28f, 0.86f, 0.96f), // Air   - cyan
        new Color(0.72f, 0.38f, 0.98f), // Space - purple
    };

    private IEnumerator ShowVehicleStatsCard()
    {
        if (_quickRestart) yield break;   // same level/vehicle replay — stats haven't changed
        if (levelText == null) yield break;
        var vc = Object.FindAnyObjectByType<VehicleController>();
        if (vc == null) yield break;

        var stats   = vc.GetDisplayStats();
        var terrain = GameManager.Instance != null ? GameManager.Instance.currentTerrain : GameManager.TerrainType.Road;
        var accent  = TerrainAccent[(int)terrain];

        // Small delay so it doesn't overlap with scene transition
        yield return new WaitForSeconds(0.2f);

        var existing = GameObject.Find("_StatsCard");
        if (existing != null) Destroy(existing);

        var canvasGO = new GameObject("_StatsCard");
        var c = canvasGO.AddComponent<Canvas>();
        c.renderMode = RenderMode.ScreenSpaceOverlay;
        c.sortingOrder = 94;
        var scaler = canvasGO.AddComponent<UnityEngine.UI.CanvasScaler>();
        scaler.uiScaleMode = UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1280f, 720f);
        scaler.matchWidthOrHeight  = 0.5f;
        canvasGO.AddComponent<UnityEngine.UI.GraphicRaycaster>();

        // Card panel - right side, vertically centred
        var cardGO  = new GameObject("Card", typeof(RectTransform));
        cardGO.transform.SetParent(canvasGO.transform, false);
        var cardRT  = cardGO.GetComponent<RectTransform>();
        // Upper-right, clear of the bottom-right GAS buttons (FWD/REV) so it never blocks controls.
        cardRT.anchorMin = new Vector2(0.68f, 0.42f);
        cardRT.anchorMax = new Vector2(0.97f, 0.93f);
        cardRT.offsetMin = cardRT.offsetMax = Vector2.zero;

        var cardImg = cardGO.AddComponent<UnityEngine.UI.Image>();
        cardImg.sprite       = SpriteGenerator.CreateRoundedRect(380, 520, 0.07f);
        cardImg.type         = UnityEngine.UI.Image.Type.Simple;
        cardImg.color        = new Color(0.05f, 0.07f, 0.15f, 0.90f);
        cardImg.raycastTarget = false;

        // Thin accent bar at top of card
        var accentGO  = new GameObject("Accent", typeof(RectTransform));
        accentGO.transform.SetParent(cardGO.transform, false);
        var accentImg = accentGO.AddComponent<UnityEngine.UI.Image>();
        accentImg.color       = accent;
        accentImg.raycastTarget = false;
        var accentRT  = accentGO.GetComponent<RectTransform>();
        accentRT.anchorMin = new Vector2(0.04f, 0.90f); accentRT.anchorMax = new Vector2(0.96f, 0.93f);
        accentRT.offsetMin = accentRT.offsetMax = Vector2.zero;

        // Vehicle name header
        MakeCardText(cardGO.transform, stats.vehicleName.ToUpperInvariant(),
            new Vector2(0.05f, 0.82f), new Vector2(0.95f, 0.93f), 26, Color.white, true);

        // Terrain label (smaller, accent colour)
        MakeCardText(cardGO.transform, terrain.ToString().ToUpperInvariant(),
            new Vector2(0.05f, 0.73f), new Vector2(0.95f, 0.83f), 16,
            new Color(accent.r, accent.g, accent.b, 0.80f), false);

        // 4 stat rows
        float rowH  = 0.125f;
        float rowY0 = 0.60f;
        for (int i = 0; i < stats.rows.Length; i++)
        {
            var row   = stats.rows[i];
            float bot = rowY0 - i * (rowH + 0.015f);
            float top = bot + rowH;

            // Row label
            MakeCardText(cardGO.transform, row.label,
                new Vector2(0.05f, bot + rowH * 0.46f), new Vector2(0.95f, top),
                13, new Color(0.75f, 0.82f, 0.96f, 1f), false);

            // Bar track
            var trackGO  = new GameObject("Track", typeof(RectTransform));
            trackGO.transform.SetParent(cardGO.transform, false);
            var trackImg = trackGO.AddComponent<UnityEngine.UI.Image>();
            trackImg.color       = new Color(1f, 1f, 1f, 0.10f);
            trackImg.raycastTarget = false;
            var trackRT  = trackGO.GetComponent<RectTransform>();
            trackRT.anchorMin = new Vector2(0.05f, bot);
            trackRT.anchorMax = new Vector2(0.95f, bot + rowH * 0.42f);
            trackRT.offsetMin = trackRT.offsetMax = Vector2.zero;

            // Bar fill
            float fillFrac = Mathf.Clamp01(row.bar);
            if (fillFrac > 0.01f)
            {
                var fillGO  = new GameObject("Fill", typeof(RectTransform));
                fillGO.transform.SetParent(cardGO.transform, false);
                var fillImg = fillGO.AddComponent<UnityEngine.UI.Image>();
                // Colour: accent at full, desaturated at low
                fillImg.color       = Color.Lerp(new Color(accent.r, accent.g, accent.b, 0.45f),
                                                 new Color(accent.r, accent.g, accent.b, 0.95f), fillFrac);
                fillImg.raycastTarget = false;
                var fillRT  = fillGO.GetComponent<RectTransform>();
                fillRT.anchorMin = new Vector2(0.05f, bot);
                fillRT.anchorMax = new Vector2(0.05f + 0.90f * fillFrac, bot + rowH * 0.42f);
                fillRT.offsetMin = fillRT.offsetMax = Vector2.zero;
            }

            // Grade label (right-aligned)
            MakeCardText(cardGO.transform, row.grade,
                new Vector2(0.05f, bot), new Vector2(0.95f, bot + rowH * 0.44f),
                11, new Color(accent.r, accent.g, accent.b, 0.85f), false,
                TMPro.TextAlignmentOptions.Right);
        }

        // Traits (bottom section)
        float traitY = rowY0 - stats.rows.Length * (rowH + 0.015f) - 0.02f;
        for (int i = 0; i < Mathf.Min(stats.traits.Length, 3); i++)
        {
            float tBot = traitY - i * 0.095f;
            MakeCardText(cardGO.transform, "* " + stats.traits[i],
                new Vector2(0.04f, tBot - 0.09f), new Vector2(0.96f, tBot),
                11, new Color(1f, 0.85f, 0.45f, 0.88f), false,
                TMPro.TextAlignmentOptions.Left);
        }

        // Canvas group for fade
        var cg = canvasGO.AddComponent<CanvasGroup>();
        cg.alpha = 0f; cg.blocksRaycasts = false;

        float t = 0f;
        while (t < 0.35f)
        {
            if (cg == null) yield break;
            t += Time.deltaTime; cg.alpha = t / 0.35f; yield return null;
        }
        yield return new WaitForSeconds(3.0f);   // clears around when the countdown ends / play begins
        t = 0f;
        while (t < 0.55f)
        {
            if (cg == null) yield break;
            t += Time.deltaTime; cg.alpha = 1f - t / 0.55f; yield return null;
        }
        if (canvasGO != null) Destroy(canvasGO);
    }

    private void MakeCardText(Transform parent, string text,
        Vector2 anchorMin, Vector2 anchorMax, float fontSize, Color color,
        bool bold, TMPro.TextAlignmentOptions align = TMPro.TextAlignmentOptions.Left)
    {
        if (levelText == null) return;
        var go = Instantiate(levelText.gameObject, parent, false);
        var tmp = go.GetComponent<TextMeshProUGUI>();
        tmp.text           = text;
        tmp.fontSize       = fontSize;
        tmp.color          = color;
        tmp.fontStyle      = bold ? TMPro.FontStyles.Bold : TMPro.FontStyles.Normal;
        tmp.alignment      = align;
        tmp.raycastTarget  = false;
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = anchorMin; rt.anchorMax = anchorMax;
        rt.offsetMin = rt.offsetMax = Vector2.zero;
    }

    // Creates 3 animated star images directly on the level complete panel.
    // Called when StarRating component isn't wired (all-procedural build).
    private IEnumerator ShowProceduralStars(int count, Transform panel)
    {
        if (panel == null) yield break;

        // Remove any previously-spawned star row
        var oldRow = panel.Find("_StarRow");
        if (oldRow != null) Destroy(oldRow.gameObject);

        var rowGO = new GameObject("_StarRow", typeof(RectTransform));
        rowGO.transform.SetParent(panel, false);
        var rowRT = rowGO.GetComponent<RectTransform>();
        // Top band of the panel, ABOVE the "Level Complete" title (timer pill hidden), so stars never overlap text.
        rowRT.anchorMin = new Vector2(0.14f, 0.79f); rowRT.anchorMax = new Vector2(0.86f, 0.91f);
        rowRT.offsetMin = rowRT.offsetMax = Vector2.zero;

        var emptyCol  = new Color(0.28f, 0.28f, 0.28f, 0.55f);
        var filledCol = new Color(1f, 0.85f, 0.10f, 1f);
        var glowCol   = new Color(1f, 1f,    0.50f, 1f);

        var sprFilled = SpriteGenerator.CreateStarPolygon(5, 0.45f, 80, filledCol);
        var sprEmpty  = SpriteGenerator.CreateStarPolygon(5, 0.45f, 80, emptyCol);

        // 3 stars evenly spaced
        float[] anchorX = { 0.12f, 0.44f, 0.76f };
        var starImgs = new UnityEngine.UI.Image[3];

        for (int i = 0; i < 3; i++)
        {
            var sGO = new GameObject($"Star{i}", typeof(RectTransform));
            sGO.transform.SetParent(rowGO.transform, false);
            var img = sGO.AddComponent<UnityEngine.UI.Image>();
            img.sprite = sprEmpty;
            img.raycastTarget = false;
            var sRT = sGO.GetComponent<RectTransform>();
            sRT.anchorMin = new Vector2(anchorX[i], 0f);
            sRT.anchorMax = new Vector2(anchorX[i] + 0.28f, 1f);
            sRT.offsetMin = sRT.offsetMax = Vector2.zero;
            sRT.localScale = Vector3.zero;
            starImgs[i] = img;
        }

        yield return new WaitForSecondsRealtime(0.15f);

        // Pop each filled star in sequence
        for (int i = 0; i < 3; i++)
        {
            bool filled = i < count;
            var img = starImgs[i];
            if (img == null) continue;

            img.sprite = filled ? sprFilled : sprEmpty;
            var sRT = img.GetComponent<RectTransform>();

            if (filled) AudioManager.Instance?.PlayStarChime(i);

            float t = 0f;
            while (t < 1f)
            {
                t += Time.unscaledDeltaTime / 0.28f;
                sRT.localScale = Vector3.one * UIAnimator.EaseOutBack(Mathf.Clamp01(t));
                img.color = Color.Lerp(emptyCol, filled ? filledCol : emptyCol, Mathf.Clamp01(t));
                yield return null;
            }
            sRT.localScale = Vector3.one;

            if (filled)
            {
                // Brief glow flash
                t = 0f;
                while (t < 1f) { t += Time.unscaledDeltaTime / 0.20f; img.color = Color.Lerp(filledCol, glowCol, Mathf.Sin(t * Mathf.PI)); yield return null; }
                img.color = filledCol;
            }

            if (i < 2) yield return new WaitForSecondsRealtime(0.15f);
        }

        // 3-star payoff: a "PERFECT!" flourish + a synchronized pulse of all three stars.
        if (count >= 3)
        {
            var perfGO = new GameObject("_PerfectLabel", typeof(RectTransform));
            perfGO.transform.SetParent(rowGO.transform, false);
            var perfRT = perfGO.GetComponent<RectTransform>();
            // ABOVE the star row at the very top of the panel - clear of the stars and the title.
            perfRT.anchorMin = new Vector2(0.1f, 1.05f); perfRT.anchorMax = new Vector2(0.9f, 1.75f);
            perfRT.offsetMin = perfRT.offsetMax = Vector2.zero;
            var perfTMP = CloneTMP(perfGO, "PERFECT!", 40, glowCol);
            perfTMP.fontStyle = TMPro.FontStyles.Bold;
            perfTMP.raycastTarget = false;
            perfRT.localScale = Vector3.zero;

            AudioManager.Instance?.PlayStarChime(4);

            float pt = 0f;
            while (pt < 1f)
            {
                pt += Time.unscaledDeltaTime / 0.35f;
                float e = UIAnimator.EaseOutBack(Mathf.Clamp01(pt));
                perfRT.localScale = Vector3.one * e;
                float pulse = 1f + 0.12f * Mathf.Sin(pt * Mathf.PI * 2f);
                for (int s = 0; s < 3; s++)
                    if (starImgs[s] != null) starImgs[s].GetComponent<RectTransform>().localScale = Vector3.one * pulse;
                yield return null;
            }
            perfRT.localScale = Vector3.one;
            for (int s = 0; s < 3; s++)
                if (starImgs[s] != null) starImgs[s].GetComponent<RectTransform>().localScale = Vector3.one;
        }
    }

    private void ShowOrRefreshOnScreenControls()
    {
        if (levelText == null) return;
        if (OnScreenControls.Instance == null)
        {
            var go = new GameObject("OnScreenControls");
            go.AddComponent<OnScreenControls>();
        }
        OnScreenControls.Instance?.Show(levelText);
    }

    private void ShowOrRefreshGameHUD()
    {
        if (levelText == null) return;
        if (GameHUD.Instance == null)
        {
            var go = new GameObject("GameHUD");
            go.AddComponent<GameHUD>();
        }
        GameHUD.Instance?.Show(levelText);

        // Create the confetti canvas once per session so BurstCenter() works on level complete.
        if (ParticleCanvas.Instance == null)
        {
            var pcGO = new GameObject("_ParticleCanvas");
            DontDestroyOnLoad(pcGO);
            var canvas = pcGO.AddComponent<Canvas>();
            canvas.renderMode  = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 120;
            var scaler = pcGO.AddComponent<UnityEngine.UI.CanvasScaler>();
            scaler.uiScaleMode         = UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1280f, 720f);
            scaler.matchWidthOrHeight  = 0.5f;
            pcGO.AddComponent<UnityEngine.UI.GraphicRaycaster>();
            pcGO.AddComponent<ParticleCanvas>();
        }
    }

    // Juicy "PARKED!" flash the instant the player succeeds - before the results panel.
    private void ShowParkedFlash()
    {
        if (levelText == null) return;
        StartCoroutine(ParkedFlashRoutine());
    }

    private IEnumerator ParkedFlashRoutine()
    {
        var canvasGO = new GameObject("_ParkedFlash");
        var c = canvasGO.AddComponent<Canvas>();
        c.renderMode = RenderMode.ScreenSpaceOverlay;
        c.sortingOrder = 150;
        var scaler = canvasGO.AddComponent<UnityEngine.UI.CanvasScaler>();
        scaler.uiScaleMode = UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1280f, 720f);
        scaler.matchWidthOrHeight = 0.5f;

        // Green screen flash
        var flashGO = new GameObject("Flash", typeof(RectTransform));
        flashGO.transform.SetParent(canvasGO.transform, false);
        var flashImg = flashGO.AddComponent<UnityEngine.UI.Image>();
        flashImg.color = new Color(0.3f, 1f, 0.5f, 0f);
        flashImg.raycastTarget = false;
        var frt = flashGO.GetComponent<RectTransform>();
        frt.anchorMin = Vector2.zero; frt.anchorMax = Vector2.one; frt.offsetMin = frt.offsetMax = Vector2.zero;

        // "PARKED!" text
        var clone = Instantiate(levelText.gameObject, canvasGO.transform, false);
        clone.SetActive(true);
        var tmp = clone.GetComponent<TextMeshProUGUI>();
        int streak = ScoreManager.Instance != null ? ScoreManager.Instance.WinStreak : 0;
        int coins  = ScoreManager.Instance != null ? ScoreManager.Instance.LastCoinsEarned : 0;
        string streakLine = streak >= 2 ? $"\n<size=55%>{streak} IN A ROW!</size>" : "";
        string coinLine   = coins  >  0 ? $"\n<size=42%><color=#FFD23C>+{coins} COINS</color></size>" : "";
        tmp.text = "PARKED!" + streakLine + coinLine;
        tmp.fontSize = 96;
        tmp.fontStyle = TMPro.FontStyles.Bold;
        tmp.alignment = TMPro.TextAlignmentOptions.Center;
        tmp.color = new Color(0.4f, 1f, 0.55f);
        tmp.raycastTarget = false;
        var trt = clone.GetComponent<RectTransform>();
        trt.anchorMin = new Vector2(0.15f, 0.42f); trt.anchorMax = new Vector2(0.85f, 0.62f);
        trt.offsetMin = trt.offsetMax = Vector2.zero;
        trt.localScale = Vector3.zero;

        // Pop in with overshoot + flash
        float t = 0f;
        while (t < 0.22f)
        {
            t += Time.unscaledDeltaTime;
            float k = Mathf.Clamp01(t / 0.22f);
            trt.localScale = Vector3.one * (1.25f * UIAnimator.EaseOutCubic(k));
            flashImg.color = new Color(0.3f, 1f, 0.5f, 0.28f * k);
            yield return null;
        }
        // Settle to 1.0 + fade flash
        t = 0f;
        while (t < 0.18f)
        {
            t += Time.unscaledDeltaTime;
            float k = Mathf.Clamp01(t / 0.18f);
            trt.localScale = Vector3.one * Mathf.Lerp(1.25f, 1f, k);
            flashImg.color = new Color(0.3f, 1f, 0.5f, 0.28f * (1f - k));
            yield return null;
        }
        yield return new WaitForSecondsRealtime(0.5f);
        // Fade text out
        t = 0f;
        while (t < 0.35f)
        {
            t += Time.unscaledDeltaTime;
            float a = 1f - Mathf.Clamp01(t / 0.35f);
            tmp.color = new Color(0.4f, 1f, 0.55f, a);
            yield return null;
        }
        Destroy(canvasGO);
    }

    // True once this level has ended (win OR loss) — gates grace-period/game-over/pause races.
    private bool levelEnded;

    public void ShowLevelComplete()
    {
        if (levelEnded) return;   // already resolved (e.g. crashed) — don't stack a win on a loss
        levelEnded = true;
        StopTimer();
        OnScreenControls.ReleaseAll();
        OnScreenControls.Instance?.SetVisible(false);
        GameHUD.Instance?.Hide();
        // Hide the timer pill + pause button so they don't sit over the level-complete panel.
        var timerCanvas = GameObject.Find("_TimerCanvas");
        if (timerCanvas != null) timerCanvas.SetActive(false);
        SetPauseButtonVisible(false);
        ShowParkedFlash();
        if (levelCompletePanel == null) return;

        int finalScore = ScoreManager.Instance != null ? ScoreManager.Instance.CalculateFinalScore() : 0;
        int level      = GameManager.Instance != null ? GameManager.Instance.currentLevel : 1;
        int target     = LevelDataManager.Instance != null
                       ? (LevelDataManager.Instance.GetLevelData(level)?.targetScore ?? 1000) : 1000;
        int prevBest   = LevelDataManager.Instance != null ? LevelDataManager.Instance.GetLevelScore(level) : 0;
        int stars      = StarRating.ScoreToStars(finalScore, target);

        levelCompletePanel.SetActive(true);
        levelCompleteCG.alpha = 0f;
        StartCoroutine(AnimateLevelComplete(finalScore, prevBest, stars));

        if (ParticleCanvas.Instance != null)
        {
            ParticleCanvas.Instance.BurstCenter();

            // Extra burst originating at the parking zone the player just earned, so the
            // payoff reads as coming FROM the spot. All guards additive: on any miss the
            // center burst above still plays. WorldToScreenPoint pixels match ParticleCanvas's
            // ScreenSpaceOverlay seeding (same space as BurstCenter's raw Screen coords).
            var zoneGO = GameObject.Find("ParkingZone");
            var cam = Camera.main;
            if (zoneGO != null && cam != null)
            {
                Vector3 sp = cam.WorldToScreenPoint(zoneGO.transform.position);
                if (sp.z > 0f && sp.x >= 0f && sp.x <= Screen.width && sp.y >= 0f && sp.y <= Screen.height)
                    ParticleCanvas.Instance.Burst(new Vector2(sp.x, sp.y));
            }
        }
        AudioManager.Instance?.PlayLevelCompleteSound();

        // Cosmetic coin fly-to-counter juice. Coins are already awarded in the economy
        // (ScoreManager.AddCoins); this only animates ASCII markers toward the top-right
        // corner where the coin total is shown on the menu. Count derived from the reward,
        // clamped so we never spawn an unbounded number of markers.
        int coinFx = ScoreManager.Instance != null ? ScoreManager.Instance.LastCoinsEarned : 0;
        StartCoroutine(CoinFlyToCounter(Mathf.Clamp(coinFx > 0 ? coinFx : 6, 6, 14)));
    }

    // Pure cosmetic: fling N small ASCII coin markers from the level-complete panel toward
    // the top-right coin-counter position. Does NOT change the coin economy. Fully null-guarded
    // so tapping through fast (which destroys the panel / swaps scene) can't NRE.
    private IEnumerator CoinFlyToCounter(int count)
    {
        if (levelText == null || levelCompletePanel == null) yield break;   // clone source / origin gone

        var existing = GameObject.Find("_CoinFxCanvas");
        if (existing != null) Destroy(existing);

        var canvasGO = new GameObject("_CoinFxCanvas");
        var c = canvasGO.AddComponent<Canvas>();
        c.renderMode = RenderMode.ScreenSpaceOverlay;
        c.sortingOrder = 96;                       // above the banner, no raycaster so no tap block
        var scaler = canvasGO.AddComponent<UnityEngine.UI.CanvasScaler>();
        scaler.uiScaleMode = UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1280f, 720f);
        scaler.matchWidthOrHeight = 0.5f;
        // NOTE: no GraphicRaycaster - this canvas must never intercept taps on the button stack.

        Vector2 origin = new Vector2(Screen.width * 0.5f, Screen.height * 0.5f);
        var panelRT = levelCompletePanel.GetComponent<RectTransform>();
        if (panelRT != null) { Vector3 wp = panelRT.position; origin = new Vector2(wp.x, wp.y); }
        Vector2 target = new Vector2(Screen.width * 0.92f, Screen.height * 0.94f);

        var markers = new System.Collections.Generic.List<RectTransform>(count);
        var starts  = new System.Collections.Generic.List<Vector2>(count);
        for (int i = 0; i < count; i++)
        {
            if (levelText == null) { if (canvasGO != null) Destroy(canvasGO); yield break; }
            var mGO = Instantiate(levelText.gameObject, canvasGO.transform, false);
            var lbl = mGO.GetComponent<TextMeshProUGUI>();
            lbl.text = (i % 2 == 0) ? "$" : "+";     // ASCII-only
            lbl.raycastTarget = false;               // never block the button stack
            lbl.fontSize = 30;
            lbl.color = new Color(1f, 0.83f, 0.24f, 1f);
            lbl.alignment = TMPro.TextAlignmentOptions.Center;
            lbl.fontStyle = TMPro.FontStyles.Bold;
            var rt = mGO.GetComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = new Vector2(0f, 0f);   // anchor bottom-left = raw px
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(48f, 48f);
            rt.anchoredPosition = origin + new Vector2(Random.Range(-60f, 60f), Random.Range(-40f, 40f));
            markers.Add(rt);
            starts.Add(rt.anchoredPosition);
        }

        const float flight = 0.6f;
        const float stagger = 0.05f;
        float total = flight + stagger * count + 0.1f;
        float elapsed = 0f;
        while (elapsed < total)
        {
            if (canvasGO == null) yield break;                    // canvas destroyed mid-tween
            for (int i = 0; i < markers.Count; i++)
            {
                var rt = markers[i];
                if (rt == null) continue;                          // this marker was destroyed
                float local = Mathf.Clamp01((elapsed - i * stagger) / flight);
                float e = 1f - (1f - local) * (1f - local);        // ease-out quad
                rt.anchoredPosition = Vector2.LerpUnclamped(starts[i], target, e);
                float s = Mathf.Lerp(1f, 0.5f, e);
                rt.localScale = new Vector3(s, s, 1f);
                var g = rt.GetComponent<TextMeshProUGUI>();
                if (g != null) { var col = g.color; col.a = 1f - Mathf.Clamp01((local - 0.7f) / 0.3f); g.color = col; }
            }
            elapsed += Time.unscaledDeltaTime;
            yield return null;
        }
        if (canvasGO != null) Destroy(canvasGO);
    }

    public void ShowGameOver() => ShowGameOver(null);

    // cause: what ended the run - e.g. "Crashed into a bus" or "Time ran out!" (null = generic).
    public void ShowGameOver(string cause)
    {
        if (levelEnded) return;   // level already resolved (e.g. parked during grace) — don't stack panels
        levelEnded = true;
        StopTimer();
        // Read progress stats BEFORE resetting the streak so the panel can show them.
        int bestStreak = ScoreManager.Instance != null ? ScoreManager.Instance.BestStreak : 0;
        int coins      = ScoreManager.Instance != null ? ScoreManager.Instance.Coins : 0;
        bool newBest   = ScoreManager.Instance != null && ScoreManager.Instance.NewBestThisRun;
        ScoreManager.Instance?.ResetStreak();   // failing breaks the win streak
        OnScreenControls.ReleaseAll();
        OnScreenControls.Instance?.SetVisible(false);
        GameHUD.Instance?.Hide();
        // Hide the timer pill + pause button so they don't linger behind the GAME OVER text.
        var timerCanvas = GameObject.Find("_TimerCanvas");
        if (timerCanvas != null) timerCanvas.SetActive(false);
        SetPauseButtonVisible(false);
        if (gameOverPanel == null) return;
        gameOverPanel.SetActive(true);
        gameOverCG.alpha = 0f;
        AudioManager.Instance?.PlayGameOverSound();
        ShowGameOverCause(cause);
        ShowGameOverStats(bestStreak, coins, newBest);
        ShowGameOverTip();
        StartCoroutine(AnimatePanel(gameOverCG, true, 0.3f));
        StartCoroutine(UIAnimator.Shake(gameOverPanel.GetComponent<RectTransform>(), 18f, 0.4f));
    }

    private TextMeshProUGUI _gameOverCauseLabel;
    // Shows what ended the run, just under the "GAME OVER" title.
    private void ShowGameOverCause(string cause)
    {
        if (gameOverPanel == null) return;
        if (_gameOverCauseLabel == null || _gameOverCauseLabel.transform.parent != gameOverPanel.transform)
        {
            var rt = NewRect("_GameOverCause", gameOverPanel.transform);
            rt.anchorMin = new Vector2(0.5f, 0.5f); rt.anchorMax = new Vector2(0.5f, 0.5f); rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = new Vector2(0f, 150f); rt.sizeDelta = new Vector2(640f, 40f);
            _gameOverCauseLabel = CloneTMP(rt.gameObject, "", 24, new Color(1f, 0.62f, 0.55f));
            _gameOverCauseLabel.alignment = TMPro.TextAlignmentOptions.Center;
            _gameOverCauseLabel.fontStyle = TMPro.FontStyles.Bold;
            _gameOverCauseLabel.raycastTarget = false;
            var lrt = _gameOverCauseLabel.rectTransform; lrt.anchorMin = Vector2.zero; lrt.anchorMax = Vector2.one; lrt.offsetMin = lrt.offsetMax = Vector2.zero;
        }
        _gameOverCauseLabel.text = string.IsNullOrEmpty(cause) ? "" : cause;
    }

    private TextMeshProUGUI _gameOverStatsLabel;
    // Shows persistent progress (best streak + coins) on the game-over panel so failing still feels like progress.
    private void ShowGameOverStats(int bestStreak, int coins, bool newBest = false)
    {
        if (gameOverPanel == null) return;
        if (_gameOverStatsLabel == null || _gameOverStatsLabel.transform.parent != gameOverPanel.transform)
        {
            var rt = NewRect("_GameOverStats", gameOverPanel.transform);
            rt.anchorMin = new Vector2(0.5f, 0.5f); rt.anchorMax = new Vector2(0.5f, 0.5f); rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = new Vector2(0f, -205f); rt.sizeDelta = new Vector2(560f, 44f);
            _gameOverStatsLabel = CloneTMP(rt.gameObject, "", 22, new Color(1f, 0.9f, 0.5f));
            _gameOverStatsLabel.alignment = TMPro.TextAlignmentOptions.Center;
            _gameOverStatsLabel.raycastTarget = false;
            var lrt = _gameOverStatsLabel.rectTransform; lrt.anchorMin = Vector2.zero; lrt.anchorMax = Vector2.one; lrt.offsetMin = lrt.offsetMax = Vector2.zero;
        }
        if (newBest)
        {
            // Celebrate: this run set a new all-time best streak.
            _gameOverStatsLabel.text  = $"<color=#5BE07A>NEW BEST STREAK: {bestStreak}!</color>    Coins: {coins:N0}";
            _gameOverStatsLabel.fontStyle = TMPro.FontStyles.Bold;
            StartCoroutine(UIAnimator.Punch(_gameOverStatsLabel.GetComponent<RectTransform>(), 1.18f, 0.5f));
        }
        else
        {
            _gameOverStatsLabel.text  = $"Best streak: {bestStreak}    Coins: {coins:N0}";
            _gameOverStatsLabel.fontStyle = TMPro.FontStyles.Normal;
        }
    }

    private static readonly string[] GameOverTips =
    {
        "Tip: Ease off before the spot - parking too fast fails the park.",
        "Tip: Line up your angle early; small corrections beat hard turns.",
        "Tip: Grab the glowing time tokens on the way to the spot.",
        "Tip: Watch the ANGLE, CENTER and SPEED bars - all must be green.",
        "Tip: Reverse is your friend for tight final adjustments.",
        "Tip: Keep a win streak going for bonus coins every 5 parks.",
    };
    private TextMeshProUGUI _gameOverTipLabel;
    // Rotating encouraging gameplay tip on the game-over panel.
    private void ShowGameOverTip()
    {
        if (gameOverPanel == null) return;
        if (_gameOverTipLabel == null || _gameOverTipLabel.transform.parent != gameOverPanel.transform)
        {
            var rt = NewRect("_GameOverTip", gameOverPanel.transform);
            rt.anchorMin = new Vector2(0.5f, 0.5f); rt.anchorMax = new Vector2(0.5f, 0.5f); rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = new Vector2(0f, -255f); rt.sizeDelta = new Vector2(760f, 40f);
            _gameOverTipLabel = CloneTMP(rt.gameObject, "", 19, new Color(0.72f, 0.80f, 0.92f));
            _gameOverTipLabel.alignment = TMPro.TextAlignmentOptions.Center;
            _gameOverTipLabel.raycastTarget = false;
            var lrt = _gameOverTipLabel.rectTransform; lrt.anchorMin = Vector2.zero; lrt.anchorMax = Vector2.one; lrt.offsetMin = lrt.offsetMax = Vector2.zero;
        }
        _gameOverTipLabel.text = GameOverTips[Random.Range(0, GameOverTips.Length)];
    }

    public void ShowPauseMenu()
    {
        if (pauseMenuPanel == null) return;
        if (levelEnded) return;   // don't stack pause over game-over/level-complete
        OnScreenControls.ReleaseAll();
        OnScreenControls.Instance?.SetVisible(false);
        pauseMenuPanel.SetActive(true);
        pauseCG.alpha = 0f;
        pauseCG.interactable = true;
        pauseCG.blocksRaycasts = true;
        StartCoroutine(AnimatePanel(pauseCG, true, 0.2f));
        var rt = pauseMenuPanel.GetComponent<RectTransform>();
        if (rt != null) StartCoroutine(UIAnimator.PopIn(rt, 0.25f));
        EnsurePauseMuteButton();
        EnsurePauseBestTime();
        AudioManager.Instance?.SetGamePaused(true);   // freeze engine/ambient with the game
        Time.timeScale = 0f;
    }

    private TextMeshProUGUI _pauseBestTimeLabel;
    // Shows the player's best completion time for the CURRENT terrain on the pause panel.
    private void EnsurePauseBestTime()
    {
        if (pauseMenuPanel == null) return;
        if (_pauseBestTimeLabel == null || _pauseBestTimeLabel.transform.parent != pauseMenuPanel.transform)
        {
            var rt = NewRect("_PauseBestTime", pauseMenuPanel.transform);
            rt.anchorMin = new Vector2(0.5f, 0.5f); rt.anchorMax = new Vector2(0.5f, 0.5f); rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = new Vector2(0f, -285f); rt.sizeDelta = new Vector2(520f, 44f);
            _pauseBestTimeLabel = CloneTMP(rt.gameObject, "", 22, new Color(0.80f, 0.86f, 1f));
            _pauseBestTimeLabel.alignment = TMPro.TextAlignmentOptions.Center;
            _pauseBestTimeLabel.raycastTarget = false;
            var lrt = _pauseBestTimeLabel.rectTransform; lrt.anchorMin = Vector2.zero; lrt.anchorMax = Vector2.one; lrt.offsetMin = lrt.offsetMax = Vector2.zero;
        }
        var terr = GameManager.Instance != null ? GameManager.Instance.currentTerrain : GameManager.TerrainType.Road;
        float best = PlayerPrefs.GetFloat($"BestTime_{terr}", -1f);
        _pauseBestTimeLabel.text = best > 0f ? $"{terr} best: {best:0.0}s" : $"{terr} best: -";
    }

    private TextMeshProUGUI _pauseMuteLabel;
    private string PauseMuteText() => (AudioManager.Instance != null && AudioManager.Instance.Muted) ? "SOUND: OFF" : "SOUND: ON";
    // Lazily adds a sound toggle to the pause panel so players can mute mid-run.
    private void EnsurePauseMuteButton()
    {
        if (pauseMenuPanel == null) return;
        if (pauseMenuPanel.transform.Find("_PauseMuteButton") == null)
        {
            var mb = NewRect("_PauseMuteButton", pauseMenuPanel.transform);
            var mimg = mb.gameObject.AddComponent<Image>();
            mimg.sprite = SpriteGenerator.CreateRoundedRect(220, 90, 0.5f);
            mimg.type = Image.Type.Sliced; mimg.color = new Color(0.20f, 0.24f, 0.34f);
            // Top-right corner of the panel, clear of the centered RESUME/RESTART/MAIN MENU stack
            // (previously it sat just below MAIN MENU and could overlap its tap area -> "can't reach Main Menu").
            mb.anchorMin = new Vector2(0.70f, 0.85f); mb.anchorMax = new Vector2(0.985f, 0.95f);
            mb.pivot = new Vector2(0.5f, 0.5f);
            mb.offsetMin = mb.offsetMax = Vector2.zero;
            // Speaker icon (left) + label
            var icoRt = NewRect("Icon", mb);
            _pauseMuteIcon = icoRt.gameObject.AddComponent<Image>();
            _pauseMuteIcon.sprite = SpriteGenerator.CreateSpeakerIcon(64, AudioManager.Instance != null && AudioManager.Instance.Muted);
            _pauseMuteIcon.raycastTarget = false;
            icoRt.anchorMin = new Vector2(0.08f, 0.2f); icoRt.anchorMax = new Vector2(0.30f, 0.8f); icoRt.offsetMin = icoRt.offsetMax = Vector2.zero;
            var lblRt = NewRect("Label", mb);
            _pauseMuteLabel = CloneTMP(lblRt.gameObject, PauseMuteText(), 20, Color.white);
            lblRt.anchorMin = new Vector2(0.30f, 0f); lblRt.anchorMax = Vector2.one; lblRt.offsetMin = lblRt.offsetMax = Vector2.zero;
            var mbtn = mb.gameObject.AddComponent<Button>();
            mbtn.onClick.AddListener(TogglePauseMute);
        }
        else
        {
            RefreshPauseMuteVisual();
        }
    }
    private Image _pauseMuteIcon;
    private void RefreshPauseMuteVisual()
    {
        if (_pauseMuteLabel != null) _pauseMuteLabel.text = PauseMuteText();
        if (_pauseMuteIcon != null)
            _pauseMuteIcon.sprite = SpriteGenerator.CreateSpeakerIcon(64, AudioManager.Instance != null && AudioManager.Instance.Muted);
    }
    private void TogglePauseMute()
    {
        AudioManager.Instance?.ToggleMute();
        RefreshPauseMuteVisual();
        AudioManager.Instance?.PlayButtonClickSound();
    }

    public void HidePauseMenu()
    {
        if (pauseMenuPanel == null) return;
        StartCoroutine(ClosePause());
    }

    private IEnumerator ClosePause()
    {
        yield return StartCoroutine(AnimatePanel(pauseCG, false, 0.18f));
        pauseMenuPanel.SetActive(false);
        Time.timeScale = 1f;
        AudioManager.Instance?.SetGamePaused(false);   // resume engine/ambient
        OnScreenControls.Instance?.SetVisible(true);
    }

    public void ShowAchievementUnlocked(ScoreManager.Achievement achievement)
    {
        if (achievementToastPanel == null) return;
        if (achievementToastText != null)
            achievementToastText.text = $"Achievement Unlocked!\n<size=90%>{achievement.name}</size>";

        if (toastRoutine != null) StopCoroutine(toastRoutine);
        toastRoutine = StartCoroutine(ToastRoutine());
    }

    private IEnumerator ToastRoutine()
    {
        achievementToastPanel.SetActive(true);
        toastCG.alpha = 0f;
        // Slide in from bottom
        if (toastRect != null)
        {
            Vector2 target = toastRect.anchoredPosition;
            toastRect.anchoredPosition = target + Vector2.down * 100f;
            StartCoroutine(UIAnimator.SlideIn(toastRect, Vector2.down * 100f, 0.3f));
        }
        yield return StartCoroutine(UIAnimator.FadeIn(toastCG, 0.25f));
        yield return new WaitForSeconds(3f);
        yield return StartCoroutine(UIAnimator.FadeOut(toastCG, 0.3f, deactivate: false));
        achievementToastPanel.SetActive(false);
    }

    //   Animation sequences  

    private IEnumerator AnimateLevelComplete(int score, int prevBest, int stars)
    {
        yield return StartCoroutine(AnimatePanel(levelCompleteCG, true, 0.3f));

        // Level label slides in
        if (levelCompleteText != null)
        {
            var rt = levelCompleteText.GetComponent<RectTransform>();
            StartCoroutine(UIAnimator.SlideIn(rt, Vector2.up * 60f, 0.4f));
            StartCoroutine(UIAnimator.Punch(rt, 1.15f, 0.4f));
        }

        yield return new WaitForSecondsRealtime(0.35f);

        // Stars: use wired StarRating if available, otherwise procedural
        if (starRating != null)
            starRating.Show(stars);
        else
            yield return StartCoroutine(ShowProceduralStars(stars, levelCompletePanel.transform));

        yield return new WaitForSecondsRealtime(0.8f);

        // Animated score counter
        if (scoreText != null)
            yield return StartCoroutine(UIAnimator.CountUp(scoreText, 0, score, 1.2f, "Score: "));

        // Best badge
        if (bestText != null)
        {
            if (score > prevBest)
            {
                bestText.text = "NEW BEST!";
                bestText.color = new Color(1f, 0.85f, 0.1f);
                StartCoroutine(UIAnimator.Punch(bestText.GetComponent<RectTransform>(), 1.2f, 0.5f));
            }
            else
            {
                bestText.text = $"Best: {prevBest:N0}";
                bestText.color = Color.white;
            }
        }

        // Time display
        if (timeText != null)
        {
            int secs = Mathf.RoundToInt(currentTime);
            timeText.text = $"Time: {secs / 60}:{(secs % 60):00}";
        }

        // Buttons pop in
        nextLevelButton?.GetComponent<RectTransform>()?.let(rt =>
            StartCoroutine(UIAnimator.PopIn(rt, 0.3f, delay: 0.1f)));
        restartButton?.GetComponent<RectTransform>()?.let(rt =>
            StartCoroutine(UIAnimator.PopIn(rt, 0.3f, delay: 0.2f)));
    }

    private IEnumerator AnimatePanel(CanvasGroup cg, bool fadeIn, float duration)
    {
        if (fadeIn)
        {
            cg.interactable = true;
            cg.blocksRaycasts = true;
            yield return StartCoroutine(UIAnimator.FadeIn(cg, duration));
        }
        else
        {
            cg.interactable = false;
            cg.blocksRaycasts = false;
            yield return StartCoroutine(UIAnimator.FadeOut(cg, duration, deactivate: false));
        }
    }

    //   HUD updates  

    private void UpdateGameUI()
    {
        if (GameManager.Instance == null) return;

        if (levelText != null)
            levelText.text = $"Level {GameManager.Instance.currentLevel}";

        var vehicle = GameManager.Instance.GetCurrentVehicle();
        if (vehicleTypeText != null)
            vehicleTypeText.text = vehicle != null ? vehicle.vehicleName : "";

        if (terrainTypeText != null)
            terrainTypeText.text = GameManager.Instance.currentTerrain.ToString();
    }

    public void UpdateProgress(float progress)
    {
        if (progressSlider == null) return;
        progressSlider.value = progress;

        if (progressFill != null)
            progressFill.color = Color.Lerp(progressLow,
                progress < 0.5f ? progressMid : progressHigh,
                progress < 0.5f ? progress * 2f : (progress - 0.5f) * 2f);
    }

    public void UpdateVehicleType(string vehicleType)
    {
        if (vehicleTypeText != null) vehicleTypeText.text = vehicleType;
    }

    public void UpdateTerrainType(string terrainType)
    {
        if (terrainTypeText != null) terrainTypeText.text = terrainType;
    }

    //   Timer  

    // 3 2 1 GO! countdown before the timer starts. Blocks player input briefly
    // so the vehicle doesn't drive off before the player is ready.
    private IEnumerator CountdownAndStartTimer()
    {
        // Consume the quick-retry flag up front so it can't survive an early return and leak
        // into a later normal level (which would wrongly skip its 3-2-1 and stats card).
        bool quick = _quickRestart;
        _quickRestart = false;
        if (levelText == null) { StartTimer(); yield break; }
        OnScreenControls.InputBlocked = true;
        // Hide the drive controls during the countdown so they don't clash with the stats card;
        // they pop back in on "GO!".
        OnScreenControls.Instance?.SetVisible(false);

        var canvasGO = new GameObject("_CountdownCanvas");
        DontDestroyOnLoad(canvasGO);
        var c = canvasGO.AddComponent<Canvas>();
        c.renderMode  = RenderMode.ScreenSpaceOverlay;
        c.sortingOrder = 160;
        var scaler = canvasGO.AddComponent<UnityEngine.UI.CanvasScaler>();
        scaler.uiScaleMode         = UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1280f, 720f);
        scaler.matchWidthOrHeight  = 0.5f;
        var cg = canvasGO.AddComponent<CanvasGroup>();
        cg.blocksRaycasts = false;

        var clone = Instantiate(levelText.gameObject, canvasGO.transform, false);
        clone.SetActive(true);
        var tmp = clone.GetComponent<TextMeshProUGUI>();
        tmp.fontSize  = 128;
        tmp.fontStyle = TMPro.FontStyles.Bold;
        tmp.alignment = TMPro.TextAlignmentOptions.Center;
        tmp.raycastTarget = false;
        var rt = clone.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.2f, 0.35f); rt.anchorMax = new Vector2(0.8f, 0.65f);
        rt.offsetMin = rt.offsetMax = Vector2.zero;

        string[] labels = { "3", "2", "1", "GO!" };
        Color[]  colors = {
            new Color(1f, 0.35f, 0.30f),   // 3 = red
            new Color(1f, 0.75f, 0.10f),   // 2 = amber
            new Color(0.25f, 0.92f, 0.38f),// 1 = green
            new Color(0.25f, 0.92f, 0.38f),// GO = green
        };

        // Quick retry: same level again — just flash "GO!" instead of the full 3-2-1.
        int startIdx = quick ? labels.Length - 1 : 0;

        for (int i = startIdx; i < labels.Length; i++)
        {
            // The countdown canvas is DontDestroyOnLoad but CleanupPersistentGameUI (on Main Menu
            // mid-countdown) destroys it; bail cleanly so we never touch a fake-null clone.
            if (tmp == null || rt == null) { OnScreenControls.InputBlocked = false; yield break; }
            tmp.text  = labels[i];
            tmp.color = colors[i];
            rt.localScale = Vector3.zero;

            bool isGo = (i == labels.Length - 1);
            AudioManager.Instance?.PlayCountdownBeep(isGo);

            // Pop-in over 0.18s
            float t = 0f;
            while (t < 0.18f)
            {
                t += Time.unscaledDeltaTime;
                if (rt == null) { OnScreenControls.InputBlocked = false; yield break; }
                rt.localScale = Vector3.one * UIAnimator.EaseOutCubic(Mathf.Clamp01(t / 0.18f)) * 1.1f;
                yield return null;
            }
            if (rt == null) { OnScreenControls.InputBlocked = false; yield break; }
            rt.localScale = Vector3.one;

            // Settle duration (shorter for "GO!")
            yield return new WaitForSecondsRealtime(isGo ? 0.30f : 0.65f);

            // Fade out between numbers
            if (!isGo)
            {
                t = 0f;
                while (t < 0.15f)
                {
                    t += Time.unscaledDeltaTime;
                    if (cg == null) { OnScreenControls.InputBlocked = false; yield break; }
                    cg.alpha = 1f - t / 0.15f;
                    yield return null;
                }
                if (cg != null) cg.alpha = 1f;
            }
        }

        // Quick fade-out on GO
        float fade = 0f;
        while (fade < 0.25f)
        {
            fade += Time.unscaledDeltaTime;
            if (cg == null) { OnScreenControls.InputBlocked = false; yield break; }
            cg.alpha = 1f - fade / 0.25f;
            yield return null;
        }
        if (canvasGO != null) Destroy(canvasGO);

        OnScreenControls.InputBlocked = false;
        OnScreenControls.Instance?.SetVisible(true);
        StartTimer();
    }

    private void StartTimer()
    {
        currentTime = 0f;
        timerRunning = true;
        if (timerRoutine != null) StopCoroutine(timerRoutine);
        timerRoutine = StartCoroutine(TimerLoop());
    }

    private void StopTimer() => timerRunning = false;

    // Grace: player is inside the zone at timer-0; give them 2s to complete the park.
    private IEnumerator GracePeriod(float seconds)
    {
        if (timerText != null)
        {
            timerText.text  = "PARK!";
            timerText.color = new Color(0.3f, 1f, 0.5f);
        }
        yield return new WaitForSeconds(seconds);
        // levelEnded is the reliable signal; the old timerText=="PARK!" check stayed true even
        // after a successful park, stacking GAME OVER on top of the win.
        if (!levelEnded)
            ShowGameOver("Time ran out!");
    }

    // Called by TimeBonusToken when the player drives over a "+5s" pickup.
    public void AddTime(float seconds)
    {
        currentTime = Mathf.Max(0f, currentTime - seconds);
        if (levelText != null) StartCoroutine(ShowTimeBonusPopup(seconds));
    }

    private IEnumerator ShowTimeBonusPopup(float seconds)
    {
        var cgo = new GameObject("_TimeBonusPopup");
        var c = cgo.AddComponent<Canvas>();
        c.renderMode = RenderMode.ScreenSpaceOverlay;
        c.sortingOrder = 155;
        var scaler = cgo.AddComponent<UnityEngine.UI.CanvasScaler>();
        scaler.uiScaleMode = UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1280f, 720f);
        scaler.matchWidthOrHeight = 0.5f;

        var lbl = Instantiate(levelText.gameObject, cgo.transform, false);
        lbl.SetActive(true);
        var tmp = lbl.GetComponent<TextMeshProUGUI>();
        tmp.text      = $"+{seconds:F0}s";
        tmp.fontSize  = 72;
        tmp.fontStyle = TMPro.FontStyles.Bold;
        tmp.alignment = TMPro.TextAlignmentOptions.Center;
        tmp.color     = new Color(1f, 0.92f, 0.20f, 1f);
        tmp.raycastTarget = false;
        var rt = lbl.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.2f, 0.55f); rt.anchorMax = new Vector2(0.8f, 0.75f);
        rt.offsetMin = rt.offsetMax = Vector2.zero;
        rt.localScale = Vector3.zero;

        // Pop in
        float t = 0f;
        while (t < 0.22f)
        {
            t += Time.unscaledDeltaTime;
            rt.localScale = Vector3.one * UIAnimator.EaseOutCubic(Mathf.Clamp01(t / 0.22f)) * 1.15f;
            yield return null;
        }
        rt.localScale = Vector3.one;

        // Drift upward and fade out
        Vector3 startPos = rt.anchoredPosition;
        t = 0f;
        float dur = 0.70f;
        while (t < dur)
        {
            t += Time.unscaledDeltaTime;
            float k = t / dur;
            rt.anchoredPosition = startPos + Vector3.up * (80f * k);
            tmp.color = new Color(1f, 0.92f, 0.20f, 1f - k);
            yield return null;
        }
        Destroy(cgo);
    }

    private IEnumerator TimerLoop()
    {
        float limit = 90f;
        if (GameManager.Instance != null && LevelDataManager.Instance != null)
        {
            var data = LevelDataManager.Instance.GetLevelData(GameManager.Instance.currentLevel);
            if (data != null) limit = data.timeLimit;
        }

        while (timerRunning)
        {
            currentTime += Time.deltaTime;
            float remaining = Mathf.Max(0f, limit - currentTime);

            if (timerText != null)
            {
                int s = Mathf.CeilToInt(remaining);
                timerText.text = $"{s / 60}:{s % 60:00}";

                // Pulse red + tick sound when under 10 seconds
                if (remaining <= 10f)
                {
                    float pulse = (Mathf.Sin(Time.time * 6f) + 1f) * 0.5f;
                    timerText.color = Color.Lerp(Color.white, new Color(1f, 0.2f, 0.2f), pulse);
                    int remSec = Mathf.CeilToInt(remaining);
                    if (remSec != _lastWarningSecond)
                    {
                        _lastWarningSecond = remSec;
                        AudioManager.Instance?.PlayTimerWarning();
                    }
                }
                else
                {
                    timerText.color = Color.white;
                    _lastWarningSecond = -1;
                }
            }

            if (remaining <= 0f)
            {
                timerRunning = false;
                // Grace period: if vehicle is inside the zone right now, allow 2s to finish parking.
                var zone = Object.FindAnyObjectByType<ParkingZone>();
                if (zone != null && zone.IsVehicleInside())
                    StartCoroutine(GracePeriod(2f));
                else
                    ShowGameOver("Time ran out!");
            }
            yield return null;
        }
    }

    //   Button handlers  

    // Set when replaying the SAME level so the intro is fast (0.5s GO, no stats card) —
    // the full 3-2-1 + stats card made the fail-retry loop feel sluggish.
    private bool _quickRestart;

    private void RestartLevel()
    {
        Time.timeScale = 1f;
        SetPanelImmediate(gameOverPanel,      gameOverCG,      false);
        SetPanelImmediate(levelCompletePanel, levelCompleteCG, false);
        SetPanelImmediate(pauseMenuPanel,     pauseCG,         false);
        _quickRestart = true;   // same level, same vehicle — skip the long intro
        GameManager.Instance?.LoadLevel(GameManager.Instance.currentLevel);
        ShowGameUI();
    }

    private void NextLevel()
    {
        Time.timeScale = 1f;
        SetPanelImmediate(levelCompletePanel, levelCompleteCG, false);
        GameManager.Instance?.CompleteLevel();
        ShowGameUI();
    }

    // Legacy compatibility
    public void ShowMainMenu() => SetPanelImmediate(mainMenuPanel, mainMenuCG, true);

    //   Terrain Selection  

    private GameObject terrainSelectCanvas;

    private static Sprite _roundedSprite;
    private static Sprite RoundedSprite =>
        _roundedSprite != null ? _roundedSprite : (_roundedSprite = SpriteGenerator.CreateRoundedRect(220, 280, 0.10f));

    // Creates a UI GameObject that always has a RectTransform.
    private static RectTransform NewRect(string name, Transform parent)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        return go.GetComponent<RectTransform>();
    }

    public void ShowTerrainSelect()
    {
        if (terrainSelectCanvas != null) Destroy(terrainSelectCanvas);

        // Hide the in-game HUD while choosing terrain
        StopTimer();
        if (gamePanel != null) gamePanel.SetActive(false);
        var timerCanvas = GameObject.Find("_TimerCanvas");
        if (timerCanvas != null) timerCanvas.SetActive(false);

        var canvasGO = new GameObject("_TerrainSelectCanvas");
        terrainSelectCanvas = canvasGO;
        var c = canvasGO.AddComponent<Canvas>();
        c.renderMode = RenderMode.ScreenSpaceOverlay;
        c.sortingOrder = 200;
        var scaler = canvasGO.AddComponent<UnityEngine.UI.CanvasScaler>();
        scaler.uiScaleMode = UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1280f, 720f);
        scaler.matchWidthOrHeight = 0.5f;
        canvasGO.AddComponent<UnityEngine.UI.GraphicRaycaster>();

        // Gradient-ish dark overlay
        var bgRt = NewRect("BG", canvasGO.transform);
        var bgImg = bgRt.gameObject.AddComponent<UnityEngine.UI.Image>();
        bgImg.color = new Color(0.04f, 0.05f, 0.10f, 0.92f);
        bgRt.anchorMin = Vector2.zero; bgRt.anchorMax = Vector2.one;
        bgRt.offsetMin = bgRt.offsetMax = Vector2.zero;

        // Title
        var titleRt = NewRect("Title", canvasGO.transform);
        CloneTMP(titleRt.gameObject, "CHOOSE YOUR TERRAIN", 56, Color.white);
        titleRt.anchorMin = new Vector2(0.1f, 0.78f); titleRt.anchorMax = new Vector2(0.9f, 0.94f);
        titleRt.offsetMin = titleRt.offsetMax = Vector2.zero;

        // Subtitle
        var subRt = NewRect("Sub", canvasGO.transform);
        var subTMP = CloneTMP(subRt.gameObject, "Each terrain changes how your vehicle handles", 24,
            new Color(0.65f, 0.75f, 1f, 1f));
        subTMP.fontStyle = TMPro.FontStyles.Normal;
        subRt.anchorMin = new Vector2(0.1f, 0.70f); subRt.anchorMax = new Vector2(0.9f, 0.78f);
        subRt.offsetMin = subRt.offsetMax = Vector2.zero;

        var terrains = new[] {
            ("ROAD",  new Color(0.20f, 0.62f, 0.26f), GameManager.TerrainType.Road,
             "Stable ground\nNo distractions\nBest for beginners"),
            ("WATER", new Color(0.13f, 0.47f, 0.85f), GameManager.TerrainType.Water,
             "Waves push your\nvehicle sideways\nFight the current"),
            ("AIR",   new Color(0.27f, 0.66f, 0.85f), GameManager.TerrainType.Air,
             "Random wind gusts\nfrom all directions\nStay in control"),
            ("SPACE", new Color(0.45f, 0.18f, 0.78f), GameManager.TerrainType.Space,
             "Zero-gravity drift\nConstant slow spin\nHardest terrain"),
        };

        float btnW = 0.205f, btnH = 0.46f, startX = 0.04f, gapX = 0.24f, btnY = 0.12f;
        for (int i = 0; i < terrains.Length; i++)
        {
            var (label, col, terrain, desc) = terrains[i];
            CreateTerrainButton(canvasGO.transform, label, col, terrain, desc,
                new Vector2(startX + i * gapX, btnY),
                new Vector2(startX + i * gapX + btnW, btnY + btnH));
        }
    }

    private void CreateTerrainButton(Transform parent, string label, Color col,
        GameManager.TerrainType terrain, string desc, Vector2 anchorMin, Vector2 anchorMax)
    {
        var rt = NewRect(label + "Btn", parent);
        var img = rt.gameObject.AddComponent<UnityEngine.UI.Image>();
        img.sprite = RoundedSprite;
        img.type = UnityEngine.UI.Image.Type.Simple;
        img.color = col;
        rt.anchorMin = anchorMin; rt.anchorMax = anchorMax;
        rt.offsetMin = rt.offsetMax = Vector2.zero;
        var btn = rt.gameObject.AddComponent<Button>();
        var t = terrain;
        btn.onClick.AddListener(() => OnTerrainSelected(t));
        if (rt.gameObject.GetComponent<ButtonAnimator>() == null)
            rt.gameObject.AddComponent<ButtonAnimator>();

        // Colored header strip behind label
        var hdrRt = NewRect("Header", rt);
        var hdrImg = hdrRt.gameObject.AddComponent<UnityEngine.UI.Image>();
        hdrImg.color = new Color(0f, 0f, 0f, 0.22f);
        hdrRt.anchorMin = new Vector2(0f, 0.70f); hdrRt.anchorMax = Vector2.one;
        hdrRt.offsetMin = hdrRt.offsetMax = Vector2.zero;

        // Label
        var lblRt = NewRect("Label", rt);
        CloneTMP(lblRt.gameObject, label, 40, Color.white);
        lblRt.anchorMin = new Vector2(0f, 0.70f); lblRt.anchorMax = Vector2.one;
        lblRt.offsetMin = lblRt.offsetMax = Vector2.zero;

        // Description
        var descRt = NewRect("Desc", rt);
        var descTMP = CloneTMP(descRt.gameObject, desc, 19, new Color(1f, 1f, 1f, 0.92f));
        descTMP.alignment = TMPro.TextAlignmentOptions.Center;
        descTMP.fontStyle = TMPro.FontStyles.Normal;
        descRt.anchorMin = Vector2.zero; descRt.anchorMax = new Vector2(1f, 0.68f);
        descRt.offsetMin = new Vector2(6f, 6f); descRt.offsetMax = new Vector2(-6f, -6f);
    }

    private TextMeshProUGUI CloneTMP(GameObject go, string text, float fontSize, Color color)
    {
        TextMeshProUGUI tmp;
        if (levelText != null)
        {
            var clone = Instantiate(levelText.gameObject, go.transform, false);
            clone.name = "Text";
            tmp = clone.GetComponent<TextMeshProUGUI>();
            var rt = clone.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
            rt.offsetMin = rt.offsetMax = Vector2.zero;
        }
        else
        {
            var textGO = new GameObject("Text");
            textGO.transform.SetParent(go.transform, false);
            tmp = textGO.AddComponent<TextMeshProUGUI>();
        }
        tmp.text = text;
        tmp.fontSize = fontSize;
        tmp.color = color;
        tmp.alignment = TMPro.TextAlignmentOptions.Center;
        tmp.fontStyle = TMPro.FontStyles.Bold;
        return tmp;
    }

    private void OnTerrainSelected(GameManager.TerrainType terrain)
    {
        if (terrainSelectCanvas != null) { Destroy(terrainSelectCanvas); terrainSelectCanvas = null; }
        GameManager.Instance?.StartGameWithTerrain(terrain);
    }
}

// Extension helper so we can call .let() inline without null checks
internal static class NullExt
{
    public static void let<T>(this T obj, System.Action<T> action) where T : class
    {
        if (obj != null) action(obj);
    }
}
