using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using System;

/// <summary>
/// Serializable class that defines a level template with specific parameters and constraints.
/// Used to create consistent and balanced level designs across different difficulty levels.
/// </summary>
[System.Serializable]
public class LevelTemplate
{
    /// <summary>
    /// Name of the template for identification and debugging
    /// </summary>
    public string templateName;

    /// <summary>
    /// Minimum level number this template can be used for
    /// </summary>
    public int minLevel;

    /// <summary>
    /// Maximum level number this template can be used for
    /// </summary>
    public int maxLevel;

    /// <summary>
    /// Multiplier for parking space size (1.0 = normal size)
    /// </summary>
    public float parkingSpaceSizeMultiplier = 1f;

    /// <summary>
    /// Multiplier for obstacle density (1.0 = normal density)
    /// </summary>
    public float obstacleDensityMultiplier = 1f;

    /// <summary>
    /// List of obstacle types allowed in this template
    /// </summary>
    public List<ObstacleType> allowedObstacleTypes = new List<ObstacleType>();

    /// <summary>
    /// Preferred terrain type for this template (-1 = use GameManager's current terrain)
    /// </summary>
    public int preferredTerrainOverride = -1;
}

/// <summary>
/// Serializable class that defines an obstacle type with specific properties and behaviors.
/// Used to create varied and interesting obstacles throughout the game.
/// </summary>
[System.Serializable]
public class ObstacleType
{
    /// <summary>
    /// Name of the obstacle type for identification
    /// </summary>
    public string name;

    /// <summary>
    /// Prefab to instantiate for this obstacle type
    /// </summary>
    public GameObject prefab;

    /// <summary>
    /// Multiplier for obstacle size (1.0 = normal size)
    /// </summary>
    public float sizeMultiplier = 1f;

    /// <summary>
    /// Weight for random selection (higher = more likely to be chosen)
    /// </summary>
    public float spawnWeight = 1f;

    /// <summary>
    /// Whether this obstacle can be rotated during placement
    /// </summary>
    public bool canRotate = true;
}

/// <summary>
/// Main class responsible for managing level generation, validation, and analytics.
/// Implements singleton pattern for global access and object pooling for performance.
/// </summary>
public class LevelManager : MonoBehaviour
{
    /// <summary>
    /// Singleton instance for global access
    /// </summary>
    public static LevelManager Instance { get; private set; }

    [Header("Level Generation")]
    [SerializeField] private GameObject parkingSpacePrefab;
    [SerializeField] private GameObject obstaclePrefab;
    [SerializeField] private float minParkingSpaceSize = 2f;
    [SerializeField] private float maxParkingSpaceSize = 5f;
    [SerializeField] private int minObstacles = 2;
    [SerializeField] private int maxObstacles = 10;

    [Header("Level Settings")]
    [SerializeField] private float levelWidth = 20f;
    [SerializeField] private float levelHeight = 20f;
    [SerializeField] private float borderWidth = 1f;
    [SerializeField] private float obstacleSpacing = 1f;
    [SerializeField] private float minObstacleSize = 0.5f;
    [SerializeField] private float maxObstacleSize = 2f;

    [Header("Difficulty Settings")]
    [SerializeField] private AnimationCurve parkingSpaceSizeCurve = AnimationCurve.Linear(0f, 1f, 1f, 0.5f);
    [SerializeField] private AnimationCurve obstacleCountCurve = AnimationCurve.Linear(0f, 0.2f, 1f, 1f);
    [SerializeField] private AnimationCurve obstacleSizeCurve = AnimationCurve.Linear(0f, 0.5f, 1f, 1f);
    [SerializeField] private AnimationCurve obstacleDensityCurve = AnimationCurve.Linear(0f, 0.5f, 1f, 1f);

    [Header("Level Templates")]
    [SerializeField] private List<LevelTemplate> levelTemplates = new List<LevelTemplate>();
    [SerializeField] private List<ObstacleType> obstacleTypes = new List<ObstacleType>();

