using ConnectFour.Core;
using UnityEngine;
using UnityEngine.SceneManagement;

public sealed class GamePopupController : MonoBehaviour
{
    [SerializeField] private GameObject popupPanel;
    [SerializeField] private GameObject winPanel;
    [SerializeField] private GameObject lossPanel;
    [SerializeField] private GameObject drawPanel;
    [SerializeField] private GameController gameController;
    [SerializeField] private string mainMenuScene = "MainMenu";

    private void Awake()
    {
        if (gameController == null) gameController = FindFirstObjectByType<GameController>();
        if (gameController != null) gameController.GameOver += HandleGameOver;
        ClosePopup();
    }

    private void OnDestroy()
    {
        if (gameController != null) gameController.GameOver -= HandleGameOver;
    }

    private void HandleGameOver(TurnResult result)
    {
        bool playerWon = result.Outcome == TurnOutcome.Win &&
            (GameData.selectedMode == GameData.GameMode.TwoPlayer || result.WinnerId == 1);
        bool playerLost = result.Outcome == TurnOutcome.Win &&
            GameData.selectedMode == GameData.GameMode.OnePlayer && result.WinnerId == 2;
        if (winPanel != null) winPanel.SetActive(playerWon);
        if (lossPanel != null) lossPanel.SetActive(playerLost);
        if (drawPanel != null) drawPanel.SetActive(result.Outcome == TurnOutcome.Draw);
        OpenPopup();
    }

    public void OpenPopup()
    {
        if (popupPanel != null) popupPanel.SetActive(true);
        if (gameController != null) gameController.SetPaused(true);
    }

    public void ResumeGame()
    {
        if (gameController != null) gameController.SetPaused(false);
        ClosePopup();
    }

    public void NewGame()
    {
        if (gameController != null) gameController.StartNewGame();
        ClosePopup();
    }

    public void ReplayGame()
    {
        GameData.BeginNewGame();
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    public void ContinueToGameModeSelection()
    {
        GameData.RequestModeSelection();
        SceneManager.LoadScene(mainMenuScene);
    }

    public void BackToMainMenu()
    {
        if (gameController != null) gameController.SaveProgress();
        SceneManager.LoadScene(mainMenuScene);
    }

    private void ClosePopup()
    {
        if (popupPanel != null) popupPanel.SetActive(false);
        if (winPanel != null) winPanel.SetActive(false);
        if (lossPanel != null) lossPanel.SetActive(false);
        if (drawPanel != null) drawPanel.SetActive(false);
    }
}