using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;
using UnityEngine.EventSystems;
using System.Collections;

public class MainMenuManager : MonoBehaviour
{
    [Header("Panels")]
    public GameObject mainPanel;
    public GameObject settingsPanel;
    public GameObject achievementsPanel;

    [Header("Canvas Groups")]
    public CanvasGroup mainCG;
    public CanvasGroup settingsCG;
    public CanvasGroup achievementsCG;

    [Header("Title")]
    public RectTransform titleRect;
    public TextMeshProUGUI titleText;
    public TextMeshProUGUI subtitleText;

    [Header("Buttons")]
    public Button playButton;
    public Button settingsButton;
    public Button achievementsButton;
    public Button quitButton;
    public RectTransform[] buttonRects;

    [Header("Stats")]
    public TextMeshProUGUI highScoreText;
    public TextMeshProUGUI totalAchievementsText;

    [Header("Settings")]
    public Slider musicVolumeSlider;
    public Slider sfxVolumeSlider;
    public Toggle vibrationToggle;

    [Header("Background")]
    public Image backgroundImage;

    private void Start()
    {
        EnsureEventSystem();
        PolishMenu();
        EnsureCanvasGroups();
        SetupButtons();
        LoadSettings();
        StartCoroutine(PlayEntranceAnimation());
        CheckDailyBonus();
        MaybeShowHowToPlay();   // first-launch how-to overlay (shown once)
    }

    // ── First-launch How-To-Play overlay (shown once) ──────────────────────────
    private const string HowToShownKey = "HowToShown";

    private void MaybeShowHowToPlay()
    {
        if (PlayerPrefs.GetInt(HowToShownKey, 0) != 0) return;   // already seen
        PlayerPrefs.SetInt(HowToShownKey, 1);                    // persist so it shows once
        PlayerPrefs.Save();
        BuildHowToOverlay();
    }

    private void BuildHowToOverlay()
    {
        // Reuses the shared scaffold: themed BG, bold title, and a single centered BACK
        // button already wired to CloseOverlay(). No extra interactive UI is added.
        BuildOverlayScaffold("HOW TO PLAY", out var content);

        string[] lines =
        {
            "Goal: park your vehicle inside the glowing zone.",
            "",
            "Steer with the arrow keys or on-screen controls.",
            "Come to a full stop inside the parking zone to clear it.",
            "Avoid the hazards and obstacles along the way.",
            "Finish quickly to earn a better time and more coins.",
            "",
            "Tip: change music, sound and vibration under SETTINGS.",
            "",
            "Press BACK or ESC to start playing.",
        };

        var body = MakeText(content, string.Join("\n", lines), 26,
                            new Color(0.90f, 0.93f, 1f), TextAlignmentOptions.Center);
        var brt = body.rectTransform;
        brt.anchorMin = new Vector2(0.10f, 0.18f);
        brt.anchorMax = new Vector2(0.90f, 0.80f);
        brt.offsetMin = brt.offsetMax = Vector2.zero;
        // MakeText already sets raycastTarget=false, so this text never blocks BACK.
    }

    // ── Daily login bonus (once per calendar day) ──────────────────────────────
    private const int DailyBonusCoins = 50;
    private void CheckDailyBonus()
    {
        if (ScoreManager.Instance == null) return;
        int today = int.Parse(System.DateTime.Now.ToString("yyyyMMdd", System.Globalization.CultureInfo.InvariantCulture));
        int last  = PlayerPrefs.GetInt("LastDailyClaim", 0);
        if (today == last) return;
        ScoreManager.Instance.AddCoins(DailyBonusCoins);
        PlayerPrefs.SetInt("LastDailyClaim", today);
        PlayerPrefs.Save();
        UpdateStats();
        StartCoroutine(ShowDailyBonusBanner(DailyBonusCoins));
    }

