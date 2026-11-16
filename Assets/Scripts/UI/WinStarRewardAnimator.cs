using System.Collections;
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
    [SerializeField] private float flyDuration = 0.65f;
    [SerializeField] private float staggerDelay = 0.12f;
    [SerializeField] private float rewardScale = 1f;
    [SerializeField] private Vector2 fallbackRewardSize = new Vector2(64f, 64f);
    [SerializeField] private float sourceHoldDuration = 0.35f;
    [SerializeField] private int maxRewardStars = 3;

    private Image sourceImage;

    private void Awake()
    {
        if (rewardSource != null)
            sourceImage = rewardSource.GetComponent<Image>();
    }

    public void PlayReward(int starCount, System.Action onComplete = null)
    {
        Debug.Log($"PlayReward called: count={starCount}, source={Describe(rewardSource)}, " +
            $"target={Describe(rewardTarget)}, sprite={(rewardSprite == null ? "NULL" : rewardSprite.name)}");
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

        ShowSourceStar();

        StartCoroutine(PlayRewardRoutine(clampedStarCount, onComplete));
    }

    private IEnumerator PlayRewardRoutine(int starCount, System.Action onComplete)
    {
        yield return new WaitForSecondsRealtime(sourceHoldDuration);
        HideSourceStar();

        for (int index = 0; index < starCount; index++)
        {
            CreateAndAnimateStar();
            if (index < starCount - 1)
                yield return new WaitForSecondsRealtime(staggerDelay);
        }

        yield return new WaitForSecondsRealtime(flyDuration);
        GameData.AddStars(starCount);
        RefreshTotalStarsText();
        onComplete?.Invoke();
    }

    private void ShowSourceStar()
    {
        if (sourceImage == null)
        {
            sourceImage = rewardSource.gameObject.AddComponent<Image>();
            Debug.Log("Created source Image component on rewardSource.");
        }

        sourceImage.sprite = rewardSprite;
        sourceImage.preserveAspect = true;
        sourceImage.enabled = true;
        rewardSource.localScale = Vector3.one * rewardScale;
    }

    private void HideSourceStar()
    {
        if (sourceImage != null)
            sourceImage.enabled = false;
    }

    private static string Describe(RectTransform target)
    {
        if (target == null) return "NULL";
        return $"{target.name} active={target.gameObject.activeInHierarchy} size={target.rect.size}";
    }

    private void RefreshTotalStarsText()
    {
        if (totalStarsText != null)
            totalStarsText.text = GameData.TotalStars.ToString();
    }

    private void CreateAndAnimateStar()
    {
        GameObject starObject = new GameObject(
            "RewardStar",
            typeof(RectTransform),
            typeof(CanvasRenderer),
            typeof(Image),
            typeof(LayoutElement));
        Transform parent = rewardSource.parent;
        if (rewardContainer != null && rewardContainer != rewardTarget && rewardContainer.GetComponent<Mask>() == null &&
            rewardContainer.GetComponent<RectMask2D>() == null)
            parent = rewardContainer;
        starObject.transform.SetParent(parent, false);

        RectTransform starTransform = starObject.GetComponent<RectTransform>();
        Image starImage = starObject.GetComponent<Image>();
        LayoutElement layoutElement = starObject.GetComponent<LayoutElement>();
        starImage.sprite = rewardSprite;
        starImage.preserveAspect = true;
        layoutElement.ignoreLayout = true;
        starTransform.anchorMin = new Vector2(0.5f, 0.5f);
        starTransform.anchorMax = new Vector2(0.5f, 0.5f);
        starTransform.pivot = new Vector2(0.5f, 0.5f);
        Vector2 rewardSize = rewardSource.rect.size;
        if (rewardSize.x <= 0f || rewardSize.y <= 0f)
            rewardSize = fallbackRewardSize;
        starTransform.sizeDelta = rewardSize;
        starTransform.position = rewardSource.position;
        starTransform.localScale = Vector3.one * rewardScale;

        Debug.Log($"RewardStar created: parent={parent.name}, source={rewardSource.position}, " +
            $"target={rewardTarget.position}, size={starTransform.sizeDelta}, sprite={rewardSprite.name}");

        StartCoroutine(AnimateStarRoutine(starTransform));
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