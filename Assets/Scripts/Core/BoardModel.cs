using System;
using System.Collections.Generic;

namespace ConnectFour.Core
{
    public sealed class BoardModel
    {
        public const int Columns = 7;
        public const int Rows = 6;
        public const int ConnectLength = 4;

        private readonly Piece[,] pieces = new Piece[Columns, Rows];
        private readonly List<PieceMovement> pendingMovements = new List<PieceMovement>();
        private bool pendingTurnSwitch;

        public BoardState State { get; private set; } = BoardState.WaitingForInput;
        public GravityDirection Gravity { get; private set; } = GravityDirection.Down;
        public int ActivePlayerId { get; private set; } = 1;
        public int? WinnerId { get; private set; }
        public bool IsDraw { get; private set; }

        public Piece GetPiece(GridPosition position)
        {
            return IsInside(position) ? pieces[position.Column, position.Row] : null;
        }

        public TurnResult DropPiece(int line)
        {
            if (State != BoardState.WaitingForInput)
                return TurnResult.Rejected("The board is not waiting for input.", State);

            if (!TryFindDropPosition(line, out GridPosition target))
                return TurnResult.Rejected("The selected line is full or invalid.", State);

            Piece piece = new RegularPiece(ActivePlayerId);
            pieces[target.Column, target.Row] = piece;
            pendingMovements.Clear();
            pendingMovements.Add(new PieceMovement(piece, GetSpawnPosition(line), target));
            pendingTurnSwitch = true;
            State = BoardState.Animating;
            return TurnResult.Accepted(new List<PieceMovement>(pendingMovements), State);
        }

        public TurnResult RotateGravity(GravityDirection direction)
        {
            if (State != BoardState.WaitingForInput)
                return TurnResult.Rejected("The board is not waiting for input.", State);

            Gravity = direction;
            pendingMovements.Clear();
            CompactBoard();
            pendingTurnSwitch = false;
            State = BoardState.Animating;
            return TurnResult.Accepted(new List<PieceMovement>(pendingMovements), State);
        }

        public TurnResult CompleteAnimation()
        {
            if (State != BoardState.Animating)
                return TurnResult.Rejected("There is no animation to complete.", State);

            foreach (PieceMovement movement in pendingMovements)
                movement.Piece.OnLanded(this, movement.To);

            WinnerId = FindWinnerId();
            IsDraw = WinnerId == null && IsFull();
            TurnOutcome outcome = WinnerId.HasValue ? TurnOutcome.Win : IsDraw ? TurnOutcome.Draw : TurnOutcome.None;
            State = outcome == TurnOutcome.None ? BoardState.WaitingForInput : BoardState.GameOver;
            pendingMovements.Clear();

            TurnResult result = TurnResult.Accepted(new List<PieceMovement>(), State);
            result.Outcome = outcome;
            result.WinnerId = WinnerId;
            if (outcome == TurnOutcome.None && pendingTurnSwitch)
                ActivePlayerId = ActivePlayerId == 1 ? 2 : 1;
            pendingTurnSwitch = false;
            return result;
        }

        private void CompactBoard()
        {
            Piece[,] compacted = new Piece[Columns, Rows];
            switch (Gravity)
            {
                case GravityDirection.Down:
                    for (int column = 0; column < Columns; column++) CompactLine(column, 0, 0, 1, compacted);
                    break;
                case GravityDirection.Up:
                    for (int column = 0; column < Columns; column++) CompactLine(column, Rows - 1, 0, -1, compacted);
                    break;
                case GravityDirection.Left:
                    for (int row = 0; row < Rows; row++) CompactLine(0, row, 1, 0, compacted);
                    break;
                case GravityDirection.Right:
                    for (int row = 0; row < Rows; row++) CompactLine(Columns - 1, row, -1, 0, compacted);
                    break;
            }

            Array.Copy(compacted, pieces, compacted.Length);
        }

        private void CompactLine(int startColumn, int startRow, int columnStep, int rowStep, Piece[,] compacted)
        {
            int column = startColumn;
            int row = startRow;
            int writeColumn = startColumn;
            int writeRow = startRow;
            int lineLength = columnStep != 0 ? Columns : Rows;
            for (int i = 0; i < lineLength; i++)
            {
                Piece piece = pieces[column, row];
                if (piece != null)
                {
                    compacted[writeColumn, writeRow] = piece;
                    if (column != writeColumn || row != writeRow)
                        pendingMovements.Add(new PieceMovement(piece, new GridPosition(column, row), new GridPosition(writeColumn, writeRow)));
                    writeColumn += columnStep;
                    writeRow += rowStep;
                }
                column += columnStep;
                row += rowStep;
            }
        }

        private int? FindWinnerId()
        {
            bool activeWins = HasWinningLine(ActivePlayerId);
            if (activeWins) return ActivePlayerId;
            for (int player = 1; player <= 2; player++)
                if (player != ActivePlayerId && HasWinningLine(player)) return player;
            return null;
        }

        private bool HasWinningLine(int ownerId)
        {
            int[] columnSteps = { 1, 0, 1, 1 };
            int[] rowSteps = { 0, 1, 1, -1 };
            for (int column = 0; column < Columns; column++)
            for (int row = 0; row < Rows; row++)
            {
                if (pieces[column, row]?.OwnerId != ownerId) continue;
                for (int direction = 0; direction < columnSteps.Length; direction++)
                {
                    int count = 1;
                    for (int distance = 1; distance < ConnectLength; distance++)
                    {
                        int nextColumn = column + columnSteps[direction] * distance;
                        int nextRow = row + rowSteps[direction] * distance;
                        if (!IsInside(new GridPosition(nextColumn, nextRow)) || pieces[nextColumn, nextRow]?.OwnerId != ownerId) break;
                        count++;
                    }
                    if (count == ConnectLength) return true;
                }
            }
            return false;
        }

        private bool TryFindDropPosition(int line, out GridPosition position)
        {
            position = default(GridPosition);
            if (Gravity == GravityDirection.Down || Gravity == GravityDirection.Up)
            {
                if (line < 0 || line >= Columns) return false;
                int start = Gravity == GravityDirection.Down ? 0 : Rows - 1;
                int step = Gravity == GravityDirection.Down ? 1 : -1;
                for (int row = start; row >= 0 && row < Rows; row += step)
                    if (pieces[line, row] == null) { position = new GridPosition(line, row); return true; }
            }
            else
            {
                if (line < 0 || line >= Rows) return false;
                int start = Gravity == GravityDirection.Left ? 0 : Columns - 1;
                int step = Gravity == GravityDirection.Left ? 1 : -1;
                for (int column = start; column >= 0 && column < Columns; column += step)
                    if (pieces[column, line] == null) { position = new GridPosition(column, line); return true; }
            }
            return false;
        }

        private GridPosition GetSpawnPosition(int line)
        {
            return Gravity == GravityDirection.Down ? new GridPosition(line, Rows) :
                Gravity == GravityDirection.Up ? new GridPosition(line, -1) :
                Gravity == GravityDirection.Left ? new GridPosition(Columns, line) : new GridPosition(-1, line);
        }

        private bool IsFull()
        {
            for (int column = 0; column < Columns; column++)
            for (int row = 0; row < Rows; row++)
                if (pieces[column, row] == null) return false;
            return true;
        }

        private static bool IsInside(GridPosition position) => position.Column >= 0 && position.Column < Columns && position.Row >= 0 && position.Row < Rows;
    }
}
