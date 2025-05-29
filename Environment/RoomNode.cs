using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace RTS.Pathfinding
{
    public class RoomNode
    {
        public RoomNode(int roomId, Vector2 center, List<Door> doors)
        {
            RoomId = roomId;
            CenterPosition = center;
            Doors = new List<Door>(doors);
        }

        public int RoomId { get; private set; }
        public Vector2 CenterPosition { get; private set; }
        public List<Door> Doors { get; }

        public float GetConnectionCost(int targetRoom)
        {
            var door = Doors.FirstOrDefault(d => d.ConnectedRoomId == targetRoom);
            return door != null && door.IsPassable() ? 1f : float.MaxValue;
        }
    }

    public class RoomEdge
    {
        public RoomEdge(int from, int to, Door door, float cost = 1f)
        {
            FromRoomId = from;
            ToRoomId = to;
            ConnectionDoor = door;
            Cost = cost;
        }

        public int FromRoomId { get; private set; }
        public int ToRoomId { get; private set; }
        public Door ConnectionDoor { get; private set; }
        public float Cost { get; private set; }
    }
}