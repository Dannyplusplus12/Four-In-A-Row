using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public sealed class WinStarRewardAnimator : MonoBehaviour
{
    [SerializeField] private RectTransform rewardSource;
    [SerializeField] private RectTransform rewardTarget;
    [SerializeField] private Transform rewardContainer;
    [SerializeField] private Sprite rewardSprite;
    [SerializeField] private TMP_Text totalStarsText;
    [SerializeField] private float fadeInDelay = 0.3f;
    [SerializeField] private float fadeInDuration = 0.35f;
    [SerializeField] private float sourceHoldDuration = 0.6f;
    [SerializeField] private float flyDuration = 0.65f;
    [SerializeField] private float staggerDelay = 0.12f;
    [SerializeField] private float starSpacing = 110f;
    [SerializeField] private float rewardScale = 1f;
    [SerializeField] private Vector2 fallbackRewardSize = new Vector2(64f, 64f);
    [SerializeField] private int maxRewardStars = 3;
    [SerializeField] private int requiredStarsToWin = GameData.DefaultRequiredStarsToWin;

    private Image sourceImage;
    private Coroutine rewardRoutine;

    private void Awake()
    {
        if (rewardSource != null)
            sourceImage = rewardSource.GetComponent<Image>();

        HideSourceStar();
        RefreshTotalStarsText();
    }

    public void PlayReward(int starCount, System.Action onComplete = null)
    {
        RefreshTotalStarsText();
        int clampedStarCount = Mathf.Clamp(starCount, 0, maxRewardStars);
        if (clampedStarCount == 0)
        {
            onComplete?.Invoke();
            return;
        }

        if (rewardSource == null || rewardTarget == null || rewardSprite == null)
        {
            Debug.LogError("WinStarRewardAnimator cannot animate: " +
                $"source={(rewardSource == null ? "NULL" : "OK")}, " +
                $"target={(rewardTarget == null ? "NULL" : "OK")}, " +
                $"sprite={(rewardSprite == null ? "NULL" : "OK")}");
            GameData.AddStars(clampedStarCount);
            RefreshTotalStarsText();
            onComplete?.Invoke();
            return;
        }

        HideSourceStar();
        if (rewardRoutine != null)
            StopCoroutine(rewardRoutine);

        rewardRoutine = StartCoroutine(PlayRewardRoutine(clampedStarCount, onComplete));
    }

    private IEnumerator PlayRewardRoutine(int starCount, System.Action onComplete)
    {
        List<Image> spawnedStars = CreateRewardStars(starCount);

        if (fadeInDelay > 0f)
            yield return new WaitForSecondsRealtime(fadeInDelay);

        if (fadeInDuration > 0f)
        {
            float elapsed = 0f;
            while (elapsed < fadeInDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float alpha = Mathf.Clamp01(elapsed / fadeInDuration);
                SetStarsAlpha(spawnedStars, alpha);
                yield return null;
            }
        }

        SetStarsAlpha(spawnedStars, 1f);

        if (sourceHoldDuration > 0f)
            yield return new WaitForSecondsRealtime(sourceHoldDuration);

        for (int index = 0; index < spawnedStars.Count; index++)
        {
            Image starImage = spawnedStars[index];
            if (starImage != null)
                StartCoroutine(AnimateStarRoutine(starImage.rectTransform));

            if (index < spawnedStars.Count - 1 && staggerDelay > 0f)
                yield return new WaitForSecondsRealtime(staggerDelay);
        }

        if (flyDuration > 0f)
            yield return new WaitForSecondsRealtime(flyDuration);

        GameData.AddStars(starCount);
        RefreshTotalStarsText();
        rewardRoutine = null;
        onComplete?.Invoke();
    }

    private void HideSourceStar()
    {
        if (sourceImage != null)
            sourceImage.enabled = false;
    }

    public void RefreshTotalStarsText()
    {
        if (totalStarsText == null) return;
        int targetStars = requiredStarsToWin > 0 ? requiredStarsToWin : GameData.RequiredStarsToWin;
        totalStarsText.text = $"{GameData.TotalStars}/{targetStars}";
    }

    private List<Image> CreateRewardStars(int starCount)
    {
        List<Image> stars = new List<Image>(starCount);
        Transform parent = rewardSource.parent;
        if (rewardContainer != null && rewardContainer != rewardTarget && rewardContainer.GetComponent<Mask>() == null &&
            rewardContainer.GetComponent<RectMask2D>() == null)
            parent = rewardContainer;

        Vector2 rewardSize = rewardSource.rect.size;
        if (rewardSize.x <= 0f || rewardSize.y <= 0f)
            rewardSize = fallbackRewardSize;

        float centerOffset = (starCount - 1) * 0.5f;
        Vector3 rightAxis = rewardSource.right;
        float worldScaleX = rewardSource.lossyScale.x;

        for (int index = 0; index < starCount; index++)
        {
            GameObject starObject = new GameObject(
                $"RewardStar_{index + 1}",
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Image),
                typeof(LayoutElement));
            starObject.transform.SetParent(parent, false);

            RectTransform starTransform = starObject.GetComponent<RectTransform>();
            Image starImage = starObject.GetComponent<Image>();
            LayoutElement layoutElement = starObject.GetComponent<LayoutElement>();

            starImage.sprite = rewardSprite;
            starImage.preserveAspect = true;
            starImage.raycastTarget = false;
            SetStarAlpha(starImage, 0f);
            layoutElement.ignoreLayout = true;

            starTransform.anchorMin = new Vector2(0.5f, 0.5f);
            starTransform.anchorMax = new Vector2(0.5f, 0.5f);
            starTransform.pivot = new Vector2(0.5f, 0.5f);
            starTransform.sizeDelta = rewardSize;

            float offsetX = (index - centerOffset) * starSpacing * worldScaleX;
            starTransform.position = rewardSource.position + rightAxis * offsetX;
            starTransform.localScale = Vector3.one * rewardScale;

            stars.Add(starImage);
        }

        return stars;
    }

    private static void SetStarsAlpha(List<Image> stars, float alpha)
    {
        for (int index = 0; index < stars.Count; index++)
        {
            if (stars[index] != null)
                SetStarAlpha(stars[index], alpha);
        }
    }

    private static void SetStarAlpha(Image image, float alpha)
    {
        Color color = image.color;
        color.a = alpha;
        image.color = color;
    }

    private IEnumerator AnimateStarRoutine(RectTransform starTransform)
    {
        Vector3 startPosition = starTransform.position;
        Vector3 targetPosition = rewardTarget.position;
        Vector3 startScale = starTransform.localScale;
        float elapsed = 0f;

        while (elapsed < flyDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float progress = Mathf.Clamp01(elapsed / flyDuration);
            float eased = 1f - Mathf.Pow(1f - progress, 3f);
            starTransform.position = Vector3.LerpUnclamped(startPosition, targetPosition, eased);
            starTransform.localScale = Vector3.Lerp(startScale, Vector3.zero, progress);
            yield return null;
        }

        Destroy(starTransform.gameObject);
    }
}