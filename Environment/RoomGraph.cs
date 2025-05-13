using System;
using System.Collections.Generic;
using UnityEngine;

namespace RTS.Pathfinding 
{
    /// <summary>
    /// Manages the room connectivity graph for hierarchical pathfinding in 2D.
    /// </summary>
    public class RoomGraph : MonoBehaviour 
    {
        [SerializeField] private GridManager gridManager;
        
        // Dictionary mapping rooms to their connecting doors
        private Dictionary<Room, List<Door>> roomConnections = new Dictionary<Room, List<Door>>();
        
        // List of all rooms in the level
        private List<Room> allRooms = new List<Room>();
        
        // List of all doors in the level
        private List<Door> allDoors = new List<Door>();
        
        // Event fired when the room graph changes
        public event Action GraphChanged;
        
        private void Awake() 
        {
            if (gridManager == null)
                gridManager = FindObjectOfType<GridManager>();
        }
        
        private void OnEnable() 
        {
            // Subscribe to door state changes
            foreach (Door door in allDoors) 
            {
                door.DoorStateChanged += OnDoorStateChanged;
            }
        }
        
        private void OnDisable() 
        {
            // Unsubscribe from door state changes
            foreach (Door door in allDoors) 
            {
                door.DoorStateChanged -= OnDoorStateChanged;
            }
        }
        
        private void OnDoorStateChanged(Door door) 
        {
            // Notify listeners that the graph has changed
            GraphChanged?.Invoke();
        }
        
        // Add a room to the graph
        public void AddRoom(Room room) 
        {
            if (!allRooms.Contains(room)) 
            {
                allRooms.Add(room);
                roomConnections[room] = new List<Door>();
            }
        }
        
        // Add a door connecting two rooms
        public Door AddDoor(int tileIndex, Room roomA, Room roomB, float baseCost = 1.0f) 
        {
            // Make sure both rooms are added to the graph
            AddRoom(roomA);
            AddRoom(roomB);
            
            // Create the door
            Door door = new Door(tileIndex, roomA, roomB, baseCost);
            
            // Add the door to the connections lists
            roomConnections[roomA].Add(door);
            roomConnections[roomB].Add(door);
            
            // Add to the master list
            allDoors.Add(door);
            
            // Subscribe to door state changes
            door.DoorStateChanged += OnDoorStateChanged;
            
            // Notify listeners that the graph has changed
            GraphChanged?.Invoke();
            
            return door;
        }
        
        // Get all doors connected to a room
        public List<Door> GetDoors(Room room) 
        {
            if (roomConnections.TryGetValue(room, out List<Door> doors))
                return doors;
                
            return new List<Door>();
        }
        
        // Find the room that contains a tile index
        public Room FindRoomContaining(int tileIndex) 
        {
            Vector2Int position = gridManager.IndexToGrid(tileIndex);
            
            foreach (Room room in allRooms) 
            {
                if (room.Contains(position))
                    return room;
            }
            
            return null;
        }
        
        // Find the nearest room to a 2D position
        public Room FindNearestRoom(Vector2 position, int maxSearchRadius = 10) 
        {
            // Convert 2D position to grid position
            Vector2Int gridPosition = gridManager.WorldToGrid(position);
            
            // First check if position is already in a room
            foreach (Room room in allRooms) 
            {
                if (room.Contains(gridPosition))
                    return room;
            }
            
            // If not, search outward for the nearest room
            for (int radius = 1; radius <= maxSearchRadius; radius++) 
            {
                for (int dx = -radius; dx <= radius; dx++) 
                {
                    for (int dy = -radius; dy <= radius; dy++) 
                    {
                        // Only check positions at the current radius
                        if (Mathf.Abs(dx) != radius && Mathf.Abs(dy) != radius)
                            continue;
                            
                        Vector2Int checkPos = new Vector2Int(gridPosition.x + dx, gridPosition.y + dy);
                        
                        foreach (Room room in allRooms) 
                        {
                            if (room.Contains(checkPos))
                                return room;
                        }
                    }
                }
            }
            
            return null;
        }
        
