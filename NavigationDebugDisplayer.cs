using UnityEngine;

namespace RTS.Pathfinding
{
    public class NavigationDebugDisplayer : MonoBehaviour
    {
        [SerializeField] private bool showRoomBounds = true;
        [SerializeField] private bool showDoors = true;
        [SerializeField] private bool showPaths = true;
        [SerializeField] private bool showReservations = true;
        [SerializeField] private Color roomBoundColor = Color.green;
        [SerializeField] private Color doorColor = Color.yellow;
        [SerializeField] private Color pathColor = Color.yellow;
        [SerializeField] private Color reservationColor = Color.red;
        private RoomBasedNavigationController controller;
        private GridManager gridManager;

        private void Start()
        {
            controller = GetComponent<RoomBasedNavigationController>();
            if (controller == null)
                controller = FindObjectOfType<RoomBasedNavigationController>();
        }

        private void OnDrawGizmos()
        {
            if (controller == null) return;

            if (showRoomBounds)
                DrawRoomBounds();
            if (showDoors)
                DrawDoors();
            if (showPaths)
                DrawActivePaths();
            if (showReservations)
                DrawReservations();
        }

        private void OnDrawGizmosSelected()
        {
            if (controller == null) return;

            // Draw more detailed info when selected
            foreach (var room in controller.GetAllRooms().Values) DrawTileGrid(room);
        }

        private void DrawRoomBounds()
        {
            Gizmos.color = roomBoundColor;
            foreach (var room in controller.GetAllRooms().Values)
            {
                var bounds = controller.GetRoomBounds(room.RoomId);
                Gizmos.DrawWireCube(bounds.center, bounds.size);
            }
        }

        private void DrawDoors()
        {
            Gizmos.color = doorColor;
            foreach (var door in controller.GetAllDoors())
            {
                // Draw door connections
                var fromRoom = controller.GetRoom(door.ConnectedRoomId);
                if (fromRoom != null)
                {
                    var fromWorld = fromRoom.GridToWorld(door.PositionInRoom);
                    var toWorld = fromRoom.GridToWorld(door.ConnectedPosition);
                    Gizmos.DrawLine(new Vector3(fromWorld.x, fromWorld.y, 0),
                        new Vector3(toWorld.x, toWorld.y, 0));
                    Gizmos.DrawWireSphere(new Vector3(fromWorld.x, fromWorld.y, 0), 0.3f);
                }
            }
        }

        private void DrawActivePaths()
        {
            Gizmos.color = pathColor;
            foreach (var path in controller.GetActivePaths())
            {
                var waypoints = path.GetWaypoints();
                for (var i = 0; i < waypoints.Count - 1; i++)
                {
                    // Would need room ID to properly convert to world positions
                    var from = new Vector3(waypoints[i].x, waypoints[i].y, 0);
                    var to = new Vector3(waypoints[i + 1].x, waypoints[i + 1].y, 0);
                    Gizmos.DrawLine(from, to);
                }
            }
        }

        private void DrawReservations()
        {
            Gizmos.color = reservationColor;
            foreach (var reservation in controller.GetActiveReservations())
            {
                var pos = new Vector3(reservation.Position.x, reservation.Position.y, 0);
                Gizmos.DrawWireCube(pos, Vector3.one * 0.8f);
            }
        }

        private void DrawTileGrid(Room room)
        {
            for (var x = 0; x < room.Width; x++)
            for (var y = 0; y < room.Height; y++)
            {
                var tile = room.GetTile(x, y);
                if (tile != null)
                {
                    var worldPos = room.GridToWorld(new Vector2Int(x, y));

                    // Color based on tile type
                    var tileColor = tile.Type switch
                    {
                        TileType.Ground => Color.green,
                        TileType.Water => Color.blue,
                        TileType.Mountain => Color.gray,
                        TileType.Forest => new Color(0, 0.5f, 0),
                        TileType.Road => Color.yellow,
                        TileType.Building => Color.red,
                        TileType.Blocked => Color.black,
                        _ => Color.white
                    };

                    if (!tile.IsWalkable)
                        tileColor *= 0.5f;
                    if (tile.IsOccupied())
                        tileColor = Color.magenta;

                    Gizmos.color = tileColor;
                    Gizmos.DrawWireCube(new Vector3(worldPos.x, worldPos.y, 0), Vector3.one * 0.9f);
                }
            }
        }
    }
}