using System.Collections.Generic;
using UnityEngine;

namespace RTS.Pathfinding
{
    public interface ICostProvider
    {
        float GetMovementCost(Tile tile, Agent agent);
        float GetHeuristicCost(Vector2Int from, Vector2Int to);
        bool ShouldAvoidTile(Tile tile, Agent agent);
    }

    public class StandardCostProvider : ICostProvider
    {
        public float GetMovementCost(Tile tile, Agent agent)
        {
            if (!tile.isWalkable) return float.MaxValue;
            return tile.baseCost;
        }

        public float GetHeuristicCost(Vector2Int from, Vector2Int to)
        {
            return Vector2Int.Distance(from, to);
        }

        public bool ShouldAvoidTile(Tile tile, Agent agent)
        {
            return !tile.isWalkable || (tile.IsOccupied() && tile.occupyingAgent != agent);
        }
    }

    public class TerrainAwareCostProvider : ICostProvider
    {
        private Dictionary<TileType, float> terrainCosts = new Dictionary<TileType, float>
        {
            { TileType.Floor, 1.0f },
            { TileType.Wall, float.MaxValue },
            { TileType.Water, 3.0f },
            { TileType.Rough, 2.0f },
            { TileType.Mud, 2.5f }
        };

        public float GetMovementCost(Tile tile, Agent agent)
        {
            if (!tile.isWalkable) return float.MaxValue;
            return terrainCosts.ContainsKey(tile.type) ? terrainCosts[tile.type] : tile.baseCost;
        }

        public float GetHeuristicCost(Vector2Int from, Vector2Int to)
        {
            return Vector2Int.Distance(from, to);
        }

        public bool ShouldAvoidTile(Tile tile, Agent agent)
        {
            return !tile.isWalkable || (tile.IsOccupied() && tile.occupyingAgent != agent);
        }
    }

    public class UnitAvoidanceCostProvider : ICostProvider
    {
        public float unitAvoidanceCost = 5.0f;

        public float GetMovementCost(Tile tile, Agent agent)
        {
            if (!tile.isWalkable) return float.MaxValue;

            float cost = tile.baseCost;
            if (tile.IsOccupied() && tile.occupyingAgent != agent)
                cost += unitAvoidanceCost;

            return cost;
        }

        public float GetHeuristicCost(Vector2Int from, Vector2Int to)
        {
            return Vector2Int.Distance(from, to);
        }

        public bool ShouldAvoidTile(Tile tile, Agent agent)
        {
            return !tile.isWalkable;
        }
    }
}