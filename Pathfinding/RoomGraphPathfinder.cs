using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace RTS.Pathfinding
{
    public class RoomGraphPathfinder
    {
        private Dictionary<int, RoomNode> roomNodes = new Dictionary<int, RoomNode>();
        private Dictionary<int, List<RoomEdge>> roomConnections = new Dictionary<int, List<RoomEdge>>();

        public List<int> FindRoomSequence(int startRoom, int endRoom)
        {
            if (startRoom == endRoom)
                return new List<int> { startRoom };

            // Use A* for room-level pathfinding
            var openSet = new List<RoomNode>();
            var closedSet = new HashSet<int>();
            var gScore = new Dictionary<int, float>();
            var fScore = new Dictionary<int, float>();
            var cameFrom = new Dictionary<int, int>();

            if (!roomNodes.ContainsKey(startRoom) || !roomNodes.ContainsKey(endRoom))
                return new List<int>();

            var startNode = roomNodes[startRoom];
            var endNode = roomNodes[endRoom];

            gScore[startRoom] = 0;
            fScore[startRoom] = Vector2.Distance(startNode.centerPosition, endNode.centerPosition);
            openSet.Add(startNode);

            while (openSet.Count > 0)
            {
                // Find node with lowest fScore
                var current = openSet.OrderBy(node => fScore.GetValueOrDefault(node.roomId, float.MaxValue)).First();

                if (current.roomId == endRoom)
                {
                    // Reconstruct path
                    var path = new List<int>();
                    var currentId = endRoom;

                    while (cameFrom.ContainsKey(currentId))
                    {
                        path.Insert(0, currentId);
                        currentId = cameFrom[currentId];
                    }

                    path.Insert(0, startRoom);
                    return path;
                }

                openSet.Remove(current);
                closedSet.Add(current.roomId);

                // Check all connected rooms
                if (roomConnections.ContainsKey(current.roomId))
                {
                    foreach (var edge in roomConnections[current.roomId])
                    {
                        if (closedSet.Contains(edge.toRoomId))
                            continue;

                        if (!edge.connectionDoor.IsPassable())
                            continue;

                        var neighbor = roomNodes[edge.toRoomId];
                        var tentativeGScore = gScore[current.roomId] + edge.cost;

                        if (!openSet.Contains(neighbor))
                            openSet.Add(neighbor);
                        else if (tentativeGScore >= gScore.GetValueOrDefault(edge.toRoomId, float.MaxValue))
                            continue;

                        cameFrom[edge.toRoomId] = current.roomId;
                        gScore[edge.toRoomId] = tentativeGScore;
                        fScore[edge.toRoomId] = tentativeGScore +
                                                Vector2.Distance(neighbor.centerPosition, endNode.centerPosition);
                    }
                }
            }

            return new List<int>(); // No path found
        }
        
        public void BuildRoomGraph(Dictionary<int, Room> rooms)
        {
            roomNodes.Clear();
            roomConnections.Clear();

            // Create room nodes
            foreach (var room in rooms.Values)
            {
                var node = new RoomNode
                {
                    roomId = room.roomId,
                    centerPosition = room.worldPosition + new Vector2(room.width / 2f, room.height / 2f),
                    doors = new List<Door>(room.doors)
                };
                roomNodes[room.roomId] = node;
                roomConnections[room.roomId] = new List<RoomEdge>();
            }

            // Create edges based on doors
            foreach (var room in rooms.Values)
            {
                foreach (var door in room.doors)
                {
                    if (rooms.ContainsKey(door.connectedRoomId))
                    {
                        var edge = new RoomEdge
                        {
                            fromRoomId = room.roomId,
                            toRoomId = door.connectedRoomId,
                            connectionDoor = door,
                            cost = CalculateRoomConnectionCost(room, rooms[door.connectedRoomId], door)
                        };
                        roomConnections[room.roomId].Add(edge);
                    }
                }
            }
        }

        private float CalculateRoomConnectionCost(Room fromRoom, Room toRoom, Door door)
        {
            var fromCenter = fromRoom.worldPosition + new Vector2(fromRoom.width / 2f, fromRoom.height / 2f);
            var toCenter = toRoom.worldPosition + new Vector2(toRoom.width / 2f, toRoom.height / 2f);
        
            float baseCost = Vector2.Distance(fromCenter, toCenter);
        
            // Add penalty if door is closed or has restrictions
            if (!door.IsPassable())
                baseCost *= 10f; // High penalty for closed doors
            
            return baseCost;
        }
    }
}