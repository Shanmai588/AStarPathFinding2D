namespace RTS.Pathfinding
{
    public enum TileType
    {
        Ground,
        Water,
        Mountain,
        Forest,
        Road,
        Building,
        Blocked
    }

    public enum AgentType
    {
        Infantry,
        Vehicle,
        Flying,
        Naval
    }

    public enum RequestPriority
    {
        High,
        Normal,
        Low
    }
}