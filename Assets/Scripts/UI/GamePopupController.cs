using System.Collections;
using System.Collections.Generic;
using ConnectFour.Core;
using UnityEngine;
using UnityEngine.SceneManagement;

public sealed class GamePopupController : MonoBehaviour
{
    [SerializeField] private GameObject popupPanel;
    [SerializeField] private GameObject settingsPanel;
    [SerializeField] private GameObject winPanel;
    [SerializeField] private GameObject lossPanel;
    [SerializeField] private GameObject drawPanel;
    [SerializeField] private GameObject continueButton;
    [SerializeField] private GameObject rematchButton;
    [SerializeField] private GameObject twoPlayerResultBanner;
    [SerializeField] private TMPro.TMP_Text twoPlayerResultTitle;
    [SerializeField] private TMPro.TMP_Text twoPlayerScoreSummary;
    [SerializeField] private GameController gameController;
    [SerializeField] private PlayerTurnView playerTurnView;
    [SerializeField] private string mainMenuScene = "MainMenu";
    [SerializeField] private float panelPopDuration = 0.35f;
    [SerializeField] private float panelShakeDuration = 0.45f;
    [SerializeField] private float panelShakeAngle = 5f;
    [SerializeField] private float continueDelay = 1f;
    [SerializeField] private float winHighlightDelay = 1.25f;
    [SerializeField] private WinStarRewardAnimator winStarRewardAnimator;

    private Coroutine popupAnimation;
    private Coroutine continuePopAnimation;
    private Coroutine delayedGameOverRoutine;

    private readonly Dictionary<RectTransform, Vector3> baseScales = new Dictionary<RectTransform, Vector3>();
    private readonly Dictionary<RectTransform, Quaternion> baseRotations = new Dictionary<RectTransform, Quaternion>();