    [Header("Analytics")]
    [SerializeField] private bool enableAnalytics = true;
    private Dictionary<int, LevelAnalytics> levelAnalytics = new Dictionary<int, LevelAnalytics>();

    [Header("Preview Settings")]
    [SerializeField] private float previewDuration = 2f;
    [SerializeField] private float previewRotationSpeed = 30f;
    private bool isPreviewing = false;

    private List<GameObject> currentLevelObjects = new List<GameObject>();
    private GameObject currentParkingSpace;
    private ObjectPool obstaclePool;
    private ObjectPool parkingSpacePool;
    private Bounds levelBounds;

    /// <summary>
    /// Event triggered when a level is generated
    /// </summary>
    public event Action<int> OnLevelGenerated;

    /// <summary>
    /// Event triggered when a level is cleared
    /// </summary>
    public event Action OnLevelCleared;

    /// <summary>
    /// Initializes the singleton instance and required components
    /// </summary>
    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            InitializePools();
            CalculateLevelBounds();
            LoadAnalytics();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    /// <summary>
    /// Loads analytics data from PlayerPrefs if enabled
    /// </summary>
    private void InitializePools()
    {
        if (parkingSpacePrefab != null)
            parkingSpacePool = new ObjectPool(parkingSpacePrefab, 2);
        if (obstaclePrefab != null)
            obstaclePool = new ObjectPool(obstaclePrefab, 20);
    }

    private void CalculateLevelBounds()
    {
        levelBounds = new Bounds(Vector3.zero, new Vector3(levelWidth, levelHeight, 1f));
    }

    private void ClearCurrentLevel()
    {
        foreach (GameObject obj in currentLevelObjects)
        {
            if (obj == null) continue;
            if (obj.CompareTag("Obstacle") && obstaclePool != null)
                obstaclePool.ReturnToPool(obj);
            else if (obj.CompareTag("ParkingSpace") && parkingSpacePool != null)
                parkingSpacePool.ReturnToPool(obj);
            else
                Destroy(obj);
        }
        currentLevelObjects.Clear();
        currentParkingSpace = null;
        OnLevelCleared?.Invoke();
    }

    private ObstacleType SelectObstacleType(List<ObstacleType> types)
    {
        if (types == null || types.Count == 0) return null;
        float totalWeight = types.Sum(t => t.spawnWeight);
        float random = UnityEngine.Random.Range(0f, totalWeight);
        foreach (var type in types)
        {
            if (random < type.spawnWeight) return type;
            random -= type.spawnWeight;
        }
        return types[0];
    }

    private void LoadAnalytics()
    {
        // Dictionary<int,T> is not supported by JsonUtility; analytics start fresh each session
    }

    private void SaveAnalytics()
    {
        // No-op: requires a custom serializer for Dictionary persistence
    }

    /// <summary>
    /// Generates a new level with the specified level number
    /// </summary>
    /// <param name="levelNumber">The level number to generate</param>
    public void GenerateLevel(int levelNumber)
    {
        ClearCurrentLevel();
        LevelTemplate template = GetTemplateForLevel(levelNumber);
        GenerateTerrain(template);
        GenerateParkingSpace(levelNumber, template);
        GenerateObstacles(levelNumber, template);
        OnLevelGenerated?.Invoke(levelNumber);
        
        if (enableAnalytics)
        {
            RecordLevelGeneration(levelNumber, template);
        }
    }

    /// <summary>
    /// Gets the appropriate template for the specified level number
    /// </summary>
    /// <param name="levelNumber">The level number to get template for</param>
    /// <returns>The selected level template</returns>
    private LevelTemplate GetTemplateForLevel(int levelNumber)
    {
        var validTemplates = levelTemplates.Where(t => levelNumber >= t.minLevel && levelNumber <= t.maxLevel).ToList();
        if (validTemplates.Count == 0) return levelTemplates[0];
        
        // Weighted random selection based on level range
        float totalWeight = validTemplates.Sum(t => t.maxLevel - t.minLevel);
        float random = UnityEngine.Random.Range(0f, totalWeight);
        
        foreach (var template in validTemplates)
        {
            if (random < (template.maxLevel - template.minLevel))
                return template;
            random -= (template.maxLevel - template.minLevel);
        }
        
        return validTemplates[0];
    }

