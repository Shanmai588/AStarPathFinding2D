using System.Collections.Generic;
using UnityEngine;
using Random = System.Random;

namespace RTS.Pathfinding
{
    public class RTSGameController : MonoBehaviour
    {
        private GridManager gridManager;
        private GridDebugDisplay debugDisplay;

        [Header("Test Settings")] [SerializeField]
        private GameObject agentPrefab;

        [SerializeField] private int numberOfTestAgents = 5;

        void Start()
        {
            gridManager = GetComponent<GridManager>();

            // Add debug display
            debugDisplay = gameObject.AddComponent<GridDebugDisplay>();

            // Create example rooms
            CreateExampleLevel();

            // Spawn test agents
            SpawnTestAgents();
        }
        private void CreateExampleLevel()
        {
            // Create Room 1 (20x20)
            var room1 = new Room(1, 20, 20);
            SetupRoomTerrain(room1);

            // Create Room 2 (15x15)
            var room2 = new Room(2, 15, 15);
            SetupRoomTerrain(room2);

            // Create Room 3 (10x25)
            var room3 = new Room(3, 10, 25);
            SetupRoomTerrain(room3);

            // Connect rooms with doors
            // Room 1 to Room 2
            var door1to2 = new Door(new Vector2Int(19, 10), 2, new Vector2Int(0, 7));
            var door2to1 = new Door(new Vector2Int(0, 7), 1, new Vector2Int(19, 10));

            // Room 2 to Room 3
            var door2to3 = new Door(new Vector2Int(14, 7), 3, new Vector2Int(0, 12));
            var door3to2 = new Door(new Vector2Int(0, 12), 2, new Vector2Int(14, 7));

            room1.AddDoor(door1to2);
            room2.AddDoor(door2to1);
            room2.AddDoor(door2to3);
            room3.AddDoor(door3to2);

            room1.AddConnectedRoom(2, room2);
            room2.AddConnectedRoom(1, room1);
            room2.AddConnectedRoom(3, room3);
            room3.AddConnectedRoom(2, room2);

            // Add rooms to grid manager
            gridManager.AddRoom(1, room1);
            gridManager.AddRoom(2, room2);
            gridManager.AddRoom(3, room3);

            // Build room graph for pathfinding
            var pathfinder = Singleton<HierarchicalPathfinder>.Instance;
            if (pathfinder != null)
            {
                var roomGraph = pathfinder.GetType().GetField("roomPathfinder",
                        System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                    ?.GetValue(pathfinder) as RoomGraphPathfinder;

                roomGraph?.BuildRoomGraph(new Dictionary<int, Room> { { 1, room1 }, { 2, room2 }, { 3, room3 } });
            }
        }

        private void SetupRoomTerrain(Room room)
        {
            // Add some varied terrain
            System.Random rand = new System.Random(room.RoomId);

            for (int x = 0; x < room.Width; x++)
            {
                for (int y = 0; y < room.Height; y++)
                {
                    var tile = room.GetTile(x, y);

                    // Add random terrain features
                    float r = (float)rand.NextDouble();
                    if (r < 0.1f)
                    {
                        tile.Type = TileType.Forest;
                    }
                    else if (r < 0.15f)
                    {
                        tile.Type = TileType.Mountain;
                    }
                    else if (r < 0.2f)
                    {
                        tile.Type = TileType.Water;
                    }
                    else if (r < 0.25f)
                    {
                        tile.Type = TileType.Road;
                    }
                    // Most tiles remain as ground
                }
            }

            // Add some blocked areas
            for (int i = 0; i < 3; i++)
            {
                int blockX = rand.Next(2, room.Width - 2);
                int blockY = rand.Next(2, room.Height - 2);

                for (int dx = -1; dx <= 1; dx++)
                {
                    for (int dy = -1; dy <= 1; dy++)
                    {
                        var tile = room.GetTile(blockX + dx, blockY + dy);
                        if (tile != null)
                            tile.Type = TileType.Building;
                    }
                }
            }
        }

        private void SpawnTestAgents()
        {
            if (agentPrefab == null)
            {
                // Create a simple agent prefab if none exists
                agentPrefab = new GameObject("Agent");
                agentPrefab.AddComponent<Agent>();
            }

            for (int i = 0; i < numberOfTestAgents; i++)
            {
                var agentGO = Instantiate(agentPrefab);
                agentGO.name = $"Agent_{i}";

                var agent = agentGO.GetComponent<Agent>();

                // Set random agent type
                agent.GetType().GetField("type",
                        System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                    ?.SetValue(agent, (AgentType)UnityEngine.Random.Range(0, 4));

                // Set starting position
                int startRoom = UnityEngine.Random.Range(1, 4);
                var room = gridManager.GetRoom(startRoom);

                if (room != null)
                {
                    Vector2Int startPos = Vector2Int.zero;
                    bool foundValidPos = false;

                    // Find a valid starting position
                    for (int attempt = 0; attempt < 50; attempt++)
                    {
                        int x = UnityEngine.Random.Range(0, room.Width);
                        int y = UnityEngine.Random.Range(0, room.Height);

                        var tile = room.GetTile(x, y);
                        if (tile != null && tile.IsWalkable && !tile.IsOccupied())
                        {
                            startPos = new Vector2Int(x, y);
                            foundValidPos = true;
                            break;
                        }
                    }

                    if (foundValidPos)
                    {
                        agent.GetType().GetField("currentPosition",
                                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                            ?.SetValue(agent, startPos);

                        agent.GetType().GetField("currentRoomId",
                                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                            ?.SetValue(agent, startRoom);

                        var tile = room.GetTile(startPos.x, startPos.y);
                        tile?.SetOccupant(agent);
                    }
                }
            }
        }

        private void HandleMouseClick()
        {
            var ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            var worldPos = ray.GetPoint(-ray.origin.z / ray.direction.z);

            // Find which room and tile was clicked
            foreach (var kvp in debugDisplay.GetType().GetField("roomOffsets",
                             System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                         ?.GetValue(debugDisplay) as Dictionary<int, Vector2>)
            {
                var room = gridManager.GetRoom(kvp.Key);
                if (room == null) continue;

                var offset = kvp.Value;
                var localPos = worldPos - new Vector3(offset.x, offset.y, 0);

                int tileX = Mathf.FloorToInt(localPos.x);
                int tileY = Mathf.FloorToInt(localPos.y);

                if (room.IsValidPosition(tileX, tileY))
                {
                    // Select all agents and send them to clicked position
                    var agents = FindObjectsOfType<Agent>();
                    foreach (var agent in agents)
                    {
                    }

                    Debug.Log($"Sending agents to Room {kvp.Key}, Position ({tileX}, {tileY})");
                    break;
                }
            }
        }

        private void HandleRightClick()
        {
            // Toggle terrain type at clicked position
            var ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            var worldPos = ray.GetPoint(-ray.origin.z / ray.direction.z);

            foreach (var kvp in debugDisplay.GetType().GetField("roomOffsets",
                             System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                         ?.GetValue(debugDisplay) as Dictionary<int, Vector2>)
            {
                var room = gridManager.GetRoom(kvp.Key);
                if (room == null) continue;

                var offset = kvp.Value;
                var localPos = worldPos - new Vector3(offset.x, offset.y, 0);

                int tileX = Mathf.FloorToInt(localPos.x);
                int tileY = Mathf.FloorToInt(localPos.y);

                if (room.IsValidPosition(tileX, tileY))
                {
                    var tile = room.GetTile(tileX, tileY);
                    if (tile != null)
                    {
                        // Cycle through tile types
                        int currentType = (int)tile.Type;
                        int nextType = (currentType + 1) % System.Enum.GetValues(typeof(TileType)).Length;

                        gridManager.UpdateTile(kvp.Key, tileX, tileY, (TileType)nextType);
                        Debug.Log($"Changed tile at ({tileX}, {tileY}) to {(TileType)nextType}");
                    }

                    break;
                }
            }
        }
    }
}