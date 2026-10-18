using System;

namespace ConnectFour.Core
{
    public abstract class Piece
    {
        public Guid Id { get; }
        public int OwnerId { get; }
        public PieceType Type { get; }

        protected Piece(int ownerId, PieceType type)
        {
            Id = Guid.NewGuid();
            OwnerId = ownerId;
            Type = type;
        }

        public virtual void OnLanded(BoardModel board, GridPosition position)
        {
        }
    }

    public sealed class RegularPiece : Piece
    {
        public RegularPiece(int ownerId) : base(ownerId, PieceType.Regular)
        {
        }
    }
}
