using System.Collections.Generic;
using UnityEngine;

namespace RTS.Pathfinding
{
    public class Room
    {
        public int roomId;
        public int width, height;
        public Vector2 worldPosition;
        public Tile[,] grid;
        public List<Door> doors = new List<Door>();
        public Dictionary<int, Room> connectedRooms = new Dictionary<int, Room>();

        public Room(int id, int w, int h, Vector2 worldPos)
        {
            roomId = id;
            width = w;
            height = h;
            worldPosition = worldPos;
            grid = new Tile[width, height];

            // Initialize grid
            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    grid[x, y] = new Tile
                    {
                        position = new Vector2Int(x, y),
                        type = TileType.Floor,
                        isWalkable = true
                    };
                }
            }
        }

        public Tile GetTile(int x, int y)
        {
            if (IsValidPosition(x, y))
                return grid[x, y];
            return null;
        }

        public void SetTile(int x, int y, Tile tile)
        {
            if (IsValidPosition(x, y))
                grid[x, y] = tile;
        }

        public List<Door> GetDoors()
        {
            return doors;
        }

        public bool IsValidPosition(int x, int y)
        {
            return x >= 0 && x < width && y >= 0 && y < height;
        }

        public List<Vector2Int> GetNeighbors(int x, int y)
        {
            var neighbors = new List<Vector2Int>();
            var directions = new Vector2Int[]
            {
                Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right
            };

            foreach (var dir in directions)
            {
                var neighbor = new Vector2Int(x, y) + dir;
                if (IsValidPosition(neighbor.x, neighbor.y))
                    neighbors.Add(neighbor);
            }

            return neighbors;
        }

        public Vector2Int WorldToGrid(Vector2 worldPos)
        {
            var localPos = worldPos - worldPosition;
            return new Vector2Int(Mathf.FloorToInt(localPos.x), Mathf.FloorToInt(localPos.y));
        }

        public Vector2 GridToWorld(Vector2Int gridPos)
        {
            return worldPosition + new Vector2(gridPos.x, gridPos.y);
        }
    }
}