    /// <summary>
    /// Generates the terrain for the current level
    /// </summary>
    /// <param name="template">The level template to use</param>
    private void GenerateTerrain(LevelTemplate template)
    {
        GameObject terrain = new GameObject("Terrain");
        SpriteRenderer terrainRenderer = terrain.AddComponent<SpriteRenderer>();
        
        GameManager.TerrainType terrainType = (template.preferredTerrainOverride >= 0)
            ? (GameManager.TerrainType)template.preferredTerrainOverride
            : GameManager.Instance.currentTerrain;
        
        Color terrainColor = GetTerrainColor(terrainType);
        terrainRenderer.color = terrainColor;
        terrainRenderer.sortingOrder = -1;

        terrain.transform.localScale = new Vector3(levelWidth, levelHeight, 1f);
        currentLevelObjects.Add(terrain);
    }

    /// <summary>
    /// Gets the color for the specified terrain type
    /// </summary>
    /// <param name="terrainType">The terrain type to get color for</param>
    /// <returns>The color for the terrain</returns>
    private Color GetTerrainColor(GameManager.TerrainType terrainType)
    {
        return terrainType switch
        {
            GameManager.TerrainType.Road => new Color(0.3f, 0.3f, 0.3f),
            GameManager.TerrainType.Water => new Color(0f, 0.5f, 1f),
            GameManager.TerrainType.Air => new Color(0.7f, 0.9f, 1f),
            _ => Color.white
        };
    }

    /// <summary>
    /// Generates the parking space for the current level
    /// </summary>
    /// <param name="levelNumber">The current level number</param>
    /// <param name="template">The level template to use</param>
    private void GenerateParkingSpace(int levelNumber, LevelTemplate template)
    {
        float size = CalculateParkingSpaceSize(levelNumber);
        currentParkingSpace = parkingSpacePool.GetFromPool();
        currentParkingSpace.transform.localScale = Vector3.one * size;
        
        Vector3 position = GetRandomPosition(size);
        currentParkingSpace.transform.position = position;
        currentParkingSpace.transform.rotation = Quaternion.Euler(0, 0, UnityEngine.Random.Range(0f, 360f));

        currentLevelObjects.Add(currentParkingSpace);
    }

    /// <summary>
    /// Generates obstacles for the current level
    /// </summary>
    /// <param name="levelNumber">The current level number</param>
    /// <param name="template">The level template to use</param>
    private void GenerateObstacles(int levelNumber, LevelTemplate template)
    {
        int obstacleCount = CalculateObstacleCount(levelNumber);
        float parkingSpaceSize = currentParkingSpace.transform.localScale.x;
        float obstacleSizeMultiplier = CalculateObstacleSizeMultiplier(levelNumber);
        float obstacleDensity = CalculateObstacleDensity(levelNumber) * template.obstacleDensityMultiplier;

        var allowedTypes = template.allowedObstacleTypes.Count > 0 ? 
            template.allowedObstacleTypes : obstacleTypes;

        for (int i = 0; i < obstacleCount; i++)
        {
            ObstacleType obstacleType = SelectObstacleType(allowedTypes);
            GameObject obstacle = obstaclePool.GetFromPool();
            
            float size = UnityEngine.Random.Range(minObstacleSize, maxObstacleSize) * 
                        obstacleSizeMultiplier * obstacleType.sizeMultiplier;
            
            obstacle.transform.localScale = Vector3.one * size;

            Vector3 position = GetValidObstaclePosition(size, parkingSpaceSize, obstacleDensity);
            obstacle.transform.position = position;
            
            if (obstacleType.canRotate)
            {
                obstacle.transform.rotation = Quaternion.Euler(0, 0, UnityEngine.Random.Range(0f, 360f));
            }

            currentLevelObjects.Add(obstacle);
        }
    }

