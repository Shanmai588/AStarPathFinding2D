using System.Collections.Generic;
using UnityEngine;

namespace RTS.Pathfinding
{
    // Strategy pattern: Interface for calculating tile traversal costs
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
            if (!tile.IsWalkable) return float.MaxValue;
            return tile.BaseCost;
        }

        public float GetHeuristicCost(Vector2Int from, Vector2Int to)
        {
            return Vector2Int.Distance(from, to);
        }

        public bool ShouldAvoidTile(Tile tile, Agent agent)
        {
            return !tile.IsWalkable;
        }
    }

    public class TerrainAwareCostProvider : ICostProvider
    {
        private readonly Dictionary<TileType, float> terrainCosts = new()
        {
            { TileType.Road, 0.5f },
            { TileType.Ground, 1f },
            { TileType.Forest, 2f },
            { TileType.Water, 3f },
            { TileType.Mountain, 4f }
        };

        public float GetMovementCost(Tile tile, Agent agent)
        {
            if (!tile.IsWalkable) return float.MaxValue;

            var capabilities = agent.GetMovementCapabilities();
            if (!capabilities.AllowedTerrain.Contains(tile.Type))
                return float.MaxValue;

            return terrainCosts.TryGetValue(tile.Type, out var cost) ? cost : tile.BaseCost;
        }

        public float GetHeuristicCost(Vector2Int from, Vector2Int to)
        {
            return Vector2Int.Distance(from, to);
        }

        public bool ShouldAvoidTile(Tile tile, Agent agent)
        {
            var capabilities = agent.GetMovementCapabilities();
            return !tile.IsWalkable || !capabilities.AllowedTerrain.Contains(tile.Type);
        }
    }

    public class UnitAvoidanceCostProvider : ICostProvider
    {
        private readonly float unitAvoidanceCost = 5f;

        public float GetMovementCost(Tile tile, Agent agent)
        {
            if (!tile.IsWalkable) return float.MaxValue;

            var cost = tile.BaseCost;
            if (tile.IsOccupied())
                cost += unitAvoidanceCost;

            return cost;
        }

        public float GetHeuristicCost(Vector2Int from, Vector2Int to)
        {
            return Vector2Int.Distance(from, to);
        }

        public bool ShouldAvoidTile(Tile tile, Agent agent)
        {
            return !tile.IsWalkable;
        }
    }
}