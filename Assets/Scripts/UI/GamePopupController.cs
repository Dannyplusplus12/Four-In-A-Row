using UnityEngine;
using UnityEngine.SceneManagement;

public sealed class GamePopupController : MonoBehaviour
{
    [SerializeField] private GameObject popupPanel;
    [SerializeField] private GameController gameController;
    [SerializeField] private string mainMenuScene = "MainMenu";

    private void Awake()
    {
        if (gameController == null) gameController = FindFirstObjectByType<GameController>();
        ClosePopup();
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

    public void BackToMainMenu()
    {
        if (gameController != null) gameController.SaveProgress();
        SceneManager.LoadScene(mainMenuScene);
    }

    private void ClosePopup()
    {
        if (popupPanel != null) popupPanel.SetActive(false);
    }
}