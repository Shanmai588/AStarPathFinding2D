using System.Collections.Generic;

namespace RTS.Pathfinding
{
    public class MovementCapabilities
    {
        private readonly List<TileType> allowedTerrain;

        public MovementCapabilities(bool fly = false, bool swim = false, float unitSize = 1f)
        {
            CanFly = fly;
            CanSwim = swim;
            Size = unitSize;
            MaxSlope = 45f;
            allowedTerrain = new List<TileType> { TileType.Ground, TileType.Road };

            if (CanSwim)
                allowedTerrain.Add(TileType.Water);
            if (CanFly)
            {
                allowedTerrain.Add(TileType.Water);
                allowedTerrain.Add(TileType.Mountain);
                allowedTerrain.Add(TileType.Forest);
            }
        }

        public bool CanFly { get; }

        public bool CanSwim { get; }

        public float Size { get; }

        public float MaxSlope { get; }

        public bool CanTraverse(Tile tile)
        {
            if (tile == null || !tile.IsWalkable)
                return false;

            if (CanFly)
                return true;

            return allowedTerrain.Contains(tile.Type);
        }
    }
}