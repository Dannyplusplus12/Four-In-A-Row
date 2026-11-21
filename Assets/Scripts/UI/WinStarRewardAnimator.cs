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

    [Header("Impact Pop & Burst")]
    [SerializeField] private RectTransform targetStarIcon;
    [SerializeField] private float popScaleMultiplier = 1.35f;
    [SerializeField] private float popDuration = 0.22f;
    [SerializeField] private Sprite burstParticleSprite;
    [SerializeField] private Color burstColorPrimary = new Color(1f, 0.85f, 0.15f, 1f);
    [SerializeField] private Color burstColorSecondary = new Color(1f, 0.96f, 0.55f, 1f);
    [SerializeField] private int burstParticleCount = 12;
    [SerializeField] private float burstDuration = 0.4f;
    [SerializeField] private Vector2 burstSpeedRange = new Vector2(140f, 320f);
    [SerializeField] private Vector2 burstSizeRange = new Vector2(22f, 40f);

    private Image sourceImage;
    private Coroutine rewardRoutine;
    private Coroutine popRoutine;
    private Vector3 targetBaseScale = Vector3.one;
    private bool hasTargetBaseScale;
    private Sprite runtimeBurstSprite;
    private int pendingStarsToAward;
    private readonly List<GameObject> activeRewardStars = new List<GameObject>();

    private void Awake()
    {
        if (requiredStarsToWin > 0)
            GameData.SetRequiredStarsToWin(requiredStarsToWin);
        if (rewardSource != null)
            sourceImage = rewardSource.GetComponent<Image>();

        ResolveTargetStarIcon();
        HideSourceStar();
        RefreshTotalStarsText();
    }

    private void OnDisable()
    {
        CleanupActiveRewardStars();
        if (pendingStarsToAward > 0)
        {
            GameData.AddStars(pendingStarsToAward);
            pendingStarsToAward = 0;
            RefreshTotalStarsText();
        }
    }

    private void CleanupActiveRewardStars()
    {
        for (int i = 0; i < activeRewardStars.Count; i++)
        {
            if (activeRewardStars[i] != null)
                Destroy(activeRewardStars[i]);
        }
        activeRewardStars.Clear();
    }

    private void OnDestroy()
    {
        if (runtimeBurstSprite != null)
        {
            Destroy(runtimeBurstSprite.texture);
            Destroy(runtimeBurstSprite);
        }
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

        ResolveTargetStarIcon();
        HideSourceStar();
        if (rewardRoutine != null)
        {
            StopCoroutine(rewardRoutine);
            CleanupActiveRewardStars();
        }

        pendingStarsToAward += clampedStarCount;
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

        if (pendingStarsToAward > 0)
        {
            GameData.AddStars(pendingStarsToAward);
            pendingStarsToAward = 0;
            RefreshTotalStarsText();
        }

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
            activeRewardStars.Add(starObject);
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
        RectTransform impactTarget = GetImpactTarget();
        Vector3 startPosition = starTransform.position;
        Vector3 targetPosition = impactTarget != null ? impactTarget.position : rewardTarget.position;
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
        HandleStarArrived(targetPosition);
    }

    private void HandleStarArrived(Vector3 impactPosition)
    {
        if (pendingStarsToAward > 0)
        {
            pendingStarsToAward--;
            GameData.AddStars(1);
            RefreshTotalStarsText();
        }

        TriggerTargetPop();
        SpawnImpactBurst(impactPosition);
    }

    private void ResolveTargetStarIcon()
    {
        if (targetStarIcon == null && rewardTarget != null)
        {
            Transform starChild = rewardTarget.Find("Star");
            if (starChild != null)
                targetStarIcon = starChild as RectTransform;
        }

        RectTransform impactTarget = GetImpactTarget();
        if (impactTarget != null && !hasTargetBaseScale)
        {
            targetBaseScale = impactTarget.localScale;
            hasTargetBaseScale = true;
        }
    }

    private RectTransform GetImpactTarget()
    {
        return targetStarIcon != null ? targetStarIcon : rewardTarget;
    }

    private void TriggerTargetPop()
    {
        RectTransform impactTarget = GetImpactTarget();
        if (impactTarget == null) return;

        if (!hasTargetBaseScale)
        {
            targetBaseScale = impactTarget.localScale;
            hasTargetBaseScale = true;
        }

        if (popRoutine != null)
            StopCoroutine(popRoutine);
        popRoutine = StartCoroutine(PopTargetRoutine(impactTarget));
    }

    private IEnumerator PopTargetRoutine(RectTransform target)
    {
        float halfDuration = Mathf.Max(0.01f, popDuration * 0.38f);
        float settleDuration = Mathf.Max(0.01f, popDuration - halfDuration);
        Vector3 peakScale = targetBaseScale * popScaleMultiplier;

        float elapsed = 0f;
        while (elapsed < halfDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / halfDuration);
            float eased = 1f - Mathf.Pow(1f - t, 2f);
            target.localScale = Vector3.LerpUnclamped(targetBaseScale, peakScale, eased);
            yield return null;
        }

        elapsed = 0f;
        while (elapsed < settleDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / settleDuration);
            float eased = 1f - Mathf.Pow(1f - t, 3f);
            target.localScale = Vector3.LerpUnclamped(peakScale, targetBaseScale, eased);
            yield return null;
        }

        target.localScale = targetBaseScale;
        popRoutine = null;
    }

    private void SpawnImpactBurst(Vector3 worldPosition)
    {
        Transform parent = GetEffectContainer();
        if (parent == null) return;

        Sprite sprite = burstParticleSprite != null ? burstParticleSprite : GetOrCreateBurstSprite();
        int count = Mathf.Max(1, burstParticleCount);

        for (int index = 0; index < count; index++)
        {
            GameObject go = new GameObject(
                "StarImpactParticle",
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Image),
                typeof(LayoutElement));
            go.transform.SetParent(parent, false);

            RectTransform rt = go.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.position = worldPosition;

            float size = Random.Range(burstSizeRange.x, burstSizeRange.y);
            rt.sizeDelta = new Vector2(size, size);

            LayoutElement layout = go.GetComponent<LayoutElement>();
            layout.ignoreLayout = true;

            Image img = go.GetComponent<Image>();
            img.sprite = sprite;
            img.preserveAspect = true;
            img.raycastTarget = false;
            Color color = Color.Lerp(burstColorPrimary, burstColorSecondary, Random.value);
            img.color = color;

            float angle = (index / (float)count) * Mathf.PI * 2f + Random.Range(-0.25f, 0.25f);
            float speed = Random.Range(burstSpeedRange.x, burstSpeedRange.y);
            Vector2 velocity = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * speed;

            StartCoroutine(AnimateBurstParticleRoutine(rt, img, color, velocity));
        }
    }

    private IEnumerator AnimateBurstParticleRoutine(RectTransform rt, Image img, Color baseColor, Vector2 velocity)
    {
        Vector3 startScale = rt.localScale;
        float elapsed = 0f;
        float duration = Mathf.Max(0.05f, burstDuration);

        while (elapsed < duration)
        {
            float dt = Time.unscaledDeltaTime;
            elapsed += dt;
            float t = Mathf.Clamp01(elapsed / duration);

            // Exponential drag so particles burst out fast and settle smoothly.
            float damping = Mathf.Exp(-4.5f * t);
            rt.position += (Vector3)(velocity * damping * dt);
            rt.localScale = Vector3.Lerp(startScale, startScale * 0.25f, t);

            Color c = baseColor;
            c.a = baseColor.a * (1f - t * t);
            img.color = c;

            yield return null;
        }

        Destroy(rt.gameObject);
    }

    private Transform GetEffectContainer()
    {
        if (rewardContainer != null && rewardContainer != rewardTarget &&
            rewardContainer.GetComponent<Mask>() == null &&
            rewardContainer.GetComponent<RectMask2D>() == null)
            return rewardContainer;

        if (rewardTarget != null && rewardTarget.parent != null)
            return rewardTarget.parent;

        return rewardSource != null ? rewardSource.parent : null;
    }

    private Sprite GetOrCreateBurstSprite()
    {
        if (runtimeBurstSprite != null) return runtimeBurstSprite;

        const int size = 32;
        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false)
        {
            wrapMode = TextureWrapMode.Clamp,
            filterMode = FilterMode.Bilinear
        };

        float center = (size - 1) * 0.5f;
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dx = (x - center) / center;
                float dy = (y - center) / center;
                float dist = Mathf.Sqrt(dx * dx + dy * dy);
                float alpha = Mathf.Clamp01(1f - dist);
                alpha = alpha * alpha;
                tex.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
            }
        }

        tex.Apply();
        runtimeBurstSprite = Sprite.Create(tex, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f));
        return runtimeBurstSprite;
    }
}