    private IEnumerator ShowDailyBonusBanner(int coins)
    {
        var canvas = FindAnyObjectByType<Canvas>();
        if (canvas == null) yield break;
        var rt = NewRect("_DailyBonus", canvas.transform);
        var img = rt.gameObject.AddComponent<Image>();
        img.sprite = _rounded; img.type = Image.Type.Simple; img.color = new Color(0.12f, 0.45f, 0.20f, 0.96f);
        img.raycastTarget = false;
        rt.anchorMin = new Vector2(0.28f, 0.855f); rt.anchorMax = new Vector2(0.72f, 0.945f);
        rt.offsetMin = rt.offsetMax = Vector2.zero;
        var lbl = MakeText(rt, $"DAILY BONUS  +{coins} COINS!", 30, new Color(1f, 0.92f, 0.45f), TextAlignmentOptions.Center);
        var lrt = lbl.rectTransform; lrt.anchorMin = Vector2.zero; lrt.anchorMax = Vector2.one; lrt.offsetMin = lrt.offsetMax = Vector2.zero;
        lbl.fontStyle = FontStyles.Bold;

        // Pop in
        float t = 0f; rt.localScale = Vector3.zero;
        while (t < 1f) { t += Time.deltaTime / 0.3f; rt.localScale = Vector3.one * Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(t)); yield return null; }
        rt.localScale = Vector3.one;
        AudioManager.Instance?.PlayTimeBonusSound();
        yield return new WaitForSeconds(2.2f);
        // Fade out
        var cg = rt.gameObject.AddComponent<CanvasGroup>();
        t = 0f; while (t < 1f) { t += Time.deltaTime / 0.5f; cg.alpha = 1f - Mathf.Clamp01(t); yield return null; }
        Destroy(rt.gameObject);
    }

    private TextMeshProUGUI _font;     // template for runtime text (inherits a working TMP font)
    private Sprite _rounded;           // rounded-rect sprite for runtime UI
    private GameObject _overlay;       // active settings/achievements overlay

    // Builds a themed background + restyles buttons at runtime (scene refs for these were unset).
    private void PolishMenu()
    {
        _font = playButton != null ? playButton.GetComponentInChildren<TextMeshProUGUI>(true)
                                   : FindAnyObjectByType<TextMeshProUGUI>();
        _rounded = SpriteGenerator.CreateRoundedRect(220, 90, 0.30f);

        // Solid camera clear so the old skybox gradient never shows
        var cam = Camera.main;
        if (cam != null)
        {
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.05f, 0.06f, 0.14f);
        }

        var canvas = FindAnyObjectByType<Canvas>();
        if (canvas != null && GameObject.Find("_MenuBackground") == null)
        {
            var bgGO = new GameObject("_MenuBackground", typeof(RectTransform));
            bgGO.transform.SetParent(canvas.transform, false);
            bgGO.transform.SetAsFirstSibling(); // behind everything
            var img = bgGO.AddComponent<Image>();
            img.sprite = SpriteGenerator.CreateMenuBackground();
            img.color = Color.white;
            img.raycastTarget = false;
            var rt = bgGO.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
            rt.offsetMin = rt.offsetMax = Vector2.zero;
        }

        // Restyle the menu buttons: rounded sprite + accent color
        var rounded = SpriteGenerator.CreateRoundedRect(240, 90, 0.30f);
        StyleButton(playButton,         rounded, new Color(0.20f, 0.62f, 0.26f));
        StyleButton(settingsButton,     rounded, new Color(0.16f, 0.45f, 0.82f));
        StyleButton(achievementsButton, rounded, new Color(0.45f, 0.20f, 0.78f));
        StyleButton(quitButton,         rounded, new Color(0.70f, 0.26f, 0.26f));

        // Runtime SHOP button (top-right corner) - opens the coin shop overlay.
        if (canvas != null && GameObject.Find("_ShopButton") == null)
        {
            var sb = NewRect("_ShopButton", canvas.transform);
            var simg = sb.gameObject.AddComponent<Image>();
            simg.sprite = _rounded; simg.type = Image.Type.Simple; simg.color = new Color(0.95f, 0.78f, 0.20f);
            sb.anchorMin = new Vector2(0.80f, 0.88f); sb.anchorMax = new Vector2(0.975f, 0.96f);
            sb.offsetMin = sb.offsetMax = Vector2.zero;
            var sbtn = sb.gameObject.AddComponent<Button>();
            sbtn.onClick.AddListener(ShowShop);
            sb.gameObject.AddComponent<ButtonAnimator>();
            var slbl = MakeText(sb, "SHOP", 26, new Color(0.1f,0.08f,0.0f), TextAlignmentOptions.Center);
            var slrt = slbl.rectTransform; slrt.anchorMin = Vector2.zero; slrt.anchorMax = Vector2.one; slrt.offsetMin = slrt.offsetMax = Vector2.zero;
            slbl.fontStyle = FontStyles.Bold;
        }

        // Runtime MUTE toggle (top-left corner) - toggles global audio, persisted.
        if (canvas != null && GameObject.Find("_MuteButton") == null)
        {
            var mb = NewRect("_MuteButton", canvas.transform);
            var mimg = mb.gameObject.AddComponent<Image>();
            mimg.sprite = _rounded; mimg.type = Image.Type.Simple; mimg.color = new Color(0.20f, 0.24f, 0.34f);
            mb.anchorMin = new Vector2(0.025f, 0.88f); mb.anchorMax = new Vector2(0.20f, 0.96f);
            mb.offsetMin = mb.offsetMax = Vector2.zero;
            var mbtn = mb.gameObject.AddComponent<Button>();
            mb.gameObject.AddComponent<ButtonAnimator>();
            // Speaker icon (left) + label (right)
            var icoRt = NewRect("_MuteIcon", mb);
            _muteIcon = icoRt.gameObject.AddComponent<Image>();
            _muteIcon.sprite = SpriteGenerator.CreateSpeakerIcon(64, AudioManager.Instance != null && AudioManager.Instance.Muted);
            _muteIcon.raycastTarget = false;
            icoRt.anchorMin = new Vector2(0.06f, 0.18f); icoRt.anchorMax = new Vector2(0.30f, 0.82f); icoRt.offsetMin = icoRt.offsetMax = Vector2.zero;
            _muteLabel = MakeText(mb, MuteText(), 24, Color.white, TextAlignmentOptions.Center);
            var mlrt = _muteLabel.rectTransform; mlrt.anchorMin = new Vector2(0.30f, 0f); mlrt.anchorMax = Vector2.one; mlrt.offsetMin = mlrt.offsetMax = Vector2.zero;
            _muteLabel.fontStyle = FontStyles.Bold;
            mbtn.onClick.AddListener(ToggleMute);
        }

        // Per-terrain best-time line (sits just below the achievements stat, above PLAY).
        if (canvas != null && GameObject.Find("_BestTimesText") == null)
        {
            var bt = NewRect("_BestTimesText", canvas.transform);
            bt.anchorMin = new Vector2(0.05f, 0.465f); bt.anchorMax = new Vector2(0.95f, 0.515f);
            bt.offsetMin = bt.offsetMax = Vector2.zero;
            _bestTimesLabel = MakeText(bt, "", 22, new Color(0.75f, 0.85f, 1f), TextAlignmentOptions.Center);
            var brt = _bestTimesLabel.rectTransform; brt.anchorMin = Vector2.zero; brt.anchorMax = Vector2.one; brt.offsetMin = brt.offsetMax = Vector2.zero;
        }
        RefreshBestTimes();
    }

    private TextMeshProUGUI _bestTimesLabel;
    // Reads the persisted per-terrain best completion times and renders a compact one-liner.
    private void RefreshBestTimes()
    {
        if (_bestTimesLabel == null) return;
        string[] terr = { "Road", "Water", "Air", "Space" };
        string[] tag  = { "Road", "Water", "Air", "Space" };
        var parts = new System.Collections.Generic.List<string>();
        for (int i = 0; i < terr.Length; i++)
        {
            float v = PlayerPrefs.GetFloat($"BestTime_{terr[i]}", -1f);
            if (v > 0f) parts.Add($"{tag[i]} {v:0.0}s");
        }
        _bestTimesLabel.text = parts.Count > 0 ? "Best times    " + string.Join("    ", parts) : "";
    }

    private TextMeshProUGUI _muteLabel;
    private Image _muteIcon;
    private string MuteText() => (AudioManager.Instance != null && AudioManager.Instance.Muted) ? "SOUND OFF" : "SOUND ON";
    private void RefreshMuteVisual()
    {
        if (_muteLabel != null) _muteLabel.text = MuteText();
        if (_muteIcon != null)
            _muteIcon.sprite = SpriteGenerator.CreateSpeakerIcon(64, AudioManager.Instance != null && AudioManager.Instance.Muted);
    }
    private void ToggleMute()
    {
        if (AudioManager.Instance != null) AudioManager.Instance.ToggleMute();
        RefreshMuteVisual();
        AudioManager.Instance?.PlayButtonClickSound();
    }

    private void ShowShop()
    {
        AudioManager.Instance?.PlayButtonClickSound();
        BuildShopOverlay();
    }

    // Flash a shop row red when the player can't afford the skin.
    private IEnumerator FlashRed(Image img)
    {
        if (img == null) yield break;
        Color baseCol = img.color;
        Color red = new Color(0.70f, 0.15f, 0.15f);
        float t = 0f;
        while (t < 0.45f)
        {
            if (img == null) yield break;
            t += Time.unscaledDeltaTime;
            // quick to red, ease back
            float k = t < 0.12f ? (t / 0.12f) : 1f - ((t - 0.12f) / 0.33f);
            img.color = Color.Lerp(baseCol, red, Mathf.Clamp01(k));
            yield return null;
        }
        if (img != null) img.color = baseCol;
    }

    private void BuildShopOverlay()
    {
        BuildOverlayScaffold("SHOP", out var content);
        var sm = ScoreManager.Instance;

        // Coin total at top
        var coinT = MakeText(content, sm != null ? $"<color=#FFD23C>Coins: {sm.Coins:N0}</color>" : "Coins: 0",
            24, Color.white, TextAlignmentOptions.Center);
        var crt = coinT.rectTransform;
        crt.anchorMin = new Vector2(0.1f, 0.78f); crt.anchorMax = new Vector2(0.9f, 0.84f);
        crt.offsetMin = crt.offsetMax = Vector2.zero;

        var skins = ScoreManager.Skins;
        int n = skins.Length;
        float top = 0.74f, bottom = 0.17f;
        float rowH = (top - bottom) / n;
        for (int idx = 0; idx < n; idx++)
        {
            int i = idx;
            var skin = skins[i];
            float y1 = top - i * rowH;
            float y0 = y1 - rowH * 0.88f;

            var row = NewRect("Skin" + i, content);
            var rimg = row.gameObject.AddComponent<Image>();
            rimg.sprite = _rounded; rimg.type = Image.Type.Simple;
            bool owned = sm != null && sm.IsSkinUnlocked(i);
            bool selected = sm != null && sm.SelectedSkin == i;
            bool gated = !string.IsNullOrEmpty(skin.requiredAchievement) && !owned;   // achievement-locked
            bool affordable = sm != null && !owned && !gated && sm.Coins >= skin.price;   // can buy right now
            rimg.color = selected    ? new Color(0.20f, 0.45f, 0.26f)
                       : affordable  ? new Color(0.28f, 0.26f, 0.12f)   // warm gold tint - "you can afford this"
                                     : new Color(0.16f, 0.17f, 0.24f);
            row.anchorMin = new Vector2(0.12f, y0); row.anchorMax = new Vector2(0.88f, y1);
            row.offsetMin = row.offsetMax = Vector2.zero;
            var rbtn = row.gameObject.AddComponent<Button>();
            var rowImgRef = rimg;
            rbtn.onClick.AddListener(() =>
            {
                var smm = ScoreManager.Instance;
                if (smm != null && smm.TryBuyOrSelectSkin(i))
                {
                    AudioManager.Instance?.PlayButtonClickSound();
                    UpdateStats();
                    BuildShopOverlay();   // refresh (owned/selected state)
                }
                else
                {
                    StartCoroutine(FlashRed(rowImgRef));   // not enough coins
                }
            });
            row.gameObject.AddComponent<ButtonAnimator>();

            // Skin preview: a little car silhouette tinted with the skin colour (Classic = per-level blue).
            var sw = NewRect("Sw", row);
            var swImg = sw.gameObject.AddComponent<Image>();
            Color preview = skin.color.a <= 0f ? new Color(0.20f, 0.60f, 1f) : new Color(skin.color.r, skin.color.g, skin.color.b, 1f);
            swImg.sprite = SpriteGenerator.CreateVehicle(GameManager.VehicleKind.Car, 96, preview);
            swImg.preserveAspect = true;
            swImg.raycastTarget = false;
            sw.anchorMin = new Vector2(0.03f, 0.12f); sw.anchorMax = new Vector2(0.17f, 0.88f); sw.offsetMin = sw.offsetMax = Vector2.zero;

            // Name (smaller font: 13 rows now share the same panel height)
            var nm = MakeText(row, skin.name, 20, Color.white, TextAlignmentOptions.Left);
            var nrt = nm.rectTransform; nrt.anchorMin = new Vector2(0.20f, 0f); nrt.anchorMax = new Vector2(0.6f, 1f); nrt.offsetMin = nrt.offsetMax = Vector2.zero;

            // State: SELECTED / OWNED (tap to use) / LOCKED (achievement) / BUY (affordable) / price
            string state = selected    ? "<color=#5BE07A>SELECTED</color>"
                         : owned       ? "TAP TO USE"
                         : gated       ? $"<color=#FF9A3C>LOCKED: {skin.requiredAchievement}</color>"
                         : affordable  ? $"<color=#5BE07A>BUY {skin.price}</color>"
                                       : $"<color=#FFD23C>{skin.price}</color>";
            var st = MakeText(row, state, 18, Color.white, TextAlignmentOptions.Right);
            var strt = st.rectTransform; strt.anchorMin = new Vector2(0.6f, 0f); strt.anchorMax = new Vector2(0.96f, 1f); strt.offsetMin = strt.offsetMax = Vector2.zero;
        }
    }

    private void StyleButton(Button btn, Sprite sprite, Color color)
    {
        if (btn == null) return;
        var img = btn.GetComponent<Image>();
        if (img != null)
        {
            img.sprite = sprite;
            img.type = Image.Type.Simple;
            img.color = color;
        }
        // Make the button's color transitions ride on the accent color
        var cb = btn.colors;
        cb.normalColor = Color.white;
        cb.highlightedColor = new Color(1.1f, 1.1f, 1.1f, 1f);
        cb.pressedColor = new Color(0.85f, 0.85f, 0.85f, 1f);
        cb.fadeDuration = 0.1f;
        btn.colors = cb;
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (_overlay != null)
                CloseOverlay();
            else
                QuitGame();
        }
    }

    private void EnsureEventSystem()
    {
        if (FindAnyObjectByType<EventSystem>() == null)
        {
            var go = new GameObject("EventSystem");
            go.AddComponent<EventSystem>();
            go.AddComponent<StandaloneInputModule>();
        }
    }

    private void EnsureCanvasGroups()
    {
        if (mainPanel != null)         mainCG         = mainPanel.GetComponent<CanvasGroup>()         ?? mainPanel.AddComponent<CanvasGroup>();
        if (settingsPanel != null)     settingsCG     = settingsPanel.GetComponent<CanvasGroup>()     ?? settingsPanel.AddComponent<CanvasGroup>();
        if (achievementsPanel != null) achievementsCG = achievementsPanel.GetComponent<CanvasGroup>() ?? achievementsPanel.AddComponent<CanvasGroup>();

        if (settingsPanel != null)     { settingsPanel.SetActive(false);     settingsCG.alpha = 0f; }
        if (achievementsPanel != null) { achievementsPanel.SetActive(false); achievementsCG.alpha = 0f; }

        // Add button animators
        foreach (var btn in FindObjectsByType<Button>(FindObjectsSortMode.None))
            if (btn.GetComponent<ButtonAnimator>() == null)
                btn.gameObject.AddComponent<ButtonAnimator>();
    }

    private void SetupButtons()
    {
        playButton?.onClick.AddListener(StartGame);
        settingsButton?.onClick.AddListener(ShowSettings);
        achievementsButton?.onClick.AddListener(ShowAchievements);
        quitButton?.onClick.AddListener(QuitGame);
        musicVolumeSlider?.onValueChanged.AddListener(SetMusicVolume);
        sfxVolumeSlider?.onValueChanged.AddListener(SetSFXVolume);
        vibrationToggle?.onValueChanged.AddListener(SetVibration);
    }

    // ── Entrance animation ────────────────────────────────────────────────────

    private IEnumerator PlayEntranceAnimation()
    {
        // Fade background in
        if (backgroundImage != null)
        {
            backgroundImage.color = Color.black;
            yield return StartCoroutine(UIAnimator.ColorFlash(backgroundImage, Color.black, 0f));
            float t = 0f;
            Color target = new Color(0.08f, 0.08f, 0.14f);
            while (t < 1f)
            {
                t += Time.deltaTime / 0.6f;
                backgroundImage.color = Color.Lerp(Color.black, target, UIAnimator.EaseOutCubic(t));
                yield return null;
            }
        }

        yield return new WaitForSeconds(0.1f);

        // Title drops in with elastic bounce
        if (titleRect != null)
        {
            titleRect.gameObject.SetActive(true);
            titleRect.localScale = Vector3.zero;
            yield return StartCoroutine(UIAnimator.PopIn(titleRect, 0.55f));
        }

        // Subtitle fades in
        if (subtitleText != null)
        {
            var cg = subtitleText.GetComponent<CanvasGroup>() ?? subtitleText.gameObject.AddComponent<CanvasGroup>();
            cg.alpha = 0f;
            subtitleText.gameObject.SetActive(true);
            yield return StartCoroutine(UIAnimator.FadeIn(cg, 0.4f));
        }

        yield return new WaitForSeconds(0.15f);

        // Buttons slide in one by one from the right
        if (buttonRects != null)
        {
            foreach (var rt in buttonRects)
            {
                if (rt == null) continue;
                rt.gameObject.SetActive(true);
                StartCoroutine(UIAnimator.SlideIn(rt, Vector2.right * 400f, 0.38f));
                yield return new WaitForSeconds(0.08f);
            }
        }

        yield return new WaitForSeconds(0.3f);
        UpdateStats();
        if (!_coinTickDone) { _coinTickDone = true; StartCoroutine(AnimateCoinTick()); }

        // Idle title pulse
        if (titleRect != null)
            StartCoroutine(IdleTitlePulse());
    }

    private IEnumerator IdleTitlePulse()
    {
        while (true)
        {
            yield return new WaitForSeconds(2.5f);
            yield return StartCoroutine(UIAnimator.Punch(titleRect, 1.04f, 0.5f));
        }
    }

    // Builds the stats line with a specific coin count (so the coin total can be animated).
    private string StatsText(long coinsShown)
    {
        var sm = ScoreManager.Instance;
        return $"Best: {sm.BestScore:N0}    Coins: {coinsShown:N0}    Top Streak: {sm.BestStreak}    Cleared: {sm.LevelsCleared}";
    }

    private bool _coinTickDone;
    // Satisfying count-up of the coin total when the menu first appears.
    private IEnumerator AnimateCoinTick()
    {
        if (ScoreManager.Instance == null || highScoreText == null) yield break;
        int target = ScoreManager.Instance.Coins;
        if (target <= 0) yield break;
        float t = 0f;
        while (t < 1f)
        {
            if (highScoreText == null) yield break;
            t += Time.deltaTime / 0.7f;
            long shown = (long)Mathf.Round(Mathf.Lerp(0f, target, Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(t))));
            highScoreText.text = StatsText(shown);
            yield return null;
        }
        highScoreText.text = StatsText(target);
    }

    private void UpdateStats()
    {
        if (ScoreManager.Instance == null) return;
        if (highScoreText != null)
            highScoreText.text = StatsText(ScoreManager.Instance.Coins);
        if (totalAchievementsText != null)
        {
            var skins = ScoreManager.Skins;
            int si = Mathf.Clamp(ScoreManager.Instance.SelectedSkin, 0, skins.Length - 1);
            totalAchievementsText.text = $"{ScoreManager.Instance.GetUnlockedAchievementCount()}/{ScoreManager.Instance.achievements.Count} Achievements    Skin: {skins[si].name}";
        }
        RefreshBestTimes();
    }

    // ── Panel transitions ─────────────────────────────────────────────────────

    private void StartGame()
    {
        AudioManager.Instance?.PlayButtonClickSound();
        StartCoroutine(TransitionToGame());
    }

    private IEnumerator TransitionToGame()
    {
        // All panels fade to black then load
        if (mainCG != null)
            yield return StartCoroutine(UIAnimator.FadeOut(mainCG, 0.35f, deactivate: false));

        var blackout = new GameObject("Blackout");
        var canvas = GetComponentInParent<Canvas>() ?? FindAnyObjectByType<Canvas>();
        blackout.transform.SetParent(canvas != null ? canvas.transform : transform, false);
        var img = blackout.AddComponent<Image>();
        img.color = new Color(0, 0, 0, 0);
        img.raycastTarget = false;
        var rt = blackout.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
        rt.offsetMin = rt.offsetMax = Vector2.zero;

        float t = 0f;
        while (t < 1f)
        {
            t += Time.deltaTime / 0.3f;
            img.color = new Color(0, 0, 0, UIAnimator.EaseInCubic(Mathf.Clamp01(t)));
            yield return null;
        }

        SceneManager.LoadScene("Game");
    }

    private void ShowSettings()
    {
        AudioManager.Instance?.PlayButtonClickSound();
        BuildSettingsOverlay();
    }

    private void ShowAchievements()
    {
        AudioManager.Instance?.PlayButtonClickSound();
        BuildAchievementsOverlay();
    }

    // ── Runtime overlay UI (scene panels were incomplete) ─────────────────────

    private RectTransform NewRect(string name, Transform parent)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        return go.GetComponent<RectTransform>();
    }

    private TextMeshProUGUI MakeText(Transform parent, string text, int size, Color col, TextAlignmentOptions align)
    {
        TextMeshProUGUI tmp;
        if (_font != null)
        {
            var clone = Instantiate(_font.gameObject, parent, false);
            clone.name = "Txt";
            clone.SetActive(true);
            tmp = clone.GetComponent<TextMeshProUGUI>();
        }
        else
        {
            var go = new GameObject("Txt", typeof(RectTransform));
            go.transform.SetParent(parent, false);
            tmp = go.AddComponent<TextMeshProUGUI>();
        }
        tmp.text = text;
        tmp.fontSize = size;
        tmp.color = col;
        tmp.alignment = align;
        tmp.fontStyle = FontStyles.Normal;
        tmp.raycastTarget = false;
        return tmp;
    }

    private void BuildOverlayScaffold(string title, out Transform content)
    {
        if (_overlay != null) Destroy(_overlay);
        var canvasGO = new GameObject("_MenuOverlay");
        _overlay = canvasGO;
        var c = canvasGO.AddComponent<Canvas>();
        c.renderMode = RenderMode.ScreenSpaceOverlay;
        c.sortingOrder = 300;
        var scaler = canvasGO.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1280f, 720f);
        scaler.matchWidthOrHeight = 0.5f;
        canvasGO.AddComponent<GraphicRaycaster>();

        var bg = NewRect("BG", canvasGO.transform);
        var bgImg = bg.gameObject.AddComponent<Image>();
        bgImg.sprite = SpriteGenerator.CreateMenuBackground();
        bgImg.color = Color.white;
        bg.anchorMin = Vector2.zero; bg.anchorMax = Vector2.one; bg.offsetMin = bg.offsetMax = Vector2.zero;

        // Title parented directly to the canvas so anchors are relative to full screen
        var t = MakeText(canvasGO.transform, title, 54, Color.white, TextAlignmentOptions.Center);
        t.fontStyle = FontStyles.Bold;
        var trt = t.rectTransform;
        trt.anchorMin = new Vector2(0.1f, 0.84f); trt.anchorMax = new Vector2(0.9f, 0.96f);
        trt.offsetMin = trt.offsetMax = Vector2.zero;

        // BACK button
        var backRt = NewRect("BackButton", canvasGO.transform);
        var backImg = backRt.gameObject.AddComponent<Image>();
        backImg.sprite = _rounded; backImg.type = Image.Type.Simple; backImg.color = new Color(0.30f, 0.34f, 0.45f);
        backRt.anchorMin = new Vector2(0.35f, 0.04f); backRt.anchorMax = new Vector2(0.65f, 0.13f);
        backRt.offsetMin = backRt.offsetMax = Vector2.zero;
        var backBtn = backRt.gameObject.AddComponent<Button>();
        backBtn.onClick.AddListener(CloseOverlay);
        backRt.gameObject.AddComponent<ButtonAnimator>();
        var backLbl = MakeText(backRt, "BACK", 30, Color.white, TextAlignmentOptions.Center);
        var blrt = backLbl.rectTransform;
        blrt.anchorMin = Vector2.zero; blrt.anchorMax = Vector2.one; blrt.offsetMin = blrt.offsetMax = Vector2.zero;

        content = canvasGO.transform;
    }

    private void CloseOverlay()
    {
        AudioManager.Instance?.PlayButtonClickSound();
        if (_overlay != null) { Destroy(_overlay); _overlay = null; }
    }

    private void BuildSettingsOverlay()
    {
        BuildOverlayScaffold("SETTINGS", out var content);
        CreateSlider(content, "Music Volume", PlayerPrefs.GetFloat("MusicVolume", 0.5f), 0.70f, SetMusicVolume);
        CreateSlider(content, "SFX Volume",   PlayerPrefs.GetFloat("SFXVolume",   0.7f), 0.54f, SetSFXVolume);
        CreateToggle(content, "Sound",        AudioManager.Instance == null || !AudioManager.Instance.Muted, 0.38f, SetSoundOn,
                     SpriteGenerator.CreateSpeakerIcon(64, AudioManager.Instance != null && AudioManager.Instance.Muted));
        CreateToggle(content, "Vibration",    PlayerPrefs.GetInt("Vibration", 1) == 1,  0.22f, SetVibration);

        // HOW TO PLAY replay button - sits in the empty band above the top slider row
        // (rows occupy y<=0.70) and below the title (y>=0.84), clear of the centered BACK
        // button (x 0.35-0.65 / y 0.04-0.13). Reuses the scaffold's rounded-button pattern.
        var howRt = NewRect("HowToPlayButton", content);
        var howImg = howRt.gameObject.AddComponent<Image>();
        howImg.sprite = _rounded; howImg.type = Image.Type.Simple; howImg.color = new Color(0.30f, 0.34f, 0.45f);
        howRt.anchorMin = new Vector2(0.30f, 0.72f); howRt.anchorMax = new Vector2(0.70f, 0.82f);
        howRt.offsetMin = howRt.offsetMax = Vector2.zero;
        var howBtn = howRt.gameObject.AddComponent<Button>();
        howBtn.onClick.AddListener(() => { AudioManager.Instance?.PlayButtonClickSound(); BuildHowToOverlay(); });
        howRt.gameObject.AddComponent<ButtonAnimator>();
        var howLbl = MakeText(howRt, "HOW TO PLAY", 28, Color.white, TextAlignmentOptions.Center);
        var hlrt = howLbl.rectTransform;
        hlrt.anchorMin = Vector2.zero; hlrt.anchorMax = Vector2.one; hlrt.offsetMin = hlrt.offsetMax = Vector2.zero;
    }

    // Settings "Sound" toggle (ON = audible) - syncs the global mute used by the
    // menu/pause SOUND buttons. Keeps all three mute entry points consistent.
    private void SetSoundOn(bool on)
    {
        AudioManager.Instance?.SetMuted(!on);
        RefreshMuteVisual();
    }

    private void CreateSlider(Transform parent, string label, float initial, float yTop, System.Action<float> onChanged)
    {
        var row = NewRect(label + "Row", parent);
        row.anchorMin = new Vector2(0.18f, yTop - 0.13f); row.anchorMax = new Vector2(0.82f, yTop);
        row.offsetMin = row.offsetMax = Vector2.zero;

        var lbl = MakeText(row, label, 26, Color.white, TextAlignmentOptions.Left);
        var lrt = lbl.rectTransform;
        lrt.anchorMin = new Vector2(0f, 0.55f); lrt.anchorMax = new Vector2(1f, 1f); lrt.offsetMin = lrt.offsetMax = Vector2.zero;

        var sRT = NewRect("Slider", row);
        sRT.anchorMin = new Vector2(0f, 0f); sRT.anchorMax = new Vector2(1f, 0.5f); sRT.offsetMin = sRT.offsetMax = Vector2.zero;
        var slider = sRT.gameObject.AddComponent<Slider>();

        var track = NewRect("BG", sRT);
        var trackImg = track.gameObject.AddComponent<Image>();
        trackImg.sprite = _rounded; trackImg.type = Image.Type.Simple; trackImg.color = new Color(1f, 1f, 1f, 0.20f);
        track.anchorMin = new Vector2(0f, 0.32f); track.anchorMax = new Vector2(1f, 0.68f); track.offsetMin = track.offsetMax = Vector2.zero;

        var fillArea = NewRect("FillArea", sRT);
        fillArea.anchorMin = new Vector2(0f, 0.32f); fillArea.anchorMax = new Vector2(1f, 0.68f);
        fillArea.offsetMin = new Vector2(12f, 0f); fillArea.offsetMax = new Vector2(-12f, 0f);
        var fill = NewRect("Fill", fillArea);
        var fillImg = fill.gameObject.AddComponent<Image>();
        fillImg.sprite = _rounded; fillImg.type = Image.Type.Simple; fillImg.color = new Color(0.30f, 0.78f, 1f, 0.95f);
        fillImg.raycastTarget = false;
        fill.anchorMin = new Vector2(0f, 0f); fill.anchorMax = new Vector2(1f, 1f); fill.offsetMin = fill.offsetMax = Vector2.zero; fill.sizeDelta = Vector2.zero;

        var handleArea = NewRect("HandleArea", sRT);
        handleArea.anchorMin = new Vector2(0f, 0f); handleArea.anchorMax = new Vector2(1f, 1f);
        handleArea.offsetMin = new Vector2(12f, 0f); handleArea.offsetMax = new Vector2(-12f, 0f);
        var handle = NewRect("Handle", handleArea);
        var handleImg = handle.gameObject.AddComponent<Image>();
        handleImg.sprite = _rounded; handleImg.type = Image.Type.Simple; handleImg.color = Color.white;
        handle.sizeDelta = new Vector2(36f, 0f);
        handle.anchorMin = new Vector2(0f, 0f); handle.anchorMax = new Vector2(0f, 1f);

        slider.fillRect = fill;
        slider.handleRect = handle;
        slider.targetGraphic = handleImg;
        slider.direction = Slider.Direction.LeftToRight;
        slider.minValue = 0f; slider.maxValue = 1f;
        slider.SetValueWithoutNotify(initial);
        slider.onValueChanged.AddListener(v => onChanged(v));
    }

    private void CreateToggle(Transform parent, string label, bool initial, float yTop, System.Action<bool> onChanged, Sprite icon = null)
    {
        var row = NewRect(label + "Row", parent);
        row.anchorMin = new Vector2(0.18f, yTop - 0.13f); row.anchorMax = new Vector2(0.82f, yTop);
        row.offsetMin = row.offsetMax = Vector2.zero;

        float labelLeft = 0f;
        if (icon != null)
        {
            var icoRt = NewRect("Icon", row);
            var icoImg = icoRt.gameObject.AddComponent<Image>();
            icoImg.sprite = icon; icoImg.preserveAspect = true; icoImg.raycastTarget = false;
            icoRt.anchorMin = new Vector2(0f, 0.12f); icoRt.anchorMax = new Vector2(0.10f, 0.88f); icoRt.offsetMin = icoRt.offsetMax = Vector2.zero;
            labelLeft = 0.12f;
        }

        var lbl = MakeText(row, label, 26, Color.white, TextAlignmentOptions.Left);
        var lrt = lbl.rectTransform;
        lrt.anchorMin = new Vector2(labelLeft, 0f); lrt.anchorMax = new Vector2(0.7f, 1f); lrt.offsetMin = lrt.offsetMax = Vector2.zero;

        var tRT = NewRect("Toggle", row);
        tRT.anchorMin = new Vector2(0.80f, 0.2f); tRT.anchorMax = new Vector2(0.98f, 0.8f);
        tRT.offsetMin = tRT.offsetMax = Vector2.zero;
        var toggle = tRT.gameObject.AddComponent<Toggle>();

        var box = NewRect("Box", tRT);
        var boxImg = box.gameObject.AddComponent<Image>();
        boxImg.sprite = _rounded; boxImg.type = Image.Type.Simple; boxImg.color = new Color(1f, 1f, 1f, 0.22f);
        box.anchorMin = Vector2.zero; box.anchorMax = Vector2.one; box.offsetMin = box.offsetMax = Vector2.zero;

        var check = NewRect("Check", box);
        var checkImg = check.gameObject.AddComponent<Image>();
        checkImg.sprite = _rounded; checkImg.type = Image.Type.Simple; checkImg.color = new Color(0.25f, 0.85f, 0.4f);
        checkImg.raycastTarget = false;
        check.anchorMin = new Vector2(0.18f, 0.18f); check.anchorMax = new Vector2(0.82f, 0.82f);
        check.offsetMin = check.offsetMax = Vector2.zero;

        toggle.targetGraphic = boxImg;
        toggle.graphic = checkImg;
        toggle.isOn = initial;
        toggle.onValueChanged.AddListener(v => onChanged(v));
    }

    private void BuildAchievementsOverlay()
    {
        BuildOverlayScaffold("ACHIEVEMENTS", out var content);
        var list = ScoreManager.Instance != null ? ScoreManager.Instance.achievements : null;
        if (list == null || list.Count == 0)
        {
            var none = MakeText(content, "No achievements yet", 28, new Color(0.7f, 0.75f, 0.85f), TextAlignmentOptions.Center);
            var nrt = none.rectTransform;
            nrt.anchorMin = new Vector2(0.1f, 0.45f); nrt.anchorMax = new Vector2(0.9f, 0.55f); nrt.offsetMin = nrt.offsetMax = Vector2.zero;
            return;
        }

        int n = list.Count;
        float top = 0.80f, bottom = 0.17f;
        float rowH = (top - bottom) / n;
        for (int i = 0; i < n; i++)
        {
            var a = list[i];
            float y1 = top - i * rowH;
            float y0 = y1 - rowH * 0.86f;

            var card = NewRect("Ach" + i, content);
            var cardImg = card.gameObject.AddComponent<Image>();
            cardImg.sprite = _rounded; cardImg.type = Image.Type.Simple;
            cardImg.color = a.isUnlocked ? new Color(0.20f, 0.45f, 0.26f) : new Color(0.16f, 0.17f, 0.24f);
            cardImg.raycastTarget = false;
            card.anchorMin = new Vector2(0.12f, y0); card.anchorMax = new Vector2(0.88f, y1);
            card.offsetMin = card.offsetMax = Vector2.zero;

            string mark = a.isUnlocked ? "[x] " : "[  ] ";  // [x] unlocked / [ ] locked
            var name = MakeText(card, mark + a.name, 26, a.isUnlocked ? new Color(1f, 0.92f, 0.5f) : Color.white, TextAlignmentOptions.Left);
            var nrt = name.rectTransform;
            nrt.anchorMin = new Vector2(0.04f, 0.5f); nrt.anchorMax = new Vector2(0.98f, 1f); nrt.offsetMin = nrt.offsetMax = Vector2.zero;

            // Progress hint for locked Streak/Coins achievements (Score-kind has no stored current value).
            string descText = a.description;
            var smA = ScoreManager.Instance;
            if (!a.isUnlocked && smA != null)
            {
                int cur = a.kind == ScoreManager.AchKind.Streak ? smA.BestStreak
                        : a.kind == ScoreManager.AchKind.Coins  ? smA.Coins
                        : -1;
                if (cur >= 0)
                    descText += $"   <color=#FFD23C>({Mathf.Min(cur, a.requiredScore)}/{a.requiredScore})</color>";
            }
            var desc = MakeText(card, descText, 18, new Color(0.85f, 0.88f, 0.95f), TextAlignmentOptions.Left);
            var drt = desc.rectTransform;
            drt.anchorMin = new Vector2(0.04f, 0.05f); drt.anchorMax = new Vector2(0.98f, 0.5f); drt.offsetMin = drt.offsetMax = Vector2.zero;
        }
    }

    public void BackToMainMenu()
    {
        AudioManager.Instance?.PlayButtonClickSound();
        if (settingsPanel != null && settingsPanel.activeSelf)
            StartCoroutine(SwapPanel(settingsPanel, settingsCG, mainPanel, mainCG));
        else
            StartCoroutine(SwapPanel(achievementsPanel, achievementsCG, mainPanel, mainCG));
    }

    private IEnumerator SwapPanel(GameObject fromPanel, CanvasGroup fromCG,
                                   GameObject toPanel, CanvasGroup toCG)
    {
        if (fromCG != null)
        {
            fromCG.interactable = false;
            fromCG.blocksRaycasts = false;
            yield return StartCoroutine(UIAnimator.FadeOut(fromCG, 0.2f, deactivate: false));
            fromPanel.SetActive(false);
        }

        toPanel.SetActive(true);
        if (toCG != null)
        {
            toCG.alpha = 0f;
            toCG.interactable = true;
            toCG.blocksRaycasts = true;
        }

        var toRt = toPanel.GetComponent<RectTransform>();
        if (toRt != null)
            StartCoroutine(UIAnimator.SlideIn(toRt, Vector2.right * 300f, 0.28f));

        if (toCG != null)
            yield return StartCoroutine(UIAnimator.FadeIn(toCG, 0.25f));
    }

    private void QuitGame()
    {
        AudioManager.Instance?.PlayButtonClickSound();
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    // ── Settings persistence ──────────────────────────────────────────────────

    private void LoadSettings()
    {
        if (musicVolumeSlider != null) musicVolumeSlider.value = PlayerPrefs.GetFloat("MusicVolume", 0.5f);
        if (sfxVolumeSlider != null)   sfxVolumeSlider.value   = PlayerPrefs.GetFloat("SFXVolume",   0.7f);
        if (vibrationToggle != null)   vibrationToggle.isOn    = PlayerPrefs.GetInt("Vibration", 1) == 1;
    }

    private void SetMusicVolume(float v) { AudioManager.Instance?.SetMusicVolume(v); PlayerPrefs.SetFloat("MusicVolume", v); PlayerPrefs.Save(); }
    private void SetSFXVolume(float v)   { AudioManager.Instance?.SetSFXVolume(v);   PlayerPrefs.SetFloat("SFXVolume",   v); PlayerPrefs.Save(); }
    private void SetVibration(bool on)   { PlayerPrefs.SetInt("Vibration", on ? 1 : 0); PlayerPrefs.Save(); }
}
