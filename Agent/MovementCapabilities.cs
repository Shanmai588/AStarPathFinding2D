using System.Collections.Generic;

namespace RTS.Pathfinding
{
    public class MovementCapabilities
    {
        public bool canFly;
        public bool canSwim;
        public float size = 1.0f;
        public List<TileType> allowedTerrain = new List<TileType> { TileType.Floor };
        public float maxSlope = 45f;

        public bool CanTraverse(Tile tile)
        {
            if (!tile.isWalkable) return false;
            return allowedTerrain.Contains(tile.type);
        }
    }
}