using UnityEngine;

[RequireComponent(typeof(Collider2D))]
[RequireComponent(typeof(SpriteRenderer))]
public class Obstacle : MonoBehaviour
{
    public enum ObstacleMovement { Static, Linear, Rotating }

    // Human-readable name of this hazard, shown on the game-over screen ("Crashed into: Bus").
    public string hazardLabel = "an obstacle";

    [Header("Movement")]
    public ObstacleMovement movementType = ObstacleMovement.Static;
    public float moveSpeed = 1f;
    public float rotateSpeed = 45f;
    public Vector2 moveDirection = Vector2.right;
    public float moveRange = 2f;

    private Vector3 startPosition;
    private float moveTimer;

    private void Start()
    {
        startPosition = transform.position;
        _sr = GetComponent<SpriteRenderer>();
        _baseColor = _sr != null ? _sr.color : Color.white;
    }

    private SpriteRenderer _sr;
    private Color _baseColor;
    private static Transform _vehicle;
    private static Rigidbody2D _vehicleRb;
    private static int _vehicleSearchFrame = -1;

    // Near-miss: graze within this many units at speed → one-time coin bonus.
    private const float NearMissBand = 1.0f;
    private const int   NearMissCoins = 2;
    private bool _nearMissScored;

    private Rigidbody2D _rb;

    private void Update()
    {
        // Rotation is visual-only; linear motion happens in FixedUpdate via MovePosition so the
        // kinematic body sweeps correctly (teleporting transform.position let fast hazards
        // register contacts a frame late or tunnel past the player's collider).
        if (movementType == ObstacleMovement.Rotating)
            transform.Rotate(Vector3.forward, rotateSpeed * Time.deltaTime);

        UpdateDangerGlow();
    }

    private void FixedUpdate()
    {
        if (movementType != ObstacleMovement.Linear) return;
        if (_rb == null) _rb = GetComponent<Rigidbody2D>();
        moveTimer += Time.fixedDeltaTime * moveSpeed;
        Vector3 target = startPosition + (Vector3)(moveDirection.normalized * Mathf.Sin(moveTimer) * moveRange);
        if (_rb != null && _rb.bodyType == RigidbodyType2D.Kinematic) _rb.MovePosition(target);
        else transform.position = target;
    }

    // Pulses red when the player's vehicle is within 1.8 units — proximity warning.
    private void UpdateDangerGlow()
    {
        if (_sr == null) return;

        // Find vehicle once per frame across all obstacles (static cache)
        if (Time.frameCount != _vehicleSearchFrame)
        {
            _vehicleSearchFrame = Time.frameCount;
            var vc = Object.FindAnyObjectByType<VehicleController>();
            _vehicle = vc != null ? vc.transform : null;
            _vehicleRb = vc != null ? vc.GetComponent<Rigidbody2D>() : null;
        }
        if (_vehicle == null) { _sr.color = _baseColor; return; }

        float dist = Vector2.Distance(_vehicle.position, transform.position);

        // Near-miss bonus: grazing this obstacle at speed (close but no crash) awards coins once.
        if (!_nearMissScored && dist < NearMissBand)
        {
            if (_vehicleRb == null) _vehicleRb = _vehicle.GetComponent<Rigidbody2D>();
            float spd = _vehicleRb != null ? _vehicleRb.linearVelocity.magnitude : 0f;
            if (spd > 1.5f)
            {
                _nearMissScored = true;
                // Consecutive grazes escalate the reward (combo).
                int combo = ScoreManager.Instance != null ? ScoreManager.Instance.RegisterNearMiss() : 1;
                int bonus = NearMissCoins * combo;
                ScoreManager.Instance?.AddCoins(bonus);
                UIManager.Instance?.ShowNearMiss(combo, bonus);
                // Tiny SAFE feedback pulse: unscaledTime-driven, self-restoring camera shake.
                // Never touches Time.timeScale; magnitude grows subtly with combo (capped).
                float nmMag = Mathf.Min(0.06f + 0.02f * (combo - 1), 0.14f);
                Camera.main?.GetComponent<CameraFollow>()?.Shake(nmMag, 0.16f);
            }
        }

        if (dist > 1.8f) { _sr.color = _baseColor; return; }

        float danger = 1f - dist / 1.8f;  // 0 at edge, 1 at touch
        float pulse  = (Mathf.Sin(Time.time * 12f) + 1f) * 0.5f;
        float glow   = danger * (0.5f + 0.5f * pulse);
        _sr.color = Color.Lerp(_baseColor, new Color(1f, 0.20f, 0.15f), glow);
    }
}

