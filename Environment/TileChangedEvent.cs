using UnityEngine;

namespace RTS.Pathfinding
{
    public interface ITileChangeListener : IEventListener<TileChangedEvent>
    {
        void OnTileChanged(TileChangedEvent eventData);
    }

    public class TileChangedEvent
    {
        public int RoomId { get; set; }
        public Vector2Int Position { get; set; }
        public TileType OldType { get; set; }
        public TileType NewType { get; set; }
    }
}