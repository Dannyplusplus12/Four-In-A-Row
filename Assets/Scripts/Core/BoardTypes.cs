using System;
using System.Collections.Generic;

namespace ConnectFour.Core
{
    public enum BoardState
    {
        WaitingForInput,
        Animating,
        GameOver
    }

    public enum GravityDirection
    {
        Down,
        Left,
        Up,
        Right
    }

    public enum PieceType
    {
        Regular
    }

    public enum TurnOutcome
    {
        None,
        Win,
        Draw
    }

    public readonly struct GridPosition : IEquatable<GridPosition>
    {
        public readonly int Column;
        public readonly int Row;

        public GridPosition(int column, int row)
        {
            Column = column;
            Row = row;
        }

        public bool Equals(GridPosition other) => Column == other.Column && Row == other.Row;
        public override bool Equals(object obj) => obj is GridPosition other && Equals(other);
        public override int GetHashCode() => (Column * 397) ^ Row;
        public override string ToString() => $"({Column}, {Row})";
        public static bool operator ==(GridPosition left, GridPosition right) => left.Equals(right);
        public static bool operator !=(GridPosition left, GridPosition right) => !left.Equals(right);
    }

    public sealed class PieceMovement
    {
        public Piece Piece { get; }
        public GridPosition From { get; }
        public GridPosition To { get; }

        public PieceMovement(Piece piece, GridPosition from, GridPosition to)
        {
            Piece = piece ?? throw new ArgumentNullException(nameof(piece));
            From = from;
            To = to;
        }
    }

    public sealed class TurnResult
    {
        public bool Success { get; }
        public string FailureReason { get; }
        public List<PieceMovement> Movements { get; }
        public BoardState State { get; internal set; }
        public TurnOutcome Outcome { get; internal set; }
        public int? WinnerId { get; internal set; }

        private TurnResult(bool success, string failureReason, List<PieceMovement> movements)
        {
            Success = success;
            FailureReason = failureReason;
            Movements = movements ?? new List<PieceMovement>();
        }

        public static TurnResult Accepted(List<PieceMovement> movements, BoardState state)
        {
            return new TurnResult(true, null, movements) { State = state };
        }

        public static TurnResult Rejected(string reason, BoardState state)
        {
            return new TurnResult(false, reason, new List<PieceMovement>()) { State = state };
        }
    }
}
