using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// Spawns UI-space confetti/star bursts entirely in C# — no Particle System component needed.
/// Call ParticleCanvas.Instance.Burst() after a successful park.
/// </summary>
public class ParticleCanvas : MonoBehaviour
{
    public static ParticleCanvas Instance { get; private set; }

    [Header("Particle Settings")]
    public int particleCount = 40;
    public float burstRadius = 180f;
    public float lifetime = 1.1f;
    public float gravity = 900f;
    public Color[] colors = {
        new Color(1f, 0.9f, 0.1f),
        new Color(0.2f, 0.8f, 1f),
        new Color(1f, 0.3f, 0.5f),
        new Color(0.3f, 1f, 0.5f),
        new Color(1f, 0.6f, 0.1f),
    };

    private RectTransform canvasRt;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else { Destroy(gameObject); return; }
        canvasRt = GetComponent<RectTransform>();
    }

    public void Burst(Vector2 screenCenter)
    {
        StartCoroutine(DoBurst(screenCenter));
    }

    public void BurstCenter()
    {
        Burst(new Vector2(Screen.width * 0.5f, Screen.height * 0.6f));
    }

    private IEnumerator DoBurst(Vector2 screenCenter)
    {
        var particles = new List<(RectTransform rt, Image img, Vector2 vel, float rot, float rotSpeed)>();

        for (int i = 0; i < particleCount; i++)
        {
            var go = new GameObject("Particle");
            go.transform.SetParent(transform, false);
            var img = go.AddComponent<Image>();
            img.raycastTarget = false;
            img.color = colors[Random.Range(0, colors.Length)];

            var rt = go.GetComponent<RectTransform>();
            bool isSquare = Random.value > 0.5f;
            rt.sizeDelta = isSquare
                ? new Vector2(Random.Range(8f, 16f), Random.Range(8f, 16f))
                : new Vector2(Random.Range(6f, 10f), Random.Range(14f, 22f));

            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                canvasRt, screenCenter, null, out Vector2 localPos);
            rt.anchoredPosition = localPos;

            float angle = Random.Range(0f, 360f) * Mathf.Deg2Rad;
            float speed = Random.Range(250f, burstRadius * 3f);
            Vector2 vel = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * speed;
            float rotSpeed = Random.Range(-480f, 480f);

            particles.Add((rt, img, vel, 0f, rotSpeed));
        }

        float elapsed = 0f;
        while (elapsed < lifetime)
        {
            float dt = Time.unscaledDeltaTime;
            elapsed += dt;
            float lifeRatio = elapsed / lifetime;

            for (int i = 0; i < particles.Count; i++)
            {
                var (rt, img, vel, rot, rotSpeed) = particles[i];
                vel += Vector2.down * gravity * dt;
                rt.anchoredPosition += vel * dt;
                rot += rotSpeed * dt;
                rt.localRotation = Quaternion.Euler(0, 0, rot);
                img.color = new Color(img.color.r, img.color.g, img.color.b,
                                      Mathf.Lerp(1f, 0f, Mathf.Pow(lifeRatio, 2f)));
                particles[i] = (rt, img, vel, rot, rotSpeed);
            }
            yield return null;
        }

        foreach (var (rt, _, _, _, _) in particles)
            Destroy(rt.gameObject);
    }

    /// Floating score number that drifts up and fades
    public void FloatingScore(int score, Vector2 screenPos, Color color)
    {
        StartCoroutine(DoFloatingScore(score, screenPos, color));
    }

    private IEnumerator DoFloatingScore(int score, Vector2 screenPos, Color color)
    {
        var go = new GameObject("FloatingScore");
        go.transform.SetParent(transform, false);
        var tmp = go.AddComponent<TMPro.TextMeshProUGUI>();
        tmp.text = score > 0 ? $"+{score}" : score.ToString();
        tmp.fontSize = 52;
        tmp.fontStyle = TMPro.FontStyles.Bold;
        tmp.alignment = TMPro.TextAlignmentOptions.Center;
        tmp.raycastTarget = false;
        tmp.color = color;

        var rt = go.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(200, 80);
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            canvasRt, screenPos, null, out Vector2 localPos);
        rt.anchoredPosition = localPos;

        float duration = 1.4f;
        float elapsed = 0f;
        Vector2 startPos = localPos;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = elapsed / duration;
            rt.anchoredPosition = startPos + Vector2.up * (120f * UIAnimator.EaseOutCubic(t));
            tmp.color = new Color(color.r, color.g, color.b, Mathf.Lerp(1f, 0f, Mathf.Pow(t, 2f)));
            yield return null;
        }
        Destroy(go);
    }
}
