using ConnectFour.Core;
using TMPro;
using UnityEngine;

public sealed class RuleInstructionView : MonoBehaviour
{
    [SerializeField] private GameController gameController;
    [SerializeField] private TMP_Text instructionText;

    private void Awake()
    {
        if (gameController == null) gameController = FindFirstObjectByType<GameController>();
    }

    private void OnEnable()
    {
        if (gameController != null)
        {
            gameController.RuleInstructionChanged += Refresh;
            gameController.TurnChanged += HandleTurnOrGravityChanged;
            gameController.GravityChanged += HandleGravityChanged;
        }
    }

    private void Start()
    {
        Refresh(gameController == null ? string.Empty : gameController.CurrentRuleInstruction);
    }

    private void OnDisable()
    {
        if (gameController != null)
        {
            gameController.RuleInstructionChanged -= Refresh;
            gameController.TurnChanged -= HandleTurnOrGravityChanged;
            gameController.GravityChanged -= HandleGravityChanged;
        }
    }

    private void HandleTurnOrGravityChanged(int _)
    {
        Refresh(gameController == null ? string.Empty : gameController.CurrentRuleInstruction);
    }

    private void HandleGravityChanged(GravityDirection _)
    {
        Refresh(gameController == null ? string.Empty : gameController.CurrentRuleInstruction);
    }

    private void Refresh(string instruction)
    {
        if (instructionText == null) return;
        bool visible = gameController != null && !string.IsNullOrEmpty(instruction);
        instructionText.gameObject.SetActive(visible);
        if (!visible) return;

        string suffix = BuildTelemetrySuffix();
        instructionText.text = string.IsNullOrEmpty(suffix) ? instruction : $"{instruction}\n{suffix}";
    }

    private string BuildTelemetrySuffix()
    {
        if (gameController == null || gameController.Model == null) return string.Empty;
        BoardModel model = gameController.Model;

        switch (model.RuleMode)
        {
            case BoardRuleMode.SwipeGravity:
                return $"Gravity: {model.Gravity}";
            case BoardRuleMode.RotateAfterTwoTurns:
                int p1Left = Mathf.Max(0, 4 - model.GetPlayerTurnCount(1));
                int p2Left = Mathf.Max(0, 4 - model.GetPlayerTurnCount(2));
                int turnsLeft = p1Left + p2Left;
                return turnsLeft > 0 ? $"Rotates in {turnsLeft} turn(s)" : "Rotating next!";
            case BoardRuleMode.BlockAfterFourTurns:
                int untilBlock = 4 - (model.TotalTurnCount % 4);
                return $"Next block in {untilBlock} turn(s)";
            default:
                return string.Empty;
        }
    }
}
