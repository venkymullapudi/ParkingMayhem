using UnityEngine;
using UnityEngine.UI;
using TMPro;

// Persistent gameplay overlay — created once per session, rebuilt each level.
// Provides:
//   1. Zone compass  – arrow on the screen edge pointing to the parking zone when off-screen.
//   2. Speedometer   – compact bar at bottom-center showing current vehicle speed.
public class GameHUD : MonoBehaviour
{
    public static GameHUD Instance { get; private set; }

    // ── Compass ───────────────────────────────────────────────────────────────
    private GameObject  _compassGO;
    private RectTransform _arrowRT;
    private TextMeshProUGUI _distLabel;
    private CanvasGroup _compassCG;

    // ── Speedometer ───────────────────────────────────────────────────────────
    private GameObject _spdGO;
    private RectTransform _spdFill;
    private TextMeshProUGUI _spdLabel;

    // ── Alignment gauge ───────────────────────────────────────────────────────
    private GameObject _alignGO;
    private RectTransform _angleFill;
    private RectTransform _centerFill;
    private RectTransform _parkSpeedFill;
    private Image _angleFillImg;
    private Image _centerFillImg;
    private Image _parkSpeedFillImg;
    private CanvasGroup _alignCG;

    // ── Shared ────────────────────────────────────────────────────────────────
    private Canvas _canvas;
    private RectTransform _canvasRT;
    private TextMeshProUGUI _fontSrc;
    private Camera _cam;

    private static readonly Color _compassColor = new Color(1f, 0.92f, 0.28f, 0.92f);  // golden
    private static readonly Color _spdLow  = new Color(0.22f, 0.85f, 0.36f);
    private static readonly Color _spdMid  = new Color(1.00f, 0.82f, 0.14f);
    private static readonly Color _spdHigh = new Color(1.00f, 0.35f, 0.18f);

    // ── Lifecycle ─────────────────────────────────────────────────────────────

    private void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void OnDestroy() { if (Instance == this) Instance = null; }

    // Called by UIManager.ShowGameUI each level
    public void Show(TextMeshProUGUI fontSrc)
    {
        _fontSrc = fontSrc;
        _cam     = Camera.main;

        if (_canvas == null) BuildCanvas();
        RebuildWidgets();
        gameObject.SetActive(true);
    }

    public void Hide()
    {
        if (_compassCG != null) _compassCG.alpha = 0f;
        if (_alignCG   != null) _alignCG.alpha   = 0f;
        gameObject.SetActive(false);
    }

    // ── Canvas setup ──────────────────────────────────────────────────────────

    private void BuildCanvas()
    {
        var cgo = new GameObject("_GameHUD_Canvas");
        cgo.transform.SetParent(transform, false);
        // (No DontDestroyOnLoad here — cgo is a child of this already-persistent GameHUD object;
        //  calling DDOL on a non-root object only logs a warning and does nothing.)

        _canvas = cgo.AddComponent<Canvas>();
        _canvas.renderMode  = RenderMode.ScreenSpaceOverlay;
        _canvas.sortingOrder = 84;  // below OSC (85) but above gameplay

        var scaler = cgo.AddComponent<CanvasScaler>();
        scaler.uiScaleMode          = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution  = new Vector2(1280f, 720f);
        scaler.matchWidthOrHeight   = 0.5f;

        cgo.AddComponent<GraphicRaycaster>();
        _canvasRT = cgo.GetComponent<RectTransform>();
    }

    // Sprites are identical every rebuild — cache them once. (Previously each level baked 5 new
    // Texture2Ds and Destroy(gameObject) does not free the runtime Sprite/Texture assets, so
    // texture memory grew unboundedly across a long session.)
    private static Sprite _sprBar320, _sprBar200;
    private static readonly System.Collections.Generic.Dictionary<Color, Sprite> _sprArrows = new();
    private static Sprite CachedBar320() => _sprBar320 ??= SpriteGenerator.CreateRoundedRect(320, 26, 0.5f);
    private static Sprite CachedBar200() => _sprBar200 ??= SpriteGenerator.CreateRoundedRect(200, 18, 0.5f);
    private static Sprite CachedArrow(Color c)
    {
        if (!_sprArrows.TryGetValue(c, out var s) || s == null)
            _sprArrows[c] = s = SpriteGenerator.CreateRegularPolygon(3, 64, c);
        return s;
    }

