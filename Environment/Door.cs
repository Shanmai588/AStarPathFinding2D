using UnityEngine;

namespace RTS.Pathfinding
{
    public class Door
    {
        public Door(Vector2Int pos, int connectedRoom, Vector2Int connectedPos)
        {
            PositionInRoom = pos;
            ConnectedRoomId = connectedRoom;
            ConnectedPosition = connectedPos;
            IsOpen = true;
        }

        public Vector2Int PositionInRoom { get; }

        public int ConnectedRoomId { get; }

        public Vector2Int ConnectedPosition { get; }

        public bool IsOpen { get; private set; }

        public ConnectionInfo GetConnectionInfo()
        {
            return new ConnectionInfo(PositionInRoom, ConnectedRoomId, ConnectedPosition);
        }

        public bool IsPassable()
        {
            return IsOpen;
        }

        public void SetOpen(bool open)
        {
            IsOpen = open;
        }
    }

    public struct ConnectionInfo
    {
        public Vector2Int FromPosition;
        public int ToRoomId;
        public Vector2Int ToPosition;

        public ConnectionInfo(Vector2Int from, int toRoom, Vector2Int to)
        {
            FromPosition = from;
            ToRoomId = toRoom;
            ToPosition = to;
        }
    }
}