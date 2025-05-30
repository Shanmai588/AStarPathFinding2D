using System.Collections.Generic;
using UnityEngine;

namespace RTS.Pathfinding
{
    public class Room
    {
        private int roomId;
        private int width, height;
        private int minX, minY; // Minimum grid coordinates (can be negative)
        private Vector2 worldPosition;
        private Tile[,] grid;
        private List<Door> doors;
        private Dictionary<int, Room> connectedRooms;

        public int RoomId => roomId;
        public int Width => width;
        public int Height => height;
        public int MinX => minX;
        public int MinY => minY;
        public int MaxX => minX + width - 1;
        public int MaxY => minY + height - 1;
        public Vector2 WorldPosition => worldPosition;

        public Room(int id, int w, int h, Vector2 worldPos, int gridMinX = 0, int gridMinY = 0)
        {
            roomId = id;
            width = w;
            height = h;
            worldPosition = worldPos;
            minX = gridMinX;
            minY = gridMinY;
            grid = new Tile[width, height];
            doors = new List<Door>();
            connectedRooms = new Dictionary<int, Room>();

            // Initialize tiles with correct grid positions
            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    int gridX = minX + x;
                    int gridY = minY + y;
                    grid[x, y] = new Tile(new Vector2Int(gridX, gridY), TileType.Ground);
                }
            }
        }

        public Tile GetTile(int x, int y)
        {
            // Convert grid coordinates to array indices
            int arrayX = x - minX;
            int arrayY = y - minY;
            
            if (arrayX >= 0 && arrayX < width && arrayY >= 0 && arrayY < height)
                return grid[arrayX, arrayY];
            return null;
        }

        public void SetTile(int x, int y, Tile tile)
        {
            int arrayX = x - minX;
            int arrayY = y - minY;
            
            if (arrayX >= 0 && arrayX < width && arrayY >= 0 && arrayY < height)
                grid[arrayX, arrayY] = tile;
        }

        public List<Door> GetDoors() => new List<Door>(doors);

        public void AddDoor(Door door)
        {
            doors.Add(door);
        }

        public bool IsValidPosition(int x, int y)
        {
            return x >= minX && x <= MaxX && y >= minY && y <= MaxY;
        }

        public List<Vector2Int> GetNeighbors(int x, int y)
        {
            var neighbors = new List<Vector2Int>();
            Vector2Int[] directions = {
                new Vector2Int(0, 1), new Vector2Int(1, 0),
                new Vector2Int(0, -1), new Vector2Int(-1, 0),
                new Vector2Int(1, 1), new Vector2Int(-1, 1),
                new Vector2Int(1, -1), new Vector2Int(-1, -1)
            };

            foreach (var dir in directions)
            {
                int nx = x + dir.x;
                int ny = y + dir.y;
                if (IsValidPosition(nx, ny))
                    neighbors.Add(new Vector2Int(nx, ny));
            }

            return neighbors;
        }

        public Vector2Int WorldToGrid(Vector2 worldPos)
        {
            Vector2 localPos = worldPos - worldPosition;
            // Proper handling of negative coordinates
            int gridX = Mathf.FloorToInt(localPos.x) + minX;
            int gridY = Mathf.FloorToInt(localPos.y) + minY;
            return new Vector2Int(gridX, gridY);
        }

        public Vector2 GridToWorld(Vector2Int gridPos)
        {
            // Convert grid position to local position first
            float localX = gridPos.x - minX + 0.5f;
            float localY = gridPos.y - minY + 0.5f;
            return worldPosition + new Vector2(localX, localY);
        }

        public void AddConnectedRoom(int id, Room room)
        {
            connectedRooms[id] = room;
        }

        public Dictionary<int, Room> GetConnectedRooms() => new Dictionary<int, Room>(connectedRooms);
        
        // Helper method to get bounds in grid coordinates
        public Bounds GetGridBounds()
        {
            var center = new Vector3((minX + MaxX) / 2f, (minY + MaxY) / 2f, 0);
            var size = new Vector3(width, height, 1);
            return new Bounds(center, size);
        }
    }
}