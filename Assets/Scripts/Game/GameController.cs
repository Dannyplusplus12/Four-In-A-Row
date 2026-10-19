using ConnectFour.Core;
using System;
using UnityEngine;

public sealed class GameController : MonoBehaviour
{
    [SerializeField] private BoardView boardView;
    [SerializeField] private GravitySensorController gravitySensor;

    public BoardModel Model { get; private set; }
    public GravityDirection CurrentGravity => Model == null ? GravityDirection.Down : Model.Gravity;
    public bool IsPaused { get; private set; }
    public event Action<int> TurnChanged;

    private bool hasQueuedGravity;
    private GravityDirection queuedGravity;

    private void Awake()
    {
        Model = new BoardModel();
        if (gravitySensor == null) gravitySensor = GetComponent<GravitySensorController>();
        if (gravitySensor != null) gravitySensor.GravityChanged += HandleGravityChanged;
    }

    private void Start()
    {
        if (!GameData.ResumeRequested) return;

        BoardSnapshot snapshot = GameData.LoadSavedGame();
        if (snapshot != null && Model.RestoreSnapshot(snapshot))
        {
            boardView?.Rebuild(Model);
            TurnChanged?.Invoke(Model.ActivePlayerId);
        }
        GameData.ClearResumeRequest();
    }

    private void OnDestroy()
    {
        if (gravitySensor != null) gravitySensor.GravityChanged -= HandleGravityChanged;
    }

    public void DropPiece(int line)
    {
        if (IsPaused || Model == null || boardView == null) return;
        TurnResult result = Model.DropPiece(line);
        if (result.Success) boardView.Animate(result.Movements, CompleteAnimation);
    }

    public void RotateGravity(GravityDirection direction)
    {
        if (IsPaused || Model == null || boardView == null) return;
        if (Model.State == BoardState.Animating)
        {
            queuedGravity = direction;
            hasQueuedGravity = true;
            return;
        }
        TurnResult result = Model.RotateGravity(direction);
        if (result.Success) boardView.Animate(result.Movements, CompleteAnimation);
    }

    private void HandleGravityChanged(GravityDirection direction)
    {
        RotateGravity(direction);
    }

    private void CompleteAnimation()
    {
        Model.CompleteAnimation();
        TurnChanged?.Invoke(Model.ActivePlayerId);
        if (Model.State == BoardState.GameOver)
            GameData.ClearSavedGame();
        else
            GameData.SaveGame(Model.CreateSnapshot());
        if (hasQueuedGravity && Model.State == BoardState.WaitingForInput)
        {
            hasQueuedGravity = false;
            RotateGravity(queuedGravity);
        }
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
        Model = new BoardModel();
        boardView?.ClearPieces();
        TurnChanged?.Invoke(Model.ActivePlayerId);
    }

    public void SaveProgress()
    {
        if (Model != null && Model.State == BoardState.WaitingForInput)
            GameData.SaveGame(Model.CreateSnapshot());
    }
}
