using UnityEngine;

// Pools skid-mark sprites and places them in world space whenever the player's
// road vehicle is sliding sideways.  Marks fade over MARK_LIFE seconds.
// DontDestroyOnLoad so the pool persists between levels; call ClearAll() each level start.
public class SkidMarkManager : MonoBehaviour
{
    public static SkidMarkManager Instance { get; private set; }

    private const int   MAX_MARKS     = 120;
    private const float MARK_LIFE     = 5f;
    private const float MIN_INTERVAL  = 0.05f;  // s between emits
    private const float TIRE_OFFSET   = 0.33f;  // half-track world units (car = ~0.66 wide)

    private SpriteRenderer[] _srs       = new SpriteRenderer[MAX_MARKS];
    private float[]          _born      = new float[MAX_MARKS];
    private float[]          _initAlpha = new float[MAX_MARKS];
    private int              _head;
    private float            _lastEmit  = -10f;
    private Sprite           _markSprite;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
        BuildPool();
    }

    private void BuildPool()
    {
        _markSprite = BuildMarkSprite();
        var pool = new GameObject("SkidMarkPool");
        pool.transform.SetParent(transform);
        for (int i = 0; i < MAX_MARKS; i++)
        {
            var go = new GameObject("M", typeof(SpriteRenderer));
            go.transform.SetParent(pool.transform);
            var sr = go.GetComponent<SpriteRenderer>();
            sr.sprite       = _markSprite;
            sr.sortingOrder = -800;
            sr.color        = Color.clear;
            _srs[i]         = sr;
            _born[i]        = -100f;
            _initAlpha[i]   = 0f;
        }
    }

    // Called from VehicleController.FixedUpdate on Road terrain when lateral slip is high.
    public void TryEmit(Vector2 pos, float angle, Vector2 right, float lateralMag)
    {
        float t = Time.time;
        if (t - _lastEmit < MIN_INTERVAL) return;
        float alpha = Mathf.Clamp01((lateralMag - 0.8f) / 2.5f) * 0.55f;
        if (alpha < 0.06f) return;
        _lastEmit = t;
        PlaceAt(pos - right * TIRE_OFFSET, angle, alpha);
        PlaceAt(pos + right * TIRE_OFFSET, angle, alpha);
    }

    private void PlaceAt(Vector2 pos, float angle, float alpha)
    {
        int i = _head % MAX_MARKS;
        _head++;
        _srs[i].transform.position = new Vector3(pos.x, pos.y, 3f);
        _srs[i].transform.rotation = Quaternion.Euler(0, 0, angle);
        _srs[i].color     = new Color(0.09f, 0.07f, 0.06f, alpha);
        _born[i]          = Time.time;
        _initAlpha[i]     = alpha;
    }

    private void Update()
    {
        float now = Time.time;
        for (int i = 0; i < MAX_MARKS; i++)
        {
            float age = now - _born[i];
            if (age >= MARK_LIFE || _initAlpha[i] <= 0f)
            {
                if (_srs[i].color.a > 0f) _srs[i].color = Color.clear;
                continue;
            }
            var c = _srs[i].color;
            _srs[i].color = new Color(c.r, c.g, c.b, _initAlpha[i] * (1f - age / MARK_LIFE));
        }
    }

    public void ClearAll()
    {
        for (int i = 0; i < MAX_MARKS; i++)
        {
            _srs[i].color   = Color.clear;
            _born[i]        = -100f;
            _initAlpha[i]   = 0f;
        }
        _head = 0;
    }

    // Narrow oval: 0.08 × 0.22 world units (ppu=100).  Long axis = vehicle forward.
    private Sprite BuildMarkSprite()
    {
        int w = 8, h = 22;
        bool toLin = QualitySettings.activeColorSpace == ColorSpace.Linear;
        var tex = new Texture2D(w, h, TextureFormat.RGBA32, false)
            { filterMode = FilterMode.Bilinear, wrapMode = TextureWrapMode.Clamp };
        var px = new Color[w * h];
        float cx = (w - 1) / 2f, cy = (h - 1) / 2f;
        for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
            {
                float dx = (x - cx) / cx, dy = (y - cy) / cy;
                float a  = Mathf.Clamp01(1.08f - Mathf.Sqrt(dx * dx + dy * dy));
                Color c  = new Color(1f, 1f, 1f, a);
                if (toLin) { var l = c.linear; c = new Color(l.r, l.g, l.b, a); }
                px[y * w + x] = c;
            }
        tex.SetPixels(px);
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, w, h), Vector2.one * 0.5f, 100f);
    }
}
