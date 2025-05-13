using System;
using System.Collections.Generic;
using UnityEngine;

namespace RTS.Pathfinding 
{
    // Represents a rectangular room in the grid
    public class Room 
    {
        // Bounds of the room in grid coordinates
        public BoundsInt Bounds;
        
        // List of doors connected to this room
        public List<Door> Doors = new List<Door>();
        
        // Unique identifier for each room
        public int RoomId { get; private set; }
        
        // Auto-incrementing counter for room IDs
        private static int nextRoomId = 0;
        
        public Room(BoundsInt bounds)
        {
            Bounds = bounds;
            RoomId = nextRoomId++;
        }
        
        // Check if a grid position is inside this room
        public bool Contains(Vector2Int gridPosition)
        {
            return gridPosition.x >= Bounds.xMin && 
                   gridPosition.x < Bounds.xMax && 
                   gridPosition.y >= Bounds.yMin && 
                   gridPosition.y < Bounds.yMax;
        }
        
        // Check if a tile index is inside this room
        public bool Contains(int tileIndex, GridManager gridManager)
        {
            Vector2Int gridPos = gridManager.IndexToGrid(tileIndex);
            return Contains(gridPos);
        }
        
        // Get the center position of the room in grid coordinates
        public Vector2Int GetCenter()
        {
            return new Vector2Int(
                Bounds.xMin + Bounds.size.x / 2,
                Bounds.yMin + Bounds.size.y / 2
            );
        }
        
        // Get the closest door to a position within this room
        public Door GetClosestDoorTo(int tileIndex, GridManager gridManager)
        {
            if (Doors.Count == 0)
                return null;
                
            Door closestDoor = null;
            float closestDistSq = float.MaxValue;
            Vector2Int pos = gridManager.IndexToGrid(tileIndex);
            
            foreach (Door door in Doors)
            {
                Vector2Int doorPos = gridManager.IndexToGrid(door.TileIndex);
                var distance = doorPos - pos;
                float distSq = distance.sqrMagnitude;
                
                if (distSq < closestDistSq)
                {
                    closestDistSq = distSq;
                    closestDoor = door;
                }
            }
            
            return closestDoor;
        }
    }
    
    // Represents a connection between two rooms
    [Serializable]
    public class Door
    {
        // Tile index where the door is located
        public int TileIndex;
        
        // The two rooms connected by this door
        public Room RoomA;
        public Room RoomB;
        
        // Base cost to traverse this door (can be higher for narrow passages, etc.)
        public float BaseCost;
        
        // Is this door currently passable?
        public bool IsOpen = true;
        
        // Event for notifying when door state changes
        public event Action<Door> DoorStateChanged;
        
        public Door(int tileIndex, Room roomA, Room roomB, float baseCost = 1.0f)
        {
            TileIndex = tileIndex;
            RoomA = roomA;
            RoomB = roomB;
            BaseCost = baseCost;
            
            // Add this door to both rooms
            roomA.Doors.Add(this);
            roomB.Doors.Add(this);
        }
        
        // Get the room on the other side from the given room
        public Room GetOtherRoom(Room room)
        {
            if (room == RoomA)
                return RoomB;
            if (room == RoomB)
                return RoomA;
                
            return null; // Door doesn't connect to the given room
        }
        
        // Set door state (open/closed)
        public void SetOpen(bool isOpen)
        {
            if (IsOpen != isOpen)
            {
                IsOpen = isOpen;
                DoorStateChanged?.Invoke(this);
            }
        }
    }
}