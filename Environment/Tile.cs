using UnityEngine;

namespace RTS.Pathfinding
{
    // Immutable runtime data for one grid cell
    public class Tile
    {
        public Vector2Int position;
        public TileType type;
        public float baseCost = 1.0f;
        public bool isWalkable = true;
        public Agent occupyingAgent;

        public float GetCost(Agent agent, ICostProvider costProvider)
        {
            return costProvider.GetMovementCost(this, agent);
        }

        public bool IsOccupied()
        {
            return occupyingAgent != null;
        }

        public void SetOccupant(Agent agent)
        {
            occupyingAgent = agent;
        }
    }
}