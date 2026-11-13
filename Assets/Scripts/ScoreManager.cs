using UnityEngine;
using System.Collections.Generic;

public class ScoreManager : MonoBehaviour
{
    public static ScoreManager Instance { get; private set; }

    // How an achievement is earned. Score = single-level final score; Streak = consecutive
    // parks (BestStreak); Coins = lifetime coin balance reached.
    public enum AchKind { Score, Streak, Coins }

    [System.Serializable]
    public class Achievement
    {
        public string name;
        public string description;
        public int requiredScore;       // threshold for this achievement's kind
        public bool isUnlocked;
        public AchKind kind;            // defaults to Score (0) — existing entries unaffected
    }

    [Header("Score Settings")]
    public int baseScore = 1000;
    public float timeBonusMultiplier = 100f;
    public float perfectParkingBonus = 500f;
    public float obstaclePenalty = 200f;

    [Header("Achievements")]
    public List<Achievement> achievements = new List<Achievement>();

    private float currentScore;
    private float levelStartTime;
    private int collisionCount;
    private bool perfectParking;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            SeedDefaultAchievements();
            LoadAchievements();
            Coins = PlayerPrefs.GetInt("Coins", 0);
            BestStreak = PlayerPrefs.GetInt("BestStreak", 0);
            SelectedSkin = PlayerPrefs.GetInt("SelectedSkin", 0);
            // Sanitize: a persisted skin that is no longer unlocked (e.g. an achievement-gated
            // one after a progress reset) must not stay selected. Runs after LoadAchievements().
            if (!IsSkinUnlocked(SelectedSkin)) { SelectedSkin = 0; PlayerPrefs.SetInt("SelectedSkin", 0); }
            LevelsCleared = PlayerPrefs.GetInt("LevelsCleared", 0);
            BestScore = PlayerPrefs.GetInt("BestScore", 0);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    // Populate a default achievement set if none were configured in the Inspector.
    private void SeedDefaultAchievements()
    {
        if (achievements != null && achievements.Count > 0) return;
        achievements = new List<Achievement>
        {
            new Achievement { name = "First Park",     description = "Complete your first level",        requiredScore = 1 },
            new Achievement { name = "Smooth Operator", description = "Finish a level scoring 1,500+",    requiredScore = 1500 },
            new Achievement { name = "Pro Parker",      description = "Finish a level scoring 2,000+",    requiredScore = 2000 },
            new Achievement { name = "Precision Ace",   description = "Finish a level scoring 2,500+",    requiredScore = 2500 },
            new Achievement { name = "Parking Legend",  description = "Finish a level scoring 3,000+",    requiredScore = 3000 },
            // Streak-based (checked on each win)
            new Achievement { name = "On a Roll",       description = "Reach a 5-park win streak",         requiredScore = 5,    kind = AchKind.Streak },
            new Achievement { name = "Unstoppable",     description = "Reach a 10-park win streak",        requiredScore = 10,   kind = AchKind.Streak },
            new Achievement { name = "Streak Master",   description = "Reach a 20-park win streak",        requiredScore = 20,   kind = AchKind.Streak },
            // Coin-based (checked when coins are earned)
            new Achievement { name = "Coin Collector",  description = "Bank 250 coins",                    requiredScore = 250,  kind = AchKind.Coins },
            new Achievement { name = "High Roller",     description = "Bank 1,000 coins",                  requiredScore = 1000, kind = AchKind.Coins },
        };
    }

    private void LoadAchievements()
    {
        // Load unlocked achievements from PlayerPrefs
        foreach (Achievement achievement in achievements)
        {
            achievement.isUnlocked = PlayerPrefs.GetInt($"Achievement_{achievement.name}", 0) == 1;
        }
    }

    public void StartLevel()
    {
        currentScore = 0;
        levelStartTime = Time.time;
        collisionCount = 0;
        perfectParking = true;
    }

