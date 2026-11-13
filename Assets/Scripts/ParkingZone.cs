using UnityEngine;
using System.Collections;

[RequireComponent(typeof(Collider2D))]
public class ParkingZone : MonoBehaviour
{
    [Header("Parking Settings")]
    public float angleToleranceDegrees = 45f;
    public float requiredStillnessDuration = 0.5f;
    public float centerToleranceFrac = 0.30f;
    public float maxParkSpeed = 1.0f;
    public bool elongatedZone = false;
    public Color idleColor    = new Color(1f, 1f, 0f, 0.4f);
    public Color successColor = new Color(0f, 1f, 0f, 0.6f);
    public Color failColor    = new Color(1f, 0f, 0f, 0.4f);

    private SpriteRenderer spriteRenderer;
    private VehicleController vehicleInZone;
    private float stillTimer;
    private bool ready = false;
    private bool parked = false;

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer != null) spriteRenderer.color = idleColor;
        GetComponent<Collider2D>().isTrigger = true;
        StartCoroutine(ActivateAfterDelay());
    }

    private IEnumerator ActivateAfterDelay()
    {
        yield return new WaitForSeconds(0.8f);
        ready = true;
    }

    private void Update()
    {
        if (!ready || parked) return;

        if (vehicleInZone == null)
        {
            // Idle pulse
            float pulse = (Mathf.Sin(Time.time * 2.2f) + 1f) * 0.5f;
            float alpha = Mathf.Lerp(0.22f, 0.52f, pulse);
            if (spriteRenderer != null)
                spriteRenderer.color = new Color(idleColor.r, idleColor.g, idleColor.b, alpha);
            stillTimer = 0f;
            return;
        }

        // A crashed (frozen) vehicle can look "still & centered" — never complete a park after a crash.
        if (vehicleInZone.HasCrashed) { stillTimer = 0f; return; }

        // Vehicle is in zone — check parking conditions
        bool centered = CheckCentered();
        bool aligned  = IsAligned();
        var rb = vehicleInZone.GetComponent<Rigidbody2D>();
        bool still = rb == null || rb.linearVelocity.magnitude <= maxParkSpeed;
        bool good = centered && aligned && still;

        if (spriteRenderer != null)
            spriteRenderer.color = good ? successColor : (centered ? failColor : idleColor);

        if (good)
        {
            stillTimer += Time.deltaTime;
            if (stillTimer >= requiredStillnessDuration)
                TriggerPark();
        }
        else
        {
            stillTimer = 0f;
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!ready || parked) return;
        var vc = other.GetComponent<VehicleController>();
        if (vc == null) return;
        vehicleInZone = vc;
        stillTimer = 0f;
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.GetComponent<VehicleController>() == null) return;
        vehicleInZone = null;
        stillTimer = 0f;
        if (spriteRenderer != null) spriteRenderer.color = idleColor;
    }

    private bool CheckCentered()
    {
        if (vehicleInZone == null) return false;
        var zoneCol = GetComponent<Collider2D>();
        Vector2 vpos = vehicleInZone.transform.position;
        // Hard gate for ALL zone types: the vehicle's CENTER must be inside the zone collider.
        // (Previously non-elongated zones only did a distance check, so a car hanging mostly
        // outside the green could still "complete" — this requires the center to be in the zone.)
        if (!zoneCol.OverlapPoint(vpos)) return false;
        if (elongatedZone) return true;   // OverlapPoint is the gate for runway/pier/dock berths
        // Plus a tightened distance-to-center check so the car sits near the middle of the spot.
        Vector2 ext = zoneCol.bounds.extents;
        float allowed = Mathf.Min(ext.x, ext.y) * centerToleranceFrac;
        float dist = Vector2.Distance(vpos, transform.position);
        return dist <= allowed;
    }

    private bool IsAligned()
    {
        if (vehicleInZone == null) return false;
        float angleDiff = Mathf.Abs(Mathf.DeltaAngle(
            vehicleInZone.transform.eulerAngles.z,
            transform.eulerAngles.z));
        if (elongatedZone)
            angleDiff = Mathf.Min(angleDiff, Mathf.Abs(180f - angleDiff));
        return angleDiff <= angleToleranceDegrees;
    }

    public bool IsVehicleInside() => vehicleInZone != null;

    private void TriggerPark()
    {
        parked = true;
        if (vehicleInZone != null) vehicleInZone.SetParked(true);
        if (spriteRenderer != null) spriteRenderer.color = successColor;

        TryVibrate();
        ScoreManager.Instance?.RegisterWin();   // grow win streak before scoring/display
        AudioManager.Instance?.PlayParkingSuccessSound(ScoreManager.Instance != null ? ScoreManager.Instance.WinStreak : 0);
        // Award coins: base 10 + a bonus that grows with the win streak.
        ScoreManager.Instance?.AddCoins(10 + (ScoreManager.Instance != null ? ScoreManager.Instance.WinStreak : 0));
        // Streak-milestone reward: every 5th consecutive park grants a coin bonus + a celebratory banner.
        int curStreak = ScoreManager.Instance != null ? ScoreManager.Instance.WinStreak : 0;
        if (curStreak > 0 && curStreak % 5 == 0)
        {
            int milestoneBonus = 25 * (curStreak / 5);   // grows: 25 @5, 50 @10, 75 @15 …
            ScoreManager.Instance?.AddCoins(milestoneBonus);
            UIManager.Instance?.ShowStreakMilestone(curStreak, milestoneBonus);
        }
        int score = ScoreManager.Instance != null ? ScoreManager.Instance.CalculateFinalScore() : 0;
        int level = GameManager.Instance != null ? GameManager.Instance.currentLevel : 1;
        // Persist the actual completion DURATION (Time.time is seconds-since-app-start, garbage here).
        float levelTime = ScoreManager.Instance != null ? ScoreManager.Instance.LastLevelTime : 0f;
        LevelDataManager.Instance?.SaveLevelProgress(level, score, levelTime);
        UIManager.Instance?.ShowLevelComplete();
    }

    private static void TryVibrate()
    {
        try { Handheld.Vibrate(); } catch { }
    }
}
