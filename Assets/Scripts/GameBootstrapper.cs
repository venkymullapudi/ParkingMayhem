using UnityEngine;

/// <summary>
/// Added to the Game scene's GameManager GameObject.
/// Spawns the first vehicle and parking zone procedurally so the game is
/// immediately playable without any external art assets.
/// </summary>
public class GameBootstrapper : MonoBehaviour
{
    [Header("Vehicle Colors")]
    public Color[] vehicleColors = {
        new Color(0.2f, 0.6f, 1f),   // Square  - blue
        new Color(1f, 0.4f, 0.1f),   // Triangle - orange
        new Color(0.2f, 0.9f, 0.3f), // Rectangle - green
        new Color(0.9f, 0.2f, 0.9f), // Pentagon - purple
        new Color(1f, 0.9f, 0.1f),   // Circle   - yellow
    };

    private void Start()
    {
        SpawnCurrentVehicle();
        SpawnParkingZone();

        if (ScoreManager.Instance != null)
            ScoreManager.Instance.StartLevel();

        if (UIManager.Instance != null)
            UIManager.Instance.ShowGameUI();
    }

    private void SpawnCurrentVehicle()
    {
        int level = GameManager.Instance != null ? GameManager.Instance.currentLevel : 1;
        int colorIndex = (level - 1) % vehicleColors.Length;

        var vehicleGo = new GameObject("Vehicle");
        vehicleGo.transform.position = new Vector3(0f, -3f, 0f);

        // Sprite
        var sr = vehicleGo.AddComponent<SpriteRenderer>();
        sr.sprite = SpriteGenerator.CreateSquare(64, vehicleColors[colorIndex]);
        sr.sortingOrder = 2;
        vehicleGo.transform.localScale = Vector3.one * 1.2f;

        // Physics
        var rb = vehicleGo.AddComponent<Rigidbody2D>();
        rb.gravityScale = 0f;
        rb.linearDamping = 1.5f;
        rb.angularDamping = 2f;

        // Collider
        vehicleGo.AddComponent<BoxCollider2D>();

        // Controller
        vehicleGo.AddComponent<VehicleController>();

        vehicleGo.tag = "Player";
    }

    private void SpawnParkingZone()
    {
        var zoneGo = new GameObject("ParkingZone");
        zoneGo.transform.position = new Vector3(2f, 2.5f, 0f);
        float angle = Random.Range(0f, 360f);
        zoneGo.transform.rotation = Quaternion.Euler(0, 0, angle);

        var sr = zoneGo.AddComponent<SpriteRenderer>();
        sr.sprite = SpriteGenerator.CreateParkingZone(128);
        sr.sortingOrder = -1;

        var col = zoneGo.AddComponent<BoxCollider2D>();
        col.isTrigger = true;

        zoneGo.AddComponent<ParkingZone>();
        zoneGo.tag = "ParkingSpace";
    }
}
