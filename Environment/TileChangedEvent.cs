using UnityEngine;

namespace RTS.Pathfinding
{
    public class TileChangedEvent
    {
        public int roomId;
        public Vector2Int position;
        public TileType oldType, newType;
    }

    public interface ITileChangeListener : IEventListener<TileChangedEvent>
    {
        void OnTileChanged(TileChangedEvent eventData);
    }
}