using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class TestFramework : MonoBehaviour
{
    private static TestFramework instance;
    private List<TestResult> testResults = new List<TestResult>();

    public class TestResult
    {
        public string testName;
        public bool passed;
        public string message;
    }

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public static void Assert(bool condition, string message)
    {
        if (!condition)
        {
            Debug.LogError($"Test Assertion Failed: {message}");
            throw new System.Exception($"Test Assertion Failed: {message}");
        }
    }

    public static void RunTests()
    {
        if (instance == null)
        {
            Debug.LogError("TestFramework not initialized. Add it to the scene first.");
            return;
        }
        instance.StartCoroutine(instance.ExecuteAllTests());
    }

    private IEnumerator ExecuteAllTests()
    {
        yield return StartCoroutine(TestVehicleController());
        yield return StartCoroutine(TestScoreManager());

        Debug.Log("All tests completed!");
        foreach (var result in testResults)
            Debug.Log($"Test: {result.testName} - {(result.passed ? "PASSED" : "FAILED")} - {result.message}");
    }

    private IEnumerator TestVehicleController()
    {
        var testName = "VehicleController Tests";
        try
        {
            var go = new GameObject("TestVehicle");
            go.AddComponent<Rigidbody2D>();
            var vehicle = go.AddComponent<VehicleController>();

            var initialPosition = vehicle.transform.position;
            vehicle.transform.Translate(Vector3.right);
            Assert(vehicle.transform.position != initialPosition, "Vehicle should move when translated");

            vehicle.SetParked(true);
            vehicle.ResetVehicle();

            testResults.Add(new TestResult { testName = testName, passed = true, message = "All tests passed" });
            Object.Destroy(go);
        }
        catch (System.Exception e)
        {
            testResults.Add(new TestResult { testName = testName, passed = false, message = e.Message });
        }
        yield return null;
    }

    private IEnumerator TestScoreManager()
    {
        var testName = "ScoreManager Tests";
        try
        {
            var go = new GameObject("TestScoreManager");
            var scoreManager = go.AddComponent<ScoreManager>();

            scoreManager.StartLevel();
            scoreManager.AddCollision();
            int score = scoreManager.CalculateFinalScore();
            Assert(score >= 0, "Score should be non-negative after collision");

            testResults.Add(new TestResult { testName = testName, passed = true, message = "All tests passed" });
            Object.Destroy(go);
        }
        catch (System.Exception e)
        {
            testResults.Add(new TestResult { testName = testName, passed = false, message = e.Message });
        }
        yield return null;
    }
}
