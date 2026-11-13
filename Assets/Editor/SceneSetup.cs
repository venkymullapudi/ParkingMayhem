using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;

/// <summary>
/// Run via ParkingRush > Setup All Scenes to create MainMenu and Game scenes
/// with all required GameObjects, scripts, and UI wired up.
/// </summary>
public static class SceneSetup
{
    [MenuItem("ParkingRush/Setup All Scenes")]
    public static void SetupAllScenes()
    {
        SetupMainMenuScene();
        SetupGameScene();
        Debug.Log("All scenes created. Open them from Assets/Scenes/.");
    }

    // ── Main Menu ────────────────────────────────────────────────────────────

    [MenuItem("ParkingRush/Setup MainMenu Scene")]
    public static void SetupMainMenuScene()
    {
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        scene.name = "MainMenu";

        // Camera
        var camGo = new GameObject("Main Camera");
        var cam = camGo.AddComponent<Camera>();
        cam.orthographic = true;
        cam.orthographicSize = 5f;
        cam.backgroundColor = new Color(0.1f, 0.1f, 0.15f);
        camGo.tag = "MainCamera";

        // Managers
        CreateManagerObject("AudioManager").AddComponent<AudioManager>();
        CreateManagerObject("ScoreManager").AddComponent<ScoreManager>();
        CreateManagerObject("LevelDataManager").AddComponent<LevelDataManager>();

        // Canvas
        var canvas = CreateCanvas("MainMenuCanvas");
        var mainPanel = CreatePanel(canvas.transform, "MainPanel", Color.clear);
        var settingsPanel = CreatePanel(canvas.transform, "SettingsPanel", new Color(0, 0, 0, 0.9f));
        var achievementsPanel = CreatePanel(canvas.transform, "AchievementsPanel", new Color(0, 0, 0, 0.9f));
        settingsPanel.SetActive(false);
        achievementsPanel.SetActive(false);

        // Main panel contents
        CreateLabel(mainPanel.transform, "TitleText", "PARKING RUSH", 60, new Vector2(0, 150));
        var highScoreText = CreateLabel(mainPanel.transform, "HighScoreText", "High Score: 0", 28, new Vector2(0, 60));
        var achievementsText = CreateLabel(mainPanel.transform, "AchievementsText", "Achievements: 0/0", 22, new Vector2(0, 20));
        var playBtn = CreateButton(mainPanel.transform, "PlayButton", "PLAY", new Vector2(0, -60));
        var settingsBtn = CreateButton(mainPanel.transform, "SettingsButton", "SETTINGS", new Vector2(0, -130));
        var achievementsBtn = CreateButton(mainPanel.transform, "AchievementsButton", "ACHIEVEMENTS", new Vector2(0, -200));
        var quitBtn = CreateButton(mainPanel.transform, "QuitButton", "QUIT", new Vector2(0, -270));

        // Settings panel
        CreateLabel(settingsPanel.transform, "SettingsTitle", "SETTINGS", 40, new Vector2(0, 180));
        CreateLabel(settingsPanel.transform, "MusicLabel", "Music Volume", 24, new Vector2(-150, 80));
        var musicSlider = CreateSlider(settingsPanel.transform, "MusicVolumeSlider", new Vector2(50, 80));
        CreateLabel(settingsPanel.transform, "SFXLabel", "SFX Volume", 24, new Vector2(-150, 20));
        var sfxSlider = CreateSlider(settingsPanel.transform, "SFXVolumeSlider", new Vector2(50, 20));
        CreateLabel(settingsPanel.transform, "VibrationLabel", "Vibration", 24, new Vector2(-150, -40));
        var vibToggle = CreateToggle(settingsPanel.transform, "VibrationToggle", new Vector2(50, -40));
        CreateButton(settingsPanel.transform, "BackButton", "BACK", new Vector2(0, -140));

        // Wire up MainMenuManager
        var mmGo = new GameObject("MainMenuManager");
        var mm = mmGo.AddComponent<MainMenuManager>();
        mm.playButton = playBtn;
        mm.settingsButton = settingsBtn;
        mm.achievementsButton = achievementsBtn;
        mm.quitButton = quitBtn;
        mm.highScoreText = highScoreText.GetComponent<TextMeshProUGUI>();
        mm.totalAchievementsText = achievementsText.GetComponent<TextMeshProUGUI>();
        mm.mainPanel = mainPanel;
        mm.settingsPanel = settingsPanel;
        mm.achievementsPanel = achievementsPanel;
        mm.musicVolumeSlider = musicSlider;
        mm.sfxVolumeSlider = sfxSlider;
        mm.vibrationToggle = vibToggle;

        EditorSceneManager.SaveScene(scene, "Assets/Scenes/MainMenu.unity");
        Debug.Log("MainMenu scene created at Assets/Scenes/MainMenu.unity");
    }