    private void Awake()
    {
        if (gameController == null) gameController = GetComponent<GameController>();
        if (gameController == null) gameController = FindFirstObjectByType<GameController>();
        if (playerTurnView == null) playerTurnView = FindFirstObjectByType<PlayerTurnView>();
        if (winStarRewardAnimator == null) winStarRewardAnimator = GetComponent<WinStarRewardAnimator>();
        if (winStarRewardAnimator == null)
            winStarRewardAnimator = FindFirstObjectByType<WinStarRewardAnimator>(FindObjectsInactive.Include);

        CacheOriginalTransform(winPanel);
        CacheOriginalTransform(lossPanel);
        CacheOriginalTransform(drawPanel);
        CacheOriginalTransform(continueButton);
        CacheOriginalTransform(rematchButton);

        winStarRewardAnimator?.RefreshTotalStarsText();
        if (gameController != null) gameController.GameOver += HandleGameOver;
        ClosePopup();
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (settingsPanel != null && settingsPanel.activeSelf)
                ResumeGame();
            else if (popupPanel == null || !popupPanel.activeSelf)
                OpenSettingsPopup();
        }
    }

    private void OnDestroy()
    {
        if (gameController != null) gameController.GameOver -= HandleGameOver;
    }

    private void CacheOriginalTransform(GameObject obj)
    {
        if (obj == null) return;
        RectTransform rt = obj.transform as RectTransform;
        if (rt == null) return;
        baseScales[rt] = rt.localScale;
        baseRotations[rt] = rt.localRotation;
    }

    private void RestoreOriginalTransform(GameObject obj)
    {
        if (obj == null) return;
        RectTransform rt = obj.transform as RectTransform;
        if (rt == null) return;
        if (baseScales.TryGetValue(rt, out Vector3 scale)) rt.localScale = scale;
        if (baseRotations.TryGetValue(rt, out Quaternion rot)) rt.localRotation = rot;
    }

    private void HandleGameOver(TurnResult result)
    {
        if (delayedGameOverRoutine != null)
            StopCoroutine(delayedGameOverRoutine);

        float delay = result.Outcome == TurnOutcome.Win && result.WinningPositions.Count > 0 ? winHighlightDelay : 0.2f;
        delayedGameOverRoutine = StartCoroutine(ShowGameOverAfterDelay(result, delay));
    }

    private IEnumerator ShowGameOverAfterDelay(TurnResult result, float delay)
    {
        if (delay > 0f)
            yield return new WaitForSecondsRealtime(delay);
        delayedGameOverRoutine = null;

        bool isDraw = result.Outcome == TurnOutcome.Draw;
        bool isSinglePlayer = GameData.selectedMode == GameData.GameMode.OnePlayer;
        // Player 1 is Human, Player 2 is Bot in 1-Player mode.
        bool playerWon = result.Outcome == TurnOutcome.Win && (!isSinglePlayer || result.WinnerId == 1);
        bool playerLost = result.Outcome == TurnOutcome.Win && isSinglePlayer && result.WinnerId == 2;
        bool awardStars = playerWon && isSinglePlayer;

        if (!isSinglePlayer)
        {
            GameData.RecordTwoPlayerResult(result.WinnerId, isDraw);
            playerTurnView?.RefreshScoreboard();
            UpdateTwoPlayerResultBanner(result.WinnerId, isDraw);
        }
        else if (twoPlayerResultBanner != null)
        {
            twoPlayerResultBanner.SetActive(false);
        }

        if (winPanel != null) winPanel.SetActive(playerWon || (isDraw && drawPanel == null));
        if (lossPanel != null) lossPanel.SetActive(playerLost);
        if (drawPanel != null) drawPanel.SetActive(isDraw);

        OpenPopup();
        if (isDraw)
            SoundManager.PlayDraw();
        else if (playerLost)
            SoundManager.PlayLose();
        else
            SoundManager.PlayWin();

        if (awardStars)
            PlayWinReward();

        GameObject activeResultPanel = isDraw && drawPanel != null
            ? drawPanel
            : (playerLost ? lossPanel : winPanel);
        PlayResultAnimation(activeResultPanel);
    }

    private void UpdateTwoPlayerResultBanner(int? winnerId, bool isDraw)
    {
        if (twoPlayerResultBanner == null) return;
        twoPlayerResultBanner.SetActive(true);

        string p1Name = string.IsNullOrWhiteSpace(GameData.player1Name) ? "Player 1" : GameData.player1Name;
        string p2Name = string.IsNullOrWhiteSpace(GameData.player2Name) ? "Player 2" : GameData.player2Name;
        bool seriesOver = GameData.IsTwoPlayerSeriesOver;

        if (twoPlayerResultTitle != null)
        {
            if (isDraw)
                twoPlayerResultTitle.text = "ROUND DRAW!";
            else if (seriesOver)
                twoPlayerResultTitle.text = winnerId == 1 ? $"{p1Name} Wins BO3!" : $"{p2Name} Wins BO3!";
            else
                twoPlayerResultTitle.text = winnerId == 1 ? $"{p1Name} Takes Round!" : $"{p2Name} Takes Round!";
        }

        if (twoPlayerScoreSummary != null)
        {
            string status = seriesOver ? "FINAL SCORE" : "BEST OF 3";
            twoPlayerScoreSummary.text = $"{status}   •   {p1Name}  {GameData.player1Score} - {GameData.player2Score}  {p2Name}";
        }

        if (rematchButton != null)
        {
            TMPro.TMP_Text btnText = rematchButton.GetComponentInChildren<TMPro.TMP_Text>(true);
            if (btnText != null)
                btnText.text = "Continue";
        }

        if (continueButton != null)
        {
            TMPro.TMP_Text continueText = continueButton.GetComponentInChildren<TMPro.TMP_Text>(true);
            if (continueText != null)
                continueText.text = "Another Mode";
        }
    }

    public void RematchTwoPlayer()
    {
        if (GameData.IsTwoPlayerSeriesOver)
        {
            GameData.ResetTwoPlayerScores();
        }
        else
        {
            // Alternate who goes first each round in BO3 for fairness
            GameData.firstMove = GameData.firstMove == GameData.FirstMoveOption.Player1
                ? GameData.FirstMoveOption.Player2
                : GameData.FirstMoveOption.Player1;
        }

        ClosePopup();
        playerTurnView?.RefreshScoreboard();
        if (gameController != null) gameController.StartNewGame();
    }

    private void PlayWinReward()
    {
        int reward = Mathf.Clamp(GameData.modeStarReward, 1, 3);
        if (winStarRewardAnimator != null)
            winStarRewardAnimator.PlayReward(reward);
        else
            GameData.AddStars(reward);
    }

    public void OpenPopup()
    {
        if (popupPanel != null) popupPanel.SetActive(true);
        if (gameController != null) gameController.SetPaused(true);
    }

    public void OpenSettingsPopup()
    {
        if (popupPanel != null && popupPanel.activeSelf) return;
        if (settingsPanel != null) settingsPanel.SetActive(true);
        if (gameController != null) gameController.SetPaused(true);
    }

    public void ResumeGame()
    {
        ClosePopup();
        if (gameController != null) gameController.SetPaused(false);
    }

    public void NewGame()
    {
        // Restart only the current match (preserving 1P stars and 2P BO3 series score)
        ClosePopup();
        if (gameController != null) gameController.StartNewGame();
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
        if (delayedGameOverRoutine != null)
        {
            StopCoroutine(delayedGameOverRoutine);
            delayedGameOverRoutine = null;
        }
        if (popupAnimation != null)
        {
            StopCoroutine(popupAnimation);
            popupAnimation = null;
        }
        if (continuePopAnimation != null)
        {
            StopCoroutine(continuePopAnimation);
            continuePopAnimation = null;
        }

        RestoreOriginalTransform(winPanel);
        RestoreOriginalTransform(lossPanel);
        RestoreOriginalTransform(drawPanel);
        RestoreOriginalTransform(continueButton);
        RestoreOriginalTransform(rematchButton);

        if (popupPanel != null) popupPanel.SetActive(false);
        if (settingsPanel != null) settingsPanel.SetActive(false);
        if (winPanel != null) winPanel.SetActive(false);
        if (lossPanel != null) lossPanel.SetActive(false);
        if (drawPanel != null) drawPanel.SetActive(false);
        if (twoPlayerResultBanner != null) twoPlayerResultBanner.SetActive(false);
        if (continueButton != null) continueButton.SetActive(false);
        if (rematchButton != null) rematchButton.SetActive(false);
    }

    private void PlayResultAnimation(GameObject resultPanel)
    {
        if (popupAnimation != null) StopCoroutine(popupAnimation);
        RestoreOriginalTransform(resultPanel);
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

        Vector3 originalScale = baseScales.TryGetValue(panelTransform, out Vector3 cachedScale) ? cachedScale : panelTransform.localScale;
        Quaternion originalRotation = baseRotations.TryGetValue(panelTransform, out Quaternion cachedRot) ? cachedRot : panelTransform.localRotation;

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
        bool isTwoPlayer = GameData.selectedMode == GameData.GameMode.TwoPlayer;
        if (rematchButton != null)
        {
            rematchButton.SetActive(isTwoPlayer);
            if (isTwoPlayer) RestoreOriginalTransform(rematchButton);
        }

        if (continueButton == null) return;
        TMPro.TMP_Text continueText = continueButton.GetComponentInChildren<TMPro.TMP_Text>(true);
        if (continueText != null)
            continueText.text = isTwoPlayer ? "Another Mode" : "Continue";
        continueButton.SetActive(true);
        RestoreOriginalTransform(continueButton);
        RectTransform buttonTransform = continueButton.transform as RectTransform;
        if (buttonTransform != null)
        {
            if (continuePopAnimation != null) StopCoroutine(continuePopAnimation);
            continuePopAnimation = StartCoroutine(ContinueButtonPopRoutine(buttonTransform));
        }
    }

    private IEnumerator ContinueButtonPopRoutine(RectTransform buttonTransform)
    {
        Vector3 originalScale = baseScales.TryGetValue(buttonTransform, out Vector3 cachedScale) ? cachedScale : buttonTransform.localScale;
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
        continuePopAnimation = null;
    }
}
