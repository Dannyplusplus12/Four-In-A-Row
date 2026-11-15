using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PlayerTurnView : MonoBehaviour
{
    [SerializeField] private GameController gameController;

    [Header("HUD Roots")]
    [SerializeField] private GameObject singlePlayerHUD;
    [SerializeField] private GameObject multiplayerHUD;

    [Header("Single Player Mode Info")]
    [SerializeField] private TMP_Text singlePlayerModeTitle;
    [SerializeField] private TMP_Text singlePlayerModeDescription;
    [SerializeField] private TMP_Text singlePlayerTurnTitle;

    [Header("Player 1 UI")]
    [SerializeField] private RectTransform playerOneRoot;
    [SerializeField] private Image playerOneStar;
    [SerializeField] private TMP_Text playerOneText;

    [Header("Player 2 UI")]
    [SerializeField] private RectTransform playerTwoRoot;
    [SerializeField] private Image playerTwoStar;
    [SerializeField] private TMP_Text playerTwoText;

    [Header("Turn Appearance")]
    [SerializeField] private float activeScale = 1.12f;
    [SerializeField] private float inactiveScale = 0.88f;
    [SerializeField] private float inactiveAlpha = 0.35f;

    private void Awake()
    {
        if (gameController == null) gameController = FindFirstObjectByType<GameController>();
    }

    private void OnEnable()
    {
        if (gameController != null) gameController.TurnChanged += Refresh;
    }

    private void Start()
    {
        ConfigureHud();
        if (gameController != null) Refresh(gameController.Model.ActivePlayerId);
    }

    private void OnDisable()
    {
        if (gameController != null) gameController.TurnChanged -= Refresh;
    }

    private void Refresh(int activePlayerId)
    {
        bool isSinglePlayer = GameData.selectedMode == GameData.GameMode.OnePlayer;
        if (isSinglePlayer)
        {
            if (playerOneRoot != null) playerOneRoot.gameObject.SetActive(false);
            if (singlePlayerTurnTitle != null)
                singlePlayerTurnTitle.text = activePlayerId == 1 ? "Lượt của bạn" : "Lượt đối thủ";
        }

        if (!isSinglePlayer && playerOneRoot != null && playerOneStar != null && playerOneText != null)
        {
            playerOneText.text = GameData.player1Name;
            playerOneStar.sprite = GameData.player1StarSprite;
            SetPlayerState(playerOneRoot, playerOneStar, playerOneText, activePlayerId == 1);
        }

        if (playerTwoRoot != null && playerTwoStar != null && playerTwoText != null)
        {
            playerTwoText.text = GameData.player2Name;
            playerTwoStar.sprite = GameData.player2StarSprite;
            SetPlayerState(playerTwoRoot, playerTwoStar, playerTwoText, activePlayerId == 2);
        }
    }

    private void ConfigureHud()
    {
        bool isSinglePlayer = GameData.selectedMode == GameData.GameMode.OnePlayer;
        SetHudActive(singlePlayerHUD, isSinglePlayer);
        SetHudActive(multiplayerHUD, !isSinglePlayer);

        if (!isSinglePlayer) return;

        if (singlePlayerModeTitle != null)
            singlePlayerModeTitle.text = GameData.selectedModeTitle;
        if (singlePlayerModeDescription != null)
            singlePlayerModeDescription.text = GameData.selectedModeDescription;
        if (playerOneRoot != null) playerOneRoot.gameObject.SetActive(false);
    }

    private void SetHudActive(GameObject hud, bool active)
    {
        if (hud != null && hud != gameObject)
            hud.SetActive(active);
    }

    private void SetPlayerState(RectTransform root, Image star, TMP_Text text, bool active)
    {
        root.localScale = Vector3.one * (active ? activeScale : inactiveScale);
        SetAlpha(star, active ? 1f : inactiveAlpha);
        SetAlpha(text, active ? 1f : inactiveAlpha);
    }

    private static void SetAlpha(Graphic graphic, float alpha)
    {
        Color color = graphic.color;
        color.a = alpha;
        graphic.color = color;
    }

}