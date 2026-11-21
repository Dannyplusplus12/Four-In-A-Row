namespace ConnectFour.Core
{
    public sealed class BoardSnapshot
    {
        public int[] OwnerIds { get; }
        public GravityDirection Gravity { get; }
        public int ActivePlayerId { get; }
        public BoardRuleMode RuleMode { get; }
        public int RotationQuarterTurns { get; }
        public int TotalTurnCount { get; }
        public int Player1TurnCount { get; }
        public int Player2TurnCount { get; }
        public int[] PieceOrder { get; }

        public BoardSnapshot(int[] ownerIds, GravityDirection gravity, int activePlayerId)
            : this(ownerIds, gravity, activePlayerId, BoardRuleMode.Classic, 0, 0, 0, 0, null)
        {
        }

        public BoardSnapshot(
            int[] ownerIds,
            GravityDirection gravity,
            int activePlayerId,
            BoardRuleMode ruleMode,
            int rotationQuarterTurns,
            int totalTurnCount,
            int player1TurnCount,
            int player2TurnCount,
            int[] pieceOrder)
        {
            OwnerIds = ownerIds == null ? null : (int[])ownerIds.Clone();
            Gravity = gravity;
            ActivePlayerId = activePlayerId;
            RuleMode = ruleMode;
            RotationQuarterTurns = ((rotationQuarterTurns % 4) + 4) % 4;
            TotalTurnCount = totalTurnCount < 0 ? 0 : totalTurnCount;
            Player1TurnCount = player1TurnCount < 0 ? 0 : player1TurnCount;
            Player2TurnCount = player2TurnCount < 0 ? 0 : player2TurnCount;
            PieceOrder = pieceOrder == null ? null : (int[])pieceOrder.Clone();
        }
    }
}
