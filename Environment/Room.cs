using System.Collections.Generic;
using UnityEngine;

namespace RTS.Pathfinding
{
    public class Room
    {
        private readonly Dictionary<int, Room> connectedRooms = new();
        private readonly List<Door> doors = new();
        private readonly Tile[,] grid;

        public Room(int id, int w, int h)
        {
            RoomId = id;
            Width = w;
            Height = h;
            grid = new Tile[w, h];
            InitializeGrid();
        }

        public int RoomId { get; }

        public int Width { get; }

        public int Height { get; }

        private void InitializeGrid()
        {
            for (var x = 0; x < Width; x++)
            for (var y = 0; y < Height; y++)
                grid[x, y] = new Tile(new Vector2Int(x, y), TileType.Ground);
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
            return new List<Door>(doors);
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
                new(0, 1), // Up
                new(1, 0), // Right
                new(0, -1), // Down
                new(-1, 0), // Left
                new(1, 1), // Up-Right
                new(1, -1), // Down-Right
                new(-1, -1), // Down-Left
                new(-1, 1) // Up-Left
            };

            foreach (var dir in directions)
            {
                var newX = x + dir.x;
                var newY = y + dir.y;
                if (IsValidPosition(newX, newY)) neighbors.Add(new Vector2Int(newX, newY));
            }

            return neighbors;
        }

        public void AddDoor(Door door)
        {
            doors.Add(door);
        }

        public void AddConnectedRoom(int roomId, Room room)
        {
            connectedRooms[roomId] = room;
        }
    }

    public struct ConnectionInfo
    {
        public Vector2Int FromPosition;
        public int ToRoomId;
        public Vector2Int ToPosition;
        public bool IsPassable;
    }
}