    public void AddCollision()
    {
        collisionCount++;
        perfectParking = false;
        currentScore = Mathf.Max(0, currentScore - obstaclePenalty);
    }

    // ── Win streak (consecutive level completions) ─────────────────────────────
    // Builds tension/reward: each completed level grows the streak (and a score bonus);
    // a game-over (crash or timeout) resets it. In-session only (resets on app restart).
    public int WinStreak { get; private set; }
    public int BestStreak { get; private set; }
    public int streakBonusPerLevel = 150;
    // True once the current run has set a new all-time best streak (until the streak resets).
    public bool NewBestThisRun { get; private set; }
    public void RegisterWin()
    {
        WinStreak++;
        if (WinStreak > BestStreak)
        {
            BestStreak = WinStreak;
            NewBestThisRun = true;
            PlayerPrefs.SetInt("BestStreak", BestStreak);
            PlayerPrefs.Save();
        }
        EvaluateAchievements(AchKind.Streak, WinStreak);

        // Lifetime levels-cleared counter (persisted; one win == one cleared level).
        LevelsCleared++;
        PlayerPrefs.SetInt("LevelsCleared", LevelsCleared);
        PlayerPrefs.Save();
    }

    // Total levels completed across all sessions (persisted).
    public int LevelsCleared { get; private set; }
    public void ResetStreak() { WinStreak = 0; NewBestThisRun = false; NearMissCombo = 0; }

    // ── Near-miss combo (consecutive obstacle grazes; resets on crash/game-over) ──
    public int NearMissCombo { get; private set; }
    public int RegisterNearMiss() { NearMissCombo++; return NearMissCombo; }

    // ── Coins (persistent currency, earned per completed level) ────────────────
    public int Coins { get; private set; }
    public int LastCoinsEarned { get; private set; }   // for the "+N coins" reward popup
    public void AddCoins(int n)
    {
        // Reflect the most recent award (including 0) so the level-complete "+N COINS" flash and
        // coin-fly never show a stale count from an earlier level that earned coins.
        LastCoinsEarned = Mathf.Max(0, n);
        Coins = Mathf.Max(0, Coins + n);
        PlayerPrefs.SetInt("Coins", Coins);
        PlayerPrefs.Save();
        if (n > 0) EvaluateAchievements(AchKind.Coins, Coins);
    }

