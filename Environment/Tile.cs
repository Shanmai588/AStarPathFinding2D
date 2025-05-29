using UnityEngine;

namespace RTS.Pathfinding
{
    // Immutable runtime data for one grid cell
    public class Tile
    {
        private float baseCost;

        public Tile(Vector2Int pos, TileType tileType, float cost = 1f)
        {
            Position = pos;
            Type = tileType;
            baseCost = cost;
            IsWalkable = tileType != TileType.Blocked && tileType != TileType.Mountain;
        }

        public Vector2Int Position { get; }

        public TileType Type { get; }

        public bool IsWalkable { get; }

        public Agent OccupyingAgent { get; private set; }

        public float GetCost(Agent agent, ICostProvider costProvider)
        {
            return costProvider.GetMovementCost(this, agent);
        }

        public bool IsOccupied()
        {
            return OccupyingAgent != null;
        }

        public void SetOccupant(Agent agent)
        {
            OccupyingAgent = agent;
        }

        public void ClearOccupant()
        {
            OccupyingAgent = null;
        }
    }
}