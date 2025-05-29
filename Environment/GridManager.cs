using System.Collections.Generic;
using UnityEngine;

namespace RTS.Pathfinding
{
    
    // Grid Management
    public class GridManager : ITileChangeListener
    {
        private Dictionary<int, Room> rooms = new Dictionary<int, Room>();
        private EventBus eventBus;

        public GridManager(EventBus bus)
        {
            eventBus = bus;
            eventBus.Subscribe<TileChangedEvent>(this);
        }

        public Room GetRoom(int roomId)
        {
            return rooms.ContainsKey(roomId) ? rooms[roomId] : null;
        }

        public Tile GetTile(int roomId, int x, int y)
        {
            var room = GetRoom(roomId);
            return room?.GetTile(x, y);
        }

        public void UpdateTile(int roomId, int x, int y, TileType newType)
        {
            var tile = GetTile(roomId, x, y);
            if (tile != null)
            {
                var oldType = tile.type;
                tile.type = newType;
            
                eventBus.Publish(new TileChangedEvent
                {
                    roomId = roomId,
                    position = new Vector2Int(x, y),
                    oldType = oldType,
                    newType = newType
                });
            }
        }

        public void AddRoom(Room room)
        {
            rooms[room.roomId] = room;
        }

        public void RemoveRoom(int roomId)
        {
            rooms.Remove(roomId);
        }

        public Dictionary<int, Room> GetAllRooms()
        {
            return rooms;
        }

        public void OnEvent(TileChangedEvent eventData)
        {
            OnTileChanged(eventData);
        }

        public void OnTileChanged(TileChangedEvent eventData)
        {
            // Handle tile change - could invalidate pathfinding cache, etc.
        }
    }

}