using ConnectFour.Core;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public sealed class GameController : MonoBehaviour
{
    [SerializeField] private BoardView boardView;
    [SerializeField] private GravitySensorController gravitySensor;

    public BoardModel Model { get; private set; }
    public GravityDirection CurrentGravity => Model == null ? GravityDirection.Down : Model.Gravity;
    public bool IsPaused { get; private set; }
    public bool IsSinglePlayer => GameData.selectedMode == GameData.GameMode.OnePlayer;
    // Player 1 is Human, Player 2 is Bot in 1-Player mode.
    public bool IsBotTurn => IsSinglePlayer && Model != null && Model.ActivePlayerId == 2;
    public bool CanAcceptPlayerInput => !IsPaused && !isPresentingRuleEffect && !IsBotTurn &&
                                        Model != null && Model.State == BoardState.WaitingForInput && boardView != null;
    public string CurrentRuleInstruction => GameData.GetRuleInstruction();
    public event Action<int> TurnChanged;
    public event Action<GravityDirection> GravityChanged;
    public event Action<string> RuleInstructionChanged;
    public event Action<TurnResult> GameOver;

    private bool hasQueuedGravity;
    private GravityDirection queuedGravity;
    private bool isPresentingRuleEffect;
    private Coroutine botTurnCoroutine;

    private void Awake()
    {
        if (boardView == null) boardView = FindFirstObjectByType<BoardView>();
        Model = new BoardModel(GameData.GetCoreRuleMode(), GameData.ResolveStartingPlayerId());
        if (gravitySensor == null) gravitySensor = GetComponent<GravitySensorController>();
        if (gravitySensor != null) gravitySensor.GravityChanged += HandleGravityChanged;
        if (GameData.selectedRuleMode == GameData.RuleMode.SwipeGravity &&
            GetComponent<SwipeInputController>() == null)
            gameObject.AddComponent<SwipeInputController>();
    }

    private void Start()
    {
        if (GameData.ResumeRequested)
        {
            BoardSnapshot snapshot = GameData.LoadSavedGame();
            if (snapshot != null && Model.RestoreSnapshot(snapshot))
            {
                if (GameData.selectedRuleMode == GameData.RuleMode.SwipeGravity &&
                    GetComponent<SwipeInputController>() == null)
                    gameObject.AddComponent<SwipeInputController>();

                boardView?.Rebuild(Model);
                boardView?.SetRotationQuarterTurns(Model.RotationQuarterTurns);
            }
        }
        GameData.ClearResumeRequest();
        boardView?.SetRotationQuarterTurns(Model.RotationQuarterTurns);
        GravityChanged?.Invoke(Model.Gravity);
        TurnChanged?.Invoke(Model.ActivePlayerId);
        RuleInstructionChanged?.Invoke(CurrentRuleInstruction);
        BeginBotTurnIfNeeded();
    }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.W))
            TriggerDebugGameOver(true);
        else if (Input.GetKeyDown(KeyCode.L))
            TriggerDebugGameOver(false);
    }

    private void TriggerDebugGameOver(bool playerWon)
    {
        TurnResult result = TurnResult.DebugGameOver(playerWon);
        Debug.Log(playerWon ? "Debug game over: WIN (W)" : "Debug game over: LOSS (L)");
        GameOver?.Invoke(result);
    }