        // Find a path between two rooms
        public List<Door> FindPath(Room startRoom, Room targetRoom) 
        {
            if (startRoom == targetRoom)
                return new List<Door>(); // Already in the target room
                
            // Set for tracking visited rooms
            HashSet<Room> visited = new HashSet<Room>();
            
            // Dictionary mapping room to its previous door in the path
            Dictionary<Room, Door> cameFrom = new Dictionary<Room, Door>();
            
            // Queue for BFS
            Queue<Room> queue = new Queue<Room>();
            queue.Enqueue(startRoom);
            visited.Add(startRoom);
            
            bool foundPath = false;
            Room current;
            // BFS to find path between rooms
            while (queue.Count > 0) 
            {
                 current = queue.Dequeue();
                
                // Check doors connected to this room
                foreach (Door door in GetDoors(current)) 
                {
                    // Skip closed doors
                    if (!door.IsOpen)
                        continue;
                        
                    Room nextRoom = door.GetOtherRoom(current);
                    
                    if (nextRoom == null || visited.Contains(nextRoom))
                        continue;
                        
                    // Record how we got to this room
                    cameFrom[nextRoom] = door;
                    
                    // Check if we reached the target
                    if (nextRoom == targetRoom) 
                    {
                        foundPath = true;
                        break;
                    }
                    
                    // Continue searching
                    visited.Add(nextRoom);
                    queue.Enqueue(nextRoom);
                }
                
                if (foundPath)
                    break;
            }
            
            // If no path was found
            if (!foundPath)
                return null;
                
            // Reconstruct the path
            List<Door> path = new List<Door>();
             current = targetRoom;
            
            while (current != startRoom) 
            {
                Door door = cameFrom[current];
                path.Add(door);
                current = door.GetOtherRoom(current);
            }
            
            // Path is from target to start, so reverse it
            path.Reverse();
            return path;
        }
        
        // Get all rooms in the level
        public List<Room> GetAllRooms() 
        {
            return new List<Room>(allRooms);
        }
        
        // Find the nearest walkable tile and the room containing it, given a target position
        public (int tileIndex, Room room) FindNearestWalkableTile(Vector2 position) 
        {
            Vector2Int gridPos = gridManager.WorldToGrid(position);
            int tileIndex = gridManager.GridToIndex(gridPos);
            
            // First try the exact position
            if (gridManager.GetTile(tileIndex).Walkable) 
            {
                Room room = FindRoomContaining(tileIndex);
                if (room != null) 
                {
                    return (tileIndex, room);
                }
            }
            
            // Search for nearest walkable tile
            int nearestWalkableTile = gridManager.FindNearestWalkableTile(tileIndex);
            if (nearestWalkableTile >= 0) 
            {
                Room room = FindRoomContaining(nearestWalkableTile);
                if (room != null) 
                {
                    return (nearestWalkableTile, room);
                }
            }
            
            // Failed to find a walkable tile in any room
            return (-1, null);
        }

        // Draw room boundaries and doors in the editor
        private void OnDrawGizmos()
        {
            if (!Application.isPlaying || gridManager == null)
                return;

            // Draw room boundaries
            foreach (Room room in allRooms)
            {
                Gizmos.color = new Color(0, 0, 1, 0.2f); // Blue for room boundaries
                
                BoundsInt bounds = room.Bounds;
                Vector2 min = gridManager.GridToWorld2D(new Vector2Int(bounds.xMin, bounds.yMin));
                Vector2 max = gridManager.GridToWorld2D(new Vector2Int(bounds.xMax, bounds.yMax));
                
                // Draw room outline
                float z = gridManager.GridToWorld(Vector2Int.zero).z;
                
                // Bottom edge
                Gizmos.DrawLine(new Vector3(min.x, min.y, z), new Vector3(max.x, min.y, z));
                // Top edge
                Gizmos.DrawLine(new Vector3(min.x, max.y, z), new Vector3(max.x, max.y, z));
                // Left edge
                Gizmos.DrawLine(new Vector3(min.x, min.y, z), new Vector3(min.x, max.y, z));
                // Right edge
                Gizmos.DrawLine(new Vector3(max.x, min.y, z), new Vector3(max.x, max.y, z));
            }

            // Draw doors
            foreach (Door door in allDoors)
            {
                Vector2Int gridPos = gridManager.IndexToGrid(door.TileIndex);
                Vector3 doorPos = gridManager.GridToWorld(gridPos);
                
                if (door.IsOpen)
                    Gizmos.color = new Color(0, 1, 0, 0.7f); // Green for open door
                else
                    Gizmos.color = new Color(1, 0, 0, 0.7f); // Red for closed door
                
                Gizmos.DrawSphere(doorPos, gridManager.CellSize * 0.3f);
            }
        }
    }
}