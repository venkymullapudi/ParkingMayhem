using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections;

/// <summary>
/// Add to any Button to get press-scale, hover-glow, and ripple feedback
/// with no external dependencies.
/// </summary>
[RequireComponent(typeof(Button))]
public class ButtonAnimator : MonoBehaviour,
    IPointerDownHandler, IPointerUpHandler,
    IPointerEnterHandler, IPointerExitHandler
{
    [Header("Press")]
    public float pressScale = 0.92f;
    public float pressDuration = 0.08f;

    [Header("Hover")]
    public float hoverScale = 1.06f;
    public float hoverDuration = 0.12f;

    [Header("Ripple")]
    public bool showRipple = true;
    public Color rippleColor = new Color(1f, 1f, 1f, 0.35f);

    private RectTransform rt;
    private Vector3 originalScale;
    private Coroutine scaleRoutine;
    private bool isPressed;

    private void Awake()
    {
        rt = GetComponent<RectTransform>();
        if (rt != null) originalScale = rt.localScale;
        else originalScale = Vector3.one;
    }

    public void OnPointerDown(PointerEventData e)
    {
        isPressed = true;
        AnimateTo(originalScale * pressScale, pressDuration);
        if (showRipple && rt != null) SpawnRipple(e.position);
        AudioManager.Instance?.PlayButtonClickSound();
    }

    public void OnPointerUp(PointerEventData e)
    {
        isPressed = false;
        AnimateTo(originalScale * hoverScale, pressDuration);
    }

    public void OnPointerEnter(PointerEventData e)
    {
        if (!isPressed) AnimateTo(originalScale * hoverScale, hoverDuration);
    }

    public void OnPointerExit(PointerEventData e)
    {
        isPressed = false;
        AnimateTo(originalScale, hoverDuration);
    }

    private void AnimateTo(Vector3 target, float duration)
    {
        if (scaleRoutine != null) StopCoroutine(scaleRoutine);
        scaleRoutine = StartCoroutine(ScaleTo(target, duration));
    }

    private IEnumerator ScaleTo(Vector3 target, float duration)
    {
        Vector3 start = rt.localScale;
        float t = 0f;
        while (t < 1f)
        {
            t += Time.unscaledDeltaTime / duration;
            rt.localScale = Vector3.Lerp(start, target, UIAnimator.EaseOutCubic(Mathf.Clamp01(t)));
            yield return null;
        }
        rt.localScale = target;
    }

    private void SpawnRipple(Vector2 screenPos)
    {
        // Create ripple as a child image that expands and fades
        var rippleGo = new GameObject("Ripple");
        rippleGo.transform.SetParent(transform, false);
        var img = rippleGo.AddComponent<UnityEngine.UI.Image>();
        img.color = rippleColor;
        img.sprite = null; // white box is fine for ripple shape
        img.raycastTarget = false;

        var rippleRt = rippleGo.GetComponent<RectTransform>();
        rippleRt.sizeDelta = Vector2.zero;

        // Convert screen position to local
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            rt, screenPos, null, out Vector2 localPoint);
        rippleRt.anchoredPosition = localPoint;

        StartCoroutine(AnimateRipple(rippleRt, img));
    }

    private IEnumerator AnimateRipple(RectTransform rippleRt, UnityEngine.UI.Image img)
    {
        float maxSize = Mathf.Max(rt.rect.width, rt.rect.height) * 2.2f;
        float duration = 0.45f;
        float t = 0f;
        while (t < 1f)
        {
            t += Time.unscaledDeltaTime / duration;
            float size = Mathf.Lerp(0f, maxSize, UIAnimator.EaseOutCubic(t));
            rippleRt.sizeDelta = Vector2.one * size;
            img.color = new Color(rippleColor.r, rippleColor.g, rippleColor.b,
                                  Mathf.Lerp(rippleColor.a, 0f, t));
            yield return null;
        }
        Destroy(rippleRt.gameObject);
    }
}