    private void RebuildWidgets()
    {
        // Destroy previous per-level widgets
        if (_compassGO != null) Destroy(_compassGO);
        if (_spdGO     != null) Destroy(_spdGO);
        if (_alignGO   != null) Destroy(_alignGO);

        BuildCompass();
        BuildSpeedometer();
        BuildAlignGauge();
    }

    // ── Zone compass ──────────────────────────────────────────────────────────

    private void BuildCompass()
    {
        _compassGO = new GameObject("Compass");
        _compassGO.transform.SetParent(_canvas.transform, false);
        _compassCG = _compassGO.AddComponent<CanvasGroup>();
        _compassCG.blocksRaycasts = false;
        _compassCG.alpha = 0f;

        // Arrow: equilateral triangle sprite (triangle = RegularPolygon(3))
        var arrowGO = new GameObject("Arrow", typeof(RectTransform));
        arrowGO.transform.SetParent(_compassGO.transform, false);
        var arrowImg = arrowGO.AddComponent<Image>();
        arrowImg.sprite = CachedArrow(new Color(_compassColor.r, _compassColor.g, _compassColor.b));
        arrowImg.raycastTarget = false;
        _arrowRT = arrowGO.GetComponent<RectTransform>();
        _arrowRT.sizeDelta = new Vector2(56f, 56f);   // larger = easier to spot while driving

        // Distance label below the arrow
        if (_fontSrc != null)
        {
            var lblGO = Object.Instantiate(_fontSrc.gameObject, _compassGO.transform, false);
            _distLabel = lblGO.GetComponent<TextMeshProUGUI>();
            _distLabel.text        = "";
            _distLabel.fontSize    = 18f;
            _distLabel.color       = _compassColor;
            _distLabel.fontStyle   = FontStyles.Bold;
            _distLabel.alignment   = TextAlignmentOptions.Center;
            _distLabel.raycastTarget = false;
            var lrt = lblGO.GetComponent<RectTransform>();
            lrt.sizeDelta = new Vector2(120f, 28f);
        }
    }

    // ── Speedometer ───────────────────────────────────────────────────────────

    private void BuildSpeedometer()
    {
        _spdGO = new GameObject("Speedometer");
        _spdGO.transform.SetParent(_canvas.transform, false);

        // Background pill
        var bgGO = new GameObject("BG", typeof(RectTransform));
        bgGO.transform.SetParent(_spdGO.transform, false);
        var bgImg = bgGO.AddComponent<Image>();
        bgImg.sprite       = CachedBar320();
        bgImg.color        = new Color(0f, 0f, 0f, 0.42f);
        bgImg.raycastTarget = false;
        var bgRT = bgGO.GetComponent<RectTransform>();
        // Bottom-centre, sits above the OSC buttons (PAD=20 + button=128 + gap=10 = y=158)
        bgRT.anchorMin = bgRT.anchorMax = new Vector2(0.5f, 0f);
        bgRT.pivot = new Vector2(0.5f, 0f);
        bgRT.anchoredPosition = new Vector2(0f, 162f);
        bgRT.sizeDelta = new Vector2(320f, 22f);

        // Fill bar (child of bg, anchored left)
        var fillGO = new GameObject("Fill", typeof(RectTransform));
        fillGO.transform.SetParent(bgGO.transform, false);
        var fillImg = fillGO.AddComponent<Image>();
        fillImg.sprite       = CachedBar320();
        fillImg.color        = _spdLow;
        fillImg.raycastTarget = false;
        _spdFill = fillGO.GetComponent<RectTransform>();
        _spdFill.anchorMin = new Vector2(0f, 0f);
        _spdFill.anchorMax = new Vector2(0f, 1f);
        _spdFill.pivot     = new Vector2(0f, 0.5f);
        _spdFill.offsetMin = Vector2.zero;
        _spdFill.offsetMax = new Vector2(0f, 0f);

        // "SPD" label to the left of the bar
        if (_fontSrc != null)
        {
            var lblGO = Object.Instantiate(_fontSrc.gameObject, bgGO.transform, false);
            _spdLabel = lblGO.GetComponent<TextMeshProUGUI>();
            _spdLabel.text       = "SPD";
            _spdLabel.fontSize   = 11f;
            _spdLabel.color      = new Color(1f, 1f, 1f, 0.50f);
            _spdLabel.fontStyle  = FontStyles.Normal;
            _spdLabel.alignment  = TextAlignmentOptions.Left;
            _spdLabel.raycastTarget = false;
            var lrt = lblGO.GetComponent<RectTransform>();
            lrt.anchorMin = Vector2.zero;
            lrt.anchorMax = Vector2.one;
            lrt.offsetMin = new Vector2(6f, 0f);
            lrt.offsetMax = Vector2.zero;
        }
    }

