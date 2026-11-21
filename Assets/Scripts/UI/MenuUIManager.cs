using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

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
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (mainMenuPanel != null && !mainMenuPanel.activeSelf)
                ShowMainMenu();
            return;
        }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        if (IsTypingInInputField()) return;
        if (Input.GetKeyDown(KeyCode.Alpha1) || Input.GetKeyDown(KeyCode.Keypad1)) StartDebugGame(GameData.RuleMode.Classic);
        else if (Input.GetKeyDown(KeyCode.Alpha2) || Input.GetKeyDown(KeyCode.Keypad2)) StartDebugGame(GameData.RuleMode.SwipeGravity);
        else if (Input.GetKeyDown(KeyCode.Alpha3) || Input.GetKeyDown(KeyCode.Keypad3)) StartDebugGame(GameData.RuleMode.DecayAfterEightTurns);
        else if (Input.GetKeyDown(KeyCode.Alpha4) || Input.GetKeyDown(KeyCode.Keypad4)) StartDebugGame(GameData.RuleMode.RotateAfterTwoTurns);
        else if (Input.GetKeyDown(KeyCode.Alpha5) || Input.GetKeyDown(KeyCode.Keypad5)) StartDebugGame(GameData.RuleMode.BlockAfterFourTurns);
        else if (Input.GetKeyDown(KeyCode.Alpha6) || Input.GetKeyDown(KeyCode.Keypad6)) StartDebugGame(GameData.RuleMode.FiveStarWin);
        else if (Input.GetKeyDown(KeyCode.Alpha7) || Input.GetKeyDown(KeyCode.Keypad7)) StartDebugGame(GameData.RuleMode.DestroyStarOnTap);
#endif
    }

    private static bool IsTypingInInputField()
    {
        if (EventSystem.current == null || EventSystem.current.currentSelectedGameObject == null) return false;
        TMP_InputField inputField = EventSystem.current.currentSelectedGameObject.GetComponent<TMP_InputField>();
        return inputField != null && inputField.isFocused;
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
        GameData.ResetTwoPlayerScores();
        OpenSelectPanel();
    }

    public void OpenStats()
    {
        if (statsPanel == null) return;
        SetAllPanelsActive(false);
        statsPanel.SetActive(true);
    }

    public void OpenOptions()
    {
        if (optionsPanel == null) return;
        SetAllPanelsActive(false);
        optionsPanel.SetActive(true);
    }

    public void StartGame()
    {
        FindFirstObjectByType<SelectPanelSetup>()?.SavePlayerSelection();
        OpenGameModeSelection();
    }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
    private void StartDebugGame(GameData.RuleMode ruleMode)
    {
        GameData.selectedMode = GameData.GameMode.OnePlayer;
        GameData.selectedRuleMode = ruleMode;
        GameData.selectedModeTitle = GameData.GetRuleModeTitle(ruleMode);
        GameData.selectedModeDescription = GameData.GetRuleInstruction();
        GameData.BeginNewGame();
        SceneManager.LoadScene("GameScene");
    }
#endif

    public void StartNewSinglePlayerGame()
    {
        GameData.ClearResumeRequest();
        GameData.ResetStars();
        GameData.BeginNewGame();
        OpenSelectPanel();
    }

    public void ResumeSinglePlayerGame()
    {
        if (!GameData.HasSavedGame) return;
        GameData.selectedMode = GameData.GameMode.OnePlayer;
        GameData.RequestResume();
        SceneManager.LoadScene("GameScene");
    }

    private void OpenSelectPanel()
    {
        SetAllPanelsActive(false);
        if (selectModePanel != null) selectModePanel.SetActive(true);
    }

    public void OpenSinglePlayerChoice()
    {
        SetAllPanelsActive(false);
        if (singlePlayerChoicePanel != null)
        {
            singlePlayerChoicePanel.SetActive(true);
            RefreshResumeButtonState();
        }
    }

    public void OpenGameModeSelection()
    {
        SetAllPanelsActive(false);
        if (gameModeSelectionPanel != null) gameModeSelectionPanel.SetActive(true);
    }

    private void RefreshResumeButtonState()
    {
        if (singlePlayerChoicePanel == null) return;
        Button[] buttons = singlePlayerChoicePanel.GetComponentsInChildren<Button>(true);
        bool hasSave = GameData.HasSavedGame;
        for (int i = 0; i < buttons.Length; i++)
        {
            if (buttons[i] != null && buttons[i].name.Contains("Resume"))
            {
                buttons[i].interactable = hasSave;
                CanvasGroup cg = buttons[i].GetComponent<CanvasGroup>();
                if (cg == null) cg = buttons[i].gameObject.AddComponent<CanvasGroup>();
                cg.alpha = hasSave ? 1f : 0.45f;
            }
        }
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
