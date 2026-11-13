using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

// Persistent on-screen control overlay: steering pair (bottom-left) + throttle pair (bottom-right).
// VehicleController reads SteerInput / ThrottleInput every frame.
//
// Steer input ramps from 0.4 → 1.0 over 0.5 s so a quick tap gives a gentle nudge
// while a sustained hold gives full lock — important for precision parking.
public class OnScreenControls : MonoBehaviour
{
    public static OnScreenControls Instance { get; private set; }

    public static float SteerInput    { get; private set; }  // -1 left .. +1 right
    public static float ThrottleInput { get; private set; }  // -1 rev  .. +1 fwd

    // Set true during countdown to block all player input
    public static bool InputBlocked { get; set; }

    private static bool s_left, s_right, s_fwd, s_rev;
    private static float s_leftTime, s_rightTime;  // seconds the button has been held

    internal static void SetLeft(bool v)  { if (!v) s_leftTime  = 0f; s_left  = v; }
    internal static void SetRight(bool v) { if (!v) s_rightTime = 0f; s_right = v; }
    internal static void SetFwd(bool v)   { s_fwd = v; }
    internal static void SetRev(bool v)   { s_rev = v; }

    private GameObject _canvasGO;

    private void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void OnDestroy()
    {
        if (Instance == this) { Instance = null; ReleaseAll(); }
    }

    private void Update()
    {
        if (InputBlocked) { SteerInput = 0f; ThrottleInput = 0f; return; }

        // Steer: ramp from 0.40 to 1.0 over 0.5 s for precision tap vs sustained hold.
        const float RAMP = 0.5f;
        const float MIN_STEER = 0.40f;

        if (s_left)
        {
            s_leftTime  += Time.unscaledDeltaTime;
            float mag = Mathf.Lerp(MIN_STEER, 1f, s_leftTime / RAMP);
            SteerInput = -mag;
        }
        else if (s_right)
        {
            s_rightTime += Time.unscaledDeltaTime;
            float mag = Mathf.Lerp(MIN_STEER, 1f, s_rightTime / RAMP);
            SteerInput = mag;
        }
        else
        {
            SteerInput = 0f;
        }

        // Throttle: FWD full power, REV at 60% (safer for backing into spots).
        ThrottleInput = Mathf.Clamp((s_fwd ? 1f : 0f) + (s_rev ? -0.6f : 0f), -1f, 1f);
    }

    public void Show(TextMeshProUGUI fontSource)
    {
        ReleaseAll();
        if (_canvasGO != null) { Destroy(_canvasGO); _canvasGO = null; }
        _canvasGO = BuildCanvas(fontSource);
    }

    public void Hide()
    {
        ReleaseAll();
        if (_canvasGO != null) { Destroy(_canvasGO); _canvasGO = null; }
    }

    // Toggle visibility without rebuilding (used to hide controls during the countdown so they
    // don't clash with the vehicle stats card).
    public void SetVisible(bool v)
    {
        if (_canvasGO != null) _canvasGO.SetActive(v);
        if (!v) ReleaseAll();
    }

    public static void ReleaseAll()
    {
        s_left = s_right = s_fwd = s_rev = false;
        s_leftTime = s_rightTime = 0f;
    }

    // ─────────────────────────────────────────────────────────────────────────────

    private GameObject BuildCanvas(TextMeshProUGUI src)
    {
        var root = new GameObject("_OSC_Canvas");
        DontDestroyOnLoad(root);

        var canvas = root.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 85;

        var scaler = root.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1280f, 720f);
        scaler.matchWidthOrHeight = 0.5f;

        root.AddComponent<GraphicRaycaster>();

        // Larger buttons for reliable thumb tapping on mobile.
        var btnSprite = SpriteGenerator.CreateRoundedRect(128, 128, 0.44f);
        const float SZ  = 128f;
        const float GAP = 14f;
        const float PAD = 20f;

        // ── Steering group (bottom-left) ──────────────────────────────────────
        var steerRT = NewRect("SteerGroup", root.transform);
        steerRT.anchorMin = steerRT.anchorMax = Vector2.zero;
        steerRT.pivot     = Vector2.zero;
        steerRT.anchoredPosition = new Vector2(PAD, PAD);
        steerRT.sizeDelta = new Vector2(SZ * 2 + GAP, SZ);

        MakeBtn(steerRT, "BtnLeft",  "L", btnSprite, src,
            new Vector2(0f,       0f), new Vector2(SZ,          SZ),
            new Color(0.18f, 0.30f, 0.75f, 0.90f), SetLeft);

        MakeBtn(steerRT, "BtnRight", "R", btnSprite, src,
            new Vector2(SZ + GAP, 0f), new Vector2(SZ * 2 + GAP, SZ),
            new Color(0.18f, 0.30f, 0.75f, 0.90f), SetRight);

        AddLabel("STEER", src, steerRT,
            new Vector2(0f, SZ + 4f), new Vector2(SZ * 2 + GAP, SZ + 28f));