#endif

    private void OnDestroy()
    {
        if (gravitySensor != null) gravitySensor.GravityChanged -= HandleGravityChanged;
    }

    public void DropPiece(int line)
    {
        if (!CanAcceptPlayerInput) return;
        TurnResult result = Model.DropPiece(line);
        if (result.Success) boardView.Animate(result.Movements, CompleteAnimation);
    }

    public void RotateGravity(GravityDirection direction)
    {
        if (IsPaused || isPresentingRuleEffect || IsBotTurn || Model == null || boardView == null ||
            GameData.selectedRuleMode != GameData.RuleMode.SwipeGravity) return;
        if (Model.State == BoardState.Animating)
        {
            queuedGravity = direction;
            hasQueuedGravity = true;
            return;
        }
        TurnResult result = Model.RotateGravity(direction);
        if (result.Success)
        {
            SoundManager.PlayRotate();
            GravityChanged?.Invoke(Model.Gravity);
            boardView.Animate(result.Movements, CompleteAnimation);
        }
    }

    public void RemovePiece(Piece piece)
    {
        if (!CanAcceptPlayerInput) return;
        TurnResult result = Model.RemovePiece(piece);
        if (result.Success)
            boardView.AnimateRuleEffects(result, CompleteAnimation);
    }

    private void HandleGravityChanged(GravityDirection direction)
    {
        if (GameData.selectedRuleMode != GameData.RuleMode.SwipeGravity) return;
        RotateGravity(direction);
    }

    private void CompleteAnimation()
    {
        if (Model == null) return;
        TurnResult completed = Model.CompleteAnimation();
        if (completed.RequiresRebuild)
        {
            isPresentingRuleEffect = true;
            boardView.AnimateRuleEffects(completed, () => FinishCompletedTurn(completed));
            return;
        }
        FinishCompletedTurn(completed);
    }

    private void FinishCompletedTurn(TurnResult completed)
    {
        if (completed.RequiresRebuild)
        {
            boardView.Rebuild(Model);
            boardView.SetRotationQuarterTurns(Model.RotationQuarterTurns);
            if (completed.RequiresRotation)
                GravityChanged?.Invoke(Model.Gravity);
            isPresentingRuleEffect = false;
        }

        if (completed.Outcome == TurnOutcome.Win && completed.WinningPositions.Count > 0)
            boardView?.HighlightWinningPieces(completed.WinningPositions, Model);

        TurnChanged?.Invoke(Model.ActivePlayerId);
        RuleInstructionChanged?.Invoke(CurrentRuleInstruction);

        if (completed.Outcome != TurnOutcome.None)
            GameOver?.Invoke(completed);

        if (Model.State == BoardState.GameOver)
            GameData.ClearSavedGame();
        else
            GameData.SaveGame(Model.CreateSnapshot());

        if (hasQueuedGravity && Model.State == BoardState.WaitingForInput && !IsBotTurn)
        {
            hasQueuedGravity = false;
            RotateGravity(queuedGravity);
            return;
        }
        hasQueuedGravity = false;
        BeginBotTurnIfNeeded();
    }

    public void SetPaused(bool paused)
    {
        IsPaused = paused;
        if (paused && Model != null && Model.State == BoardState.WaitingForInput)
            GameData.SaveGame(Model.CreateSnapshot());
        else if (!paused)
            BeginBotTurnIfNeeded();
    }

    public void StartNewGame()
    {
        StopBotTurn();
        boardView?.StopAnimations();
        hasQueuedGravity = false;
        isPresentingRuleEffect = false;
        GameData.BeginNewGame();
        IsPaused = false;
        Model = new BoardModel(GameData.GetCoreRuleMode(), GameData.ResolveStartingPlayerId());
        boardView?.ClearPieces();
        boardView?.SetRotationQuarterTurns(0);
        GravityChanged?.Invoke(Model.Gravity);
        TurnChanged?.Invoke(Model.ActivePlayerId);
        RuleInstructionChanged?.Invoke(CurrentRuleInstruction);
        BeginBotTurnIfNeeded();
    }

    public void SaveProgress()
    {
        if (Model != null && Model.State == BoardState.WaitingForInput)
            GameData.SaveGame(Model.CreateSnapshot());
    }

    private void StopBotTurn()
    {
        if (botTurnCoroutine != null)
        {
            StopCoroutine(botTurnCoroutine);
            botTurnCoroutine = null;
        }
    }

    private void BeginBotTurnIfNeeded()
    {
        if (botTurnCoroutine != null) return;
        if (!IsPaused && !isPresentingRuleEffect && IsBotTurn && Model != null && Model.State == BoardState.WaitingForInput)
            botTurnCoroutine = StartCoroutine(BotTurnRoutine());
    }

    private IEnumerator BotTurnRoutine()
    {
        yield return new WaitForSeconds(0.45f);
        botTurnCoroutine = null;
        if (IsPaused || isPresentingRuleEffect || !IsBotTurn || Model == null || Model.State != BoardState.WaitingForInput)
            yield break;

        int chosenLine = ChooseBotLine();
        if (chosenLine < 0) yield break;

        TurnResult result = Model.DropPiece(chosenLine);
        if (result.Success)
            boardView.Animate(result.Movements, CompleteAnimation);
    }

    private int ChooseBotLine()
    {
        int lineCount = Model.Gravity == GravityDirection.Down || Model.Gravity == GravityDirection.Up
            ? BoardModel.Columns
            : BoardModel.Rows;

        List<int> validLines = new List<int>(lineCount);
        for (int line = 0; line < lineCount; line++)
        {
            if (Model.CanDropInLine(line))
                validLines.Add(line);
        }
        if (validLines.Count == 0) return -1;

        int difficulty = Mathf.Clamp(GameData.difficultyLevel, 1, 3);
        bool evaluateTactics = difficulty >= 2 || UnityEngine.Random.value < 0.35f;

        if (evaluateTactics)
        {
            // 1. Take immediate winning move for Bot (Player 2)
            int winLine = FindImmediateWinningLine(validLines, 2);
            if (winLine >= 0) return winLine;

            // 2. Block immediate winning move for Human (Player 1)
            int blockLine = FindImmediateWinningLine(validLines, 1);
            if (blockLine >= 0) return blockLine;
        }

        if (difficulty >= 3)
        {
            // 3. Hard: filter out moves that immediately hand the Human a win on the next turn
            List<int> safeLines = new List<int>(validLines.Count);
            foreach (int line in validLines)
            {
                if (!GivesOpponentImmediateWin(line))
                    safeLines.Add(line);
            }
            if (safeLines.Count > 0)
                validLines = safeLines;
        }

        if (difficulty >= 2)
        {
            // Prefer lines closer to the center
            float center = (lineCount - 1) * 0.5f;
            validLines.Sort((a, b) => Mathf.Abs(a - center).CompareTo(Mathf.Abs(b - center)));
            int topChoices = Mathf.Min(difficulty == 3 ? 2 : 3, validLines.Count);
            return validLines[UnityEngine.Random.Range(0, topChoices)];
        }

        return validLines[UnityEngine.Random.Range(0, validLines.Count)];
    }

    private int FindImmediateWinningLine(List<int> validLines, int playerId)
    {
        BoardSnapshot snapshot = Model.CreateSnapshot();
        foreach (int line in validLines)
        {
            BoardModel sim = new BoardModel(Model.RuleMode);
            if (!sim.RestoreSnapshot(snapshot)) continue;
            // Temporarily set active player to test their move
            BoardSnapshot playerSnapshot = new BoardSnapshot(
                snapshot.OwnerIds,
                snapshot.Gravity,
                playerId,
                snapshot.RuleMode,
                snapshot.RotationQuarterTurns,
                snapshot.TotalTurnCount,
                snapshot.Player1TurnCount,
                snapshot.Player2TurnCount,
                snapshot.PieceOrder);
            if (!sim.RestoreSnapshot(playerSnapshot)) continue;
            TurnResult drop = sim.DropPiece(line);
            if (!drop.Success) continue;
            TurnResult done = sim.CompleteAnimation();
            if (done.Outcome == TurnOutcome.Win && done.WinnerId == playerId)
                return line;
        }
        return -1;
    }

    private bool GivesOpponentImmediateWin(int botLine)
    {
        BoardSnapshot snapshot = Model.CreateSnapshot();
        BoardModel afterBot = new BoardModel(Model.RuleMode);
        if (!afterBot.RestoreSnapshot(snapshot)) return false;
        TurnResult botDrop = afterBot.DropPiece(botLine);
        if (!botDrop.Success) return false;
        TurnResult botDone = afterBot.CompleteAnimation();
        if (botDone.Outcome != TurnOutcome.None) return false;

        int lineCount = afterBot.Gravity == GravityDirection.Down || afterBot.Gravity == GravityDirection.Up
            ? BoardModel.Columns
            : BoardModel.Rows;
        BoardSnapshot afterBotSnap = afterBot.CreateSnapshot();
        for (int line = 0; line < lineCount; line++)
        {
            if (!afterBot.CanDropInLine(line)) continue;
            BoardModel afterHuman = new BoardModel(afterBot.RuleMode);
            if (!afterHuman.RestoreSnapshot(afterBotSnap)) continue;
            TurnResult humanDrop = afterHuman.DropPiece(line);
            if (!humanDrop.Success) continue;
            TurnResult humanDone = afterHuman.CompleteAnimation();
            if (humanDone.Outcome == TurnOutcome.Win && humanDone.WinnerId == 1)
                return true;
        }
        return false;
    }
}