    // ── Game Scene ───────────────────────────────────────────────────────────

    [MenuItem("ParkingRush/Setup Game Scene")]
    public static void SetupGameScene()
    {
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        scene.name = "Game";

        // Camera
        var camGo = new GameObject("Main Camera");
        var cam = camGo.AddComponent<Camera>();
        cam.orthographic = true;
        cam.orthographicSize = 8f;
        cam.backgroundColor = new Color(0.15f, 0.15f, 0.2f);
        camGo.tag = "MainCamera";

        // Managers
        CreateManagerObject("GameManager").AddComponent<GameManager>();
        CreateManagerObject("LevelManager").AddComponent<LevelManager>();
        CreateManagerObject("InputManager").AddComponent<InputManager>();
        CreateManagerObject("AudioManager").AddComponent<AudioManager>();
        CreateManagerObject("ScoreManager").AddComponent<ScoreManager>();
        CreateManagerObject("LevelDataManager").AddComponent<LevelDataManager>();
        CreateManagerObject("EventSystem").AddComponent<UnityEngine.EventSystems.EventSystem>()
            .gameObject.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();

        // Canvas
        var canvas = CreateCanvas("GameCanvas");

        // HUD panel
        var hudPanel = CreatePanel(canvas.transform, "HUDPanel", Color.clear);
        var levelText = CreateLabel(hudPanel.transform, "LevelText", "Level: 1", 32, new Vector2(-350, 230));
        var vehicleText = CreateLabel(hudPanel.transform, "VehicleTypeText", "Vehicle: Square", 22, new Vector2(-350, 190));
        var terrainText = CreateLabel(hudPanel.transform, "TerrainTypeText", "Terrain: Road", 22, new Vector2(-350, 155));
        var progressSlider = CreateSlider(hudPanel.transform, "ProgressSlider", new Vector2(0, 260));
        var pauseBtn = CreateButton(hudPanel.transform, "PauseButton", "II", new Vector2(360, 230));

        // Pause menu
        var pausePanel = CreatePanel(canvas.transform, "PauseMenuPanel", new Color(0, 0, 0, 0.85f));
        CreateLabel(pausePanel.transform, "PauseTitle", "PAUSED", 50, new Vector2(0, 100));
        CreateButton(pausePanel.transform, "ResumeButton", "RESUME", new Vector2(0, 20));
        CreateButton(pausePanel.transform, "RestartButton2", "RESTART", new Vector2(0, -50));
        CreateButton(pausePanel.transform, "MainMenuButton", "MAIN MENU", new Vector2(0, -120));
        pausePanel.SetActive(false);

        // Level complete panel
        var completePanel = CreatePanel(canvas.transform, "LevelCompletePanel", new Color(0, 0.15f, 0, 0.92f));
        CreateLabel(completePanel.transform, "LevelCompleteText", "Level Complete!", 50, new Vector2(0, 120));
        var scoreText = CreateLabel(completePanel.transform, "ScoreText", "Score: 0", 36, new Vector2(0, 50));
        var timeText = CreateLabel(completePanel.transform, "TimeText", "Time: 0:00", 28, new Vector2(0, 10));
        var nextBtn = CreateButton(completePanel.transform, "NextLevelButton", "NEXT LEVEL", new Vector2(0, -60));
        var restartBtn = CreateButton(completePanel.transform, "RestartButton", "RESTART", new Vector2(0, -130));
        completePanel.SetActive(false);

        // Game over panel
        var gameOverPanel = CreatePanel(canvas.transform, "GameOverPanel", new Color(0.15f, 0, 0, 0.92f));
        CreateLabel(gameOverPanel.transform, "GameOverText", "GAME OVER", 50, new Vector2(0, 80));
        CreateButton(gameOverPanel.transform, "RetryButton", "RETRY", new Vector2(0, -10));
        CreateButton(gameOverPanel.transform, "MenuButton", "MENU", new Vector2(0, -80));
        gameOverPanel.SetActive(false);

        // Achievement toast
        var toastPanel = CreatePanel(canvas.transform, "AchievementToast", new Color(0.8f, 0.6f, 0f, 0.95f));
        var toastRt = toastPanel.GetComponent<RectTransform>();
        toastRt.anchoredPosition = new Vector2(0, -260);
        toastRt.sizeDelta = new Vector2(500, 80);
        var toastText = CreateLabel(toastPanel.transform, "ToastText", "Achievement Unlocked!", 22, Vector2.zero);
        toastPanel.SetActive(false);

        // Wire up UIManager
        var uiGo = new GameObject("UIManager");
        var ui = uiGo.AddComponent<UIManager>();
        ui.gamePanel = hudPanel;
        ui.levelCompletePanel = completePanel;
        ui.gameOverPanel = gameOverPanel;
        ui.pauseMenuPanel = pausePanel;
        ui.achievementToastPanel = toastPanel;
        ui.achievementToastText = toastText.GetComponent<TextMeshProUGUI>();
        ui.levelText = levelText.GetComponent<TextMeshProUGUI>();
        ui.vehicleTypeText = vehicleText.GetComponent<TextMeshProUGUI>();
        ui.terrainTypeText = terrainText.GetComponent<TextMeshProUGUI>();
        ui.progressSlider = progressSlider;
        ui.nextLevelButton = nextBtn;
        ui.restartButton = restartBtn;

        EditorSceneManager.SaveScene(scene, "Assets/Scenes/Game.unity");
        Debug.Log("Game scene created at Assets/Scenes/Game.unity");
    }

