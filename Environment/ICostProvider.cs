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
            if (!agent.GetMovementCapabilities().CanTraverse(tile))
                return float.MaxValue;

            return tile.Type switch
            {
                TileType.Road => 0.5f,
                TileType.Ground => 1f,
                TileType.Forest => 2f,
                TileType.Water => agent.GetMovementCapabilities().CanSwim ? 1.5f : float.MaxValue,
                _ => float.MaxValue
            };
        }

        public float GetHeuristicCost(Vector2Int from, Vector2Int to)
        {
            return Vector2Int.Distance(from, to);
        }

        public bool ShouldAvoidTile(Tile tile, Agent agent)
        {
            return tile.IsOccupied() && tile.OccupyingAgent != agent;
        }
    }

    public class TerrainAwareCostProvider : ICostProvider
    {
        private readonly Dictionary<TileType, float> terrainCosts;

        public TerrainAwareCostProvider()
        {
            terrainCosts = new Dictionary<TileType, float>
            {
                { TileType.Road, 0.3f },
                { TileType.Ground, 1f },
                { TileType.Forest, 2.5f },
                { TileType.Water, 3f },
                { TileType.Mountain, 5f }
            };
        }

        public float GetMovementCost(Tile tile, Agent agent)
        {
            if (!agent.GetMovementCapabilities().CanTraverse(tile))
                return float.MaxValue;

            return terrainCosts.TryGetValue(tile.Type, out var cost) ? cost : float.MaxValue;
        }

        public float GetHeuristicCost(Vector2Int from, Vector2Int to)
        {
            return Vector2Int.Distance(from, to) * 1.1f;
        }

        public bool ShouldAvoidTile(Tile tile, Agent agent)
        {
            return tile.IsOccupied() && tile.OccupyingAgent != agent;
        }
    }

    public class UnitAvoidanceCostProvider : ICostProvider
    {
        private readonly float unitAvoidanceCost = 10f;

        public float GetMovementCost(Tile tile, Agent agent)
        {
            if (!agent.GetMovementCapabilities().CanTraverse(tile))
                return float.MaxValue;

            var baseCost = new StandardCostProvider().GetMovementCost(tile, agent);

            if (tile.IsOccupied() && tile.OccupyingAgent != agent)
                return baseCost + unitAvoidanceCost;

            return baseCost;
        }

        public float GetHeuristicCost(Vector2Int from, Vector2Int to)
        {
            return Vector2Int.Distance(from, to);
        }

        public bool ShouldAvoidTile(Tile tile, Agent agent)
        {
            return false; // We handle avoidance through cost instead
        }
    }
}