    /// <summary>
    /// Gets a random position within the level bounds
    /// </summary>
    /// <param name="size">The size of the object to place</param>
    /// <returns>A random position within bounds</returns>
    private Vector3 GetRandomPosition(float size)
    {
        float maxX = (levelWidth - size) / 2f;
        float maxY = (levelHeight - size) / 2f;
        return new Vector3(
            UnityEngine.Random.Range(-maxX, maxX),
            UnityEngine.Random.Range(-maxY, maxY),
            0f
        );
    }

    /// <summary>
    /// Gets a valid position for an obstacle that doesn't overlap with other objects
    /// </summary>
    /// <param name="obstacleSize">The size of the obstacle</param>
    /// <param name="parkingSpaceSize">The size of the parking space</param>
    /// <param name="density">The current obstacle density</param>
    /// <returns>A valid position for the obstacle</returns>
    private Vector3 GetValidObstaclePosition(float obstacleSize, float parkingSpaceSize, float density)
    {
        Vector3 position;
        int attempts = 0;
        const int maxAttempts = 100;
        float spacing = obstacleSpacing * density;

        do
        {
            position = GetRandomPosition(obstacleSize);
            attempts++;
        } while (attempts < maxAttempts && 
                (IsOverlappingParkingSpace(position, obstacleSize, parkingSpaceSize) ||
                 IsOverlappingOtherObstacles(position, obstacleSize, spacing)));

        return position;
    }

    /// <summary>
    /// Checks if a position would overlap with the parking space
    /// </summary>
    /// <param name="position">The position to check</param>
    /// <param name="size">The size of the object</param>
    /// <param name="parkingSpaceSize">The size of the parking space</param>
    /// <returns>True if the position overlaps with the parking space</returns>
    private bool IsOverlappingParkingSpace(Vector3 position, float size, float parkingSpaceSize)
    {
        float minDistance = (size + parkingSpaceSize) / 2f + obstacleSpacing;
        return Vector3.Distance(position, currentParkingSpace.transform.position) < minDistance;
    }

    /// <summary>
    /// Checks if a position would overlap with other obstacles
    /// </summary>
    /// <param name="position">The position to check</param>
    /// <param name="size">The size of the object</param>
    /// <param name="spacing">The required spacing between objects</param>
    /// <returns>True if the position overlaps with other obstacles</returns>
    private bool IsOverlappingOtherObstacles(Vector3 position, float size, float spacing)
    {
        return currentLevelObjects
            .Where(obj => obj.CompareTag("Obstacle"))
            .Any(obj => Vector3.Distance(position, obj.transform.position) < (size + obj.transform.localScale.x) / 2f + spacing);
    }

    /// <summary>
    /// Calculates the parking space size based on level number
    /// </summary>
    /// <param name="level">The current level number</param>
    /// <returns>The calculated parking space size</returns>
    private float CalculateParkingSpaceSize(int level)
    {
        float normalizedLevel = Mathf.Clamp01(level / 100f);
        return Mathf.Lerp(maxParkingSpaceSize, minParkingSpaceSize, parkingSpaceSizeCurve.Evaluate(normalizedLevel));
    }

    /// <summary>
    /// Calculates the number of obstacles based on level number
    /// </summary>
    /// <param name="level">The current level number</param>
    /// <returns>The calculated number of obstacles</returns>
    private int CalculateObstacleCount(int level)
    {
        float normalizedLevel = Mathf.Clamp01(level / 100f);
        return Mathf.RoundToInt(Mathf.Lerp(minObstacles, maxObstacles, obstacleCountCurve.Evaluate(normalizedLevel)));
    }

    /// <summary>
    /// Calculates the obstacle size multiplier based on level number
    /// </summary>
    /// <param name="level">The current level number</param>
    /// <returns>The calculated obstacle size multiplier</returns>
    private float CalculateObstacleSizeMultiplier(int level)
    {
        float normalizedLevel = Mathf.Clamp01(level / 100f);
        return obstacleSizeCurve.Evaluate(normalizedLevel);
    }