    // ── Alignment gauge ───────────────────────────────────────────────────────
    // Three bars shown left-of-center when the vehicle is within 3.5 units of the zone.
    // "ANGLE" shows angle match, "CENTER" shows position closeness, "SPEED" shows whether
    // the vehicle is slow enough to park — all three must be green simultaneously (and held
    // briefly) to complete the park, so all three need to be visible, not just two of three.

    private void BuildAlignGauge()
    {
        _alignGO = new GameObject("AlignGauge", typeof(RectTransform));
        _alignGO.transform.SetParent(_canvas.transform, false);
        _alignCG = _alignGO.AddComponent<CanvasGroup>();
        _alignCG.blocksRaycasts = false;
        _alignCG.alpha = 0f;

        float barW = 200f, barH = 18f, gap = 30f;
        // Positioned lower-left, above the L/R buttons
        var root = _alignGO.GetComponent<RectTransform>();
        if (root == null) { Destroy(_alignGO); _alignGO = null; return; }
        root.anchorMin = root.anchorMax = new Vector2(0.08f, 0.0f);
        root.pivot     = new Vector2(0f, 0f);
        root.anchoredPosition = new Vector2(0f, 162f);
        root.sizeDelta = new Vector2(barW, barH * 3 + gap * 1.5f);

        _angleFill     = BuildBar(_alignGO.transform, 0, barW, barH, gap, "ANGLE",  out _angleFillImg);
        _centerFill    = BuildBar(_alignGO.transform, 1, barW, barH, gap, "CENTER", out _centerFillImg);
        _parkSpeedFill = BuildBar(_alignGO.transform, 2, barW, barH, gap, "SPEED",  out _parkSpeedFillImg);
    }

    private RectTransform BuildBar(Transform parent, int row, float barW, float barH, float gap,
                                   string label, out Image fillImg)
    {
        float yBase = row * (barH + gap * 0.5f);

        // Label
        if (_fontSrc != null)
        {
            var lblGO = Object.Instantiate(_fontSrc.gameObject, parent, false);
            var lbl = lblGO.GetComponent<TextMeshProUGUI>();
            lbl.text = label; lbl.fontSize = 10f;
            lbl.color = new Color(0.75f, 0.85f, 1f, 0.70f);
            lbl.fontStyle = FontStyles.Normal;
            lbl.alignment = TextAlignmentOptions.Left;
            lbl.raycastTarget = false;
            var lrt = lblGO.GetComponent<RectTransform>();
            lrt.anchorMin = lrt.anchorMax = Vector2.zero;
            lrt.pivot = Vector2.zero;
            lrt.anchoredPosition = new Vector2(0f, yBase + barH + 2f);
            lrt.sizeDelta = new Vector2(barW, 14f);
        }

        // Track
        var tGO = new GameObject("Track", typeof(RectTransform));
        tGO.transform.SetParent(parent, false);
        var tImg = tGO.AddComponent<Image>();
        tImg.color = new Color(1f, 1f, 1f, 0.10f);
        tImg.raycastTarget = false;
        var tRT = tGO.GetComponent<RectTransform>();
        tRT.anchorMin = tRT.anchorMax = Vector2.zero;
        tRT.pivot     = Vector2.zero;
        tRT.anchoredPosition = new Vector2(0f, yBase);
        tRT.sizeDelta = new Vector2(barW, barH);

        // Fill
        var fGO = new GameObject("Fill", typeof(RectTransform));
        fGO.transform.SetParent(tGO.transform, false);
        fillImg = fGO.AddComponent<Image>();
        fillImg.sprite       = CachedBar200();
        fillImg.color        = new Color(0.22f, 0.85f, 0.36f);
        fillImg.raycastTarget = false;
        var fRT = fGO.GetComponent<RectTransform>();
        fRT.anchorMin = new Vector2(0f, 0f);
        fRT.anchorMax = new Vector2(0f, 1f);
        fRT.pivot     = new Vector2(0f, 0.5f);
        fRT.offsetMin = Vector2.zero;
        fRT.offsetMax = new Vector2(0f, 0f);

        return fRT;
    }

    // ── Per-frame updates ─────────────────────────────────────────────────────

    private void Update()
    {
        if (_cam == null) _cam = Camera.main;
        UpdateSpeedometer();
        UpdateCompass();
        UpdateAlignGauge();
    }

