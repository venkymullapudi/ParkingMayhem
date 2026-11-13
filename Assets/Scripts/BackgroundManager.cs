using UnityEngine;

// Creates a themed full-screen background that follows the camera and fills the view.
public class BackgroundManager : MonoBehaviour
{
    public static BackgroundManager Instance { get; private set; }

    private SpriteRenderer bgRenderer;
    private GameObject bgGO;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    public void SetTerrain(GameManager.TerrainType terrain)
    {
        var cam = Camera.main;
        if (cam == null) return;

        // Solid clear so nothing bleeds at edges
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = Color.black;

        if (bgGO == null)
        {
            bgGO = new GameObject("_Background");
            bgRenderer = bgGO.AddComponent<SpriteRenderer>();
            bgRenderer.sortingOrder = -1000;
        }

        // Parent to camera so it always fills the view as the camera follows the vehicle
        bgGO.transform.SetParent(cam.transform, false);
        bgGO.transform.localPosition = new Vector3(0f, 0f, 20f);
        bgGO.transform.localRotation = Quaternion.identity;

        // Pick a background variant by level: cycle among the variants unlocked so far, with a
        // new (more exotic) one unlocking every 4 levels — so higher levels show nebulas, black
        // holes, the moon, deserts, sunsets, etc.
        int level = GameManager.Instance != null ? GameManager.Instance.currentLevel : 1;
        int count = SpriteGenerator.BackgroundVariantCount(terrain);
        int unlocked = Mathf.Clamp(level / 4 + 1, 1, count);
        int variant = (level - 1) % unlocked;
        bgRenderer.sprite = SpriteGenerator.CreateBackground(terrain, variant);

        // Scale to cover the orthographic view (+ margin)
        float viewH = cam.orthographicSize * 2f;
        float viewW = viewH * cam.aspect;
        var sp = bgRenderer.sprite;
        float spriteW = sp.bounds.size.x;
        float spriteH = sp.bounds.size.y;
        float scaleX = (viewW / spriteW) * 1.15f;
        float scaleY = (viewH / spriteH) * 1.15f;
        bgGO.transform.localScale = new Vector3(scaleX, scaleY, 1f);
    }
}