    // ── Vehicle skins (coin sink) ──────────────────────────────────────────────
    // requiredAchievement: when set, the skin is earned by unlocking that achievement (never coin-buyable).
    public struct Skin { public string name; public Color color; public int price; public string requiredAchievement; }
    public static readonly Skin[] Skins = new Skin[]
    {
        new Skin { name = "Classic", color = new Color(0,0,0,0),               price = 0   }, // a=0 → per-level colour
        new Skin { name = "Crimson", color = new Color(0.85f,0.18f,0.18f),     price = 100 },
        new Skin { name = "Emerald", color = new Color(0.16f,0.70f,0.34f),     price = 150 },
        new Skin { name = "Ocean",   color = new Color(0.16f,0.45f,0.85f),     price = 150 },
        new Skin { name = "Violet",  color = new Color(0.55f,0.25f,0.80f),     price = 200 },
        new Skin { name = "Gold",    color = new Color(0.95f,0.78f,0.20f),     price = 300 },
        new Skin { name = "Sunset",  color = new Color(1.00f,0.45f,0.15f),     price = 250 },
        new Skin { name = "Mint",    color = new Color(0.40f,0.90f,0.70f),     price = 250 },
        new Skin { name = "Slate",   color = new Color(0.45f,0.48f,0.55f),     price = 350 },
        new Skin { name = "Rose Gold", color = new Color(0.90f,0.55f,0.50f),   price = 400 },
        new Skin { name = "Midnight",  color = new Color(0.10f,0.12f,0.30f),   price = 500 },
        new Skin { name = "Neon Lime", color = new Color(0.65f,0.95f,0.15f),   price = 600 },
        new Skin { name = "Champion",  color = new Color(0.98f,0.90f,0.65f),   price = 0, requiredAchievement = "Parking Legend" },
    };
    public int SelectedSkin { get; private set; }
    public bool IsSkinUnlocked(int i)
    {
        if (i == 0) return true;
        if (i < 0 || i >= Skins.Length) return false;
        var s = Skins[i];
        if (!string.IsNullOrEmpty(s.requiredAchievement))
        {
            // Achievement-gated: unlocked exactly when the named achievement is earned.
            foreach (var a in achievements)
                if (a.name == s.requiredAchievement) return a.isUnlocked;
            return false;
        }
        return PlayerPrefs.GetInt($"Skin_{i}", 0) == 1;
    }
    public void SelectSkin(int i)
    {
        if (i < 0 || i >= Skins.Length || !IsSkinUnlocked(i)) return;
        SelectedSkin = i;
        PlayerPrefs.SetInt("SelectedSkin", i);
        PlayerPrefs.Save();
    }
    // Returns true if the skin is now selected (bought it, or already owned). False if too poor.
    public bool TryBuyOrSelectSkin(int i)
    {
        if (i < 0 || i >= Skins.Length) return false;
        if (IsSkinUnlocked(i)) { SelectSkin(i); return true; }
        // Achievement-gated skins can never be coin-purchased (Champion has price 0 — without
        // this guard the fall-through would auto-buy it).
        if (!string.IsNullOrEmpty(Skins[i].requiredAchievement)) return false;
        if (Coins < Skins[i].price) return false;
        AddCoins(-Skins[i].price);
        PlayerPrefs.SetInt($"Skin_{i}", 1);
        SelectSkin(i);
        return true;
    }
    // Chosen skin tint, or null to keep the default per-level colour.
    public Color? SelectedSkinColor()
    {
        int idx = Mathf.Clamp(SelectedSkin, 0, Skins.Length - 1);
        if (!IsSkinUnlocked(idx)) idx = 0;   // never render a skin the player does not own
        var s = Skins[idx];
        return s.color.a <= 0f ? (Color?)null : s.color;
    }

    // ── Per-terrain best completion time (persisted) ───────────────────────────
    public float LastLevelTime { get; private set; }
    public float TerrainBestTime { get; private set; }   // best (fastest) time for the just-finished terrain
    public bool  IsNewTerrainBest { get; private set; }
    private void RecordTerrainBestTime(float timeTaken)
    {
        LastLevelTime = timeTaken;
        var terr = GameManager.Instance != null ? GameManager.Instance.currentTerrain : GameManager.TerrainType.Road;
        string key = $"BestTime_{terr}";
        float prev = PlayerPrefs.GetFloat(key, float.MaxValue);
        if (timeTaken < prev)
        {
            IsNewTerrainBest = true;
            PlayerPrefs.SetFloat(key, timeTaken);
            PlayerPrefs.Save();
            TerrainBestTime = timeTaken;
        }
        else
        {
            IsNewTerrainBest = false;
            TerrainBestTime = prev;
        }
    }

    // Highest single-level score ever achieved (persisted) — the menu "Best" stat.
    public int BestScore { get; private set; }

    public int CalculateFinalScore()
    {
        float timeTaken = Time.time - levelStartTime;
        RecordTerrainBestTime(timeTaken);
        // Null-guard: past the final level GetLevelData returns null (fall back to 90s limit).
        var ld = LevelDataManager.Instance != null ? LevelDataManager.Instance.GetLevelData(GameManager.Instance.currentLevel) : null;
        float limit = ld != null ? ld.timeLimit : 90f;
        float timeBonus = Mathf.Max(0, timeBonusMultiplier * (limit - timeTaken));

        float perfectBonus = perfectParking ? perfectParkingBonus : 0f;
        // Streak bonus scales with the current streak (capped) — read-only, so calling this
        // method more than once per level is safe.
        float streakBonus = Mathf.Min(WinStreak, 20) * streakBonusPerLevel;

        int finalScore = Mathf.RoundToInt(baseScore + timeBonus + perfectBonus + streakBonus - (collisionCount * obstaclePenalty) + currentScore);

        // Track the lifetime best single-level score (menu "Best" previously derived from the
        // transient currentLevel, which terrain-select resets to 1 — it mostly showed L1's score).
        if (finalScore > BestScore)
        {
            BestScore = finalScore;
            PlayerPrefs.SetInt("BestScore", BestScore);
            PlayerPrefs.Save();
        }

        // Update achievements
        CheckAchievements(finalScore);

        return finalScore;
    }

