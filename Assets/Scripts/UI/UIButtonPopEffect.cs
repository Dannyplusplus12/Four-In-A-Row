using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;

[RequireComponent(typeof(RectTransform))]
public class UIButtonPopEffect : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
{
    [SerializeField] private float pressedScale = 0.92f;
    [SerializeField] private float overshootScale = 1.08f;
    [SerializeField] private float duration = 0.18f;

    private RectTransform rectTransform;
    private Vector3 originalScale;
    private Coroutine currentAnim;

    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        originalScale = rectTransform.localScale;
    }

    private void OnDisable()
    {
        if (currentAnim != null) StopCoroutine(currentAnim);
        if (rectTransform != null) rectTransform.localScale = originalScale;
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (currentAnim != null) StopCoroutine(currentAnim);
        currentAnim = StartCoroutine(ScaleTo(originalScale * pressedScale, 0.06f));
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        PlayPop();
    }

    public void PlayPop()
    {
        if (currentAnim != null) StopCoroutine(currentAnim);
        rectTransform.localScale = originalScale;
        currentAnim = StartCoroutine(PopOut());
    }

    public void PlayPopAtCurrentScale()
    {
        originalScale = rectTransform.localScale;
        PlayPop();
    }

    private IEnumerator PopOut()
    {
        Vector3 start = rectTransform.localScale;
        Vector3 peak = originalScale * overshootScale;
        float half = duration * 0.5f;
        float t = 0f;

        while (t < half)
        {
            t += Time.deltaTime;
            rectTransform.localScale = Vector3.Lerp(start, peak, Mathf.SmoothStep(0f, 1f, t / half));
            yield return null;
        }

        t = 0f;
        while (t < half)
        {
            t += Time.deltaTime;
            rectTransform.localScale = Vector3.Lerp(peak, originalScale, Mathf.SmoothStep(0f, 1f, t / half));
            yield return null;
        }

        rectTransform.localScale = originalScale;
        currentAnim = null;
    }

    private IEnumerator ScaleTo(Vector3 target, float time)
    {
        Vector3 start = rectTransform.localScale;
        float t = 0f;
        while (t < time)
        {
            t += Time.deltaTime;
            rectTransform.localScale = Vector3.Lerp(start, target, t / time);
            yield return null;
        }
        rectTransform.localScale = target;
    }
}