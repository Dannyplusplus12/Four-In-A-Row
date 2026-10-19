namespace ConnectFour.Core
{
    public sealed class BoardSnapshot
    {
        public int[] OwnerIds { get; }
        public GravityDirection Gravity { get; }
        public int ActivePlayerId { get; }

        public BoardSnapshot(int[] ownerIds, GravityDirection gravity, int activePlayerId)
        {
            OwnerIds = ownerIds == null ? null : (int[])ownerIds.Clone();
            Gravity = gravity;
            ActivePlayerId = activePlayerId;
        }
    }
}