    private void CheckAchievements(int score)
    {
        // Score-kind achievements use the just-completed level's final score.
        EvaluateAchievements(AchKind.Score, score);
    }

    // Unlocks any not-yet-earned achievement of the given kind whose threshold is met by `value`.
    private void EvaluateAchievements(AchKind kind, int value)
    {
        foreach (Achievement achievement in achievements)
        {
            if (achievement.kind != kind) continue;
            if (!achievement.isUnlocked && value >= achievement.requiredScore)
            {
                achievement.isUnlocked = true;
                PlayerPrefs.SetInt($"Achievement_{achievement.name}", 1);
                PlayerPrefs.Save();
                UIManager.Instance?.ShowAchievementUnlocked(achievement);
            }
        }
    }

    public int GetTotalScore()
    {
        if (GameManager.Instance == null || LevelDataManager.Instance == null) return 0;
        int totalScore = 0;
        for (int i = 1; i <= GameManager.Instance.currentLevel; i++)
            totalScore += LevelDataManager.Instance.GetLevelScore(i);
        return totalScore;
    }

    public int GetUnlockedAchievementCount()
    {
        int count = 0;
        foreach (Achievement achievement in achievements)
        {
            if (achievement.isUnlocked)
            {
                count++;
            }
        }
        return count;
    }

    public void ResetProgress()
    {
        // Reset all scores and achievements (keys are terrain-scoped; also clear the legacy
        // unscoped keys from older builds)
        foreach (var terr in new[] { "Road", "Water", "Air", "Space" })
            for (int i = 1; i <= LevelDataManager.Instance.totalLevels; i++)
            {
                PlayerPrefs.DeleteKey($"Level_{terr}_{i}_Score");
                PlayerPrefs.DeleteKey($"Level_{terr}_{i}_Time");
            }
        for (int i = 1; i <= LevelDataManager.Instance.totalLevels; i++)
        {
            PlayerPrefs.DeleteKey($"Level_{i}_Score");
            PlayerPrefs.DeleteKey($"Level_{i}_Time");
        }

        foreach (Achievement achievement in achievements)
        {
            PlayerPrefs.DeleteKey($"Achievement_{achievement.name}");
            achievement.isUnlocked = false;
        }

        // Previously left intact, so "reset" kept coins/streaks/skins/best-times and achievements
        // could instantly re-unlock from stale values. Clear both prefs AND in-memory state.
        PlayerPrefs.DeleteKey("Coins");
        PlayerPrefs.DeleteKey("BestStreak");
        PlayerPrefs.DeleteKey("LevelsCleared");
        PlayerPrefs.DeleteKey("BestScore");
        PlayerPrefs.DeleteKey("SelectedSkin");
        PlayerPrefs.DeleteKey("LastDailyClaim");
        for (int i = 0; i < Skins.Length; i++) PlayerPrefs.DeleteKey($"Skin_{i}");
        foreach (var terr in new[] { "Road", "Water", "Air", "Space" })
            PlayerPrefs.DeleteKey($"BestTime_{terr}");
        Coins = 0; BestStreak = 0; LevelsCleared = 0; BestScore = 0;
        WinStreak = 0; NewBestThisRun = false; NearMissCombo = 0; SelectedSkin = 0;

        PlayerPrefs.Save();
    }
} 