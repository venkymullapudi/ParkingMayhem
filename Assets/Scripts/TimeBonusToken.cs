using UnityEngine;
using System.Collections;

// Golden "+5s" pickup token. The player collects it by driving over it.
// Adds 5 seconds to the level timer and plays a chime.
[RequireComponent(typeof(CircleCollider2D))]
public class TimeBonusToken : MonoBehaviour
{
    private static readonly Color _gold  = new Color(1.00f, 0.88f, 0.20f, 1.00f);
    private static readonly Color _glow  = new Color(1.00f, 0.96f, 0.60f, 0.55f);

    private SpriteRenderer _ring;
    private SpriteRenderer _fill;
    private bool _collected;

    private void Awake()
    {
        // Outer glow ring
        var outerGO = new GameObject("Ring");
        outerGO.transform.SetParent(transform, false);
        _ring = outerGO.AddComponent<SpriteRenderer>();
        _ring.sprite       = SpriteGenerator.CreateRegularPolygon(24, 64, _glow);
        _ring.sortingOrder = 3;
        _ring.color        = _glow;
        outerGO.transform.localScale = Vector3.one * 1.15f;

        // Inner solid circle
        var innerGO = new GameObject("Fill");
        innerGO.transform.SetParent(transform, false);
        _fill = innerGO.AddComponent<SpriteRenderer>();
        _fill.sprite       = SpriteGenerator.CreateRegularPolygon(24, 64, _gold);
        _fill.sortingOrder = 4;
        _fill.color        = _gold;
        innerGO.transform.localScale = Vector3.one * 0.75f;

        // Label "+5"
        // (text is costly at runtime — we skip TMP and encode it visually via the gold color)

        var col = GetComponent<CircleCollider2D>();
        col.isTrigger = true;
        col.radius    = 0.55f;

        transform.localScale = Vector3.one * 0.90f;
    }

    private void Update()
    {
        if (_collected) return;
        // Gentle rotate + pulse
        transform.Rotate(Vector3.forward, 60f * Time.deltaTime);
        float pulse = 0.88f + 0.12f * Mathf.Sin(Time.time * 3.2f);
        if (_ring != null) _ring.transform.localScale = Vector3.one * (1.15f * pulse);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (_collected) return;
        if (other.GetComponent<VehicleController>() == null) return;

        _collected = true;
        UIManager.Instance?.AddTime(5f);
        AudioManager.Instance?.PlayTimeBonusSound();
        StartCoroutine(CollectAnimation());
    }

    private IEnumerator CollectAnimation()
    {
        float t = 0f;
        Vector3 startScale = transform.localScale;
        while (t < 0.30f)
        {
            t += Time.unscaledDeltaTime;
            float k = Mathf.Clamp01(t / 0.30f);
            transform.localScale = Vector3.Lerp(startScale, Vector3.one * 2.0f, k);
            Color c = Color.Lerp(_gold, new Color(1f, 1f, 1f, 0f), k);
            if (_fill != null) _fill.color = c;
            if (_ring != null) _ring.color = new Color(_glow.r, _glow.g, _glow.b, _glow.a * (1f - k));
            yield return null;
        }
        Destroy(gameObject);
    }
}
