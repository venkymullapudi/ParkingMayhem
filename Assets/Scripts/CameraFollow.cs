using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    public Transform target;
    public float smoothSpeed  = 5f;
    public Vector2 deadZone   = new Vector2(1.5f, 1.5f);

    // Zoom range: zooms out at high speed (better view), in at low speed (precision)
    public float baseSize = 3.2f;
    public float maxSize  = 4.4f;
    public float minSize  = 2.6f;

    private float _fixedZ;
    private Camera _cam;
    private VehicleController _vc;

    private void Awake()
    {
        _fixedZ = transform.position.z;
        _cam    = GetComponent<Camera>();
        if (_cam != null) _cam.orthographicSize = baseSize;
    }

    // ── Camera shake (punchy: directional kick + fast decay) ────────────────────
    private Vector3 _shakeOffset;
    private float   _shakeMag;
    private float   _shakeStart;
    private float   _shakeDur;
    private Vector2 _shakeDir;

    // Back-compat overload (omnidirectional tremor). NOTE: arg order is (magnitude, duration).
    public void Shake(float magnitude = 0.18f, float duration = 0.30f)
    {
        Shake(magnitude, duration, Vector2.zero);
    }

    // Directional kick: 'dir' is the way the camera is punched (need not be normalized;
    // Vector2.zero => pure random tremor).
    public void Shake(float magnitude, float duration, Vector2 dir)
    {
        _shakeMag   = Mathf.Max(0f, magnitude);
        _shakeDur   = Mathf.Max(0.01f, duration);
        _shakeStart = Time.unscaledTime;
        _shakeDir   = dir.sqrMagnitude > 1e-6f ? dir.normalized : Vector2.zero;
    }

    private void LateUpdate()
    {
        if (target == null) return;

        // Remove last frame's shake so the follow math works on the CLEAN anchor. The shake is
        // re-applied as a pure overlay via a single assignment at the end -- so it never bakes
        // into the position (the dead-zone would otherwise absorb the small residual and drift
        // the camera off the car permanently across repeated crashes).
        Vector3 basePos = transform.position - _shakeOffset;

        // ── Follow ────────────────────────────────────────────────────────────
        Vector3 tgt = target.position;
        float dx = tgt.x - basePos.x;
        float dy = tgt.y - basePos.y;
        float tx = Mathf.Abs(dx) > deadZone.x ? basePos.x + (dx - Mathf.Sign(dx) * deadZone.x) : basePos.x;
        float ty = Mathf.Abs(dy) > deadZone.y ? basePos.y + (dy - Mathf.Sign(dy) * deadZone.y) : basePos.y;
        basePos = Vector3.Lerp(basePos, new Vector3(tx, ty, _fixedZ), smoothSpeed * Time.deltaTime);

        // ── Speed-reactive zoom ───────────────────────────────────────────────
        if (_cam != null)
        {
            if (_vc == null) _vc = target.GetComponent<VehicleController>();
            if (_vc != null)
            {
                float normSpeed  = Mathf.Clamp01(_vc.CurrentSpeed / Mathf.Max(_vc.MaxSpeed, 0.1f));
                float targetSize = Mathf.Lerp(minSize, maxSize, normSpeed * normSpeed); // squared = gentle low, big high
                _cam.orthographicSize = Mathf.Lerp(_cam.orthographicSize, targetSize, Time.deltaTime * 1.8f);
            }
        }

        // ── Shake ── recomputed each frame (zero when inactive); applied as an overlay only.
        float elapsed = Time.unscaledTime - _shakeStart;
        if (_shakeMag > 0f && elapsed < _shakeDur)
        {
            float p     = elapsed / _shakeDur;          // 0..1 progress
            float decay = (1f - p) * (1f - p);          // fast quadratic falloff
            Vector3 kick   = (Vector3)_shakeDir * (_shakeMag * decay * (1f - p) * 1.2f);
            Vector3 tremor = UnityEngine.Random.insideUnitSphere * (_shakeMag * decay * 0.5f);
            tremor.z = 0f;
            _shakeOffset = kick + tremor;
        }
        else
        {
            _shakeMag    = 0f;                 // stop re-entering until next Shake()
            _shakeOffset = Vector3.zero;       // guarantee camera returns to rest
        }

        transform.position = basePos + _shakeOffset;
    }
}
