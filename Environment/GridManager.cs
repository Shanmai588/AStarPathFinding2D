using System.Collections.Generic;
using UnityEngine;

namespace RTS.Pathfinding
{
    /// <summary>
    ///     Manages a 2D grid of tiles for pathfinding.
    /// </summary>
    public class GridManager : MonoBehaviour
    {
        private EventBus eventBus;
        private HierarchicalPathfinder pathfinder;
        private PathRequestManager pathRequestManager;
        private ReservationTable reservationTable;
        private readonly Dictionary<int, Room> rooms = new();

        private void Awake()
        {
            eventBus = new EventBus();
            reservationTable = Singleton<ReservationTable>.Instance;
            pathRequestManager = Singleton<PathRequestManager>.Instance;
            pathfinder = Singleton<HierarchicalPathfinder>.Instance;
        }

        private void Update()
        {
            pathRequestManager.ProcessRequests();
            reservationTable.UpdateReservations(Time.deltaTime);
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
            if (room != null)
            {
                var tile = room.GetTile(x, y);
                if (tile != null)
                {
                    var oldType = tile.Type;
                    tile.Type = newType;

                    var tileEvent = new TileChangedEvent
                    {
                        RoomId = roomId,
                        Position = new Vector2Int(x, y),
                        OldType = oldType,
                        NewType = newType
                    };
                    eventBus.Publish(tileEvent);
                }
            }
        }

        public Path GetPath(PathRequest request)
        {
            return pathfinder.FindPath(request);
        }

        public void RegisterForTileChanges(ITileChangeListener listener)
        {
            eventBus.Subscribe(listener);
        }

        public void AddRoom(int roomId, Room room)
        {
            rooms[roomId] = room;
        }
    }
}