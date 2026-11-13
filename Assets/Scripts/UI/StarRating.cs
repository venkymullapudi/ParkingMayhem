using UnityEngine;
using UnityEngine.UI;
using System.Collections;

/// <summary>
/// Animates 1-3 stars sequentially with a pop-in + glow effect.
/// Attach to the level-complete panel; call Show(starCount).
/// </summary>
public class StarRating : MonoBehaviour
{
    [Header("Star Images (assign 3)")]
    public Image[] stars = new Image[3];

    [Header("Colors")]
    public Color emptyColor = new Color(0.3f, 0.3f, 0.3f, 0.5f);
    public Color filledColor = new Color(1f, 0.85f, 0.1f);
    public Color glowColor   = new Color(1f, 1f, 0.4f);

    private void Awake()
    {
        foreach (var s in stars)
            if (s != null) s.color = emptyColor;
    }

    public void Show(int count)
    {
        count = Mathf.Clamp(count, 0, 3);
        StopAllCoroutines();
        StartCoroutine(AnimateStars(count));
    }

    public static int ScoreToStars(int score, int target)
    {
        float ratio = (float)score / Mathf.Max(1, target);
        if (ratio >= 1f)   return 3;
        if (ratio >= 0.6f) return 2;
        if (ratio >= 0.3f) return 1;
        return 0;
    }

    private IEnumerator AnimateStars(int count)
    {
        // Reset all
        foreach (var s in stars)
        {
            if (s == null) continue;
            s.color = emptyColor;
            s.transform.localScale = Vector3.one;
        }

        yield return new WaitForSecondsRealtime(0.3f);

        for (int i = 0; i < stars.Length; i++)
        {
            if (stars[i] == null) continue;
            bool filled = i < count;
            if (filled)
            {
                yield return StartCoroutine(PopStar(stars[i]));
                yield return new WaitForSecondsRealtime(0.18f);
            }
            else
            {
                stars[i].color = emptyColor;
            }
        }
    }

    private IEnumerator PopStar(Image star)
    {
        // Scale up with overshoot
        float t = 0f;
        float duration = 0.35f;
        while (t < 1f)
        {
            t += Time.unscaledDeltaTime / duration;
            float s = UIAnimator.EaseOutBack(Mathf.Clamp01(t));
            star.transform.localScale = Vector3.one * s;
            star.color = Color.Lerp(emptyColor, filledColor, UIAnimator.EaseOutCubic(Mathf.Clamp01(t)));
            yield return null;
        }
        star.transform.localScale = Vector3.one;
        star.color = filledColor;

        // Glow flash
        yield return StartCoroutine(UIAnimator.ColorFlash(star, glowColor, 0.25f));
    }
}
