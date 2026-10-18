using ConnectFour.Core;
using UnityEngine;

public sealed class GameController : MonoBehaviour
{
    [SerializeField] private BoardView boardView;
    [SerializeField] private GravitySensorController gravitySensor;

    public BoardModel Model { get; private set; }
    public GravityDirection CurrentGravity => Model == null ? GravityDirection.Down : Model.Gravity;

    private bool hasQueuedGravity;
    private GravityDirection queuedGravity;

    private void Awake()
    {
        Model = new BoardModel();
        if (gravitySensor == null) gravitySensor = GetComponent<GravitySensorController>();
        if (gravitySensor != null) gravitySensor.GravityChanged += HandleGravityChanged;
    }

    private void OnDestroy()
    {
        if (gravitySensor != null) gravitySensor.GravityChanged -= HandleGravityChanged;
    }

    public void DropPiece(int line)
    {
        if (Model == null || boardView == null) return;
        TurnResult result = Model.DropPiece(line);
        if (result.Success) boardView.Animate(result.Movements, CompleteAnimation);
    }

    public void RotateGravity(GravityDirection direction)
    {
        if (Model == null || boardView == null) return;
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
        if (hasQueuedGravity && Model.State == BoardState.WaitingForInput)
        {
            hasQueuedGravity = false;
            RotateGravity(queuedGravity);
        }
    }
}
