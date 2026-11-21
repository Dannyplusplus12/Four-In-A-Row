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
        private readonly List<Piece> pendingRemovedPieces = new List<Piece>();
        private readonly List<GridPosition> winningPositions = new List<GridPosition>();
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
        private bool pendingRequiresRebuild;
        private bool pendingRequiresRotation;
        private BoardRuleMode ruleMode;

        public BoardState State { get; private set; } = BoardState.WaitingForInput;
        public GravityDirection Gravity { get; private set; } = GravityDirection.Down;
        public int ActivePlayerId { get; private set; } = 1;
        public int? WinnerId { get; private set; }
        public bool IsDraw { get; private set; }
        public BoardRuleMode RuleMode => ruleMode;
        public int RotationQuarterTurns { get; private set; }
        public int TotalTurnCount => totalTurnCount;
        public int WinningLength => ruleMode == BoardRuleMode.FiveStarWin ? 5 : ConnectLength;
        public IReadOnlyList<GridPosition> WinningPositions => winningPositions;

        public BoardModel() : this(BoardRuleMode.Classic, 1)
        {
        }

        public BoardModel(BoardRuleMode ruleMode) : this(ruleMode, 1)
        {
        }

        public BoardModel(BoardRuleMode ruleMode, int startingPlayerId)
        {
            this.ruleMode = ruleMode;
            ActivePlayerId = startingPlayerId == 2 ? 2 : 1;
        }

        public int GetPlayerTurnCount(int playerId)
        {
            return playerId == 1 || playerId == 2 ? playerTurnCounts[playerId] : 0;
        }

        public Piece GetOldestPiece(int ownerId)
        {
            if (ownerId != 1 && ownerId != 2) return null;
            foreach (Piece candidate in playerPieceQueues[ownerId])
            {
                if (TryFindPiece(candidate, out _))
                    return candidate;
            }
            return null;
        }

        public bool CanDropInLine(int line)
        {
            return TryFindDropPosition(line, out _);
        }

        public bool TryPreviewDrop(int line, out GridPosition target)
        {
            return TryFindDropPosition(line, out target);
        }

        public BoardSnapshot CreateSnapshot()
        {
            int[] ownerIds = new int[Columns * Rows];
            Dictionary<Piece, int> pieceIndexMap = new Dictionary<Piece, int>();
            for (int column = 0; column < Columns; column++)
            for (int row = 0; row < Rows; row++)
            {
                int index = column * Rows + row;
                Piece piece = pieces[column, row];
                ownerIds[index] = piece == null ? 0 : piece.Type == PieceType.Block ? -1 : piece.OwnerId;
                if (piece != null && piece.Type == PieceType.Regular)
                    pieceIndexMap[piece] = index;
            }

            List<int> pieceOrder = new List<int>();
            for (int player = 1; player <= 2; player++)
            {
                foreach (Piece queued in playerPieceQueues[player])
                {
                    if (pieceIndexMap.TryGetValue(queued, out int idx))
                        pieceOrder.Add(idx);
                }
            }

            return new BoardSnapshot(
                ownerIds,
                Gravity,
                ActivePlayerId,
                ruleMode,
                RotationQuarterTurns,
                totalTurnCount,
                playerTurnCounts[1],
                playerTurnCounts[2],
                pieceOrder.ToArray());
        }

        public bool RestoreSnapshot(BoardSnapshot snapshot)
        {
            if (snapshot == null || snapshot.OwnerIds == null || snapshot.OwnerIds.Length != Columns * Rows)
                return false;
            if (snapshot.Gravity < GravityDirection.Down || snapshot.Gravity > GravityDirection.Right)
                return false;

            Piece[,] restored = new Piece[Columns, Rows];
            for (int column = 0; column < Columns; column++)
            for (int row = 0; row < Rows; row++)
            {
                int ownerId = snapshot.OwnerIds[column * Rows + row];
                if (ownerId < -1 || ownerId > 2) return false;
                restored[column, row] = ownerId == 0 ? null : ownerId == -1 ? new BlockPiece() : new RegularPiece(ownerId);
            }

            Array.Copy(restored, pieces, restored.Length);
            ruleMode = snapshot.RuleMode;
            Gravity = snapshot.Gravity;
            ActivePlayerId = snapshot.ActivePlayerId == 2 ? 2 : 1;
            RotationQuarterTurns = ((snapshot.RotationQuarterTurns % 4) + 4) % 4;
            WinnerId = null;
            IsDraw = false;
            winningPositions.Clear();
            playerPieceQueues[1].Clear();
            playerPieceQueues[2].Clear();
            playerTurnCounts[1] = snapshot.Player1TurnCount;
            playerTurnCounts[2] = snapshot.Player2TurnCount;
            totalTurnCount = snapshot.TotalTurnCount;

            HashSet<Piece> enqueued = new HashSet<Piece>();
            if (snapshot.PieceOrder != null)
            {
                foreach (int index in snapshot.PieceOrder)
                {
                    if (index < 0 || index >= Columns * Rows) continue;
                    int column = index / Rows;
                    int row = index % Rows;
                    Piece piece = pieces[column, row];
                    if (piece != null && piece.Type == PieceType.Regular && (piece.OwnerId == 1 || piece.OwnerId == 2) && enqueued.Add(piece))
                        playerPieceQueues[piece.OwnerId].Enqueue(piece);
                }
            }

            for (int column = 0; column < Columns; column++)
            for (int row = 0; row < Rows; row++)
            {
                Piece piece = pieces[column, row];
                if (piece == null || piece.Type != PieceType.Regular) continue;
                if ((piece.OwnerId == 1 || piece.OwnerId == 2) && enqueued.Add(piece))
                    playerPieceQueues[piece.OwnerId].Enqueue(piece);
            }

            pendingMovements.Clear();
            pendingRemovedPieces.Clear();
            pendingTurnSwitch = false;
            pendingRequiresRebuild = false;
            pendingRequiresRotation = false;
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
            if (ruleMode == BoardRuleMode.SwipeGravity && direction == Gravity)
                return TurnResult.Rejected("Gravity is already in this direction.", State);

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
            CompactBoard();
            pendingTurnSwitch = true;
            pendingRequiresRebuild = false;
            State = BoardState.Animating;

            TurnResult result = TurnResult.Accepted(new List<PieceMovement>(pendingMovements), State);
            result.RemovedPieces.Add(piece);
            return result;
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

            EvaluateOutcome(out int? winnerId, out bool isDraw, out List<GridPosition> winCoords);
            WinnerId = winnerId;
            IsDraw = isDraw;
            winningPositions.Clear();
            if (winCoords != null) winningPositions.AddRange(winCoords);

            TurnOutcome outcome = WinnerId.HasValue ? TurnOutcome.Win : IsDraw ? TurnOutcome.Draw : TurnOutcome.None;
            State = outcome == TurnOutcome.None ? BoardState.WaitingForInput : BoardState.GameOver;
            TurnResult result = TurnResult.Accepted(new List<PieceMovement>(pendingMovements), State);
            result.Outcome = outcome;
            result.WinnerId = WinnerId;
            result.WinningPositions.AddRange(winningPositions);
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

        private void ApplyTurnRuleEffects()
        {
            if (ruleMode == BoardRuleMode.DecayAfterEightTurns && playerPieceQueues[ActivePlayerId].Count > 8)
                RemoveOldestPiece(ActivePlayerId);

            if (ruleMode == BoardRuleMode.RotateAfterTwoTurns &&
                playerTurnCounts[1] >= 4 && playerTurnCounts[2] >= 4)
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

        private void EvaluateOutcome(out int? winnerId, out bool isDraw, out List<GridPosition> winCoords)
        {
            bool p1Wins = TryFindWinningLine(1, out List<GridPosition> p1Coords);
            bool p2Wins = TryFindWinningLine(2, out List<GridPosition> p2Coords);

            if (p1Wins && p2Wins)
            {
                winnerId = null;
                isDraw = true;
                winCoords = null;
                return;
            }

            if (p1Wins)
            {
                winnerId = 1;
                isDraw = false;
                winCoords = p1Coords;
                return;
            }

            if (p2Wins)
            {
                winnerId = 2;
                isDraw = false;
                winCoords = p2Coords;
                return;
            }

            winnerId = null;
            isDraw = IsFull();
            winCoords = null;
        }

        private bool TryFindWinningLine(int ownerId, out List<GridPosition> linePositions)
        {
            int[] columnSteps = { 1, 0, 1, 1 };
            int[] rowSteps = { 0, 1, 1, -1 };
            int winningLength = WinningLength;
            for (int column = 0; column < Columns; column++)
            for (int row = 0; row < Rows; row++)
            {
                if (pieces[column, row]?.OwnerId != ownerId) continue;
                for (int direction = 0; direction < columnSteps.Length; direction++)
                {
                    List<GridPosition> candidate = new List<GridPosition>(winningLength)
                    {
                        new GridPosition(column, row)
                    };
                    for (int distance = 1; distance < winningLength; distance++)
                    {
                        int nextColumn = column + columnSteps[direction] * distance;
                        int nextRow = row + rowSteps[direction] * distance;
                        GridPosition nextPos = new GridPosition(nextColumn, nextRow);
                        if (!IsInside(nextPos) || pieces[nextColumn, nextRow]?.OwnerId != ownerId) break;
                        candidate.Add(nextPos);
                    }
                    if (candidate.Count == winningLength)
                    {
                        linePositions = candidate;
                        return true;
                    }
                }
            }
            linePositions = null;
            return false;
        }

        private bool TryFindDropPosition(int line, out GridPosition position)
        {
            position = default(GridPosition);
            if (Gravity == GravityDirection.Down || Gravity == GravityDirection.Up)
            {
                if (line < 0 || line >= Columns) return false;
                // Scan from spawn edge toward gravity floor; stop at first occupied cell so pieces never pass through a BlockPiece.
                int spawnRow = Gravity == GravityDirection.Down ? Rows - 1 : 0;
                int step = Gravity == GravityDirection.Down ? -1 : 1;
                bool found = false;
                for (int row = spawnRow; row >= 0 && row < Rows; row += step)
                {
                    if (pieces[line, row] != null) break;
                    position = new GridPosition(line, row);
                    found = true;
                }
                return found;
            }
            else
            {
                if (line < 0 || line >= Rows) return false;
                int spawnColumn = Gravity == GravityDirection.Left ? Columns - 1 : 0;
                int step = Gravity == GravityDirection.Left ? -1 : 1;
                bool found = false;
                for (int column = spawnColumn; column >= 0 && column < Columns; column += step)
                {
                    if (pieces[column, line] != null) break;
                    position = new GridPosition(column, line);
                    found = true;
                }
                return found;
            }
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
