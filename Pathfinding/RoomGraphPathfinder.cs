using System.Collections.Generic;
using UnityEngine;

namespace RTS.Pathfinding
{
    public class RoomNode
    {
        public int RoomId { get; set; }
        public Vector2 CenterPosition { get; set; }
        public List<Door> Doors { get; set; }

        public float GetConnectionCost(int targetRoom)
        {
            foreach (var door in Doors)
                if (door.ConnectedRoomId == targetRoom)
                    return 1f; // Base cost
            return float.MaxValue;
        }
    }

    public class RoomEdge
    {
        public int FromRoomId { get; set; }
        public int ToRoomId { get; set; }
        public Door ConnectionDoor { get; set; }
        public float Cost { get; set; }
    }

    public class RoomGraphPathfinder
    {
        private readonly Dictionary<int, List<RoomEdge>> roomConnections = new();
        private readonly Dictionary<int, RoomNode> roomNodes = new();

        public List<int> FindRoomSequence(int startRoom, int endRoom)
        {
            if (startRoom == endRoom)
                return new List<int> { startRoom };

            var openSet = new HashSet<int> { startRoom };
            var cameFrom = new Dictionary<int, int>();
            var gScore = new Dictionary<int, float> { { startRoom, 0 } };
            var fScore = new Dictionary<int, float> { { startRoom, GetHeuristic(startRoom, endRoom) } };

            while (openSet.Count > 0)
            {
                var current = GetLowestFScore(openSet, fScore);

                if (current == endRoom)
                    return ReconstructPath(cameFrom, current);

                openSet.Remove(current);

                if (!roomConnections.ContainsKey(current))
                    continue;

                foreach (var edge in roomConnections[current])
                {
                    if (!edge.ConnectionDoor.IsPassable())
                        continue;

                    var tentativeGScore = gScore[current] + edge.Cost;

                    if (!gScore.ContainsKey(edge.ToRoomId) || tentativeGScore < gScore[edge.ToRoomId])
                    {
                        cameFrom[edge.ToRoomId] = current;
                        gScore[edge.ToRoomId] = tentativeGScore;
                        fScore[edge.ToRoomId] = tentativeGScore + GetHeuristic(edge.ToRoomId, endRoom);
                        openSet.Add(edge.ToRoomId);
                    }
                }
            }

            return null; // No path found
        }

        private int GetLowestFScore(HashSet<int> openSet, Dictionary<int, float> fScore)
        {
            var lowest = -1;
            var lowestScore = float.MaxValue;

            foreach (var node in openSet)
                if (fScore.TryGetValue(node, out var score) && score < lowestScore)
                {
                    lowest = node;
                    lowestScore = score;
                }

            return lowest;
        }

        private float GetHeuristic(int from, int to)
        {
            if (roomNodes.TryGetValue(from, out var fromNode) &&
                roomNodes.TryGetValue(to, out var toNode))
                return Vector2.Distance(fromNode.CenterPosition, toNode.CenterPosition);

            return 0;
        }

        private List<int> ReconstructPath(Dictionary<int, int> cameFrom, int current)
        {
            var path = new List<int> { current };

            while (cameFrom.ContainsKey(current))
            {
                current = cameFrom[current];
                path.Insert(0, current);
            }

            return path;
        }

        public void BuildRoomGraph(Dictionary<int, Room> rooms)
        {
            roomNodes.Clear();
            roomConnections.Clear();

            foreach (var kvp in rooms)
            {
                var roomId = kvp.Key;
                var room = kvp.Value;

                var node = new RoomNode
                {
                    RoomId = roomId,
                    CenterPosition = new Vector2(room.Width / 2f, room.Height / 2f),
                    Doors = room.GetDoors()
                };

                roomNodes[roomId] = node;

                var edges = new List<RoomEdge>();
                foreach (var door in room.GetDoors())
                    edges.Add(new RoomEdge
                    {
                        FromRoomId = roomId,
                        ToRoomId = door.ConnectedRoomId,
                        ConnectionDoor = door,
                        Cost = 1f // Base cost, can be modified
                    });

                roomConnections[roomId] = edges;
            }
        }
    }
}