using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections.Generic;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [System.Serializable]
    public class VehicleData
    {
        public string vehicleName;
        public GameObject vehiclePrefab;
        public int wheelCount;
        public VehicleType vehicleType;
    }

    public enum VehicleType
    {
        Square, Triangle, Rectangle, Pentagon,
        Octagon, Circle, Airplane, Ship, Yacht, Boat, Amoeba
    }

    public enum TerrainType { Road, Water, Air, Space }

    public enum VehicleKind
    {
        Car, Truck, Bus,            // Road
        Boat, Yacht, Ship,          // Water
        Plane, Jet, Helicopter,     // Air
        Rocket, Shuttle, Satellite, // Space
        Blob, Star                  // Bizarre (very high levels)
    }

    private static readonly VehicleKind[] BizarreKinds = { VehicleKind.Blob, VehicleKind.Star };

    private static readonly VehicleKind[] RoadKinds  = { VehicleKind.Car, VehicleKind.Truck, VehicleKind.Bus };
    private static readonly VehicleKind[] WaterKinds = { VehicleKind.Boat, VehicleKind.Yacht, VehicleKind.Ship };
    private static readonly VehicleKind[] AirKinds   = { VehicleKind.Plane, VehicleKind.Jet, VehicleKind.Helicopter };
    private static readonly VehicleKind[] SpaceKinds = { VehicleKind.Rocket, VehicleKind.Shuttle, VehicleKind.Satellite };

    public static VehicleKind[] KindsForTerrain(TerrainType t) => t switch
    {
        TerrainType.Water => WaterKinds,
        TerrainType.Air   => AirKinds,
        TerrainType.Space => SpaceKinds,
        _                 => RoadKinds,
    };

    // The player's vehicle for a given terrain/level. New types are introduced gradually as levels
    // climb, then cycle so higher levels mix all of a terrain's vehicles. At very high levels (30+),
    // bizarre shapes appear to ramp up the challenge.
    public static VehicleKind KindForLevel(TerrainType t, int level)
    {
        if (level >= 30 && level % 3 == 0)              // every 3rd level past 30 → bizarre
            return BizarreKinds[(level / 3) % BizarreKinds.Length];

        var kinds = KindsForTerrain(t);
        // Levels 1-3 = the terrain's first/simplest vehicle; a new type every 3 levels, then cycle.
        int idx = Mathf.Max(0, (level - 1) / 3) % kinds.Length;
        return kinds[idx];
    }

    [Header("Game Settings")]
    public List<VehicleData> availableVehicles;
    public TerrainType currentTerrain;
    public int currentLevel = 1;
    public float windStrength = 0f;
    public float waveHeight = 0f;

    [Header("Level Settings")]
    public int totalLevels = 2000;
    public float levelDifficultyMultiplier = 1.1f;

    [Header("Vehicle Colors")]
    public Color[] vehicleColors = {
        new Color(0.2f, 0.6f, 1f),
        new Color(1f, 0.4f, 0.1f),
        new Color(0.2f, 0.9f, 0.3f),
        new Color(0.9f, 0.2f, 0.9f),
        new Color(1f, 0.9f, 0.1f),
    };

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            SceneManager.sceneLoaded += OnSceneLoaded;
        }
        else Destroy(gameObject);
    }

    private void OnDestroy() => SceneManager.sceneLoaded -= OnSceneLoaded;

    private void Start()
    {
        InitializeGame();
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // Show terrain selection first; UIManager will call StartGameWithTerrain() on pick
        if (scene.name == "Game")
            UIManager.Instance?.ShowTerrainSelect();
    }

    // Called by UIManager after the player picks a terrain.
    // Always resets to level 1 so terrain-select acts as "start fresh" — players who choose
    // a terrain expect gentle early levels, not to resume at whatever level they left off.
    public void StartGameWithTerrain(TerrainType terrain)
    {
        currentTerrain = terrain;
        currentLevel   = 1;
        // Starting fresh at level 1 also resets the win streak — otherwise re-selecting a terrain
        // lets players farm easy L1-3 wins for ever-growing streak coin bonuses.
        ScoreManager.Instance?.ResetStreak();
        UpdateEnvironmentEffects();
        BootstrapGameScene();
    }

    public void ForceCompleteCurrentLevel() => UIManager.Instance?.ShowLevelComplete();

    public void BootstrapGameScene()
    {
        var old = GameObject.Find("Vehicle");
        if (old != null) Destroy(old);
        var oldZ = GameObject.Find("ParkingZone");
        if (oldZ != null) Destroy(oldZ);
        var oldObs = GameObject.Find("Obstacles");
        if (oldObs != null) Destroy(oldObs);
        var oldTokens = GameObject.Find("TimeBonusTokens");
        if (oldTokens != null) Destroy(oldTokens);
        var oldDeco = GameObject.Find("Decorations");
        if (oldDeco != null) Destroy(oldDeco);
        var oldPuffs = GameObject.Find("_ThrustPuffs");
        if (oldPuffs != null) Destroy(oldPuffs);

        // Ensure SkidMarkManager exists and clear any marks from the previous level.
        if (SkidMarkManager.Instance == null)
            new GameObject("SkidMarkManager").AddComponent<SkidMarkManager>();
        SkidMarkManager.Instance?.ClearAll();

        SpawnVehicle();
        SpawnParkingZone();
        SpawnObstacles();
        SpawnDecorations();
        SetupBackground();

        ScoreManager.Instance?.StartLevel();
        var audioKind = KindForLevel(currentTerrain, currentLevel);
        AudioManager.Instance?.StartLevel(currentTerrain, audioKind, currentLevel);
        UIManager.Instance?.ShowGameUI();
        SpawnTimeBonusTokens();
    }

    // ── Shape / difficulty progression ───────────────────────────────────────

    // Returns the shape code for a level:
    //   3-8  = regular polygon with that many sides
    //   100+ = star polygon with (value-100) points
    public static int GetSidesForLevel(int level)
    {
        if (level <= 2)  return 4;    // square
        if (level <= 5)  return 3;    // triangle
        if (level <= 9)  return 5;    // pentagon
        if (level <= 14) return 6;    // hexagon
        if (level <= 19) return 7;    // heptagon
        if (level <= 24) return 8;    // octagon
        // Stars — 5-pointed, 6-pointed, 7-pointed, cycling
        int starPoints = 5 + ((level - 25) / 5) % 3; // 5, 6, 7, then repeats
        return 100 + starPoints;
    }

    // Gentle, forgiving early; tightens steadily later.
    public static float GetAngleToleranceForLevel(int level)
    {
        if (level <= 3)  return 40f;   // forgiving but must be roughly straight
        if (level <= 6)  return 35f;   // eased 33->35: softens the L4-6 spike (triangles + first spot rotation)
        if (level <= 10) return 31f;   // eased 28->31: fairer while players still learn controls
        if (level <= 15) return 23f;
        if (level <= 20) return 19f;
        if (level <= 30) return 15f;
        return 12f;
    }

    // Parallel-parking layout: every 4th Road level from L10 (10, 14, 18, ...) brackets the
    // spot with two parked cars, forcing a classic reverse-in maneuver. Road terrain only.
    private bool IsParallelParkingLevel()
    {
        return currentTerrain == TerrainType.Road
            && currentLevel >= 10
            && (currentLevel - 10) % 4 == 0;
    }

    // Berth/approach challenge for NON-Road terrains -- the Water/Air/Space analogue of
    // parallel parking (see IsParallelParkingLevel). Every 4th level from L12 (12, 16, 20, ...)
    // flanks the spot with two static obstacles that narrow the berth / form an approach gate.
    // Road is excluded here and this uses a different phase (L12 vs L10) than parallel parking,
    // so the two challenges can NEVER fire on the same level.
    private bool IsBerthChallengeLevel()
    {
        return currentTerrain != TerrainType.Road
            && currentLevel >= 12
            && (currentLevel - 12) % 4 == 0;
    }

    // Parking-spot rotation difficulty: upright early, partial mid, full random late.
    public static float GetSpotRotation(int level)
    {
        if (level <= 4) return 0f;                       // upright — easy to read
        if (level <= 9) return Random.Range(-55f, 55f);  // gentle angles
        return Random.Range(0f, 360f);                   // anything goes
    }

    // ── Spawn positions (level-scaled: near at L1, far apart at higher levels) ──

    // Base spawn positions — kept near screen center so the player starts visible.
    private static readonly Vector2[] _baseSpawn = {
        new Vector2( 0.0f, -1.4f),
        new Vector2(-1.2f, -1.4f),
        new Vector2( 1.2f, -1.4f),
        new Vector2( 0.0f, -1.8f),
        new Vector2(-1.8f, -0.4f),
        new Vector2( 1.8f, -1.0f),
        new Vector2(-0.8f, -1.8f),
        new Vector2( 1.4f, -0.2f),
    };

    // Zone positions spread across a wide area — higher levels push zones off-screen,
    // forcing the player to navigate and use the compass arrow.
    private static readonly Vector2[] _baseZone = {
        new Vector2( 0.0f,  5.5f),
        new Vector2(-4.5f,  3.5f),
        new Vector2( 4.0f,  4.0f),
        new Vector2( 5.5f, -1.5f),
        new Vector2(-5.0f, -1.0f),
        new Vector2( 3.0f, -4.5f),
        new Vector2(-3.5f,  5.0f),
        new Vector2( 0.0f, -5.5f),
    };

    private static Vector2 GetSpawnPos(int level)
    {
        int idx = (level - 1) % _baseSpawn.Length;
        // At very high levels the parking lot is larger, so nudge spawn out a little.
        float scale = 1f + Mathf.Clamp01((level - 1) / 40f) * 0.6f;
        return _baseSpawn[idx] * scale;
    }

    private static Vector2 GetZonePos(int level)
    {
        int idx = (level - 1) % _baseZone.Length;
        // Early levels: shorter distance so the zone is findable; ramps up quickly.
        float t = Mathf.Clamp01((level - 1) / 12f);
        float scale = Mathf.Lerp(0.50f, 1.0f + (level / 35f), t);
        return _baseZone[idx] * scale;
    }

    // Keep legacy array names to avoid breaking old references (not used by SpawnVehicle/Zone any more).
    private static readonly Vector2[] spawnPositions = _baseSpawn;
    private static readonly Vector2[] zonePositions  = _baseZone;

    private void SetupBackground()
    {
        if (BackgroundManager.Instance == null)
        {
            var go = new GameObject("BackgroundManager");
            go.AddComponent<BackgroundManager>();
        }
        BackgroundManager.Instance.SetTerrain(currentTerrain);
    }

    private void SpawnVehicle()
    {
        int colorIndex = (currentLevel - 1) % vehicleColors.Length;
        var sp = GetSpawnPos(currentLevel);

        var go = new GameObject("Vehicle");
        go.transform.position = new Vector3(sp.x, sp.y, 5f);
        var sr = go.AddComponent<SpriteRenderer>();
        var kind = KindForLevel(currentTerrain, currentLevel);
        // Selected shop skin overrides the per-level colour (Classic keeps per-level).
        Color baseCol = (ScoreManager.Instance != null ? ScoreManager.Instance.SelectedSkinColor() : null)
                        ?? vehicleColors[colorIndex];
        var vehicleSprite = SpriteGenerator.CreateVehicle(kind, 256, baseCol);
        sr.sprite = vehicleSprite;
        sr.sortingOrder = 2;
        go.transform.localScale = Vector3.one * 1.3f;

        // Soft drop shadow for depth (polished-minimalist look)
        var shadow = new GameObject("Shadow");
        shadow.transform.SetParent(go.transform, false);
        shadow.transform.localPosition = new Vector3(0.06f, -0.08f, 0f);
        shadow.transform.localScale = Vector3.one * 1.06f;
        var ssr = shadow.AddComponent<SpriteRenderer>();
        ssr.sprite = vehicleSprite;
        ssr.color = new Color(0f, 0f, 0f, 0.32f);
        ssr.sortingOrder = 1;
        var rb = go.AddComponent<Rigidbody2D>();
        rb.gravityScale = 0f;
        rb.linearDamping = 3f;
        rb.angularDamping = 5f;
        go.AddComponent<BoxCollider2D>();
        var vc = go.AddComponent<VehicleController>();
        vc.ConfigureForTerrain(currentTerrain, kind);

        var trail = go.AddComponent<ThrustTrail>();
        trail.Init(currentTerrain);

        // High-level handling: per-terrain difficulty escalation — harder to control as levels climb.
        if (currentLevel >= 12)
        {
            float tg = Mathf.Clamp01((currentLevel - 12) / 38f); // 0 at L12 → 1 at L50+
            switch (currentTerrain)
            {
                case TerrainType.Road:   // slippery + faster (less grip, higher top speed)
                    vc.ApplyHandling(turnMul: Mathf.Lerp(1f, 0.85f, tg), gripMul: Mathf.Lerp(1f, 0.55f, tg), moveMul: Mathf.Lerp(1f, 1.25f, tg));
                    break;
                case TerrainType.Water:  // more slide/coast
                    vc.ApplyHandling(gripMul: Mathf.Lerp(1f, 0.70f, tg), moveMul: Mathf.Lerp(1f, 1.12f, tg));
                    break;
                case TerrainType.Air:    // twitchier steering, faster
                    vc.ApplyHandling(turnMul: Mathf.Lerp(1f, 0.80f, tg), moveMul: Mathf.Lerp(1f, 1.15f, tg));
                    break;
                case TerrainType.Space:  // more spin + drift
                    vc.ApplyHandling(turnMul: Mathf.Lerp(1f, 1.18f, tg), gripMul: Mathf.Lerp(1f, 0.5f, tg));
                    break;
            }
        }

        var cam = Camera.main;
        if (cam != null)
        {
            // Closer, more immersive view (was zoomed out too far)
            cam.orthographic = true;
            cam.orthographicSize = 3.2f;
            var follow = cam.GetComponent<CameraFollow>();
            if (follow == null) follow = cam.gameObject.AddComponent<CameraFollow>();
            follow.target = go.transform;
            follow.deadZone = new Vector2(0.7f, 0.7f);  // track the vehicle more tightly
            follow.smoothSpeed = 6f;
        }
    }

    private void SpawnParkingZone()
    {
        var zp = GetZonePos(currentLevel);

        var go = new GameObject("ParkingZone");
        go.transform.position = new Vector3(zp.x, zp.y, 5f);

        var sr = go.AddComponent<SpriteRenderer>();
        sr.sortingOrder = -1;

        bool runway = currentTerrain == TerrainType.Air;
        bool dock   = currentTerrain == TerrainType.Space;
        if (runway)
        {
            sr.sprite = SpriteGenerator.CreateRunwayZone();
            go.transform.localScale = Vector3.one;
            float rwRot = currentLevel <= 6 ? 0f : Random.Range(-25f, 25f);
            go.transform.rotation = Quaternion.Euler(0, 0, rwRot);

            // Level 7+: back the runway with an aircraft carrier hull so the
            // landing strip looks like it's on a carrier deck (purely visual, no physics).
            if (currentLevel >= 7)
            {
                var carrierGO = new GameObject("CarrierShip");
                carrierGO.transform.SetParent(go.transform, false);
                carrierGO.transform.localPosition = Vector3.zero;
                var csr = carrierGO.AddComponent<SpriteRenderer>();
                csr.sprite = SpriteGenerator.CreateAircraftCarrier();
                csr.sortingOrder = -2;  // behind zone sprite (-1)
            }
        }
        else if (dock)
        {
            sr.sprite = SpriteGenerator.CreateDockingPort();
            go.transform.localScale = Vector3.one;
            float dockRot = currentLevel <= 4 ? 0f : Random.Range(-30f, 30f);
            go.transform.rotation = Quaternion.Euler(0, 0, dockRot);

            // Level 7+: back the docking port with a full space station so the
            // port looks like the hub of a real station (purely visual, no physics).
            if (currentLevel >= 7)
            {
                var stationGO = new GameObject("SpaceStation");
                stationGO.transform.SetParent(go.transform, false);
                stationGO.transform.localPosition = Vector3.zero;
                var ssr = stationGO.AddComponent<SpriteRenderer>();
                ssr.sprite = SpriteGenerator.CreateSpaceStation();
                ssr.sortingOrder = -2;  // behind zone sprite (-1)
            }
        }
        else
        {
            var kind = KindForLevel(currentTerrain, currentLevel);
            sr.sprite = SpriteGenerator.CreateVehicleZone(kind, 320);
            go.transform.rotation = Quaternion.Euler(0, 0, GetSpotRotation(currentLevel));
            float zoneScale = currentLevel <= 3 ? 1.72f
                            : currentLevel <= 6 ? 1.62f
                            : Mathf.Lerp(1.56f, 1.20f, Mathf.Clamp01((currentLevel - 6) / 24f));
            go.transform.localScale = Vector3.one * zoneScale;

            // Water level 7+: back the boat berth with a wooden pier so the spot reads as a
            // moored dock (purely visual, no physics) — mirrors the Air carrier / Space station.
            if (currentTerrain == TerrainType.Water && currentLevel >= 7)
            {
                var pierGO = new GameObject("Pier");
                pierGO.transform.SetParent(go.transform, false);
                pierGO.transform.localPosition = Vector3.zero;
                // Counter the zone's localScale so the pier renders at a consistent world size.
                pierGO.transform.localScale = Vector3.one / Mathf.Max(0.01f, zoneScale);
                var psr = pierGO.AddComponent<SpriteRenderer>();
                psr.sprite = SpriteGenerator.CreatePier();
                psr.sortingOrder = -2;  // behind zone sprite (-1)
            }
        }

        // Add collider AFTER sprite + scale so BoxCollider2D auto-sizes to the sprite correctly.
        var col = go.AddComponent<BoxCollider2D>();
        col.isTrigger = true;
        var zone = go.AddComponent<ParkingZone>();
        zone.angleToleranceDegrees = GetAngleToleranceForLevel(currentLevel);
        if (runway || dock)
        {
            zone.elongatedZone = true;
            zone.angleToleranceDegrees = Mathf.Min(zone.angleToleranceDegrees, 22f);
        }

        // Moving zone: starts at level 12, speed and range increase gradually
        if (currentLevel >= 12)
        {
            var mover = go.AddComponent<ZoneMover>();
            float t = Mathf.Clamp01((currentLevel - 12) / 30f); // 0 at L12, 1 at L42+
            mover.speed = Mathf.Lerp(0.4f, 1.2f, t);
            mover.rangeX = Mathf.Lerp(1.0f, 3.0f, t);
            mover.rangeY = currentLevel >= 20 ? Mathf.Lerp(0f, 1.5f, Mathf.Clamp01((currentLevel - 20) / 20f)) : 0f;
            mover.phaseOffset = UnityEngine.Random.Range(0f, Mathf.PI * 2f);
        }
    }

    // Traffic / hazards you must avoid — hitting one is game over. Density grows with level,
    // and from mid levels some of them move, turning each level into a puzzle to thread through.
    private void SpawnObstacles()
    {
        // Starts at L4, +1 every 2 levels, up to 9 — denser, harder later.
        int count = Mathf.Clamp((currentLevel - 2) / 2, 0, 9);
        if (count <= 0) return;

        Vector2 spawnPos = GetSpawnPos(currentLevel);
        Vector2 zonePos  = GetZonePos(currentLevel);

        var container = new GameObject("Obstacles");

        // Parallel-parking layout (see IsParallelParkingLevel): two static parked cars sit
        // directly ahead of and behind the spot along its facing axis, leaving a gap of
        // ~1.6x the vehicle length so the player must reverse in. They go into the container
        // FIRST so the random loop's proximity check keeps other hazards clear of them.
        if (IsParallelParkingLevel())
        {
            var zoneObj = Object.FindFirstObjectByType<ParkingZone>();
            float zRot = zoneObj != null ? zoneObj.transform.eulerAngles.z : 0f;
            // Zone must stay put or it drifts out of its bracket gap (mover starts at L12).
            if (zoneObj != null)
            {
                var zmover = zoneObj.GetComponent<ZoneMover>();
                if (zmover != null) Destroy(zmover);
            }
            Vector2 axis = Quaternion.Euler(0, 0, zRot) * Vector2.up;
            for (int i = 0; i < 2; i++)
            {
                Vector2 bpos = zonePos + axis * (i == 0 ? 1.6f : -1.6f);
                var bracket = new GameObject("BracketCar");
                bracket.transform.SetParent(container.transform);
                bracket.transform.position = new Vector3(bpos.x, bpos.y, 5f);
                bracket.transform.rotation = Quaternion.Euler(0, 0, zRot); // aligned with the spot
                bracket.transform.localScale = Vector3.one * 1.25f;
                var bsr = bracket.AddComponent<SpriteRenderer>();
                bsr.sprite = SpriteGenerator.CreateHazard(currentTerrain, i, 256);
                bsr.sortingOrder = 1;
                // Same order as the loop below: collider + body BEFORE Obstacle (RequireComponent).
                var bcol = bracket.AddComponent<BoxCollider2D>();
                bcol.size *= 0.6f;
                var brb = bracket.AddComponent<Rigidbody2D>();
                brb.gravityScale = 0f;
                brb.bodyType = RigidbodyType2D.Static;
                var bmarker = bracket.AddComponent<Obstacle>();
                bmarker.hazardLabel = HazardName(currentTerrain, i);
            }
        }
        // Berth/approach flankers (see IsBerthChallengeLevel): two static obstacles set
        // PERPENDICULAR to the spawn->zone approach axis, offset +/-1.4u from the spot, so the
        // player must thread between them to reach the berth. Water reads as dock pilings; Air/
        // Space read as an approach gate. Placed FIRST (like BracketCar) so the random loop's
        // proximity check keeps other hazards clear of them. Mutually exclusive with the
        // parallel-parking block above (that is Road-only; this is non-Road only).
        if (IsBerthChallengeLevel())
        {
            var zoneObj = Object.FindFirstObjectByType<ParkingZone>();
            // Zone must stay put or it drifts out of the gap (ZoneMover starts at L12).
            if (zoneObj != null)
            {
                var zmover = zoneObj.GetComponent<ZoneMover>();
                if (zmover != null) Destroy(zmover);
            }
            // Gate opening straddles the approach direction (spawn->zone), so the player drives
            // through it head-on. Fall back to +X if spawn==zone (never happens, but be safe).
            Vector2 approach = (zonePos - spawnPos).sqrMagnitude > 0.0001f
                             ? (zonePos - spawnPos).normalized : Vector2.right;
            Vector2 gateAxis = Vector2.Perpendicular(approach); // across the approach lane
            // Elongated zones (Air runway / Water dock) extend well past their center along a
            // rotation that is INDEPENDENT of the approach, so a fixed 1.4u flanker can land ON
            // the landing strip and make the level unwinnable. Push the offset out past the
            // zone's world half-extent (+ obstacle half-size) for those, and keep a hard
            // OverlapPoint guard below as a belt-and-suspenders skip.
            var zoneCol = zoneObj != null ? zoneObj.GetComponent<Collider2D>() : null;
            float flankOffset = 1.4f;
            if (zoneObj != null && zoneObj.elongatedZone && zoneCol != null)
                flankOffset = Mathf.Max(1.4f, zoneCol.bounds.extents.magnitude + 0.9f);
            for (int i = 0; i < 2; i++)
            {
                Vector2 fpos = zonePos + gateAxis * (i == 0 ? flankOffset : -flankOffset);
                // Never let a flanker land on top of the player's spawn.
                if (Vector2.Distance(fpos, spawnPos) < 3.4f) continue;
                // Never let a flanker land inside the parking zone itself (unwinnable).
                if (zoneCol != null && zoneCol.OverlapPoint(fpos)) continue;
                bool isGate = currentTerrain == TerrainType.Air || currentTerrain == TerrainType.Space;
                var flank = new GameObject(isGate ? "ApproachGate" : "DockPiling");
                flank.transform.SetParent(container.transform);
                flank.transform.position = new Vector3(fpos.x, fpos.y, 5f);
                flank.transform.rotation = Quaternion.Euler(0, 0, Random.Range(0f, 360f));
                flank.transform.localScale = Vector3.one * 1.25f;
                var fsr = flank.AddComponent<SpriteRenderer>();
                fsr.sprite = SpriteGenerator.CreateHazard(currentTerrain, i, 256);
                fsr.sortingOrder = 1;
                // collider + body BEFORE Obstacle (RequireComponent(Collider2D)).
                var fcol = flank.AddComponent<BoxCollider2D>();
                fcol.size *= 0.6f;
                var frb = flank.AddComponent<Rigidbody2D>();
                frb.gravityScale = 0f;
                frb.bodyType = RigidbodyType2D.Static;
                var fmarker = flank.AddComponent<Obstacle>();
                fmarker.hazardLabel = HazardName(currentTerrain, i);
            }
        }
        bool moving = currentLevel >= 9;                 // moving traffic/hazards from L9
        float moveChance = Mathf.Clamp01((currentLevel - 9) / 20f) * 0.6f;

        // Determine placement area: obstacles fill the corridor between spawn and zone.
        Vector2 mid = (spawnPos + zonePos) * 0.5f;
        float halfW = Mathf.Max(3.5f, Vector2.Distance(spawnPos, zonePos) * 0.55f);
        float halfH = Mathf.Max(3.0f, halfW * 0.75f);

        int placed = 0, attempts = 0;
        while (placed < count && attempts < 120)
        {
            attempts++;
            var pos = new Vector2(
                mid.x + Random.Range(-halfW, halfW),
                mid.y + Random.Range(-halfH, halfH));
            // Keep well clear of the vehicle spawn (no instant collision — large vehicles like the
            // Bus need a bigger buffer or an obstacle overlaps them at spawn) and the spot (path exists).
            if (Vector2.Distance(pos, spawnPos) < 3.4f) continue;
            if (Vector2.Distance(pos, zonePos) < 2.1f) continue;
            bool tooClose = false;
            foreach (Transform existing in container.transform)
                if (Vector2.Distance(pos, existing.position) < 1.4f) { tooClose = true; break; }
            if (tooClose) continue;

            var ob = new GameObject("Obstacle");
            ob.transform.SetParent(container.transform);
            ob.transform.position = new Vector3(pos.x, pos.y, 5f);
            ob.transform.rotation = Quaternion.Euler(0, 0, Random.Range(0f, 360f));
            ob.transform.localScale = Vector3.one * 1.25f;
            var osr = ob.AddComponent<SpriteRenderer>();
            osr.sprite = SpriteGenerator.CreateHazard(currentTerrain, placed, 256);
            osr.sortingOrder = 1;

            // Add the collider + body BEFORE the Obstacle component: Obstacle has
            // [RequireComponent(typeof(Collider2D))], and Collider2D is abstract — adding Obstacle
            // first makes AddComponent return null (→ NRE at L9+ where 'marker' is used).
            // Collider auto-sizes to the sprite, which includes transparent padding around the
            // visible hazard — tighten it so you only crash on real contact (no "phantom" hits on
            // empty space near a hazard). ~0.6x of the padded box matches the drawn shape.
            var ocol = ob.AddComponent<BoxCollider2D>();
            ocol.size *= 0.6f;
            var orb = ob.AddComponent<Rigidbody2D>();
            orb.gravityScale = 0f;
            var marker = ob.AddComponent<Obstacle>();
            marker.hazardLabel = HazardName(currentTerrain, placed);

            // Clamp the patrol range so the sweep can never reach the parking zone or the
            // player's spawn (clearances were checked at the spawn POINT only, so a 1.6u
            // sine path could sweep directly over the spot). moveRange must NEVER exceed slack —
            // if there isn't at least 0.3u of safe travel, the hazard stays static (a 0.3f Clamp
            // floor here would re-inflate the amplitude above slack and defeat the guard).
            float slack = Mathf.Min(Vector2.Distance(pos, zonePos) - 2.1f,
                                    Vector2.Distance(pos, spawnPos) - 3.4f);
            float amp = Mathf.Min(Random.Range(0.8f, 1.6f), slack);
            if (moving && marker != null && Random.value < moveChance && amp >= 0.3f)
            {
                marker.movementType = Obstacle.ObstacleMovement.Linear;
                // Faster-moving hazards at higher levels (up to ~2.2x by L52+).
                float obSpeedMul = 1f + Mathf.Clamp01((currentLevel - 12) / 40f) * 1.2f;
                marker.moveSpeed = Random.Range(0.6f, 1.2f) * obSpeedMul;
                marker.moveRange = Mathf.Min(amp, 1.6f);
                marker.moveDirection = Random.insideUnitCircle.normalized;
                orb.bodyType = RigidbodyType2D.Kinematic; // moved via transform, still collides
            }
            else
            {
                orb.bodyType = RigidbodyType2D.Static;
            }
            placed++;
        }
    }

    // Human-readable hazard name for a given terrain/index — MUST mirror SpriteGenerator.CreateHazard.
    private static string HazardName(TerrainType terrain, int index)
    {
        switch (terrain)
        {
            case TerrainType.Water: return (index % 3 == 0) ? "a whale" : (index % 3 == 1) ? "a dolphin" : "a ship";
            case TerrainType.Space: return "an asteroid";
            case TerrainType.Air:   return new[] { "a plane", "a jet", "a helicopter" }[index % 3];
            default:                return (index % 4 == 3) ? "a pedestrian" : new[] { "a car", "a truck", "a bus" }[index % 3];
        }
    }

    // ── Time-bonus tokens ──────────────────────────────────────────────────────
    // Golden "+5s" pickups scattered between spawn and zone — give the player extra
    // time if they explore. Starts at level 2: 1 token; +1 every 4 levels, capped at 4.
    private void SpawnTimeBonusTokens()
    {
        if (currentLevel < 2) return;
        int tokenCount = Mathf.Clamp(1 + (currentLevel - 2) / 4, 1, 4);

        var sp = GetSpawnPos(currentLevel);
        var zp = GetZonePos(currentLevel);
        var container = new GameObject("TimeBonusTokens");

        int placed = 0, attempts = 0;
        while (placed < tokenCount && attempts < 60)
        {
            attempts++;
            // Random point along the spawn→zone corridor
            float t = Random.Range(0.2f, 0.8f);
            Vector2 along = Vector2.Lerp(sp, zp, t);
            Vector2 perp  = Vector2.Perpendicular((zp - sp).normalized) * Random.Range(-1.8f, 1.8f);
            Vector2 pos   = along + perp;

            if (Vector2.Distance(pos, sp) < 1.8f) continue;
            if (Vector2.Distance(pos, zp) < 1.8f) continue;
            bool clash = false;
            foreach (Transform existing in container.transform)
                if (Vector2.Distance(pos, existing.position) < 1.2f) { clash = true; break; }
            if (clash) continue;

            var go = new GameObject("TimeBonusToken");
            go.transform.SetParent(container.transform);
            go.transform.position = new Vector3(pos.x, pos.y, 4f);
            go.AddComponent<TimeBonusToken>();
            placed++;
        }
    }

    // ── World decorations (purely visual, no physics) ─────────────────────────
    // Scattered static objects that make each level feel less empty.
    // Road: green trees + orange traffic cones.  Water: red/green buoys.
    // Air and Space use dramatic backgrounds that don't need ground clutter.
    private void SpawnDecorations()
    {
        if (currentTerrain == TerrainType.Air || currentTerrain == TerrainType.Space) return;

        var sp = GetSpawnPos(currentLevel);
        var zp = GetZonePos(currentLevel);
        float spread = Mathf.Max(6f, Vector2.Distance(sp, zp) * 1.1f);

        var container = new GameObject("Decorations");

        int treeCount = currentTerrain == TerrainType.Road ? 12 : 8;
        int coneCount = currentTerrain == TerrainType.Road ? 4  : 0;

        // Trees (road) or buoys (water)
        for (int i = 0; i < treeCount; i++)
        {
            Vector2 pos = RandomDecoPos(sp, zp, spread, 30, 1.8f, 2.2f);
            var go = new GameObject("Deco");
            go.transform.SetParent(container.transform);
            go.transform.position = new Vector3(pos.x, pos.y, 4.5f);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sortingOrder = -2;

            if (currentTerrain == TerrainType.Road)
            {
                float s = Random.Range(0.50f, 0.85f);
                sr.sprite = SpriteGenerator.CreateCircle(64,
                    new Color(0.14f + Random.Range(0f, 0.06f),
                              0.55f + Random.Range(-0.08f, 0.10f),
                              0.12f));
                go.transform.localScale = Vector3.one * s;
            }
            else // Water buoys
            {
                sr.sprite = SpriteGenerator.CreateCircle(32,
                    i % 2 == 0 ? new Color(0.9f, 0.15f, 0.15f) : new Color(0.15f, 0.82f, 0.25f));
                go.transform.localScale = Vector3.one * 0.28f;
            }
        }

        // Traffic cones (road only)
        for (int i = 0; i < coneCount; i++)
        {
            Vector2 pos = RandomDecoPos(sp, zp, spread, 20, 1.2f, 1.8f);
            var go = new GameObject("Cone");
            go.transform.SetParent(container.transform);
            go.transform.position = new Vector3(pos.x, pos.y, 4.5f);
            go.transform.rotation = Quaternion.Euler(0, 0, Random.Range(0f, 360f));
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sortingOrder = -2;
            sr.sprite = SpriteGenerator.CreateRegularPolygon(3, 32, new Color(1f, 0.50f, 0.05f));
            go.transform.localScale = Vector3.one * 0.40f;
        }
    }

    // Returns a random point for a decoration, avoiding spawn/zone by the given radii.
    private static Vector2 RandomDecoPos(
        Vector2 sp, Vector2 zp, float spread,
        int maxAttempts, float spawnClear, float zoneClear)
    {
        for (int a = 0; a < maxAttempts; a++)
        {
            var pos = new Vector2(
                Random.Range(-spread, spread),
                Random.Range(-spread, spread));
            if (Vector2.Distance(pos, sp) < spawnClear) continue;
            if (Vector2.Distance(pos, zp) < zoneClear)  continue;
            return pos;
        }
        return new Vector2(Random.Range(-spread, spread), Random.Range(-spread, spread));
    }

    // ── Level management ──────────────────────────────────────────────────────

    private void InitializeGame()
    {
        currentLevel = 1;
        currentTerrain = TerrainType.Road;
        windStrength = 0f;
        waveHeight = 0f;
    }

    public void LoadLevel(int levelNumber)
    {
        currentLevel = levelNumber;
        UpdateEnvironmentEffects();
        BootstrapGameScene();
    }

    public void CompleteLevel()
    {
        currentLevel++;
        if (currentLevel > totalLevels)
        {
            // Finished the final level: clamp (currentLevel=totalLevels+1 made GetLevelData return
            // null downstream) and return to terrain select instead of silently dead-ending.
            Debug.Log("All levels complete!");
            currentLevel = totalLevels;
            UIManager.Instance?.ShowTerrainSelect();
            return;
        }
        LoadLevel(currentLevel);
    }

    public void UpdateEnvironmentEffects()
    {
        switch (currentTerrain)
        {
            case TerrainType.Road:
                windStrength = 0f;
                waveHeight   = 0f;
                break;
            case TerrainType.Water:   // stronger current + waves at higher levels
                windStrength = Mathf.Min(currentLevel * 0.05f, 2.5f);
                waveHeight   = Mathf.Min(currentLevel * 0.03f, 1.6f);
                break;
            case TerrainType.Air:     // gustier, stronger wind at higher levels
                windStrength = Mathf.Min(currentLevel * 0.18f, 9f);
                waveHeight   = 0f;
                break;
            case TerrainType.Space:   // gentle drift that grows — nudges the near-frictionless craft
                windStrength = Mathf.Min(currentLevel * 0.03f, 1.2f);
                waveHeight   = 0f;
                break;
        }
    }

    public VehicleData GetCurrentVehicle()
    {
        if (availableVehicles == null || availableVehicles.Count == 0) return null;
        int vehicleIndex = Mathf.Clamp(currentLevel / 10, 0, availableVehicles.Count - 1);
        return availableVehicles[vehicleIndex];
    }
}
