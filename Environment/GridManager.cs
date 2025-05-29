using System.Collections.Generic;
using UnityEngine;

namespace RTS.Pathfinding
{
    // Grid Management
    public class GridManager
    {
        private readonly EventBus eventBus;
        private readonly Dictionary<int, Room> rooms;

        public GridManager(EventBus bus)
        {
            rooms = new Dictionary<int, Room>();
            eventBus = bus;
        }

        public Room GetRoom(int roomId)
        {
            return rooms.TryGetValue(roomId, out var room) ? room : null;
        }

        public Tile GetTile(int roomId, int x, int y)
        {
            var room = GetRoom(roomId);
            return room?.GetTile(x, y);
        }

        public void UpdateTile(int roomId, int x, int y, TileType newType)
        {
            var room = GetRoom(roomId);
            if (room == null) return;

            var tile = room.GetTile(x, y);
            if (tile == null) return;

            var oldType = tile.Type;
            var newTile = new Tile(new Vector2Int(x, y), newType);
            room.SetTile(x, y, newTile);

            // Publish tile change event
            var changeEvent = new TileChangedEvent(roomId, new Vector2Int(x, y), oldType, newType);
            eventBus.Publish(changeEvent);
        }

        public void RegisterForTileChanges(ITileChangeListener listener)
        {
            eventBus.Subscribe(new TileChangeListenerAdapter(listener));
        }

        public void AddRoom(Room room)
        {
            if (room != null)
                rooms[room.RoomId] = room;
        }

        public void RemoveRoom(int roomId)
        {
            rooms.Remove(roomId);
        }

        public Dictionary<int, Room> GetAllRooms()
        {
            return new Dictionary<int, Room>(rooms);
        }
    }
}