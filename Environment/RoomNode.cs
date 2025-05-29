using System.Collections.Generic;
using UnityEngine;

namespace RTS.Pathfinding
{
    public class RoomNode
    {
        public int roomId;
        public Vector2 centerPosition;
        public List<Door> doors = new List<Door>();

        public float GetConnectionCost(int targetRoom)
        {
            return Vector2.Distance(centerPosition, new Vector2(targetRoom, 0)); // Simplified
        }
    }
    
    public class RoomEdge
    {
        public int fromRoomId, toRoomId;
        public Door connectionDoor;
        public float cost;
    }

}