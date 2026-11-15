using ConnectFour.Core;
using UnityEngine;
using UnityEngine.EventSystems;

public sealed class BoardPieceInput : MonoBehaviour, IPointerClickHandler
{
    private Piece piece;
    private GameController gameController;

    public void Initialize(Piece target, GameController controller)
    {
        piece = target;
        gameController = controller;
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (piece != null && gameController != null)
            gameController.RemovePiece(piece);
    }
}