    // ── Helpers ──────────────────────────────────────────────────────────────

    private static GameObject CreateManagerObject(string name)
    {
        var go = new GameObject(name);
        return go;
    }

    private static Canvas CreateCanvas(string name)
    {
        var go = new GameObject(name);
        var canvas = go.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        go.AddComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        go.GetComponent<CanvasScaler>().referenceResolution = new Vector2(1080, 1920);
        go.AddComponent<GraphicRaycaster>();
        return canvas;
    }

    private static GameObject CreatePanel(Transform parent, string name, Color color)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        var rt = go.AddComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = rt.offsetMax = Vector2.zero;
        if (color.a > 0)
        {
            var img = go.AddComponent<Image>();
            img.color = color;
        }
        return go;
    }

    private static GameObject CreateLabel(Transform parent, string name, string text, int fontSize, Vector2 pos)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        var rt = go.AddComponent<RectTransform>();
        rt.anchoredPosition = pos;
        rt.sizeDelta = new Vector2(600, 60);
        var tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = fontSize;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = Color.white;
        return go;
    }

    private static Button CreateButton(Transform parent, string name, string label, Vector2 pos)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        var rt = go.AddComponent<RectTransform>();
        rt.anchoredPosition = pos;
        rt.sizeDelta = new Vector2(340, 60);
        var img = go.AddComponent<Image>();
        img.color = new Color(0.2f, 0.5f, 1f);
        var btn = go.AddComponent<Button>();

        var textGo = new GameObject("Text");
        textGo.transform.SetParent(go.transform, false);
        var trt = textGo.AddComponent<RectTransform>();
        trt.anchorMin = Vector2.zero;
        trt.anchorMax = Vector2.one;
        trt.offsetMin = trt.offsetMax = Vector2.zero;
        var tmp = textGo.AddComponent<TextMeshProUGUI>();
        tmp.text = label;
        tmp.fontSize = 26;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = Color.white;

        return btn;
    }

    private static Slider CreateSlider(Transform parent, string name, Vector2 pos)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        var rt = go.AddComponent<RectTransform>();
        rt.anchoredPosition = pos;
        rt.sizeDelta = new Vector2(300, 30);
        var slider = go.AddComponent<Slider>();
        slider.minValue = 0f;
        slider.maxValue = 1f;
        slider.value = 0.7f;
        return slider;
    }

    private static Toggle CreateToggle(Transform parent, string name, Vector2 pos)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        var rt = go.AddComponent<RectTransform>();
        rt.anchoredPosition = pos;
        rt.sizeDelta = new Vector2(50, 50);
        var toggle = go.AddComponent<Toggle>();
        toggle.isOn = true;
        return toggle;
    }
}
