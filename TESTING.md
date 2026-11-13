# Testing Guide for Parking Rush

## Unit Testing in Unity

### 1. Setting Up Test Framework
```csharp
// Example test setup in Unity
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

public class VehicleTests
{
    private GameObject testVehicle;
    private VehicleController vehicleController;

    [SetUp]
    public void Setup()
    {
        // Create test vehicle
        testVehicle = new GameObject("TestVehicle");
        vehicleController = testVehicle.AddComponent<VehicleController>();
        
        // Setup test environment
        GameObject testEnvironment = new GameObject("TestEnvironment");
        // Add necessary components for testing
    }

    [TearDown]
    public void Teardown()
    {
        // Clean up test objects
        Object.Destroy(testVehicle);
    }
}
```

### 2. Running Tests in Unity
1. Open Unity Test Runner:
   - Window > General > Test Runner
   - Select "PlayMode" or "EditMode" tests
   - Click "Run All" or select specific tests

2. Command Line Testing:
```bash
# Run all tests
/Applications/Unity/Hub/Editor/2022.3.0f1/Unity.app/Contents/MacOS/Unity -runTests -testPlatform PlayMode -projectPath /path/to/project

# Run specific test category
/Applications/Unity/Hub/Editor/2022.3.0f1/Unity.app/Contents/MacOS/Unity -runTests -testPlatform PlayMode -testCategory VehicleTests -projectPath /path/to/project
```

### 3. Test Categories

#### Vehicle Tests
```csharp
[Test]
public void Vehicle_MoveForward_ChangesPosition()
{
    // Arrange
    Vector3 startPosition = testVehicle.transform.position;
    
    // Act
    vehicleController.Move(1f); // Move forward
    
    // Assert
    Assert.Greater(testVehicle.transform.position.z, startPosition.z);
}

[Test]
public void Vehicle_Rotate_ChangesRotation()
{
    // Arrange
    Quaternion startRotation = testVehicle.transform.rotation;
    
    // Act
    vehicleController.Rotate(90f); // Rotate 90 degrees
    
    // Assert
    Assert.AreNotEqual(startRotation, testVehicle.transform.rotation);
}
```

#### Level Tests
```csharp
[Test]
public void Level_Generate_ValidParkingSpace()
{
    // Arrange
    LevelManager levelManager = new GameObject("LevelManager").AddComponent<LevelManager>();
    
    // Act
    levelManager.GenerateLevel(1);
    
    // Assert
    Assert.IsNotNull(levelManager.CurrentParkingSpace);
    Assert.IsTrue(levelManager.ValidateLevel(1));
}
```

#### Physics Tests
```csharp
[UnityTest]
public IEnumerator Vehicle_Collision_TriggersEvent()
{
    // Arrange
    bool collisionDetected = false;
    vehicleController.OnCollision += () => collisionDetected = true;
    
    // Act
    GameObject obstacle = new GameObject("Obstacle");
    obstacle.transform.position = testVehicle.transform.position;
    
    // Wait for physics update
    yield return new WaitForFixedUpdate();
    
    // Assert
    Assert.IsTrue(collisionDetected);
}
```

## Performance Testing

### 1. Profiling Setup
1. Open Unity Profiler:
   - Window > Analysis > Profiler
   - Enable "Deep Profile" for detailed analysis
   - Record performance data during gameplay

### 2. Performance Metrics
```csharp
public class PerformanceMonitor : MonoBehaviour
{
    private float frameRate;
    private float memoryUsage;
    
    void Update()
    {
        // Monitor frame rate
        frameRate = 1.0f / Time.deltaTime;
        
        // Monitor memory usage
        memoryUsage = System.GC.GetTotalMemory(false) / 1024f / 1024f;
        
        // Log performance data
        Debug.Log($"FPS: {frameRate}, Memory: {memoryUsage}MB");
    }
}
```

### 3. Load Testing
```csharp
[Test]
public void Level_Generate_1000Levels_Performance()
{
    // Arrange
    LevelManager levelManager = new GameObject("LevelManager").AddComponent<LevelManager>();
    System.Diagnostics.Stopwatch stopwatch = new System.Diagnostics.Stopwatch();
    
    // Act
    stopwatch.Start();
    for (int i = 1; i <= 1000; i++)
    {
        levelManager.GenerateLevel(i);
    }
    stopwatch.Stop();
    
    // Assert
    Assert.Less(stopwatch.ElapsedMilliseconds, 5000); // Should complete in under 5 seconds
}
```

## Integration Testing

### 1. Game Flow Tests
```csharp
[UnityTest]
public IEnumerator Game_CompleteLevel_UnlocksNext()
{
    // Arrange
    GameManager gameManager = new GameObject("GameManager").AddComponent<GameManager>();
    int currentLevel = 1;
    
    // Act
    gameManager.StartLevel(currentLevel);
    yield return new WaitForSeconds(1f);
    gameManager.CompleteLevel();
    
    // Assert
    Assert.IsTrue(gameManager.IsLevelUnlocked(currentLevel + 1));
}
```

### 2. Save/Load Tests
```csharp
[Test]
public void Game_SaveProgress_LoadsCorrectly()
{
    // Arrange
    GameManager gameManager = new GameObject("GameManager").AddComponent<GameManager>();
    gameManager.CompleteLevel(1);
    gameManager.CompleteLevel(2);
    
    // Act
    gameManager.SaveProgress();
    gameManager.LoadProgress();
    
    // Assert
    Assert.IsTrue(gameManager.IsLevelCompleted(1));
    Assert.IsTrue(gameManager.IsLevelCompleted(2));
}
```

## Continuous Integration

### 1. GitHub Actions Setup
```yaml
name: Unity Tests

on:
  push:
    branches: [ main ]
  pull_request:
    branches: [ main ]

jobs:
  test:
    runs-on: ubuntu-latest
    steps:
    - uses: actions/checkout@v2
    - uses: game-ci/unity-test-runner@v2
      with:
        unityVersion: 2022.3.0f1
        testMode: PlayMode
```

### 2. Automated Build Testing
```yaml
name: Unity Build

on:
  push:
    branches: [ main ]
  pull_request:
    branches: [ main ]

jobs:
  build:
    runs-on: ubuntu-latest
    steps:
    - uses: actions/checkout@v2
    - uses: game-ci/unity-builder@v2
      with:
        unityVersion: 2022.3.0f1
        targetPlatform: Android
```

## Mobile Testing

### 1. Device Testing
1. Connect Android/iOS device
2. Enable Developer Options
3. Build and Run directly to device
4. Monitor performance and input

### 2. Emulator Testing
1. Set up Android Emulator
2. Configure iOS Simulator
3. Test different screen sizes
4. Test different OS versions

## Test Coverage Reporting

### 1. Setup Coverage
```csharp
// Add to Assembly Definition
[assembly: InternalsVisibleTo("UnityEngine.TestRunner")]
[assembly: InternalsVisibleTo("UnityEditor.TestRunner")]
```

### 2. Generate Report
```bash
/Applications/Unity/Hub/Editor/2022.3.0f1/Unity.app/Contents/MacOS/Unity -runTests -testPlatform PlayMode -coverage -projectPath /path/to/project
```

## Best Practices

1. **Test Organization**
   - Group tests by feature
   - Use descriptive test names
   - Follow AAA pattern (Arrange, Act, Assert)

2. **Test Data**
   - Use test fixtures
   - Create mock objects
   - Clean up after tests

3. **Performance**
   - Test with realistic data
   - Monitor memory usage
   - Check frame rate

4. **Mobile Specific**
   - Test touch input
   - Check screen resolutions
   - Verify performance on target devices 