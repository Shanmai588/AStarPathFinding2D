using System.Collections.Generic;
using UnityEngine;

namespace RTS.Pathfinding
{
    public class GridDebugDisplay : MonoBehaviour
{
    [Header("Display Settings")]
    [SerializeField] private bool showGrid = true;
    [SerializeField] private bool showTileTypes = true;
    [SerializeField] private bool showRoomIds = true;
    [SerializeField] private bool showDoors = true;
    [SerializeField] private bool showPaths = true;
    [SerializeField] private bool showAgents = true;
    [SerializeField] private bool showReservations = false;
    
    [Header("Visual Settings")]
    [SerializeField] private float tileSize = 1f;
    [SerializeField] private float gridLineWidth = 0.05f;
    
    [Header("Colors")]
    [SerializeField] private Color gridColor = new Color(0.5f, 0.5f, 0.5f, 0.3f);
    [SerializeField] private Color groundColor = new Color(0.4f, 0.8f, 0.4f, 0.5f);
    [SerializeField] private Color waterColor = new Color(0.2f, 0.5f, 0.9f, 0.5f);
    [SerializeField] private Color mountainColor = new Color(0.6f, 0.6f, 0.6f, 0.5f);
    [SerializeField] private Color forestColor = new Color(0.2f, 0.6f, 0.2f, 0.5f);
    [SerializeField] private Color roadColor = new Color(0.8f, 0.7f, 0.5f, 0.5f);
    [SerializeField] private Color buildingColor = new Color(0.4f, 0.4f, 0.4f, 0.8f);
    [SerializeField] private Color blockedColor = new Color(0.2f, 0.2f, 0.2f, 0.8f);
    [SerializeField] private Color doorColor = new Color(1f, 1f, 0f, 0.8f);
    [SerializeField] private Color pathColor = new Color(0f, 1f, 1f, 0.8f);
    [SerializeField] private Color agentColor = new Color(1f, 0f, 0f, 1f);
    [SerializeField] private Color reservationColor = new Color(1f, 0.5f, 0f, 0.3f);
    
    private GridManager gridManager;
    private Dictionary<int, Vector2> roomOffsets = new Dictionary<int, Vector2>();
    private Camera mainCamera;
    
    void Start()
    {
        gridManager = FindFirstObjectByType<GridManager>();
        mainCamera = Camera.main;
        CalculateRoomOffsets();
    }
    
    void OnDrawGizmos()
    {
        if (gridManager == null || !Application.isPlaying) return;
        
        DrawRooms();
        DrawAgentsAndPaths();
    }
    
    private void CalculateRoomOffsets()
    {
        // Simple layout - place rooms side by side
        float currentX = 0;
        
        for (int i = 1; i <= 10; i++) // Check first 10 room IDs
        {
            var room = gridManager.GetRoom(i);
            if (room != null)
            {
                roomOffsets[i] = new Vector2(currentX, 0);
                currentX += room.Width * tileSize + 2f; // 2 unit gap between rooms
            }
        }
    }
    
    private void DrawRooms()
    {
        foreach (var kvp in roomOffsets)
        {
            var room = gridManager.GetRoom(kvp.Key);
            if (room == null) continue;
            
            var offset = kvp.Value;
            
            // Draw grid
            if (showGrid)
                DrawRoomGrid(room, offset);
            
            // Draw tiles
            if (showTileTypes)
                DrawRoomTiles(room, offset);
            
            // Draw room ID
            if (showRoomIds)
                DrawRoomId(room, offset);
            
            // Draw doors
            if (showDoors)
                DrawRoomDoors(room, offset);
        }
    }
    
    private void DrawRoomGrid(Room room, Vector2 offset)
    {
        Gizmos.color = gridColor;
        
        // Vertical lines
        for (int x = 0; x <= room.Width; x++)
        {
            Vector3 start = new Vector3(offset.x + x * tileSize, offset.y, 0);
            Vector3 end = new Vector3(offset.x + x * tileSize, offset.y + room.Height * tileSize, 0);
            Gizmos.DrawLine(start, end);
        }
        
        // Horizontal lines
        for (int y = 0; y <= room.Height; y++)
        {
            Vector3 start = new Vector3(offset.x, offset.y + y * tileSize, 0);
            Vector3 end = new Vector3(offset.x + room.Width * tileSize, offset.y + y * tileSize, 0);
            Gizmos.DrawLine(start, end);
        }
    }
    
    private void DrawRoomTiles(Room room, Vector2 offset)
    {
        for (int x = 0; x < room.Width; x++)
        {
            for (int y = 0; y < room.Height; y++)
            {
                var tile = room.GetTile(x, y);
                if (tile == null) continue;
                
                Color color = GetTileColor(tile.Type);
                Gizmos.color = color;
                
                Vector3 center = new Vector3(
                    offset.x + x * tileSize + tileSize * 0.5f,
                    offset.y + y * tileSize + tileSize * 0.5f,
                    0
                );
                
                Gizmos.DrawCube(center, Vector3.one * tileSize * 0.9f);
                
                
            }
        }
    }
    
    private Color GetTileColor(TileType type)
    {
        return type switch
        {
            TileType.Ground => groundColor,
            TileType.Water => waterColor,
            TileType.Mountain => mountainColor,
            TileType.Forest => forestColor,
            TileType.Road => roadColor,
            TileType.Building => buildingColor,
            TileType.Blocked => blockedColor,
            _ => groundColor
        };
    }
    