    private void UpdateSpeedometer()
    {
        if (_spdFill == null) return;
        var vc = FindVehicle();
        if (vc == null) { _spdFill.offsetMax = new Vector2(0f, 0f); return; }

        float norm = Mathf.Clamp01(vc.CurrentSpeed / Mathf.Max(vc.MaxSpeed, 0.1f));
        // Animate fill width: offsetMax.x goes 0 → barWidth (320px)
        float targetW = norm * 320f;
        float curW = _spdFill.offsetMax.x;
        _spdFill.offsetMax = new Vector2(Mathf.Lerp(curW, targetW, Time.deltaTime * 8f), 0f);

        // Color gradient
        var fillImg = _spdFill.GetComponent<Image>();
        if (fillImg != null)
        {
            Color col = norm < 0.5f
                ? Color.Lerp(_spdLow,  _spdMid,  norm * 2f)
                : Color.Lerp(_spdMid,  _spdHigh, (norm - 0.5f) * 2f);
            fillImg.color = col;
        }

        // Numeric readout — scale world-units/s to a fun km/h equivalent
        if (_spdLabel != null)
        {
            int kmh = Mathf.RoundToInt(vc.CurrentSpeed * 22f);
            _spdLabel.text    = $"{kmh} km/h";
            _spdLabel.fontSize = 11f;
            _spdLabel.color   = new Color(1f, 1f, 1f, 0.55f + norm * 0.45f);
        }
    }

