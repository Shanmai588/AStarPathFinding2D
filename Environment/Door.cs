using UnityEngine;

namespace RTS.Pathfinding
{
    public class Door
    {
        public Vector2Int positionInRoom;
        public int connectedRoomId;
        public Vector2Int connectedPosition;
        public bool isOpen = true;

        public struct ConnectionInfo
        {
            public int roomId;
            public Vector2Int position;
        }

        public ConnectionInfo GetConnectionInfo()
        {
            return new ConnectionInfo { roomId = connectedRoomId, position = connectedPosition };
        }

        public bool IsPassable()
        {
            return isOpen;
        }
    }

}