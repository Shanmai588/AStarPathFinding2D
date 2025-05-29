using UnityEngine;

namespace RTS.Pathfinding
{
    public class NavigationDebugDisplayer : MonoBehaviour
    {
        [Header("Visualization Settings")] [SerializeField]
        private bool showRoomBounds = true;

        [SerializeField] private bool showDoors = true;
        [SerializeField] private bool showPaths = true;
        [SerializeField] private bool showReservations = false;

        [Header("Colors")] [SerializeField] private Color roomBoundColor = Color.blue;
        [SerializeField] private Color doorColor = Color.green;
        [SerializeField] private Color pathColor = Color.yellow;
        [SerializeField] private Color reservationColor = Color.red;

        private RoomBasedNavigationController controller;
        private GridManager gridManager;

        private void Start()
        {
            controller = RoomBasedNavigationController.Instance;
            if (controller != null)
            {
              gridManager = controller.GridManager;
            }
        }

        private void OnDrawGizmos()
        {
            if (controller == null) return;

            if (showRoomBounds) DrawRoomBounds();
            if (showDoors) DrawDoors();
            if (showPaths) DrawActivePaths();
            if (showReservations) DrawReservations();
        }

        private void OnDrawGizmosSelected()
        {
            if (controller == null) return;

            // Draw detailed tile grid for selected rooms
            foreach (var room in controller.GetAllRooms().Values)
            {
                DrawTileGrid(room);
            }
        }

        private void DrawRoomBounds()
        {
            Gizmos.color = roomBoundColor;
            foreach (var room in controller.GetAllRooms().Values)
            {
                var bounds = controller.GetRoomBounds(room.roomId);
                Gizmos.DrawWireCube(bounds.center, bounds.size);
            }
        }

        private void DrawDoors()
        {
            Gizmos.color = doorColor;
            foreach (var door in controller.GetAllDoors())
            {
                var room = controller.GetRoom(0); // This needs proper room lookup
                if (room != null)
                {
                    var worldPos = controller.GridToWorld(room.roomId, door.positionInRoom);
                    Gizmos.DrawWireSphere(worldPos, 0.5f);
                }
            }
        }

        private void DrawActivePaths()
        {
            Gizmos.color = pathColor;
            foreach (var path in controller.GetActivePaths())
            {
                if (path != null && path.waypoints.Count > 1)
                {
                    for (int i = 0; i < path.waypoints.Count - 1; i++)
                    {
                        var from = controller.GridToWorld(0, path.waypoints[i]); // Simplified room lookup
                        var to = controller.GridToWorld(0, path.waypoints[i + 1]);
                        Gizmos.DrawLine(from, to);
                    }
                }
            }
        }

        private void DrawReservations()
        {
            Gizmos.color = reservationColor;
            foreach (var reservation in controller.GetActiveReservations())
            {
                var worldPos = controller.GridToWorld(0, reservation.position); // Simplified
                Gizmos.DrawWireCube(worldPos, Vector3.one * 0.8f);
            }
        }

        private void DrawTileGrid(Room room)
        {
            for (int x = 0; x < room.width; x++)
            {
                for (int y = 0; y < room.height; y++)
                {
                    var tile = room.GetTile(x, y);
                    if (tile != null)
                    {
                        var worldPos = room.GridToWorld(new Vector2Int(x, y));

                        // Color based on tile type
                        switch (tile.type)
                        {
                            case TileType.Wall:
                                Gizmos.color = Color.red;
                                break;
                            case TileType.Water:
                                Gizmos.color = Color.blue;
                                break;
                            case TileType.Rough:
                                Gizmos.color = Color.brown;
                                break;
                            case TileType.Mud:
                                Gizmos.color = Color.yellow;
                                break;
                            default:
                                Gizmos.color = Color.white;
                                break;
                        }

                        if (!tile.isWalkable)
                            Gizmos.color = Color.red;

                        Gizmos.DrawWireCube(worldPos, Vector3.one * 0.9f);
                    }
                }
            }
        }
    }
}