    /// <summary>
    /// Calculates the obstacle density based on level number
    /// </summary>
    /// <param name="level">The current level number</param>
    /// <returns>The calculated obstacle density</returns>
    private float CalculateObstacleDensity(int level)
    {
        float normalizedLevel = Mathf.Clamp01(level / 100f);
        return obstacleDensityCurve.Evaluate(normalizedLevel);
    }

    /// <summary>
    /// Checks if a vehicle is successfully parked
    /// </summary>
    /// <param name="vehicle">The vehicle to check</param>
    /// <returns>True if the vehicle is successfully parked</returns>
    public bool CheckParkingSuccess(VehicleController vehicle)
    {
        if (currentParkingSpace == null || vehicle == null) return false;

        float distance = Vector3.Distance(vehicle.transform.position, currentParkingSpace.transform.position);
        float angleDifference = Quaternion.Angle(vehicle.transform.rotation, currentParkingSpace.transform.rotation);
        
        float sizeTolerance = currentParkingSpace.transform.localScale.x * 0.1f;
        float angleTolerance = 10f;

        return distance < sizeTolerance && angleDifference < angleTolerance;
    }

    /// <summary>
    /// Checks if a position is within the level bounds
    /// </summary>
    /// <param name="position">The position to check</param>
    /// <param name="size">The size of the object</param>
    /// <returns>True if the position is within bounds</returns>
    public bool IsPositionInBounds(Vector3 position, float size)
    {
        return levelBounds.Contains(position) && 
               Mathf.Abs(position.x) <= (levelWidth - size) / 2f &&
               Mathf.Abs(position.y) <= (levelHeight - size) / 2f;
    }

    /// <summary>
    /// Records level generation analytics
    /// </summary>
    /// <param name="levelNumber">The level number</param>
    /// <param name="template">The template used</param>
    private void RecordLevelGeneration(int levelNumber, LevelTemplate template)
    {
        if (!levelAnalytics.ContainsKey(levelNumber))
        {
            levelAnalytics[levelNumber] = new LevelAnalytics();
        }
        
        levelAnalytics[levelNumber].generationCount++;
        levelAnalytics[levelNumber].lastTemplateUsed = template.templateName;
        levelAnalytics[levelNumber].lastGenerationTime = DateTime.Now;
        
        SaveAnalytics();
    }

    /// <summary>
    /// Class for storing level analytics data
    /// </summary>
    [System.Serializable]
    private class LevelAnalytics
    {
        public int generationCount;
        public string lastTemplateUsed;
        public DateTime lastGenerationTime;
        public float averageCompletionTime;
        public int successCount;
        public int failureCount;
    }

    /// <summary>
    /// Class for managing object pooling
    /// </summary>
    private class ObjectPool
    {
        private GameObject prefab;
        private Queue<GameObject> pool = new Queue<GameObject>();
        private int initialSize;
        private Transform poolParent;

        public ObjectPool(GameObject prefab, int initialSize)
        {
            this.prefab = prefab;
            this.initialSize = initialSize;
            CreatePoolParent();
            InitializePool();
        }

        private void CreatePoolParent()
        {
            poolParent = new GameObject($"{prefab.name}Pool").transform;
            poolParent.SetParent(LevelManager.Instance.transform);
        }

        private void InitializePool()
        {
            for (int i = 0; i < initialSize; i++)
            {
                CreateNewObject();
            }
        }

        private void CreateNewObject()
        {
            GameObject obj = Instantiate(prefab, poolParent);
            obj.SetActive(false);
            pool.Enqueue(obj);
        }

        public GameObject GetFromPool()
        {
            if (pool.Count == 0)
            {
                CreateNewObject();
            }

            GameObject obj = pool.Dequeue();
            obj.SetActive(true);
            return obj;
        }

        public void ReturnToPool(GameObject obj)
        {
            obj.SetActive(false);
            obj.transform.SetParent(poolParent);
            pool.Enqueue(obj);
        }
    }
} 