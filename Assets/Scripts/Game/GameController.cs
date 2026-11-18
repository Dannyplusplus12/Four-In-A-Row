using ConnectFour.Core;
using System;
using System.Collections;
using UnityEngine;

public sealed class GameController : MonoBehaviour
{
    [SerializeField] private BoardView boardView;
    [SerializeField] private GravitySensorController gravitySensor;

    public BoardModel Model { get; private set; }
    public GravityDirection CurrentGravity => Model == null ? GravityDirection.Down : Model.Gravity;
    public bool IsPaused { get; private set; }
    public bool IsSinglePlayer => GameData.selectedMode == GameData.GameMode.OnePlayer;
    public bool IsBotTurn => IsSinglePlayer && Model != null && Model.ActivePlayerId == 1;
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

    private void Awake()
    {
        Model = new BoardModel(GameData.GetCoreRuleMode());
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
                boardView?.Rebuild(Model);
                boardView?.SetRotationQuarterTurns(Model.RotationQuarterTurns);
                TurnChanged?.Invoke(Model.ActivePlayerId);
            }
        }
        GameData.ClearResumeRequest();
        boardView?.SetRotationQuarterTurns(Model.RotationQuarterTurns);
        GravityChanged?.Invoke(Model.Gravity);
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
        if (IsPaused || isPresentingRuleEffect || IsBotTurn || Model == null || boardView == null) return;
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
            GravityChanged?.Invoke(Model.Gravity);
            boardView.Animate(result.Movements, CompleteAnimation);
        }
    }

    public void RemovePiece(Piece piece)
    {
        if (IsPaused || isPresentingRuleEffect || IsBotTurn || Model == null || boardView == null) return;
        TurnResult result = Model.RemovePiece(piece);
        if (result.Success) boardView.AnimateRuleEffects(result, CompleteAnimation);
    }

    private void HandleGravityChanged(GravityDirection direction)
    {
        if (GameData.selectedRuleMode != GameData.RuleMode.SwipeGravity) return;
        RotateGravity(direction);
    }

    private void CompleteAnimation()
    {
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
        TurnChanged?.Invoke(Model.ActivePlayerId);
        if (completed.Outcome != TurnOutcome.None)
            GameOver?.Invoke(completed);
        if (Model.State == BoardState.GameOver)
            GameData.ClearSavedGame();
        else
            GameData.SaveGame(Model.CreateSnapshot());
        if (hasQueuedGravity && Model.State == BoardState.WaitingForInput)
        {
            hasQueuedGravity = false;
            RotateGravity(queuedGravity);
        }
        BeginBotTurnIfNeeded();
    }

    public void SetPaused(bool paused)
    {
        IsPaused = paused;
        if (paused && Model != null && Model.State == BoardState.WaitingForInput)
            GameData.SaveGame(Model.CreateSnapshot());
    }

    public void StartNewGame()
    {
        GameData.BeginNewGame();
        IsPaused = false;
        Model = new BoardModel(GameData.GetCoreRuleMode());
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

    private void BeginBotTurnIfNeeded()
    {
        if (IsBotTurn && Model.State == BoardState.WaitingForInput)
            StartCoroutine(BotTurnRoutine());
    }

    private IEnumerator BotTurnRoutine()
    {
        yield return new WaitForSeconds(0.35f);
        if (IsPaused || !IsBotTurn || Model.State != BoardState.WaitingForInput) yield break;

        int lineCount = Model.Gravity == GravityDirection.Down || Model.Gravity == GravityDirection.Up
            ? BoardModel.Columns
            : BoardModel.Rows;
        int start = UnityEngine.Random.Range(0, lineCount);
        for (int offset = 0; offset < lineCount; offset++)
        {
            int line = (start + offset) % lineCount;
            TurnResult result = Model.DropPiece(line);
            if (!result.Success) continue;
            boardView.Animate(result.Movements, CompleteAnimation);
            yield break;
        }
    }
}
