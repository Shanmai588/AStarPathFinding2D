using UnityEngine;

namespace RTS.Pathfinding
{
    public class NavigationSystemSetup : MonoBehaviour
    {
        [SerializeField] private RoomBasedNavigationController navigationController;
        [SerializeField] private GameObject unitPrefab;

        private void Start()
        {
            // Create some example rooms
            CreateExampleRooms();

            // Spawn some units
            SpawnExampleUnits();
        }

        private void CreateExampleRooms()
        {
            // Create Room 1
            var room1 = new Room(1, 20, 20, Vector2.zero);

            // Set some tiles
            for (var x = 5; x < 15; x++) room1.SetTile(x, 10, new Tile(new Vector2Int(x, 10), TileType.Water));

            // Add a door to connect to room 2
            var door1to2 = new Door(new Vector2Int(19, 10), 2, new Vector2Int(0, 10));
            room1.AddDoor(door1to2);

            // Create Room 2
            var room2 = new Room(2, 20, 20, new Vector2(20, 0));

            // Add door back to room 1
            var door2to1 = new Door(new Vector2Int(0, 10), 1, new Vector2Int(19, 10));
            room2.AddDoor(door2to1);

            // Connect the rooms
            room1.AddConnectedRoom(2, room2);
            room2.AddConnectedRoom(1, room1);

            // Add rooms to the navigation system
            navigationController.AddRoom(room1);
            navigationController.AddRoom(room2);
        }

        private void SpawnExampleUnits()
        {
            // Spawn an infantry unit
            SpawnUnit(AgentType.Infantry, new Vector2(5, 5), 1);
        }

        private void SpawnUnit(AgentType type, Vector2 worldPos, int roomId)
        {
            var unitGO = Instantiate(unitPrefab, new Vector3(worldPos.x, worldPos.y, 0), Quaternion.identity);

            // Create agent
            int actualRoom;
            var gridPos = navigationController.WorldToGrid(worldPos, out actualRoom);
            var agent = new Agent(unitGO.GetInstanceID(), type, gridPos, actualRoom, navigationController);

            // Add motor component first
            var motor = unitGO.GetComponent<Motor>();
            if (motor == null)
                motor = unitGO.AddComponent<Motor>();

            // Add mover component
            var mover = unitGO.GetComponent<RTSUnitMover>();
            if (mover == null)
                mover = unitGO.AddComponent<RTSUnitMover>();

            // Initialize components
            mover.Initialize(agent, navigationController);

            // Register agent
            navigationController.RegisterAgent(agent);

            Debug.Log(
                $"Spawned {type} unit at world position {worldPos}, grid position {gridPos} in room {actualRoom}");
        }
    }
}