using TMPro;
using UnityEngine;

public sealed class RuleInstructionView : MonoBehaviour
{
    [SerializeField] private GameController gameController;
    [SerializeField] private TMP_Text instructionText;

    private void Awake()
    {
        if (gameController == null) gameController = FindFirstObjectByType<GameController>();
        if (instructionText == null) instructionText = CreateRuntimeLabel();
    }

    private void OnEnable()
    {
        if (gameController != null)
        {
            gameController.RuleInstructionChanged += Refresh;
            gameController.TurnChanged += HandleTurnChanged;
        }
    }

    private void Start()
    {
        Refresh(gameController == null ? string.Empty : gameController.CurrentRuleInstruction);
        HandleTurnChanged(gameController == null ? 1 : gameController.Model.ActivePlayerId);
    }

    private void OnDisable()
    {
        if (gameController != null)
        {
            gameController.RuleInstructionChanged -= Refresh;
            gameController.TurnChanged -= HandleTurnChanged;
        }
    }

    private void Refresh(string instruction)
    {
        bool visible = gameController != null && gameController.IsSinglePlayer;
        if (instructionText != null)
        {
            instructionText.gameObject.SetActive(visible);
            instructionText.text = instruction;
        }
    }

    private void HandleTurnChanged(int activePlayerId)
    {
        if (instructionText == null || gameController == null || !gameController.IsSinglePlayer) return;
        instructionText.text = activePlayerId == 2
            ? "Bot is thinking..."
            : gameController.CurrentRuleInstruction;
    }

    private static TMP_Text CreateRuntimeLabel()
    {
        Canvas canvas = FindFirstObjectByType<Canvas>();
        if (canvas == null) return null;

        GameObject labelObject = new GameObject("RuleInstructionLabel");
        labelObject.transform.SetParent(canvas.transform, false);
        RectTransform rect = labelObject.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 1f);
        rect.anchorMax = new Vector2(0.5f, 1f);
        rect.pivot = new Vector2(0.5f, 1f);
        rect.anchoredPosition = new Vector2(0f, -32f);
        rect.sizeDelta = new Vector2(900f, 64f);

        TextMeshProUGUI text = labelObject.AddComponent<TextMeshProUGUI>();
        text.alignment = TextAlignmentOptions.Center;
        text.fontSize = 30f;
        text.color = Color.white;
        text.raycastTarget = false;
        return text;
    }
}