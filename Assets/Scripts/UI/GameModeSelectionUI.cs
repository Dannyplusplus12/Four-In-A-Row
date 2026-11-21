using System;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

public sealed class GameModeSelectionUI : MonoBehaviour
{
    [Serializable]
    public sealed class ModeDefinition
    {
        public GameData.RuleMode ruleMode;
        public string title;
        [TextArea] public string subtitle;
        [Range(1, 3)] public int difficulty = 1;
    }

    [SerializeField] private ModeDefinition[] modes;
    [SerializeField] private GameObject modeItemPrefab;
    [SerializeField] private Transform modeItemContainer;
    [SerializeField] private TMP_Text totalStarsText;
    [SerializeField] private string gameScene = "GameScene";

    private void OnEnable()
    {
        if (!Application.isPlaying) return;
        RefreshTotalStarsText();
        BuildModeList();
    }

    public void RefreshTotalStarsText()
    {
        Transform starsRoot = transform.Find("BottomBar/Stars");
        if (starsRoot != null)
            starsRoot.gameObject.SetActive(GameData.selectedMode == GameData.GameMode.OnePlayer);

        if (totalStarsText == null)
        {
            Transform starsTextTransform = transform.Find("BottomBar/Stars/Text (TMP)");
            if (starsTextTransform != null)
                totalStarsText = starsTextTransform.GetComponent<TMP_Text>();
        }

        if (totalStarsText == null) return;
        int targetStars = GameData.RequiredStarsToWin > 0 ? GameData.RequiredStarsToWin : GameData.DefaultRequiredStarsToWin;
        totalStarsText.text = $"{GameData.TotalStars}/{targetStars}";
    }

    public void BuildModeList()
    {
        if (modeItemPrefab == null || modeItemContainer == null || modes == null) return;

        for (int index = modeItemContainer.childCount - 1; index >= 0; index--)
            Destroy(modeItemContainer.GetChild(index).gameObject);

        foreach (ModeDefinition mode in modes)
        {
            GameModeItemUI item = Instantiate(modeItemPrefab, modeItemContainer).GetComponent<GameModeItemUI>();
            if (item != null) item.Setup(mode.title, mode.subtitle, mode.ruleMode, mode.difficulty, this);
        }
    }

    public void SelectMode(GameData.RuleMode ruleMode, int difficulty, string title, string description)
    {
        GameData.selectedRuleMode = ruleMode;
        GameData.modeStarReward = Mathf.Clamp(difficulty, 1, 3);
        GameData.selectedModeTitle = title;
        GameData.selectedModeDescription = description;
        if (!GameData.ResumeRequested)
            GameData.BeginNewGame();
        SceneManager.LoadScene(gameScene);
    }
}