    private void DrawRoomId(Room room, Vector2 offset)
    {
        #if UNITY_EDITOR
        var style = new GUIStyle();
        style.normal.textColor = Color.white;
        style.fontSize = 20;
        style.fontStyle = FontStyle.Bold;
        style.alignment = TextAnchor.MiddleCenter;
        
        var worldPos = new Vector3(
            offset.x + room.Width * tileSize * 0.5f,
            offset.y + room.Height * tileSize + 1f,
            0
        );
        
        UnityEditor.Handles.Label(worldPos, $"Room {room.RoomId}", style);
        #endif
    }
    
    private void DrawRoomDoors(Room room, Vector2 offset)
    {
        Gizmos.color = doorColor;
        
        foreach (var door in room.GetDoors())
        {
            Vector3 doorPos = new Vector3(
                offset.x + door.PositionInRoom.x * tileSize + tileSize * 0.5f,
                offset.y + door.PositionInRoom.y * tileSize + tileSize * 0.5f,
                0
            );
            
            // Draw door marker
            Gizmos.DrawWireSphere(doorPos, tileSize * 0.3f);
            
            // Draw connection line to other room
            if (roomOffsets.TryGetValue(door.ConnectedRoomId, out Vector2 targetOffset))
            {
                Vector3 targetPos = new Vector3(
                    targetOffset.x + door.ConnectedPosition.x * tileSize + tileSize * 0.5f,
                    targetOffset.y + door.ConnectedPosition.y * tileSize + tileSize * 0.5f,
                    0
                );
                
                Gizmos.color = new Color(doorColor.r, doorColor.g, doorColor.b, 0.3f);
                Gizmos.DrawLine(doorPos, targetPos);
            }
        }
    }
    
    private void DrawAgentsAndPaths()
    {
        var agents = FindObjectsOfType<Agent>();
        
        foreach (var agent in agents)
        {
            if (!roomOffsets.TryGetValue(agent.CurrentRoomId, out Vector2 offset))
                continue;
            
            // Draw agent
            if (showAgents)
            {
                Gizmos.color = GetAgentColor(agent.Type);
                Vector3 agentPos = new Vector3(
                    offset.x + agent.CurrentPosition.x * tileSize + tileSize * 0.5f,
                    offset.y + agent.CurrentPosition.y * tileSize + tileSize * 0.5f,
                    0.1f // Slightly above tiles
                );
                
                Gizmos.DrawSphere(agentPos, tileSize * 0.4f);
                
                // Draw agent type icon
                DrawAgentTypeIcon(agent.Type, agentPos);
            }
            
            // Draw path
            if (showPaths)
            {
                DrawAgentPath(agent, offset);
            }
        }
    }
    
    private Color GetAgentColor(AgentType type)
    {
        return type switch
        {
            AgentType.Infantry => new Color(0f, 1f, 0f, 1f),
            AgentType.Vehicle => new Color(0.5f, 0.5f, 1f, 1f),
            AgentType.Flying => new Color(1f, 1f, 0f, 1f),
            AgentType.Naval => new Color(0f, 0.5f, 1f, 1f),
            _ => agentColor
        };
    }
    
    private void DrawAgentTypeIcon(AgentType type, Vector3 position)
    {
        Gizmos.color = Color.white;
        
        switch (type)
        {
            case AgentType.Infantry:
                // Draw simple person icon
                Gizmos.DrawLine(position + Vector3.up * 0.2f, position + Vector3.down * 0.2f);
                Gizmos.DrawLine(position + Vector3.left * 0.1f, position + Vector3.right * 0.1f);
                break;
            case AgentType.Vehicle:
                // Draw simple car icon
                Gizmos.DrawWireCube(position, new Vector3(0.3f, 0.2f, 0.1f));
                break;
            case AgentType.Flying:
                // Draw simple plane icon
                Gizmos.DrawLine(position + Vector3.left * 0.3f, position + Vector3.right * 0.3f);
                Gizmos.DrawLine(position + Vector3.forward * 0.2f, position + Vector3.back * 0.2f);
                break;
            case AgentType.Naval:
                // Draw simple boat icon
                Gizmos.DrawLine(position + Vector3.left * 0.2f + Vector3.down * 0.1f, 
                               position + Vector3.right * 0.2f + Vector3.down * 0.1f);
                break;
        }
    }
    
    private void DrawAgentPath(Agent agent, Vector2 offset)
    {
        // Use reflection to get the current path (since it's private)
        var pathField = agent.GetType().GetField("currentPath", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        
        if (pathField != null)
        {
            var path = pathField.GetValue(agent) as Path;
            if (path != null && path.IsValid)
            {
                var waypoints = path.GetWaypoints();
                if (waypoints.Count > 1)
                {
                    Gizmos.color = pathColor;
                    
                    for (int i = 0; i < waypoints.Count - 1; i++)
                    {
                        Vector3 start = new Vector3(
                            offset.x + waypoints[i].x * tileSize + tileSize * 0.5f,
                            offset.y + waypoints[i].y * tileSize + tileSize * 0.5f,
                            0.05f
                        );
                        
                        Vector3 end = new Vector3(
                            offset.x + waypoints[i + 1].x * tileSize + tileSize * 0.5f,
                            offset.y + waypoints[i + 1].y * tileSize + tileSize * 0.5f,
                            0.05f
                        );
                        
                        Gizmos.DrawLine(start, end);
                        
                        // Draw waypoint marker
                        Gizmos.DrawSphere(end, tileSize * 0.1f);
                    }
                }
            }
        }
    }
}

}