using UnityEngine;

namespace RTS.Pathfinding
{
    public class Door
    {
        public Door(Vector2Int pos, int targetRoom, Vector2Int targetPos)
        {
            PositionInRoom = pos;
            ConnectedRoomId = targetRoom;
            ConnectedPosition = targetPos;
            IsOpen = true;
        }

        public Vector2Int PositionInRoom { get; }

        public int ConnectedRoomId { get; }

        public Vector2Int ConnectedPosition { get; }

        public bool IsOpen { get; }

        public ConnectionInfo GetConnectionInfo()
        {
            return new ConnectionInfo
            {
                FromPosition = PositionInRoom,
                ToRoomId = ConnectedRoomId,
                ToPosition = ConnectedPosition,
                IsPassable = IsOpen
            };
        }

        public bool IsPassable()
        {
            return IsOpen;
        }
    }
}