using TMPro;
using UnityEngine;
using UnityEngine.UI;

public sealed class GameModeItemUI : MonoBehaviour
{
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private TMP_Text subtitleText;
    [SerializeField] private Image[] difficultyStars;
    [SerializeField] private Sprite filledStarSprite;
    [SerializeField] private Sprite emptyStarSprite;
    [SerializeField] private Button selectButton;
    private int difficulty;

    public void Setup(string title, string subtitle, GameData.RuleMode ruleMode, int configuredDifficulty, GameModeSelectionUI owner)
    {
        if (titleText != null) titleText.text = title;
        if (subtitleText != null) subtitleText.text = subtitle;

        int maxStars = difficultyStars != null && difficultyStars.Length > 0 ? difficultyStars.Length : 3;
        difficulty = Mathf.Clamp(configuredDifficulty, 1, maxStars);
        bool showDifficultyStars = GameData.selectedMode == GameData.GameMode.OnePlayer;
        if (difficultyStars != null)
        {
            for (int index = 0; index < difficultyStars.Length; index++)
            {
                if (difficultyStars[index] == null) continue;
                difficultyStars[index].gameObject.SetActive(showDifficultyStars);
                if (!showDifficultyStars) continue;
                difficultyStars[index].sprite = index < difficulty ? filledStarSprite : emptyStarSprite;
                difficultyStars[index].enabled = true;
            }
        }

        if (selectButton != null)
        {
            selectButton.onClick.RemoveAllListeners();
            if (owner != null)
                selectButton.onClick.AddListener(() => owner.SelectMode(ruleMode, difficulty, title, subtitle));
        }
    }
}