    private void UpdateCompass()
    {
        if (_arrowRT == null || _compassCG == null) return;

        // Self-heal the camera reference: _cam is captured once in Show(), but GameHUD is
        // DontDestroyOnLoad and the game camera is recreated per scene, so a stale/destroyed
        // camera would leave the compass permanently hidden (WorldToViewportPoint needs it).
        if (_cam == null) _cam = Camera.main;

        var zone = FindZone();
        var vc   = FindVehicle();
        if (zone == null || vc == null || _cam == null)
        {
            _compassCG.alpha = Mathf.MoveTowards(_compassCG.alpha, 0f, Time.deltaTime * 3f);
            return;
        }

        Vector3 worldPos  = zone.position;
        Vector3 viewPos   = _cam.WorldToViewportPoint(worldPos);
        bool onScreen     = viewPos.z > 0f
                          && viewPos.x > 0.04f && viewPos.x < 0.96f
                          && viewPos.y > 0.06f && viewPos.y < 0.94f;
        float dist        = Vector2.Distance(vc.transform.position, zone.position);

        // Always guide the player toward the spot (the camera follows the car, so the world
        // scrolls past and it can be disorienting to tell where the spot is). Fade the guide
        // out only once you're basically on it, so it never clutters the final park.
        bool nearlyParked = dist < 2.0f && onScreen;
        float targetAlpha = nearlyParked ? 0f : 1f;
        _compassCG.alpha  = Mathf.MoveTowards(_compassCG.alpha, targetAlpha, Time.deltaTime * 3f);

        if (_compassCG.alpha < 0.02f) return;

        Vector2 screenCenter = new Vector2(Screen.width * 0.5f, Screen.height * 0.5f);
        Vector3 screenPos3   = _cam.WorldToScreenPoint(worldPos);
        Vector2 screenPos    = new Vector2(screenPos3.x, screenPos3.y);
        if (viewPos.z < 0f) screenPos = screenCenter + (screenCenter - screenPos); // flip when behind

        Vector2 dir = (screenPos - screenCenter);
        if (dir.sqrMagnitude < 0.01f) dir = Vector2.up; else dir.Normalize();

        Vector2 anchorScreen;
        if (onScreen)
        {
            // Zone is visible: hover the arrow just above the spot so it labels it directly.
            anchorScreen = screenPos + Vector2.up * 46f;
        }
        else
        {
            // Zone is off-screen: pin the arrow to the screen edge along the direction to it.
            float mx = Screen.width  * 0.5f - 72f;
            float my = Screen.height * 0.5f - 72f;
            float sx = (dir.x != 0f) ? mx / Mathf.Abs(dir.x) : float.MaxValue;
            float sy = (dir.y != 0f) ? my / Mathf.Abs(dir.y) : float.MaxValue;
            anchorScreen = screenCenter + dir * Mathf.Min(sx, sy);
        }

        // Convert screen position to canvas local position
        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
                _canvasRT, anchorScreen, null, out Vector2 localPos))
        {
            _arrowRT.anchoredPosition = localPos;
            if (_distLabel != null)
            {
                var dlRT = _distLabel.GetComponent<RectTransform>();
                // Keep the label on-screen: when edge-pinned, offset toward the centre (-dir),
                // not outward; when hovering above the spot, sit just below the arrow.
                dlRT.anchoredPosition = localPos + (onScreen ? Vector2.up * -34f : -dir * 34f);
            }
        }

        // Rotate arrow: point AT the zone when edge-pinned; point straight down at it when hovering above.
        float angle = onScreen ? -90f : Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
        _arrowRT.localRotation = Quaternion.Euler(0f, 0f, angle);

        if (_distLabel != null)
            _distLabel.text = $"{dist:F0}m";
    }

    private void UpdateAlignGauge()
    {
        if (_alignCG == null || _angleFill == null || _centerFill == null || _parkSpeedFill == null) return;

        var vc   = FindVehicle();
        var zone = FindZone();

        const float SHOW_DIST = 3.5f;
        bool show = vc != null && zone != null
                    && Vector2.Distance(vc.transform.position, zone.position) < SHOW_DIST;

        float targetA = show ? 1f : 0f;
        _alignCG.alpha = Mathf.MoveTowards(_alignCG.alpha, targetA, Time.deltaTime * 4f);

        if (_alignCG.alpha < 0.02f || vc == null || zone == null) return;

        // Angle match: 1.0 = perfectly aligned, 0.0 = 90°+ off
        var pz = Object.FindAnyObjectByType<ParkingZone>();
        float angleTol = pz != null ? pz.angleToleranceDegrees : 35f;
        float angleDiff = Mathf.Abs(Mathf.DeltaAngle(
            vc.transform.eulerAngles.z, zone.eulerAngles.z));
        if (pz != null && pz.elongatedZone)
            angleDiff = Mathf.Min(angleDiff, Mathf.Abs(180f - angleDiff));
        float angleScore = Mathf.Clamp01(1f - angleDiff / Mathf.Max(angleTol * 2f, 1f));

        // Center match: 1.0 = centered, 0.0 = at or beyond SHOW_DIST
        var col = zone.GetComponent<Collider2D>();
        float allowed = col != null
            ? Mathf.Min(col.bounds.extents.x, col.bounds.extents.y) * 0.38f
            : 0.5f;
        float dist = Vector2.Distance(vc.transform.position, zone.position);
        float centerScore = Mathf.Clamp01(1f - (dist - allowed) / (SHOW_DIST - allowed));

        // Speed match: 1.0 = stopped, 0.0 = at or above the zone's max-park speed.
        float speedScore = 1f;
        var rb = vc.GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            float maxSpeed = pz != null ? pz.maxParkSpeed : 1f;
            speedScore = Mathf.Clamp01(1f - rb.linearVelocity.magnitude / Mathf.Max(maxSpeed, 0.01f));
        }

        SetBar(_angleFill,     _angleFillImg,     angleScore,  200f);
        SetBar(_centerFill,    _centerFillImg,    centerScore, 200f);
        SetBar(_parkSpeedFill, _parkSpeedFillImg, speedScore,  200f);
    }

    private static readonly Color _barGood = new Color(0.22f, 0.85f, 0.36f);
    private static readonly Color _barMid  = new Color(1.00f, 0.80f, 0.14f);
    private static readonly Color _barBad  = new Color(1.00f, 0.30f, 0.22f);

    private static void SetBar(RectTransform fill, Image img, float score, float barW)
    {
        if (fill == null || img == null) return;
        float cur = fill.offsetMax.x;
        float tgt = score * barW;
        fill.offsetMax = new Vector2(Mathf.Lerp(cur, tgt, Time.deltaTime * 10f), 0f);
        img.color = score > 0.65f ? _barGood
                  : score > 0.32f ? Color.Lerp(_barBad, _barMid, (score - 0.32f) / 0.33f)
                  : _barBad;
    }

    // ── Cache helpers (re-find each update in case level reloaded) ────────────

    private VehicleController _vcCache;
    private Transform _zoneCache;
    private int _cacheFrame = -1;

    private VehicleController FindVehicle()
    {
        if (Time.frameCount != _cacheFrame) RefreshCache();
        return _vcCache;
    }

    private Transform FindZone()
    {
        if (Time.frameCount != _cacheFrame) RefreshCache();
        return _zoneCache;
    }

    private void RefreshCache()
    {
        _cacheFrame = Time.frameCount;
        _vcCache    = Object.FindAnyObjectByType<VehicleController>();
        var zone    = Object.FindAnyObjectByType<ParkingZone>();
        _zoneCache  = zone != null ? zone.transform : null;
    }
}
