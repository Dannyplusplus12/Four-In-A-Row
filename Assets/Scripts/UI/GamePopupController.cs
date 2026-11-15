using System.Collections;
using ConnectFour.Core;
using UnityEngine;
using UnityEngine.SceneManagement;

public sealed class GamePopupController : MonoBehaviour
{
    [SerializeField] private GameObject popupPanel;
    [SerializeField] private GameObject winPanel;
    [SerializeField] private GameObject lossPanel;
    [SerializeField] private GameObject drawPanel;
    [SerializeField] private GameObject continueButton;
    [SerializeField] private GameController gameController;
    [SerializeField] private string mainMenuScene = "MainMenu";
    [SerializeField] private float panelPopDuration = 0.35f;
    [SerializeField] private float panelShakeDuration = 0.45f;
    [SerializeField] private float panelShakeAngle = 5f;
    [SerializeField] private float continueDelay = 1f;

    private Coroutine popupAnimation;

    private void Awake()
    {
        if (gameController == null) gameController = GetComponent<GameController>();
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
        bool isWinOrDraw = result.Outcome == TurnOutcome.Win || result.Outcome == TurnOutcome.Draw;
        bool playerWon = isWinOrDraw &&
            (result.Outcome == TurnOutcome.Draw ||
             GameData.selectedMode == GameData.GameMode.TwoPlayer || result.WinnerId == 1);
        bool playerLost = result.Outcome == TurnOutcome.Win &&
            GameData.selectedMode == GameData.GameMode.OnePlayer && result.WinnerId == 2;
        if (winPanel != null) winPanel.SetActive(playerWon);
        if (lossPanel != null) lossPanel.SetActive(playerLost);
        if (drawPanel != null) drawPanel.SetActive(false);
        OpenPopup();
        PlayResultAnimation(playerWon ? winPanel : lossPanel);
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
        if (popupAnimation != null)
        {
            StopCoroutine(popupAnimation);
            popupAnimation = null;
        }
        if (popupPanel != null) popupPanel.SetActive(false);
        if (winPanel != null) winPanel.SetActive(false);
        if (lossPanel != null) lossPanel.SetActive(false);
        if (drawPanel != null) drawPanel.SetActive(false);
        if (continueButton != null) continueButton.SetActive(false);
    }

    private void PlayResultAnimation(GameObject resultPanel)
    {
        if (popupAnimation != null) StopCoroutine(popupAnimation);
        popupAnimation = StartCoroutine(ResultAnimationRoutine(resultPanel));
    }

    private IEnumerator ResultAnimationRoutine(GameObject resultPanel)
    {
        if (continueButton != null) continueButton.SetActive(false);
        float animationElapsed = 0f;
        if (resultPanel == null)
        {
            yield return new WaitForSecondsRealtime(continueDelay);
            ShowContinueButton();
            yield break;
        }

        RectTransform panelTransform = resultPanel.transform as RectTransform;
        if (panelTransform == null)
        {
            yield return new WaitForSecondsRealtime(continueDelay);
            ShowContinueButton();
            yield break;
        }

        Vector3 originalScale = panelTransform.localScale;
        Quaternion originalRotation = panelTransform.localRotation;
        float elapsed = 0f;
        panelTransform.localScale = originalScale * 0.65f;
        panelTransform.localRotation = originalRotation;

        while (elapsed < panelPopDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            animationElapsed += Time.unscaledDeltaTime;
            float progress = Mathf.Clamp01(elapsed / panelPopDuration);
            float eased = 1f - Mathf.Pow(1f - progress, 3f);
            panelTransform.localScale = Vector3.LerpUnclamped(originalScale * 0.65f, originalScale, eased);
            yield return null;
        }

        elapsed = 0f;
        while (elapsed < panelShakeDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            animationElapsed += Time.unscaledDeltaTime;
            float progress = Mathf.Clamp01(elapsed / panelShakeDuration);
            float damping = 1f - progress;
            float angle = Mathf.Sin(progress * Mathf.PI * 8f) * panelShakeAngle * damping;
            panelTransform.localRotation = originalRotation * Quaternion.Euler(0f, 0f, angle);
            yield return null;
        }

        panelTransform.localScale = originalScale;
        panelTransform.localRotation = originalRotation;
        float remainingDelay = Mathf.Max(0f, continueDelay - animationElapsed);
        if (remainingDelay > 0f) yield return new WaitForSecondsRealtime(remainingDelay);
        ShowContinueButton();
        popupAnimation = null;
    }

    private void ShowContinueButton()
    {
        if (continueButton == null) return;
        continueButton.SetActive(true);
        RectTransform buttonTransform = continueButton.transform as RectTransform;
        if (buttonTransform != null) StartCoroutine(ContinueButtonPopRoutine(buttonTransform));
    }

    private IEnumerator ContinueButtonPopRoutine(RectTransform buttonTransform)
    {
        Vector3 originalScale = buttonTransform.localScale;
        float elapsed = 0f;
        buttonTransform.localScale = originalScale * 0.65f;
        while (elapsed < panelPopDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float progress = Mathf.Clamp01(elapsed / panelPopDuration);
            float eased = 1f - Mathf.Pow(1f - progress, 3f);
            buttonTransform.localScale = Vector3.LerpUnclamped(originalScale * 0.65f, originalScale, eased);
            yield return null;
        }
        buttonTransform.localScale = originalScale;
    }
}