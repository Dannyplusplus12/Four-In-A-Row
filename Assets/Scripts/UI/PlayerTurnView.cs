using TMPro;
using UnityEngine;
using UnityEngine.UI;

public sealed class PlayerTurnView : MonoBehaviour
{
    [SerializeField] private GameController gameController;
    [SerializeField] private float activeScale = 1.12f;
    [SerializeField] private float inactiveScale = 0.88f;
    [SerializeField] private float inactiveAlpha = 0.35f;

    private RectTransform playerOneRoot;
    private RectTransform playerTwoRoot;
    private Image playerOneStar;
    private Image playerTwoStar;
    private TMP_Text playerOneText;
    private TMP_Text playerTwoText;

    private void Awake()
    {
        if (gameController == null) gameController = FindFirstObjectByType<GameController>();
        Canvas canvas = FindFirstObjectByType<Canvas>();
        if (canvas == null) return;

        RectTransform root = CreateRect("PlayerTurnHUD", canvas.transform);
        root.anchorMin = new Vector2(0, 1);
        root.anchorMax = new Vector2(1, 1);
        root.pivot = new Vector2(0.5f, 1);
        root.anchoredPosition = new Vector2(0, -24);
        root.sizeDelta = new Vector2(-48, 120);

        playerOneRoot = CreatePlayer(root, "PlayerOne", false, out playerOneStar, out playerOneText);
        playerTwoRoot = CreatePlayer(root, "PlayerTwo", true, out playerTwoStar, out playerTwoText);
        Refresh(1);
    }

    private void OnEnable()
    {
        if (gameController != null) gameController.TurnChanged += Refresh;
    }

    private void Start()
    {
        if (gameController != null) Refresh(gameController.Model.ActivePlayerId);
    }

    private void OnDisable()
    {
        if (gameController != null) gameController.TurnChanged -= Refresh;
    }

    private RectTransform CreatePlayer(RectTransform parent, string objectName, bool right,
        out Image star, out TMP_Text text)
    {
        RectTransform root = CreateRect(objectName, parent);
        root.anchorMin = new Vector2(right ? 0.5f : 0, 0);
        root.anchorMax = new Vector2(right ? 1 : 0.5f, 1);
        root.offsetMin = new Vector2(12, 0);
        root.offsetMax = new Vector2(-12, 0);

        GameObject starObject = new GameObject("Star", typeof(RectTransform), typeof(Image));
        starObject.transform.SetParent(root, false);
        RectTransform starRect = starObject.GetComponent<RectTransform>();
        starRect.anchorMin = new Vector2(right ? 1 : 0, 0.5f);
        starRect.anchorMax = starRect.anchorMin;
        starRect.pivot = new Vector2(0.5f, 0.5f);
        starRect.anchoredPosition = new Vector2(right ? -38 : 38, 0);
        starRect.sizeDelta = new Vector2(72, 72);
        star = starObject.GetComponent<Image>();
        star.preserveAspect = true;

        GameObject textObject = new GameObject("Name", typeof(RectTransform), typeof(TextMeshProUGUI));
        textObject.transform.SetParent(root, false);
        RectTransform textRect = textObject.GetComponent<RectTransform>();
        textRect.anchorMin = new Vector2(0, 0);
        textRect.anchorMax = new Vector2(1, 1);
        textRect.offsetMin = new Vector2(right ? 12 : 82, 0);
        textRect.offsetMax = new Vector2(right ? -82 : -12, 0);
        text = textObject.GetComponent<TMP_Text>();
        text.alignment = right ? TextAlignmentOptions.Right : TextAlignmentOptions.Left;
        text.fontSize = 34;
        text.enableAutoSizing = true;
        text.fontSizeMin = 16;
        text.fontSizeMax = 34;
        text.textWrappingMode = TextWrappingModes.NoWrap;
        return root;
    }

    private void Refresh(int activePlayerId)
    {
        if (playerOneRoot == null) return;
        playerOneText.text = GameData.player1Name;
        playerTwoText.text = GameData.player2Name;
        playerOneStar.sprite = GameData.player1StarSprite;
        playerTwoStar.sprite = GameData.player2StarSprite;
        SetPlayerState(playerOneRoot, playerOneStar, playerOneText, activePlayerId == 1);
        SetPlayerState(playerTwoRoot, playerTwoStar, playerTwoText, activePlayerId == 2);
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

    private static RectTransform CreateRect(string objectName, Transform parent)
    {
        GameObject obj = new GameObject(objectName, typeof(RectTransform));
        obj.transform.SetParent(parent, false);
        return obj.GetComponent<RectTransform>();
    }
}