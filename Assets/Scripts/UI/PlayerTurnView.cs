using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PlayerTurnView : MonoBehaviour
{
    [SerializeField] private GameController gameController;
    [SerializeField] private BoardView boardView;

    [Header("HUD Roots")]
    [SerializeField] private GameObject singlePlayerHUD;
    [SerializeField] private GameObject multiplayerHUD;
    [SerializeField] private GameObject singlePlayerStarsBadge;

    [Header("Single Player Mode Info")]
    [SerializeField] private TMP_Text singlePlayerModeTitle;
    [SerializeField] private TMP_Text singlePlayerModeDescription;
    [SerializeField] private TMP_Text singlePlayerTurnTitle;

    [Header("Single Player Bottom Indicator")]
    [SerializeField] private RectTransform playerOneRoot;
    [SerializeField] private Image playerOneStar;
    [SerializeField] private TMP_Text playerOneText;
    [SerializeField] private RectTransform playerTwoRoot;
    [SerializeField] private Image playerTwoStar;
    [SerializeField] private TMP_Text playerTwoText;

    [Header("Multiplayer BO3 HUD")]
    [SerializeField] private GameObject multiplayerScorePanel;
    [SerializeField] private TMP_Text multiplayerModeTitle;
    [SerializeField] private TMP_Text multiplayerModeDescription;
    [SerializeField] private TMP_Text multiplayerTurnBanner;
    [SerializeField] private TMP_Text multiplayerScoreText;
    [SerializeField] private TMP_Text multiplayerDrawsText;
    [SerializeField] private RectTransform mpPlayerOneRoot;
    [SerializeField] private Image mpPlayerOneStar;
    [SerializeField] private TMP_Text mpPlayerOneName;
    [SerializeField] private Image[] mpPlayerOnePips;
    [SerializeField] private RectTransform mpPlayerTwoRoot;
    [SerializeField] private Image mpPlayerTwoStar;
    [SerializeField] private TMP_Text mpPlayerTwoName;
    [SerializeField] private Image[] mpPlayerTwoPips;
    [SerializeField] private Sprite filledPipSprite;
    [SerializeField] private Sprite emptyPipSprite;

    [Header("Turn Appearance")]
    [SerializeField] private float activeScale = 1.06f;
    [SerializeField] private float inactiveScale = 0.94f;
    [SerializeField] private float inactiveAlpha = 0.50f;

    private static readonly Color GreenTurnColor = new Color(0.11f, 0.50f, 0.24f, 1f);
    private static readonly Color RedTurnColor = new Color(0.78f, 0.17f, 0.25f, 1f);
    private static readonly Color PipFilledColor = Color.white;
    private static readonly Color PipEmptyColor = new Color(0.65f, 0.72f, 0.84f, 0.45f);

    private void Awake()
    {
        if (gameController == null) gameController = FindFirstObjectByType<GameController>();
        if (boardView == null) boardView = FindFirstObjectByType<BoardView>();
    }

    private void OnEnable()
    {
        if (gameController != null) gameController.TurnChanged += Refresh;
    }

    private void Start()
    {
        ConfigureHud();
        if (gameController != null && gameController.Model != null)
            Refresh(gameController.Model.ActivePlayerId);
    }

    private void OnDisable()
    {
        if (gameController != null) gameController.TurnChanged -= Refresh;
    }

    private Sprite ResolvePlayerSprite(bool isPlayerOne)
    {
        Sprite assigned = isPlayerOne ? GameData.player1StarSprite : GameData.player2StarSprite;
        if (assigned != null) return assigned;

        if (boardView == null) boardView = FindFirstObjectByType<BoardView>();
        if (boardView == null) return null;

        bool useGreen = isPlayerOne ? GameData.isP1Green : !GameData.isP1Green;
        return useGreen ? boardView.GreenStarSprite : boardView.RedStarSprite;
    }

    public void RefreshScoreboard()
    {
        if (multiplayerScoreText != null)
            multiplayerScoreText.text = $"{GameData.player1Score} - {GameData.player2Score}";

        if (multiplayerDrawsText != null)
        {
            int roundNum = GameData.IsTwoPlayerSeriesOver
                ? GameData.player1Score + GameData.player2Score + GameData.twoPlayerDraws
                : GameData.TwoPlayerCurrentRound;
            multiplayerDrawsText.gameObject.SetActive(true);
            multiplayerDrawsText.text = $"BO3  •  ROUND {roundNum}";
        }

        UpdatePips(mpPlayerOnePips, GameData.player1Score);
        UpdatePips(mpPlayerTwoPips, GameData.player2Score);
    }

    private void UpdatePips(Image[] pips, int wins)
    {
        if (pips == null) return;
        for (int i = 0; i < pips.Length; i++)
        {
            if (pips[i] == null) continue;
            bool earned = i < wins;
            if (filledPipSprite != null)
                pips[i].sprite = earned ? filledPipSprite : (emptyPipSprite != null ? emptyPipSprite : filledPipSprite);
            pips[i].color = earned ? PipFilledColor : PipEmptyColor;
        }
    }

    private void Refresh(int activePlayerId)
    {
        bool isSinglePlayer = GameData.selectedMode == GameData.GameMode.OnePlayer;
        Sprite p1Sprite = ResolvePlayerSprite(true);
        Sprite p2Sprite = ResolvePlayerSprite(false);
        string p1Name = string.IsNullOrWhiteSpace(GameData.player1Name) ? "Player 1" : GameData.player1Name;
        string p2Name = string.IsNullOrWhiteSpace(GameData.player2Name) ? "Player 2" : GameData.player2Name;

        if (isSinglePlayer)
        {
            if (singlePlayerTurnTitle != null)
                singlePlayerTurnTitle.text = activePlayerId == 1 ? "Your Turn" : "Computer's Turn";

            if (playerOneRoot != null) playerOneRoot.gameObject.SetActive(false);

            if (playerTwoRoot != null && playerTwoStar != null && playerTwoText != null)
            {
                playerTwoRoot.gameObject.SetActive(true);
                playerTwoText.text = p1Name;
                if (p1Sprite != null)
                {
                    playerTwoStar.sprite = p1Sprite;
                    playerTwoStar.enabled = true;
                }
                else
                {
                    playerTwoStar.enabled = false;
                }
                SetPlayerState(playerTwoRoot, playerTwoStar, playerTwoText, activePlayerId == 1);
            }
            return;
        }

        RefreshScoreboard();

        if (multiplayerScorePanel != null)
        {
            if (playerOneRoot != null) playerOneRoot.gameObject.SetActive(false);
            if (playerTwoRoot != null) playerTwoRoot.gameObject.SetActive(false);

            if (mpPlayerOneRoot != null && mpPlayerOneStar != null && mpPlayerOneName != null)
            {
                mpPlayerOneName.text = p1Name;
                if (p1Sprite != null) { mpPlayerOneStar.sprite = p1Sprite; mpPlayerOneStar.enabled = true; }
                SetCardState(mpPlayerOneRoot, activePlayerId == 1);
            }

            if (mpPlayerTwoRoot != null && mpPlayerTwoStar != null && mpPlayerTwoName != null)
            {
                mpPlayerTwoName.text = p2Name;
                if (p2Sprite != null) { mpPlayerTwoStar.sprite = p2Sprite; mpPlayerTwoStar.enabled = true; }
                SetCardState(mpPlayerTwoRoot, activePlayerId == 2);
            }

            if (multiplayerTurnBanner != null)
            {
                string activeName = activePlayerId == 1 ? p1Name : p2Name;
                bool activeUsesGreen = activePlayerId == 1 ? GameData.isP1Green : !GameData.isP1Green;
                multiplayerTurnBanner.text = $"{activeName}'s Turn";
                multiplayerTurnBanner.color = activeUsesGreen ? GreenTurnColor : RedTurnColor;
            }
            return;
        }

        if (playerOneRoot != null && playerOneStar != null && playerOneText != null)
        {
            playerOneRoot.gameObject.SetActive(true);
            playerOneText.text = p1Name;
            if (p1Sprite != null) { playerOneStar.sprite = p1Sprite; playerOneStar.enabled = true; }
            SetPlayerState(playerOneRoot, playerOneStar, playerOneText, activePlayerId == 1);
        }

        if (playerTwoRoot != null && playerTwoStar != null && playerTwoText != null)
        {
            playerTwoRoot.gameObject.SetActive(true);
            playerTwoText.text = p2Name;
            if (p2Sprite != null) { playerTwoStar.sprite = p2Sprite; playerTwoStar.enabled = true; }
            SetPlayerState(playerTwoRoot, playerTwoStar, playerTwoText, activePlayerId == 2);
        }
    }

    private void ConfigureHud()
    {
        bool isSinglePlayer = GameData.selectedMode == GameData.GameMode.OnePlayer;
        SetHudActive(singlePlayerHUD, isSinglePlayer);
        SetHudActive(multiplayerHUD, true);
        if (multiplayerScorePanel != null)
            multiplayerScorePanel.SetActive(!isSinglePlayer);
        if (singlePlayerStarsBadge != null)
            singlePlayerStarsBadge.SetActive(isSinglePlayer);

        if (singlePlayerModeTitle != null)
            singlePlayerModeTitle.text = GameData.selectedModeTitle;
        if (singlePlayerModeDescription != null)
            singlePlayerModeDescription.text = GameData.selectedModeDescription;

        if (multiplayerModeTitle != null)
            multiplayerModeTitle.text = GameData.selectedModeTitle;
        if (multiplayerModeDescription != null)
            multiplayerModeDescription.text = GameData.selectedModeDescription;

        if (isSinglePlayer && playerOneRoot != null)
            playerOneRoot.gameObject.SetActive(false);

        RefreshScoreboard();
    }

    private void SetHudActive(GameObject hud, bool active)
    {
        if (hud != null && hud != gameObject)
            hud.SetActive(active);
    }

    private void SetCardState(RectTransform root, bool active)
    {
        root.localScale = Vector3.one * (active ? activeScale : inactiveScale);
        CanvasGroup cg = root.GetComponent<CanvasGroup>();
        if (cg == null) cg = root.gameObject.AddComponent<CanvasGroup>();
        cg.alpha = active ? 1f : inactiveAlpha;
    }

    private void SetPlayerState(RectTransform root, Image star, TMP_Text text, bool active)
    {
        root.localScale = Vector3.one * (active ? activeScale : inactiveScale);
        SetAlpha(star, active ? 1f : inactiveAlpha);
        SetAlpha(text, active ? 1f : inactiveAlpha);
    }

    private static void SetAlpha(Graphic graphic, float alpha)
    {
        if (graphic == null) return;
        Color color = graphic.color;
        color.a = alpha;
        graphic.color = color;
    }
}
