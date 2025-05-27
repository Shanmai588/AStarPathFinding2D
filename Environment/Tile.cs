using UnityEngine;

namespace RTS.Pathfinding
{
    // Immutable runtime data for one grid cell
    public class Tile
    {
        private Agent occupyingAgent;
        private TileType type;

        public Tile(Vector2Int pos, TileType tileType)
        {
            Position = pos;
            type = tileType;
            UpdateWalkability();
            UpdateBaseCost();
        }

        public Vector2Int Position { get; }

        public TileType Type
        {
            get => type;
            set
            {
                type = value;
                UpdateWalkability();
            }
        }

        public float BaseCost { get; private set; }

        public bool IsWalkable { get; private set; }

        private void UpdateWalkability()
        {
            IsWalkable = type != TileType.Blocked && type != TileType.Building;
        }

        private void UpdateBaseCost()
        {
            BaseCost = type switch
            {
                TileType.Road => 0.5f,
                TileType.Ground => 1f,
                TileType.Forest => 1.5f,
                TileType.Water => 2f,
                TileType.Mountain => 3f,
                _ => 1f
            };
        }

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