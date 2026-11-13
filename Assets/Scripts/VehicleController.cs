using UnityEngine;
using System;

public class VehicleController : MonoBehaviour
{
    [Header("Vehicle Settings")]
    [SerializeField] private float moveSpeed = 1.8f;
    [SerializeField] private float rotationSpeed = 55f;
    [SerializeField] private float maxSpeed = 3f;
    [SerializeField] private float brakeForce = 4f;

    [Header("Wheel Settings")]
    [SerializeField] public Transform[] wheels;
    [SerializeField] private float wheelRotationSpeed = 200f;

    private Rigidbody2D rb;
    private Vector2 movement;
    private float rotation;
    private bool isParked;
    private float currentSpeed;
    private Vector2 lastPosition;
    private float lastRotation;

    public event Action OnCollision;
    public event Action OnParked;
    public event Action<float> OnSpeedChanged;

    public float CurrentSpeed => currentSpeed;
    public float MaxSpeed     => maxSpeed;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        if (rb == null)
        {
            rb = gameObject.AddComponent<Rigidbody2D>();
            rb.gravityScale = 0f;
            rb.linearDamping = 0.5f;
            rb.angularDamping = 0.5f;
        }
    }

    private void Start()
    {
        lastPosition = transform.position;
        lastRotation = transform.rotation.eulerAngles.z;
    }

    private void Update()
    {
        if (isParked) return;

        HandleInput();
        UpdateWheels();
        UpdateSpeed();
    }

    private void FixedUpdate()
    {
        if (isParked) return;

        ApplyMovement();
        ApplyEnvironmentalEffects();
    }

    private void HandleInput()
    {
        movement = Vector2.zero;
        rotation = 0f;

        if (OnScreenControls.Instance != null)
        {
            rotation   = OnScreenControls.SteerInput;
            movement.y = OnScreenControls.ThrottleInput;
        }

        bool oscActive = Mathf.Abs(rotation) > 0.01f || Mathf.Abs(movement.y) > 0.01f;

        if (!oscActive && !OnScreenControls.InputBlocked)
        {
            // Keyboard fallback (editor / desktop only). Must honor InputBlocked like the
            // on-screen controls do, or the car stays drivable via keys while paused/menus open.
            movement.x = Input.GetAxis("Horizontal");
            movement.y = Input.GetAxis("Vertical");
            rotation   = -Input.GetAxis("Horizontal");
        }
    }

    private void UpdateSpeed()
    {
        if (Time.deltaTime <= 0f) return;   // paused (timeScale 0): avoid 0/0 = NaN speed
        Vector2 positionDelta = (Vector2)transform.position - lastPosition;
        float rotationDelta = Mathf.DeltaAngle(transform.rotation.eulerAngles.z, lastRotation);

        currentSpeed = positionDelta.magnitude / Time.deltaTime;
        OnSpeedChanged?.Invoke(currentSpeed);

        AudioManager.Instance?.UpdateEngineState(currentSpeed / maxSpeed, movement.y);

        lastPosition = transform.position;
        lastRotation = transform.rotation.eulerAngles.z;
    }

    // ── Handling model ────────────────────────────────────────────────────────
    [Header("Handling")]
    [SerializeField] private float turnRate = 130f;   // deg/sec at full steer + full speed
    [SerializeField] private float grip = 0.93f;      // 1 = no sideways slip, 0 = ice/vacuum
    [SerializeField] private bool  allowReverse = true;
    private bool  freeRotate = false;     // spacecraft can pivot without forward speed
    private float minControlSpeed = 0f;  // planes stall below this speed (0 = no minimum)

    private GameManager.TerrainType _terrain;
    private GameManager.VehicleKind _kind;

    // Configure physics for a terrain + vehicle kind (called by GameManager on spawn).
    // Also reconfigures the Rigidbody2D damping, which varies strongly by terrain.
    public void ConfigureForTerrain(GameManager.TerrainType terrain,
                                    GameManager.VehicleKind kind = GameManager.VehicleKind.Car)
    {
        _terrain = terrain;
        _kind    = kind;
        switch (terrain)
        {
            case GameManager.TerrainType.Road:  ConfigureRoad(kind);  break;
            case GameManager.TerrainType.Water: ConfigureWater(kind); break;
            case GameManager.TerrainType.Air:   ConfigureAir(kind);   break;
            case GameManager.TerrainType.Space: ConfigureSpace(kind); break;
        }
        // Push damping values into the Rigidbody2D (set in GameManager before this call).
        if (rb != null)
        {
            rb.linearDamping  = _rbLinearDamp;
            rb.angularDamping = _rbAngularDamp;
        }
    }

    private float _rbLinearDamp;
    private float _rbAngularDamp;

    // ── Road: high grip, quick braking, steering authority at all speeds ──────
    private void ConfigureRoad(GameManager.VehicleKind kind)
    {
        turnRate = 132f; grip = 0.95f; moveSpeed = 2.4f; maxSpeed = 4.5f;
        brakeForce = 8.0f; _rbLinearDamp = 2.5f; _rbAngularDamp = 5f;
        allowReverse = true; freeRotate = false; minControlSpeed = 0f;
        switch (kind)
        {
            case GameManager.VehicleKind.Truck:
                // Heavier: slower to start, wider turns, longer stopping distance
                moveSpeed *= 0.78f; turnRate *= 0.78f; maxSpeed *= 0.82f;
                brakeForce *= 0.82f; _rbLinearDamp = 2.2f;
                break;
            case GameManager.VehicleKind.Bus:
                // Heaviest road vehicle: sluggish acceleration, very wide turns
                moveSpeed *= 0.62f; turnRate *= 0.66f; maxSpeed *= 0.74f;
                brakeForce *= 0.72f; _rbLinearDamp = 2.0f;
                break;
        }
    }

    // ── Water: no direct braking, water drag only, rudder needs speed ─────────
    // Boats coast to a stop - you cannot instantly halt like a car.
    private void ConfigureWater(GameManager.VehicleKind kind)
    {
        turnRate = 82f; grip = 0.62f; moveSpeed = 1.9f; maxSpeed = 3.8f;
        brakeForce = 0.65f; _rbLinearDamp = 1.1f; _rbAngularDamp = 3f;
        allowReverse = true; freeRotate = false; minControlSpeed = 0f;
        switch (kind)
        {
            case GameManager.VehicleKind.Yacht:
                // Sail-driven: graceful but sluggish turns, strong lateral hull drag
                turnRate *= 0.80f; grip = 0.72f; maxSpeed *= 0.88f;
                brakeForce *= 0.70f; _rbLinearDamp = 1.0f; allowReverse = false;
                break;
            case GameManager.VehicleKind.Ship:
                // Massive: extremely slow turning, colossal momentum, barely brakes
                turnRate *= 0.55f; moveSpeed *= 0.68f; maxSpeed *= 0.62f;
                brakeForce *= 0.35f; _rbLinearDamp = 0.75f; _rbAngularDamp = 2f;
                allowReverse = false;
                break;
        }
    }

    // ── Air: planes need forward speed; helicopters can hover ─────────────────
    private void ConfigureAir(GameManager.VehicleKind kind)
    {
        if (kind == GameManager.VehicleKind.Helicopter)
        {
            // Helicopter: omnidirectional thrust, can hover and reverse
            turnRate = 118f; grip = 0.58f; moveSpeed = 2.8f; maxSpeed = 5.0f;
            brakeForce = 2.4f; _rbLinearDamp = 1.4f; _rbAngularDamp = 4f;
            allowReverse = true; freeRotate = false; minControlSpeed = 0f;
        }
        else
        {
            // Plane/Jet: needs speed to control (like a real aircraft).
            // Below minControlSpeed the plane begins to stall - steering authority drops sharply.
            bool isJet = (kind == GameManager.VehicleKind.Jet);
            turnRate = 78f; grip = 0.25f;
            moveSpeed = isJet ? 4.0f : 3.2f; maxSpeed = isJet ? 8.0f : 6.0f;
            // Very low passive drag - planes glide, they don't coast-to-a-stop.
            brakeForce = 0.28f; _rbLinearDamp = 0.16f; _rbAngularDamp = 2.0f;
            allowReverse = false; freeRotate = false;
            minControlSpeed = isJet ? 1.8f : 1.4f;
        }
    }

    // ── Space: near-zero drag, true inertia, thruster-only propulsion ─────────
    // The ship keeps moving unless you thrust in the opposite direction.
    private void ConfigureSpace(GameManager.VehicleKind kind)
    {
        turnRate = 148f; grip = 0.02f; moveSpeed = 1.3f; maxSpeed = 5.5f;
        brakeForce = 0.04f; _rbLinearDamp = 0.02f; _rbAngularDamp = 0.4f;
        allowReverse = true; freeRotate = true; minControlSpeed = 0f;
        switch (kind)
        {
            case GameManager.VehicleKind.Satellite:
                // Reaction wheels only - slow rotation, very little thrust
                turnRate *= 0.50f; moveSpeed *= 0.58f; maxSpeed *= 0.68f;
                break;
            case GameManager.VehicleKind.Rocket:
                // High thrust axis, but rotation is slow (gimballed engine)
                moveSpeed *= 1.35f; maxSpeed *= 1.25f; turnRate *= 0.75f;
                break;
            // Shuttle: default parameters
        }
    }

    // High-level difficulty knobs (called after ConfigureForTerrain for high levels).
    public void ApplyHandling(float turnMul = 1f, float gripMul = 1f, float moveMul = 1f)
    {
        turnRate  *= turnMul;
        grip       = Mathf.Clamp01(grip * gripMul);
        moveSpeed *= moveMul;
    }

    private void ApplyMovement()
    {
        float throttle = movement.y;
        if (!allowReverse && throttle < 0f) throttle = 0f;

        float speed = rb.linearVelocity.magnitude;

        // ── Control authority (plane stall model) ─────────────────────────────
        // Below minControlSpeed, steering and lateral grip drop sharply -
        // the aircraft loses aerodynamic authority and wants to fly straight.
        float controlAuth = 1f;
        if (minControlSpeed > 0f)
        {
            float stallRange = minControlSpeed * 0.8f;
            controlAuth = Mathf.Clamp01((speed - minControlSpeed * 0.35f) / stallRange);
        }

        // ── Engine thrust ─────────────────────────────────────────────────────
        if (Mathf.Abs(throttle) > 0.01f)
            rb.AddForce((Vector2)transform.up * throttle * moveSpeed * (throttle < 0f ? 0.50f : 1f));

        // ── Steering ──────────────────────────────────────────────────────────
        if (Mathf.Abs(rotation) > 0.01f)
        {
            // Speed-coupled turn rate: real turning radius, no spin-in-place.
            // freeRotate (spacecraft) can pivot at any speed via thrusters.
            float speedFactor = freeRotate ? 1f
                : (0.28f + 0.72f * Mathf.Clamp01(speed / (maxSpeed * 0.45f)));
            speedFactor *= controlAuth;

            // Steering input reverses when reversing, like a real car/boat rudder.
            float fwdDot = Vector2.Dot(rb.linearVelocity, (Vector2)transform.up);
            float dir = freeRotate ? 1f : (fwdDot < -0.05f ? -1f : 1f);
            rb.angularVelocity = -rotation * turnRate * speedFactor * dir;
        }
        else
        {
            rb.angularVelocity = Mathf.Lerp(rb.angularVelocity, 0f, 10f * Time.fixedDeltaTime);
        }

        // ── Lateral grip / hull drag ───────────────────────────────────────────
        // Bleeds off sideways velocity so the vehicle tracks along its facing.
        // For planes at low speed, we let the momentum carry them (reduced authority).
        if (!freeRotate && grip > 0.01f)
        {
            Vector2 fwd      = transform.up;
            Vector2 forwardV = fwd * Vector2.Dot(rb.linearVelocity, fwd);
            Vector2 lateralV = rb.linearVelocity - forwardV;
            float effectiveGrip = grip * controlAuth;
            rb.linearVelocity = forwardV + lateralV * (1f - effectiveGrip);

            if (_terrain == GameManager.TerrainType.Road)
                SkidMarkManager.Instance?.TryEmit(
                    transform.position, transform.eulerAngles.z,
                    (Vector2)transform.right, lateralV.magnitude);
        }

        // ── Speed cap ─────────────────────────────────────────────────────────
        if (rb.linearVelocity.magnitude > maxSpeed)
            rb.linearVelocity = rb.linearVelocity.normalized * maxSpeed;

        // ── Coasting deceleration ─────────────────────────────────────────────
        // Road: brakes hard.  Water: gentle water drag only.
        // Plane: glides (barely decelerates).  Space: almost no deceleration at all.
        if (Mathf.Abs(throttle) < 0.01f && brakeForce > 0.001f)
            rb.linearVelocity = Vector2.Lerp(rb.linearVelocity, Vector2.zero,
                                             brakeForce * Time.fixedDeltaTime);
    }

    private float spaceDriftTimer = 0f;
    private Vector2 spaceDriftDir = Vector2.zero;
    private Vector2 windDir = Vector2.right;   // persistent wind direction for air
    private float   windShiftTimer = 0f;
    private Vector2 currentDir = Vector2.right; // tidal/ocean current for water

    // While the player is actively driving, environment disturbs fully; when nearly stopped
    // and not steering it eases off so you can actually hold the spot.
    private float envScale = 1f;

    private void ApplyEnvironmentalEffects()
    {
        if (GameManager.Instance == null) return;

        bool driving = Mathf.Abs(movement.y) > 0.01f || Mathf.Abs(rotation) > 0.01f;
        bool slow    = rb.linearVelocity.magnitude < 0.6f;
        envScale = (!driving && slow) ? 0.15f : 1f;

        switch (GameManager.Instance.currentTerrain)
        {
            case GameManager.TerrainType.Air:   ApplyWindEffect();    break;
            case GameManager.TerrainType.Water:  ApplyCurrentEffect(); break;
            case GameManager.TerrainType.Space:  ApplySpaceEffect();   break;
        }
    }

    // Persistent gust from a slowly-rotating direction (feels like real wind, not random noise).
    private void ApplyWindEffect()
    {
        float w = GameManager.Instance.windStrength * envScale;
        if (w <= 0f) return;

        // Gust direction drifts slowly; occasional sharp gusts add turbulence
        windShiftTimer -= Time.fixedDeltaTime;
        if (windShiftTimer <= 0f)
        {
            windShiftTimer = UnityEngine.Random.Range(3f, 6f);
            float angle = UnityEngine.Random.Range(-45f, 45f);
            windDir = Quaternion.Euler(0f, 0f, angle) * windDir;
            windDir.Normalize();
        }
        // Steady component + small random turbulence
        Vector2 gust = windDir * w + UnityEngine.Random.insideUnitCircle * (w * 0.3f);
        rb.AddForce(gust);
    }

    // Ocean current / tide pushes the boat in a fixed direction with gentle wave oscillation.
    // Importantly: the current also pushes proportionally harder when moving fast (hull drag
    // increases with speed) and perpendicular to heading when turning (hydrodynamic resistance).
    private void ApplyCurrentEffect()
    {
        float h = GameManager.Instance.waveHeight * envScale;
        if (h <= 0f) return;

        // Steady current in a fixed direction plus a slow cross-current oscillation
        float sway = Mathf.Sin(Time.time * 0.8f) * 0.35f + Mathf.Sin(Time.time * 1.7f) * 0.2f;
        Vector2 current = (currentDir + Vector2.Perpendicular(currentDir) * sway) * h;
        rb.AddForce(current);

        // Extra hydrodynamic drag proportional to speed (water resists fast movement)
        float speed = rb.linearVelocity.magnitude;
        if (speed > 0.5f)
            rb.AddForce(-rb.linearVelocity.normalized * (speed * speed * 0.12f * envScale));
    }

    private void ApplySpaceEffect()
    {
        // Solar wind / micro-debris: periodic directional impulse (not random every frame)
        spaceDriftTimer -= Time.fixedDeltaTime;
        if (spaceDriftTimer <= 0f)
        {
            spaceDriftTimer = UnityEngine.Random.Range(2.5f, 5f);
            spaceDriftDir   = UnityEngine.Random.insideUnitCircle.normalized;
        }
        rb.AddForce(spaceDriftDir * 0.35f * envScale);
        // Slow residual tumble from gyroscopic effects / angular momentum
        rb.AddTorque(Mathf.Sin(Time.time * 0.4f) * 1.2f * envScale);
    }

    private void UpdateWheels()
    {
        if (wheels == null || wheels.Length == 0) return;

        float wheelRotation = movement.y * wheelRotationSpeed * Time.deltaTime;
        foreach (Transform wheel in wheels)
        {
            if (wheel != null)
            {
                wheel.Rotate(Vector3.forward, wheelRotation);
            }
        }
    }

    public void SetParked(bool parked)
    {
        isParked = parked;
        if (parked)
        {
            rb.linearVelocity = Vector2.zero;
            rb.angularVelocity = 0f;
            OnParked?.Invoke();
        }
    }

    private bool crashed = false;
    // Exposed so ParkingZone can refuse to complete a park after a crash froze the vehicle.
    public bool HasCrashed => crashed;

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (isParked || crashed) return;
        // Hitting any obstacle/hazard ends the level.
        var hitObstacle = collision.gameObject.GetComponent<Obstacle>();
        if (hitObstacle != null)
        {
            crashed = true;
            OnCollision?.Invoke();
            rb.linearVelocity = Vector2.zero;
            rb.angularVelocity = 0f;
            isParked = true; // freeze control
            AudioManager.Instance?.PlayCollisionSound();
            // Punch the camera away from the impact point (contact normal), snappy + directional.
            Vector2 kickDir = collision.contactCount > 0 ? -collision.GetContact(0).normal : Vector2.zero;
            Camera.main?.GetComponent<CameraFollow>()?.Shake(0.32f, 0.22f, kickDir);
            Handheld.Vibrate();
            UIManager.Instance?.ShowGameOver($"Crashed into {hitObstacle.hazardLabel}");
        }
    }

    public void ResetVehicle()
    {
        SetParked(false);
        rb.linearVelocity = Vector2.zero;
        rb.angularVelocity = 0f;
        transform.rotation = Quaternion.identity;
    }

    // ── Vehicle stats for the HUD card ───────────────────────────────────────────

    public struct StatRow
    {
        public string label;  // terrain-appropriate label
        public float  bar;    // 0..1 fill amount
        public string grade;  // "Excellent" / "Good" / "Fair" / "Poor"
    }

    public struct VehicleStats
    {
        public string    vehicleName;
        public StatRow[] rows;    // always 4
        public string[]  traits;  // special characteristic strings
    }

    public VehicleStats GetDisplayStats()
    {
        return _terrain switch
        {
            GameManager.TerrainType.Road  => StatsRoad(),
            GameManager.TerrainType.Water => StatsWater(),
            GameManager.TerrainType.Air   => StatsAir(),
            GameManager.TerrainType.Space => StatsSpace(),
            _ => StatsRoad()
        };
    }

    // Grade word from normalised 0..1 value.
    private static string G(float v) =>
        v >= 0.80f ? "Excellent" : v >= 0.58f ? "Good" : v >= 0.36f ? "Fair" : "Poor";

    // ── Road stats ────────────────────────────────────────────────────────────
    private VehicleStats StatsRoad()
    {
        var traits = new System.Collections.Generic.List<string>();
        if (_kind == GameManager.VehicleKind.Truck) traits.Add("Wide turn radius");
        if (_kind == GameManager.VehicleKind.Bus)   traits.Add("Needs extra space to manoeuvre");
        traits.Add(allowReverse ? "Full reverse gear" : "No reverse");

        float spd = Mathf.Clamp01(maxSpeed   / 4.6f);
        float str = Mathf.Clamp01(turnRate   / 133f);
        float grp = grip;
        float brk = Mathf.Clamp01(brakeForce / 8.2f);

        return new VehicleStats
        {
            vehicleName = _kind.ToString(),
            rows = new[]
            {
                new StatRow { label = "Top Speed",   bar = spd, grade = G(spd) },
                new StatRow { label = "Steering",    bar = str, grade = G(str) },
                new StatRow { label = "Tire Grip",   bar = grp, grade = G(grp) },
                new StatRow { label = "Brake Force", bar = brk, grade = G(brk) },
            },
            traits = traits.ToArray()
        };
    }

    // ── Water stats ───────────────────────────────────────────────────────────
    private VehicleStats StatsWater()
    {
        // "Coasting" bar inverted: low brakeForce = coasts longer.
        float coastBar = Mathf.Clamp01(1f - brakeForce / 0.68f);

        var traits = new System.Collections.Generic.List<string>();
        if (!allowReverse) traits.Add("No reverse (propulsion type)");
        traits.Add("No brakes - manage speed early");
        if (_kind == GameManager.VehicleKind.Ship) traits.Add("Enormous stopping distance");

        float spd = Mathf.Clamp01(maxSpeed / 3.9f);
        float rud = Mathf.Clamp01(turnRate / 83f);
        float hul = grip;

        return new VehicleStats
        {
            vehicleName = _kind.ToString(),
            rows = new[]
            {
                new StatRow { label = "Cruising Speed",    bar = spd,      grade = G(spd)      },
                new StatRow { label = "Rudder Response",   bar = rud,      grade = G(rud)      },
                new StatRow { label = "Hull Stability",    bar = hul,      grade = G(hul)      },
                new StatRow { label = "Coasting Distance", bar = coastBar, grade = G(coastBar) },
            },
            traits = traits.ToArray()
        };
    }

    // ── Air stats ─────────────────────────────────────────────────────────────
    private VehicleStats StatsAir()
    {
        bool heli = (_kind == GameManager.VehicleKind.Helicopter);

        var traits = new System.Collections.Generic.List<string>();
        if (!allowReverse)       traits.Add("No reverse thrust");
        if (minControlSpeed > 0f) traits.Add($"Stalls below {minControlSpeed:F1} m/s - keep moving");
        if (heli)                traits.Add("Can hover in place");
        // Glide distance: lower brakeForce = glides further
        float glideBar = heli ? 0f : Mathf.Clamp01(1f - brakeForce / 0.30f);
        if (!heli && glideBar > 0.5f) traits.Add("Long glide when engine off");

        float spd  = Mathf.Clamp01(maxSpeed  / (heli ? 5.2f : 8.2f));
        float bank = Mathf.Clamp01(turnRate  / (heli ? 120f : 80f));
        float stab = grip;
        float ctrl = heli ? Mathf.Clamp01(brakeForce / 2.5f) : glideBar;

        return new VehicleStats
        {
            vehicleName = _kind.ToString(),
            rows = new[]
            {
                new StatRow { label = "Airspeed",                  bar = spd,  grade = G(spd)  },
                new StatRow { label = heli ? "Yaw Rate" : "Bank Rate",   bar = bank, grade = G(bank) },
                new StatRow { label = heli ? "Air Stability" : "Slip Angle",  bar = stab, grade = G(stab) },
                new StatRow { label = heli ? "Engine Brake" : "Glide Range", bar = ctrl, grade = G(ctrl) },
            },
            traits = traits.ToArray()
        };
    }

    // ── Space stats ───────────────────────────────────────────────────────────
    private VehicleStats StatsSpace()
    {
        // In space: higher thrust = higher moveSpeed; rotation = turnRate.
        // Drift = inverse of grip (near-zero grip = maximum drift).
        float thr  = Mathf.Clamp01(moveSpeed / 1.80f);
        float rot  = Mathf.Clamp01(turnRate  / 152f);
        float drft = 1f - Mathf.Clamp01(grip / 0.05f); // higher = more drift
        float brak = Mathf.Clamp01(brakeForce / 0.06f);

        return new VehicleStats
        {
            vehicleName = _kind.ToString(),
            rows = new[]
            {
                new StatRow { label = "Thruster Output",   bar = thr,  grade = G(thr)  },
                new StatRow { label = "Rotation Rate",     bar = rot,  grade = G(rot)  },
                new StatRow { label = "Drift Factor",      bar = drft, grade = G(drft) },
                new StatRow { label = "Passive Braking",   bar = brak, grade = G(brak) },
            },
            traits = new[]
            {
                "Near-zero drag - inertia carries you",
                "Thrust in reverse to stop",
                freeRotate ? "Free rotation via thrusters" : "Limited yaw",
            }
        };
    }
} 