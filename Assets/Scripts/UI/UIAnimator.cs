using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using TMPro;

/// <summary>
/// Pure coroutine-based animation helpers — no external dependencies.
/// All easing is built-in. Call from any MonoBehaviour via StartCoroutine.
/// </summary>
public static class UIAnimator
{
    // ── Easing ────────────────────────────────────────────────────────────────

    public static float EaseOutBack(float t)
    {
        const float c1 = 1.70158f, c3 = c1 + 1f;
        return 1f + c3 * Mathf.Pow(t - 1f, 3f) + c1 * Mathf.Pow(t - 1f, 2f);
    }

    public static float EaseOutElastic(float t)
    {
        if (t <= 0f) return 0f;
        if (t >= 1f) return 1f;
        const float c4 = (2f * Mathf.PI) / 3f;
        return Mathf.Pow(2f, -10f * t) * Mathf.Sin((t * 10f - 0.75f) * c4) + 1f;
    }

    public static float EaseOutCubic(float t) => 1f - Mathf.Pow(1f - t, 3f);
    public static float EaseInCubic(float t) => t * t * t;
    public static float EaseInOutCubic(float t) =>
        t < 0.5f ? 4f * t * t * t : 1f - Mathf.Pow(-2f * t + 2f, 3f) / 2f;

    // ── Fade ─────────────────────────────────────────────────────────────────

    public static IEnumerator FadeIn(CanvasGroup cg, float duration, float delay = 0f)
    {
        if (delay > 0f) yield return new WaitForSecondsRealtime(delay);
        cg.gameObject.SetActive(true);
        float t = 0f;
        while (t < 1f)
        {
            t += Time.unscaledDeltaTime / duration;
            cg.alpha = EaseOutCubic(Mathf.Clamp01(t));
            yield return null;
        }
        cg.alpha = 1f;
    }

    public static IEnumerator FadeOut(CanvasGroup cg, float duration, bool deactivate = true)
    {
        float t = 0f;
        float startAlpha = cg.alpha;
        while (t < 1f)
        {
            t += Time.unscaledDeltaTime / duration;
            cg.alpha = Mathf.Lerp(startAlpha, 0f, EaseInCubic(Mathf.Clamp01(t)));
            yield return null;
        }
        cg.alpha = 0f;
        if (deactivate) cg.gameObject.SetActive(false);
    }

    // ── Scale ─────────────────────────────────────────────────────────────────

    public static IEnumerator PopIn(RectTransform rt, float duration, float delay = 0f)
    {
        if (delay > 0f) yield return new WaitForSecondsRealtime(delay);
        rt.gameObject.SetActive(true);
        float t = 0f;
        while (t < 1f)
        {
            t += Time.unscaledDeltaTime / duration;
            float s = EaseOutBack(Mathf.Clamp01(t));
            rt.localScale = Vector3.one * s;
            yield return null;
        }
        rt.localScale = Vector3.one;
    }

    public static IEnumerator PopOut(RectTransform rt, float duration)
    {
        float t = 0f;
        while (t < 1f)
        {
            t += Time.unscaledDeltaTime / duration;
            float s = Mathf.Lerp(1f, 0f, EaseInCubic(Mathf.Clamp01(t)));
            rt.localScale = Vector3.one * s;
            yield return null;
        }
        rt.localScale = Vector3.zero;
        rt.gameObject.SetActive(false);
    }

    public static IEnumerator Punch(RectTransform rt, float scale, float duration)
    {
        Vector3 original = rt.localScale;
        float half = duration * 0.5f;
        float t = 0f;
        while (t < 1f)
        {
            t += Time.unscaledDeltaTime / half;
            rt.localScale = Vector3.Lerp(original, original * scale, EaseOutCubic(Mathf.Clamp01(t)));
            yield return null;
        }
        t = 0f;
        while (t < 1f)
        {
            t += Time.unscaledDeltaTime / half;
            rt.localScale = Vector3.Lerp(original * scale, original, EaseOutElastic(Mathf.Clamp01(t)));
            yield return null;
        }
        rt.localScale = original;
    }

    // ── Slide ─────────────────────────────────────────────────────────────────

    public static IEnumerator SlideIn(RectTransform rt, Vector2 fromOffset, float duration, float delay = 0f)
    {
        if (delay > 0f) yield return new WaitForSecondsRealtime(delay);
        rt.gameObject.SetActive(true);
        Vector2 target = rt.anchoredPosition;
        rt.anchoredPosition = target + fromOffset;
        float t = 0f;
        while (t < 1f)
        {
            t += Time.unscaledDeltaTime / duration;
            rt.anchoredPosition = Vector2.Lerp(target + fromOffset, target, EaseOutCubic(Mathf.Clamp01(t)));
            yield return null;
        }
        rt.anchoredPosition = target;
    }

    public static IEnumerator SlideOut(RectTransform rt, Vector2 toOffset, float duration, bool deactivate = true)
    {
        Vector2 start = rt.anchoredPosition;
        float t = 0f;
        while (t < 1f)
        {
            t += Time.unscaledDeltaTime / duration;
            rt.anchoredPosition = Vector2.Lerp(start, start + toOffset, EaseInCubic(Mathf.Clamp01(t)));
            yield return null;
        }
        if (deactivate) rt.gameObject.SetActive(false);
    }

    // ── Number counter ────────────────────────────────────────────────────────

    public static IEnumerator CountUp(TextMeshProUGUI label, int from, int to, float duration, string prefix = "", string suffix = "")
    {
        float t = 0f;
        while (t < 1f)
        {
            t += Time.unscaledDeltaTime / duration;
            int val = Mathf.RoundToInt(Mathf.Lerp(from, to, EaseOutCubic(Mathf.Clamp01(t))));
            label.text = prefix + val.ToString("N0") + suffix;
            yield return null;
        }
        label.text = prefix + to.ToString("N0") + suffix;
    }

    // ── Color flash ───────────────────────────────────────────────────────────

    public static IEnumerator ColorFlash(Graphic graphic, Color flashColor, float duration)
    {
        Color original = graphic.color;
        float t = 0f;
        while (t < 1f)
        {
            t += Time.unscaledDeltaTime / (duration * 0.5f);
            graphic.color = Color.Lerp(original, flashColor, EaseOutCubic(Mathf.Clamp01(t)));
            yield return null;
        }
        t = 0f;
        while (t < 1f)
        {
            t += Time.unscaledDeltaTime / (duration * 0.5f);
            graphic.color = Color.Lerp(flashColor, original, EaseOutCubic(Mathf.Clamp01(t)));
            yield return null;
        }
        graphic.color = original;
    }

    // ── Shake ─────────────────────────────────────────────────────────────────

    public static IEnumerator Shake(RectTransform rt, float magnitude, float duration)
    {
        Vector3 original = rt.localPosition;
        float elapsed = 0f;
        while (elapsed < duration)
        {
            float strength = Mathf.Lerp(magnitude, 0f, elapsed / duration);
            rt.localPosition = original + (Vector3)Random.insideUnitCircle * strength;
            elapsed += Time.unscaledDeltaTime;
            yield return null;
        }
        rt.localPosition = original;
    }
}
