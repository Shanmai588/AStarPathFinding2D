using UnityEngine;

namespace RTS.Pathfinding
{
    public interface ITileChangeListener
    {
        void OnTileChanged(TileChangedEvent eventData);
    }

    public class TileChangedEvent
    {
        public TileChangedEvent(int roomId, Vector2Int pos, TileType oldType, TileType newType)
        {
            RoomId = roomId;
            Position = pos;
            OldType = oldType;
            NewType = newType;
        }

        public int RoomId { get; private set; }
        public Vector2Int Position { get; private set; }
        public TileType OldType { get; private set; }
        public TileType NewType { get; private set; }
    }
}