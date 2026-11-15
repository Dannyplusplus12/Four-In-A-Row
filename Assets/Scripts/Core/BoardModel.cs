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
        private readonly Queue<Piece>[] playerPieceQueues =
        {
            null,
            new Queue<Piece>(),
            new Queue<Piece>()
        };
        private readonly int[] playerTurnCounts = new int[3];
            private int totalTurnCount;
            private readonly System.Random random = new System.Random();
        private bool pendingTurnSwitch;
        private BoardRuleMode ruleMode;

        public BoardState State { get; private set; } = BoardState.WaitingForInput;
        public GravityDirection Gravity { get; private set; } = GravityDirection.Down;
        public int ActivePlayerId { get; private set; } = 1;
        public int? WinnerId { get; private set; }
        public bool IsDraw { get; private set; }
        public BoardRuleMode RuleMode => ruleMode;
        public int RotationQuarterTurns { get; private set; }

        public BoardModel() : this(BoardRuleMode.Classic)
        {
        }

        public BoardModel(BoardRuleMode ruleMode)
        {
            this.ruleMode = ruleMode;
        }

        public BoardSnapshot CreateSnapshot()
        {
            int[] ownerIds = new int[Columns * Rows];
            for (int column = 0; column < Columns; column++)
            for (int row = 0; row < Rows; row++)
                ownerIds[column * Rows + row] = pieces[column, row] == null ? 0 : pieces[column, row].Type == PieceType.Block ? -1 : pieces[column, row].OwnerId;

            return new BoardSnapshot(ownerIds, Gravity, ActivePlayerId);
        }

        public bool RestoreSnapshot(BoardSnapshot snapshot)
        {
            if (snapshot == null || snapshot.OwnerIds == null || snapshot.OwnerIds.Length != Columns * Rows)
                return false;
            if (snapshot.Gravity < GravityDirection.Down || snapshot.Gravity > GravityDirection.Right)
                return false;

            for (int column = 0; column < Columns; column++)
            for (int row = 0; row < Rows; row++)
            {
                int ownerId = snapshot.OwnerIds[column * Rows + row];
                if (ownerId != 0 && ownerId != 1 && ownerId != 2) return false;
                if (ownerId < -1 || ownerId > 2) return false;
                pieces[column, row] = ownerId == 0 ? null : ownerId == -1 ? new BlockPiece() : new RegularPiece(ownerId);
            }

            Gravity = snapshot.Gravity;
            ActivePlayerId = snapshot.ActivePlayerId == 2 ? 2 : 1;
            RotationQuarterTurns = 0;
            WinnerId = null;
            IsDraw = false;
            playerPieceQueues[1].Clear();
            playerPieceQueues[2].Clear();
            playerTurnCounts[1] = 0;
            playerTurnCounts[2] = 0;
            totalTurnCount = 0;
            for (int column = 0; column < Columns; column++)
            for (int row = 0; row < Rows; row++)
            {
                Piece piece = pieces[column, row];
                if (piece == null) continue;
                playerPieceQueues[piece.OwnerId].Enqueue(piece);
            }
            pendingMovements.Clear();
            pendingTurnSwitch = false;
            State = BoardState.WaitingForInput;
            return true;
        }

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
            playerPieceQueues[ActivePlayerId].Enqueue(piece);
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
            pendingTurnSwitch = ruleMode == BoardRuleMode.SwipeGravity;
            State = BoardState.Animating;
            return TurnResult.Accepted(new List<PieceMovement>(pendingMovements), State);
        }

        public TurnResult RemovePiece(Piece piece)
        {
            if (ruleMode != BoardRuleMode.DestroyStarOnTap || State != BoardState.WaitingForInput)
                return TurnResult.Rejected("This rule does not allow removing pieces.", State);
            if (piece == null || piece.Type != PieceType.Regular || !TryFindPiece(piece, out GridPosition position))
                return TurnResult.Rejected("The selected piece cannot be removed.", State);

            pieces[position.Column, position.Row] = null;
            pendingMovements.Clear();
            pendingRemovedPieces.Clear();
            pendingRemovedPieces.Add(piece);
            pendingTurnSwitch = true;
            pendingRequiresRebuild = true;
            State = BoardState.Animating;
            return TurnResult.Accepted(new List<PieceMovement>(), State);
        }

        public TurnResult CompleteAnimation()
        {
            if (State != BoardState.Animating)
                return TurnResult.Rejected("There is no animation to complete.", State);

            foreach (PieceMovement movement in pendingMovements)
                movement.Piece.OnLanded(this, movement.To);

            pendingMovements.Clear();

            if (pendingTurnSwitch)
            {
                playerTurnCounts[ActivePlayerId]++;
                totalTurnCount++;
                ApplyTurnRuleEffects();
            }

            WinnerId = FindWinnerId();
            IsDraw = WinnerId == null && IsFull();
            TurnOutcome outcome = WinnerId.HasValue ? TurnOutcome.Win : IsDraw ? TurnOutcome.Draw : TurnOutcome.None;
            State = outcome == TurnOutcome.None ? BoardState.WaitingForInput : BoardState.GameOver;
            TurnResult result = TurnResult.Accepted(new List<PieceMovement>(pendingMovements), State);
            result.Outcome = outcome;
            result.WinnerId = WinnerId;
            result.RemovedPieces.AddRange(pendingRemovedPieces);
            result.RequiresRebuild = pendingRequiresRebuild;
            result.RequiresRotation = pendingRequiresRotation;
            result.RotationQuarterTurns = RotationQuarterTurns;
            if (outcome == TurnOutcome.None && pendingTurnSwitch)
                ActivePlayerId = ActivePlayerId == 1 ? 2 : 1;
            pendingTurnSwitch = false;
            pendingRemovedPieces.Clear();
            pendingRequiresRebuild = false;
            pendingRequiresRotation = false;
            return result;
        }

        private readonly List<Piece> pendingRemovedPieces = new List<Piece>();
        private bool pendingRequiresRebuild;
        private bool pendingRequiresRotation;

        private void ApplyTurnRuleEffects()
        {
            if (ruleMode == BoardRuleMode.DecayAfterEightTurns && playerPieceQueues[ActivePlayerId].Count > 8)
                RemoveOldestPiece(ActivePlayerId);

            if (ruleMode == BoardRuleMode.RotateAfterTwoTurns &&
                ActivePlayerId == 2 && playerTurnCounts[1] >= 4 && playerTurnCounts[2] >= 4)
            {
                Gravity = NextClockwise(Gravity);
                CompactBoard();
                RotationQuarterTurns = (RotationQuarterTurns + 1) % 4;
                playerTurnCounts[1] = 0;
                playerTurnCounts[2] = 0;
                pendingRequiresRebuild = true;
                pendingRequiresRotation = true;
            }

            if (ruleMode == BoardRuleMode.BlockAfterFourTurns && totalTurnCount % 4 == 0)
                AddRandomBlock();
        }

        private void AddRandomBlock()
        {
            List<int> availableColumns = new List<int>();
            for (int column = 0; column < Columns; column++)
            {
                for (int row = 0; row < Rows; row++)
                {
                    if (pieces[column, row] == null)
                    {
                        availableColumns.Add(column);
                        break;
                    }
                }
            }
            if (availableColumns.Count == 0) return;

            int columnToBlock = availableColumns[random.Next(availableColumns.Count)];
            for (int row = 0; row < Rows; row++)
            {
                if (pieces[columnToBlock, row] != null) continue;
                pieces[columnToBlock, row] = new BlockPiece();
                break;
            }
            pendingRequiresRebuild = true;
        }

        private void RemoveOldestPiece(int ownerId)
        {
            Queue<Piece> queue = playerPieceQueues[ownerId];
            while (queue.Count > 0)
            {
                Piece candidate = queue.Dequeue();
                if (!TryFindPiece(candidate, out GridPosition position)) continue;
                pieces[position.Column, position.Row] = null;
                pendingRemovedPieces.Add(candidate);
                CompactBoard();
                pendingRequiresRebuild = true;
                return;
            }
        }

        private bool TryFindPiece(Piece piece, out GridPosition position)
        {
            for (int column = 0; column < Columns; column++)
            for (int row = 0; row < Rows; row++)
                if (pieces[column, row] == piece)
                {
                    position = new GridPosition(column, row);
                    return true;
                }
            position = default(GridPosition);
            return false;
        }

        private static GravityDirection NextClockwise(GravityDirection direction)
        {
            switch (direction)
            {
                case GravityDirection.Down: return GravityDirection.Left;
                case GravityDirection.Left: return GravityDirection.Up;
                case GravityDirection.Up: return GravityDirection.Right;
                default: return GravityDirection.Down;
            }
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
            int lineLength = columnStep != 0 ? Columns : Rows;
            GridPosition[] positions = new GridPosition[lineLength];
            for (int index = 0; index < lineLength; index++)
            {
                positions[index] = new GridPosition(startColumn + columnStep * index, startRow + rowStep * index);
            }

            int segmentStart = 0;
            for (int index = 0; index <= lineLength; index++)
            {
                bool isBlock = index < lineLength && pieces[positions[index].Column, positions[index].Row]?.Type == PieceType.Block;
                if (isBlock)
                {
                    CompactSegment(positions, segmentStart, index, compacted);
                    GridPosition blockPosition = positions[index];
                    compacted[blockPosition.Column, blockPosition.Row] = pieces[blockPosition.Column, blockPosition.Row];
                    segmentStart = index + 1;
                }
            }

            CompactSegment(positions, segmentStart, lineLength, compacted);
        }

        private void CompactSegment(GridPosition[] positions, int start, int end, Piece[,] compacted)
        {
            List<Piece> movablePieces = new List<Piece>();
            for (int index = start; index < end; index++)
            {
                GridPosition position = positions[index];
                Piece piece = pieces[position.Column, position.Row];
                if (piece != null) movablePieces.Add(piece);
            }

            for (int index = 0; index < movablePieces.Count; index++)
            {
                Piece piece = movablePieces[index];
                GridPosition from = FindPiecePosition(positions, start, end, piece);
                GridPosition to = positions[start + index];
                compacted[to.Column, to.Row] = piece;
                if (from != to)
                    pendingMovements.Add(new PieceMovement(piece, from, to));
            }
        }

        private GridPosition FindPiecePosition(GridPosition[] positions, int start, int end, Piece piece)
        {
            for (int index = start; index < end; index++)
            {
                GridPosition position = positions[index];
                if (pieces[position.Column, position.Row] == piece) return position;
            }
            return default(GridPosition);
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
                    int winningLength = ruleMode == BoardRuleMode.FiveStarWin ? 5 : ConnectLength;
                    for (int distance = 1; distance < winningLength; distance++)
                    {
                        int nextColumn = column + columnSteps[direction] * distance;
                        int nextRow = row + rowSteps[direction] * distance;
                        if (!IsInside(new GridPosition(nextColumn, nextRow)) || pieces[nextColumn, nextRow]?.OwnerId != ownerId) break;
                        count++;
                    }
                    if (count == winningLength) return true;
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
