using UnityEngine;
using UnityEngine.SceneManagement;

public class MenuUIManager : MonoBehaviour
{
    [Header("Panels")]
    public GameObject mainMenuPanel;
    public GameObject selectModePanel;
    public GameObject singlePlayerChoicePanel;
    public GameObject gameModeSelectionPanel;
    public GameObject statsPanel;
    public GameObject optionsPanel;

    private void Start()
    {
        ResolveGameModeSelectionPanel();

        if (GameData.ModeSelectionRequested)
        {
            GameData.ClearModeSelectionRequest();
            OpenGameModeSelection();
        }
        else
        {
            ShowMainMenu();
        }
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Alpha1) || Input.GetKeyDown(KeyCode.Keypad1)) StartDebugGame(GameData.RuleMode.Classic);
        else if (Input.GetKeyDown(KeyCode.Alpha2) || Input.GetKeyDown(KeyCode.Keypad2)) StartDebugGame(GameData.RuleMode.SwipeGravity);
        else if (Input.GetKeyDown(KeyCode.Alpha3) || Input.GetKeyDown(KeyCode.Keypad3)) StartDebugGame(GameData.RuleMode.DecayAfterEightTurns);
        else if (Input.GetKeyDown(KeyCode.Alpha4) || Input.GetKeyDown(KeyCode.Keypad4)) StartDebugGame(GameData.RuleMode.RotateAfterTwoTurns);
        else if (Input.GetKeyDown(KeyCode.Alpha5) || Input.GetKeyDown(KeyCode.Keypad5)) StartDebugGame(GameData.RuleMode.BlockAfterFourTurns);
        else if (Input.GetKeyDown(KeyCode.Alpha6) || Input.GetKeyDown(KeyCode.Keypad6)) StartDebugGame(GameData.RuleMode.FiveStarWin);
        else if (Input.GetKeyDown(KeyCode.Alpha7) || Input.GetKeyDown(KeyCode.Keypad7)) StartDebugGame(GameData.RuleMode.DestroyStarOnTap);
    }

    public void ShowMainMenu()
    {
        SetAllPanelsActive(false);
        if (mainMenuPanel != null) mainMenuPanel.SetActive(true);
    }

    public void OpenOnePlayerMode()
    {
        GameData.ClearResumeRequest();
        GameData.selectedMode = GameData.GameMode.OnePlayer;
        OpenSinglePlayerChoice();
    }

    public void OpenTwoPlayerMode()
    {
        GameData.ClearResumeRequest();
        GameData.selectedMode = GameData.GameMode.TwoPlayer;
        OpenSelectPanel();
    }

    public void OpenStats()
    {
        SetAllPanelsActive(false);
        if (statsPanel != null) statsPanel.SetActive(true);
    }

    public void OpenOptions()
    {
        SetAllPanelsActive(false);
        if (optionsPanel != null) optionsPanel.SetActive(true);
    }

    public void StartGame()
    {
        FindFirstObjectByType<SelectPanelSetup>()?.SavePlayerSelection();
        OpenGameModeSelection();
    }

    private void StartDebugGame(GameData.RuleMode ruleMode)
    {
        GameData.selectedMode = GameData.GameMode.OnePlayer;
        GameData.selectedRuleMode = ruleMode;
        GameData.selectedModeTitle = ruleMode.ToString();
        GameData.selectedModeDescription = GameData.GetRuleInstruction();
        GameData.BeginNewGame();
        SceneManager.LoadScene("GameScene");
    }

    public void StartNewSinglePlayerGame()
    {
        GameData.ClearResumeRequest();
        GameData.BeginNewGame();
        OpenGameModeSelection();
    }

    public void ResumeSinglePlayerGame()
    {
        GameData.selectedMode = GameData.GameMode.OnePlayer;
        GameData.RequestResume();
        OpenSelectPanel();
    }

    private void OpenSelectPanel()
    {
        SetAllPanelsActive(false);
        if (selectModePanel != null) selectModePanel.SetActive(true);
    }

    public void OpenSinglePlayerChoice()
    {
        SetAllPanelsActive(false);
        if (singlePlayerChoicePanel != null) singlePlayerChoicePanel.SetActive(true);
    }

    public void OpenGameModeSelection()
    {
        SetAllPanelsActive(false);
        if (gameModeSelectionPanel != null) gameModeSelectionPanel.SetActive(true);
    }

    private void SetAllPanelsActive(bool active)
    {
        if (mainMenuPanel != null) mainMenuPanel.SetActive(active);
        if (selectModePanel != null) selectModePanel.SetActive(active);
        if (singlePlayerChoicePanel != null) singlePlayerChoicePanel.SetActive(active);
        if (gameModeSelectionPanel != null) gameModeSelectionPanel.SetActive(active);
        if (statsPanel != null) statsPanel.SetActive(active);
        if (optionsPanel != null) optionsPanel.SetActive(active);
    }

    private void ResolveGameModeSelectionPanel()
    {
        if (gameModeSelectionPanel != null) return;

        GameModeSelectionUI selectionUI = FindFirstObjectByType<GameModeSelectionUI>(FindObjectsInactive.Include);
        if (selectionUI != null)
        {
            gameModeSelectionPanel = selectionUI.gameObject;
            return;
        }

        GameObject panel = GameObject.Find("GameModeSelectionPanel");
        if (panel != null) gameModeSelectionPanel = panel;
    }
}