        // ── Throttle group (bottom-right) ─────────────────────────────────────
        var thrRT = NewRect("ThrGroup", root.transform);
        thrRT.anchorMin = thrRT.anchorMax = new Vector2(1f, 0f);
        thrRT.pivot     = new Vector2(1f, 0f);
        thrRT.anchoredPosition = new Vector2(-PAD, PAD);
        thrRT.sizeDelta = new Vector2(SZ, SZ * 2 + GAP);

        MakeBtn(thrRT, "BtnFwd", "FWD", btnSprite, src,
            new Vector2(0f, SZ + GAP), new Vector2(SZ, SZ * 2 + GAP),
            new Color(0.12f, 0.68f, 0.22f, 0.90f), SetFwd);

        MakeBtn(thrRT, "BtnRev", "REV", btnSprite, src,
            new Vector2(0f, 0f), new Vector2(SZ, SZ),
            new Color(0.78f, 0.16f, 0.16f, 0.90f), SetRev);

        AddLabel("GAS", src, thrRT,
            new Vector2(0f, SZ * 2 + GAP + 4f), new Vector2(SZ, SZ * 2 + GAP + 28f));
        // (Removed redundant "REV" caption — the button already reads "REV", and the stray
        //  label overlapped the build watermark at the bottom-right.)

        return root;
    }

    private static RectTransform NewRect(string name, Transform parent)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        return go.GetComponent<RectTransform>();
    }

    private static void MakeBtn(RectTransform parent, string name, string icon,
        Sprite sprite, TextMeshProUGUI src, Vector2 oMin, Vector2 oMax,
        Color col, System.Action<bool> setter)
    {
        var rt = NewRect(name, parent);
        rt.anchorMin = rt.anchorMax = Vector2.zero;
        rt.pivot = Vector2.zero;
        rt.offsetMin = oMin;
        rt.offsetMax = oMax;

        var img = rt.gameObject.AddComponent<Image>();
        img.sprite = sprite;
        img.type   = Image.Type.Simple;
        img.color  = col;

        if (src != null)
        {
            var lblGO = Object.Instantiate(src.gameObject, rt, false);
            lblGO.name = "Icon";
            var tmp = lblGO.GetComponent<TextMeshProUGUI>();
            tmp.text          = icon;
            tmp.fontSize      = icon.Length > 1 ? 30f : 52f;
            tmp.alignment     = TMPro.TextAlignmentOptions.Center;
            tmp.color         = Color.white;
            tmp.fontStyle     = TMPro.FontStyles.Bold;
            tmp.raycastTarget = false;
            tmp.outlineColor  = new Color32(0, 0, 0, 210);
            tmp.outlineWidth  = 0.16f;
            var lrt = lblGO.GetComponent<RectTransform>();
            lrt.anchorMin = Vector2.zero;
            lrt.anchorMax = Vector2.one;
            lrt.offsetMin = lrt.offsetMax = Vector2.zero;
        }

        var hold = rt.gameObject.AddComponent<HoldButton>();
        hold.Init(setter);
    }

    private static void AddLabel(string text, TextMeshProUGUI src, RectTransform parent,
        Vector2 oMin, Vector2 oMax)
    {
        if (src == null) return;
        var rt = NewRect("Lbl_" + text, parent);
        rt.anchorMin = rt.anchorMax = Vector2.zero;
        rt.pivot = Vector2.zero;
        rt.offsetMin = oMin;
        rt.offsetMax = oMax;
        var lblGO = Object.Instantiate(src.gameObject, rt, false);
        var tmp = lblGO.GetComponent<TextMeshProUGUI>();
        tmp.text          = text;
        tmp.fontSize      = 14f;
        tmp.alignment     = TMPro.TextAlignmentOptions.Center;
        tmp.color         = new Color(1f, 1f, 1f, 0.45f);
        tmp.fontStyle     = TMPro.FontStyles.Normal;
        tmp.raycastTarget = false;
        var lrt = lblGO.GetComponent<RectTransform>();
        lrt.anchorMin = Vector2.zero;
        lrt.anchorMax = Vector2.one;
        lrt.offsetMin = lrt.offsetMax = Vector2.zero;
    }
}

// ─── Tracks press/release for a held driving button ──────────────────────────
public class HoldButton : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
{
    private System.Action<bool> _setter;
    private bool _held;

    public void Init(System.Action<bool> setter) => _setter = setter;

    public void OnPointerDown(PointerEventData _)
    {
        _held = true;
        _setter?.Invoke(true);
        transform.localScale = Vector3.one * 0.88f;
    }

    public void OnPointerUp(PointerEventData _)
    {
        if (!_held) return;
        _held = false;
        _setter?.Invoke(false);
        transform.localScale = Vector3.one;
    }

    public void OnPointerExit(PointerEventData _)
    {
        if (!_held) return;
        _held = false;
        _setter?.Invoke(false);
        transform.localScale = Vector3.one;
    }

    private void OnDisable()
    {
        if (_held) { _held = false; _setter?.Invoke(false); }
    }
}
