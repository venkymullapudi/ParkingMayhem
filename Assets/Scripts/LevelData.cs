using UnityEngine;
using System.Collections.Generic;

[System.Serializable]
public class LevelData
{
    public int levelNumber;
    public GameManager.VehicleType vehicleType;
    public GameManager.TerrainType terrainType;
    public float parkingSpaceSize;
    public int obstacleCount;
    public float windStrength;
    public float waveHeight;
    public float timeLimit;
    public int targetScore;
}

public class LevelDataManager : MonoBehaviour
{
    public static LevelDataManager Instance { get; private set; }

    [Header("Level Settings")]
    public List<LevelData> levelDataList = new List<LevelData>();
    public int totalLevels = 2000;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            GenerateLevelData();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void GenerateLevelData()
    {
        levelDataList.Clear();

        for (int i = 1; i <= totalLevels; i++)
        {
            LevelData levelData = new LevelData
            {
                levelNumber = i,
                vehicleType = GetVehicleTypeForLevel(i),
                terrainType = GetTerrainTypeForLevel(i),
                parkingSpaceSize = CalculateParkingSpaceSize(i),
                obstacleCount = CalculateObstacleCount(i),
                windStrength = CalculateWindStrength(i),
                waveHeight = CalculateWaveHeight(i),
                timeLimit = CalculateTimeLimit(i),
                targetScore = CalculateTargetScore(i)
            };

            levelDataList.Add(levelData);
        }
    }

    private GameManager.VehicleType GetVehicleTypeForLevel(int level)
    {
        // Progressive vehicle types based on level
        int vehicleTypeCount = System.Enum.GetValues(typeof(GameManager.VehicleType)).Length;
        int vehicleIndex = Mathf.Min(level / 10, vehicleTypeCount - 1);
        return (GameManager.VehicleType)vehicleIndex;
    }

    private GameManager.TerrainType GetTerrainTypeForLevel(int level)
    {
        if (level <= 50)
            return GameManager.TerrainType.Road;
        else if (level <= 100)
            return GameManager.TerrainType.Water;
        else
            return GameManager.TerrainType.Air;
    }

    private float CalculateParkingSpaceSize(int level)
    {
        // Decrease size as levels progress
        return Mathf.Lerp(5f, 2f, Mathf.Clamp01(level / 100f));
    }

    private int CalculateObstacleCount(int level)
    {
        // Increase obstacles as levels progress
        return Mathf.RoundToInt(Mathf.Lerp(2, 10, Mathf.Clamp01(level / 100f)));
    }

    private float CalculateWindStrength(int level)
    {
        // Increase wind strength in air levels
        if (GetTerrainTypeForLevel(level) == GameManager.TerrainType.Air)
        {
            return Mathf.Lerp(0f, 10f, Mathf.Clamp01((level - 100) / 100f));
        }
        return 0f;
    }

    private float CalculateWaveHeight(int level)
    {
        // Increase wave height in water levels
        if (GetTerrainTypeForLevel(level) == GameManager.TerrainType.Water)
        {
            return Mathf.Lerp(0f, 3f, Mathf.Clamp01((level - 50) / 50f));
        }
        return 0f;
    }

    private float CalculateTimeLimit(int level)
    {
        // Decrease time limit as levels progress: 90s at L1 -> 58s at L100+. Floor raised
        // 50->58 so late elongated Air/Water zones (longest approach + wind) stay fair.
        return Mathf.Lerp(90f, 58f, Mathf.Clamp01(level / 100f));
    }

    private int CalculateTargetScore(int level)
    {
        // Increase target score as levels progress
        return Mathf.RoundToInt(Mathf.Lerp(1000, 5000, Mathf.Clamp01(level / 100f)));
    }

    public LevelData GetLevelData(int levelNumber)
    {
        if (levelNumber > 0 && levelNumber <= levelDataList.Count)
        {
            return levelDataList[levelNumber - 1];
        }
        return null;
    }

    // Keys are terrain-scoped: previously "Level_5_Score" collided across terrains (Water L5
    // clobbered Road L5) and every replay overwrote a better result unconditionally.
    private static string Terr() =>
        GameManager.Instance != null ? GameManager.Instance.currentTerrain.ToString() : "Road";

    public void SaveLevelProgress(int level, int score, float time)
    {
        string sKey = $"Level_{Terr()}_{level}_Score";
        if (score > PlayerPrefs.GetInt(sKey, 0))   // keep the best score only
        {
            PlayerPrefs.SetInt(sKey, score);
            PlayerPrefs.SetFloat($"Level_{Terr()}_{level}_Time", time);
        }
        PlayerPrefs.Save();
    }

    public int GetLevelScore(int level)
    {
        return PlayerPrefs.GetInt($"Level_{Terr()}_{level}_Score", 0);
    }

    public float GetLevelTime(int level)
    {
        return PlayerPrefs.GetFloat($"Level_{Terr()}_{level}_Time", 0f);
    }
} 