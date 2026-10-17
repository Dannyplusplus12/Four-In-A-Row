using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class ConnectFourGameUI : MonoBehaviour
{
    [Header("Player Labels")]
    [SerializeField] private TMP_Text playerOneNameText;
    [SerializeField] private TMP_Text playerTwoNameText;
    [SerializeField] private TMP_Text turnText;

    [Header("Result Popup")]
    [SerializeField] private GameObject resultPanel;
    [SerializeField] private TMP_Text resultText;
    [SerializeField] private Button restartButton;
    [SerializeField] private Button mainMenuButton;

    private void Awake()
    {
        if (restartButton != null)
        {
            restartButton.onClick.RemoveAllListeners();
            restartButton.onClick.AddListener(RestartGame);
        }

        if (mainMenuButton != null)
        {
            mainMenuButton.onClick.RemoveAllListeners();
            mainMenuButton.onClick.AddListener(BackToMainMenu);
        }

        if (resultPanel != null) resultPanel.SetActive(false);
        SetPlayerNames();
    }

    public void ShowGameStarted(int player)
    {
        if (resultPanel != null) resultPanel.SetActive(false);
        ShowTurn(player);
    }

    public void ShowTurn(int player)
    {
        if (turnText != null)
            turnText.text = GetPlayerName(player) + "'s turn";
    }

    public void ShowWinner(int player)
    {
        ShowResult(GetPlayerName(player) + " wins!");
    }

    public void ShowDraw()
    {
        ShowResult("Draw!");
    }

    private void ShowResult(string message)
    {
        if (resultText != null) resultText.text = message;
        if (resultPanel != null) resultPanel.SetActive(true);
    }

    private void SetPlayerNames()
    {
        if (playerOneNameText != null) playerOneNameText.text = string.IsNullOrWhiteSpace(GameData.player1Name) ? "Player 1" : GameData.player1Name;
        if (playerTwoNameText != null) playerTwoNameText.text = GameData.selectedMode == GameData.GameMode.OnePlayer ? "Computer" : (string.IsNullOrWhiteSpace(GameData.player2Name) ? "Player 2" : GameData.player2Name);
    }

    private string GetPlayerName(int player)
    {
        if (player == 1) return string.IsNullOrWhiteSpace(GameData.player1Name) ? "Player 1" : GameData.player1Name;
        return GameData.selectedMode == GameData.GameMode.OnePlayer ? "Computer" : (string.IsNullOrWhiteSpace(GameData.player2Name) ? "Player 2" : GameData.player2Name);
    }

    private void RestartGame()
    {
        ConnectFourGameController controller = FindFirstObjectByType<ConnectFourGameController>();
        if (controller != null) controller.RestartGame();
    }

    private void BackToMainMenu()
    {
        SceneManager.LoadScene("MainMenu");
    }
}