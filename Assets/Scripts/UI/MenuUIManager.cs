using UnityEngine;
using UnityEngine.SceneManagement;

public class MenuUIManager : MonoBehaviour
{
    [Header("Panels")]
    public GameObject mainMenuPanel;
    public GameObject selectModePanel;
    public GameObject statsPanel;
    public GameObject optionsPanel;

    private void Start()
    {
        ShowMainMenu();
    }

    public void ShowMainMenu()
    {
        SetAllPanelsActive(false);
        if (mainMenuPanel != null) mainMenuPanel.SetActive(true);
    }

    public void OpenOnePlayerMode()
    {
        GameData.selectedMode = GameData.GameMode.OnePlayer;
        OpenSelectPanel();
    }

    public void OpenTwoPlayerMode()
    {
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
        SceneManager.LoadScene("GameScene");
    }

    private void OpenSelectPanel()
    {
        SetAllPanelsActive(false);
        if (selectModePanel != null) selectModePanel.SetActive(true);
    }

    private void SetAllPanelsActive(bool active)
    {
        if (mainMenuPanel != null) mainMenuPanel.SetActive(active);
        if (selectModePanel != null) selectModePanel.SetActive(active);
        if (statsPanel != null) statsPanel.SetActive(active);
        if (optionsPanel != null) optionsPanel.SetActive(active);
    }
}