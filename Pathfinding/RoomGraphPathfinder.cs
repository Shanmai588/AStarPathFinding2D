using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace RTS.Pathfinding
{
    public class RoomGraphPathfinder
    {
        private readonly Dictionary<int, List<RoomEdge>> roomConnections;
        private readonly Dictionary<int, RoomNode> roomNodes;

        public RoomGraphPathfinder()
        {
            roomNodes = new Dictionary<int, RoomNode>();
            roomConnections = new Dictionary<int, List<RoomEdge>>();
        }

        public List<int> FindRoomSequence(int startRoom, int endRoom)
        {
            if (startRoom == endRoom)
                return new List<int> { startRoom };

            var openSet = new Dictionary<int, float>();
            var cameFrom = new Dictionary<int, int>();
            var gScore = new Dictionary<int, float>();

            openSet[startRoom] = 0;
            gScore[startRoom] = 0;

            while (openSet.Count > 0)
            {
                var current = openSet.OrderBy(kvp => kvp.Value).First().Key;
                openSet.Remove(current);

                if (current == endRoom)
                    return ReconstructRoomPath(cameFrom, current);

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
                        openSet[edge.ToRoomId] = tentativeGScore;
                    }
                }
            }

            return null; // No path found
        }

        public void BuildRoomGraph(Dictionary<int, Room> rooms)
        {
            roomNodes.Clear();
            roomConnections.Clear();

            foreach (var kvp in rooms)
            {
                var room = kvp.Value;
                var center = room.WorldPosition + new Vector2(room.Width / 2f, room.Height / 2f);
                var node = new RoomNode(room.RoomId, center, room.GetDoors());
                roomNodes[room.RoomId] = node;

                var edges = new List<RoomEdge>();
                foreach (var door in room.GetDoors()) edges.Add(new RoomEdge(room.RoomId, door.ConnectedRoomId, door));
                roomConnections[room.RoomId] = edges;
            }
        }

        private List<int> ReconstructRoomPath(Dictionary<int, int> cameFrom, int current)
        {
            var path = new List<int> { current };

            while (cameFrom.ContainsKey(current))
            {
                current = cameFrom[current];
                path.Add(current);
            }

            path.Reverse();
            return path;
        }
    }
}