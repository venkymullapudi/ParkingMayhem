using UnityEngine;

// Spawns soft puff sprites behind the vehicle when the throttle is active.
// Terrain-specific colors: gray exhaust (road), white spray (water),
// pale contrail (air), orange thruster flame (space).
// Attach to the Vehicle GameObject; call Init() after ConfigureForTerrain().
public class ThrustTrail : MonoBehaviour
{
    private const int   MAX_PUFFS     = 36;
    private const float PUFF_LIFE     = 0.55f;
    private const float EMIT_INTERVAL = 0.06f;
    private const float REAR_OFFSET   = 0.55f;   // world units behind vehicle center

    private SpriteRenderer[] _srs      = new SpriteRenderer[MAX_PUFFS];
    private float[]          _born     = new float[MAX_PUFFS];
    private float[]          _initSc   = new float[MAX_PUFFS];
    private int              _head;
    private float            _lastEmit = -10f;
    private Color            _puffCol;
    private Rigidbody2D      _rb;
    private GameObject       _container;

    private static Sprite _puffSprite;

    public void Init(GameManager.TerrainType terrain)
    {
        _rb = GetComponent<Rigidbody2D>();

        _puffCol = terrain switch
        {
            GameManager.TerrainType.Road  => new Color(0.55f, 0.55f, 0.60f, 0.42f),  // grey exhaust
            GameManager.TerrainType.Water => new Color(0.80f, 0.92f, 1.00f, 0.48f),  // white spray
            GameManager.TerrainType.Air   => new Color(0.90f, 0.94f, 1.00f, 0.30f),  // pale contrail
            GameManager.TerrainType.Space => new Color(1.00f, 0.55f, 0.18f, 0.70f),  // orange flame
            _ => new Color(0.65f, 0.65f, 0.65f, 0.40f)
        };

        if (_puffSprite == null)
            _puffSprite = SpriteGenerator.CreateCircle(32, Color.white);

        _container = new GameObject("_ThrustPuffs");
        _container.transform.parent = null;

        for (int i = 0; i < MAX_PUFFS; i++)
        {
            var go = new GameObject("P", typeof(SpriteRenderer));
            go.transform.SetParent(_container.transform);
            var sr = go.GetComponent<SpriteRenderer>();
            sr.sprite       = _puffSprite;
            sr.sortingOrder = 0;
            sr.color        = Color.clear;
            _srs[i]  = sr;
            _born[i] = -100f;
        }
    }

    private void Update()
    {
        float t = Time.time;

        // Emit while throttle active (forward OR reverse) and the vehicle is moving
        bool throttleOn = Mathf.Abs(OnScreenControls.ThrottleInput) > 0.2f;
        bool moving     = _rb != null && _rb.linearVelocity.magnitude > 0.3f;
        if (throttleOn && moving && t - _lastEmit >= EMIT_INTERVAL)
        {
            _lastEmit = t;
            Vector2 rear = (Vector2)transform.position - (Vector2)transform.up * REAR_OFFSET;
            rear += Random.insideUnitCircle * 0.09f;
            Emit(rear);
        }

        // Age: expand + fade each active puff
        for (int i = 0; i < MAX_PUFFS; i++)
        {
            float age = t - _born[i];
            if (age >= PUFF_LIFE || _born[i] < 0f) { _srs[i].color = Color.clear; continue; }
            float life = age / PUFF_LIFE;
            float sc   = Mathf.Lerp(_initSc[i], _initSc[i] * 2.4f, life * life);
            _srs[i].transform.localScale = Vector3.one * sc;
            _srs[i].color = new Color(_puffCol.r, _puffCol.g, _puffCol.b,
                                      _puffCol.a * (1f - life * life));
        }
    }

    private void Emit(Vector2 pos)
    {
        int i = _head % MAX_PUFFS;
        _head++;
        float sc   = Random.Range(0.22f, 0.40f);
        var   t3   = new Vector3(pos.x, pos.y, 4.8f);
        _srs[i].transform.position   = t3;
        _srs[i].transform.localScale = Vector3.one * sc;
        _srs[i].color                = _puffCol;
        _born[i]  = Time.time;
        _initSc[i] = sc;
    }

    private void OnDestroy()
    {
        if (_container != null) Destroy(_container);
    }
}
