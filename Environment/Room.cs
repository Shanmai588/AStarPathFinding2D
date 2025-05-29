using System.Collections.Generic;
using UnityEngine;

namespace RTS.Pathfinding
{
    public class Room
    {
        private readonly Dictionary<int, Room> connectedRooms;
        private readonly List<Door> doors;
        private readonly Tile[,] grid;

        public Room(int id, int w, int h, Vector2 worldPos)
        {
            RoomId = id;
            Width = w;
            Height = h;
            WorldPosition = worldPos;
            grid = new Tile[Width, Height];
            doors = new List<Door>();
            connectedRooms = new Dictionary<int, Room>();

            // Initialize tiles
            for (var x = 0; x < Width; x++)
            for (var y = 0; y < Height; y++)
                grid[x, y] = new Tile(new Vector2Int(x, y), TileType.Ground);
        }

        public int RoomId { get; }

        public int Width { get; }

        public int Height { get; }

        public Vector2 WorldPosition { get; }

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
            return new List<Door>(doors);
        }

        public void AddDoor(Door door)
        {
            doors.Add(door);
        }

        public bool IsValidPosition(int x, int y)
        {
            return x >= 0 && x < Width && y >= 0 && y < Height;
        }

        public List<Vector2Int> GetNeighbors(int x, int y)
        {
            var neighbors = new List<Vector2Int>();
            Vector2Int[] directions =
            {
                new(0, 1), new(1, 0),
                new(0, -1), new(-1, 0),
                new(1, 1), new(-1, 1),
                new(1, -1), new(-1, -1)
            };

            foreach (var dir in directions)
            {
                var nx = x + dir.x;
                var ny = y + dir.y;
                if (IsValidPosition(nx, ny))
                    neighbors.Add(new Vector2Int(nx, ny));
            }

            return neighbors;
        }

        public Vector2Int WorldToGrid(Vector2 worldPos)
        {
            var localPos = worldPos - WorldPosition;
            return new Vector2Int(Mathf.FloorToInt(localPos.x), Mathf.FloorToInt(localPos.y));
        }

        public Vector2 GridToWorld(Vector2Int gridPos)
        {
            return WorldPosition + new Vector2(gridPos.x + 0.5f, gridPos.y + 0.5f);
        }

        public void AddConnectedRoom(int id, Room room)
        {
            connectedRooms[id] = room;
        }

        public Dictionary<int, Room> GetConnectedRooms()
        {
            return new Dictionary<int, Room>(connectedRooms);
        }
    }
}