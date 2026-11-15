using System;
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
    }

    [SerializeField] private ModeDefinition[] modes;
    [SerializeField] private GameObject modeItemPrefab;
    [SerializeField] private Transform modeItemContainer;
    [SerializeField] private string gameScene = "GameScene";

    private void OnEnable()
    {
        if (!Application.isPlaying) return;
        BuildModeList();
    }

    public void BuildModeList()
    {
        if (modeItemPrefab == null || modeItemContainer == null || modes == null) return;

        for (int index = modeItemContainer.childCount - 1; index >= 0; index--)
            Destroy(modeItemContainer.GetChild(index).gameObject);

        foreach (ModeDefinition mode in modes)
        {
            GameModeItemUI item = Instantiate(modeItemPrefab, modeItemContainer).GetComponent<GameModeItemUI>();
            if (item != null) item.Setup(mode.title, mode.subtitle, mode.ruleMode, this);
        }
    }

    public void SelectMode(GameData.RuleMode ruleMode, int difficulty, string title, string description)
    {
        GameData.selectedMode = GameData.GameMode.OnePlayer;
        GameData.selectedRuleMode = ruleMode;
        GameData.difficultyLevel = difficulty;
        GameData.selectedModeTitle = title;
        GameData.selectedModeDescription = description;
        GameData.BeginNewGame();
        SceneManager.LoadScene(gameScene);
    }
}