using System;
using System.Collections.Generic;
using UnityEngine;

namespace RTS.Pathfinding 
{
    // Command object representing a pathfinding request
    public class PathRequest
    {
        // Start and goal tile indices
        public int StartIndex { get; private set; }
        public int GoalIndex { get; private set; }
        
        // The agent making the request
        public AStarNavAgent Agent { get; private set; }
        
        // Cost calculation strategy
        public ICostProvider CostProvider { get; private set; }
        
        // Callback for when the path is ready
        public Action<PathResult> OnPathReady { get; private set; }
        
        // Room information for hierarchical pathfinding
        public Room StartRoom { get; set; }
        public Room GoalRoom { get; set; }
        
        public PathRequest(int startIndex, int goalIndex, AStarNavAgent agent, ICostProvider costProvider, Action<PathResult> onPathReady)
        {
            StartIndex = startIndex;
            GoalIndex = goalIndex;
            Agent = agent;
            CostProvider = costProvider ?? new DefaultCostProvider();
            OnPathReady = onPathReady;
        }
    }
    
    // Result of a pathfinding request
    public class PathResult
    {
        // Original request
        public PathRequest Request { get; private set; }
        
        // Success flag
        public bool Success { get; private set; }
        
        // List of waypoints in the path
        public List<Vector3> Waypoints { get; private set; }
        
        // List of tile indices in the path
        public List<int> PathIndices { get; private set; }
        
        // For coarse paths: list of doors to pass through
        public List<Door> DoorsToPass { get; private set; }
        
        // Constructor for successful paths
        public PathResult(PathRequest request, List<int> pathIndices, List<Vector3> waypoints, List<Door> doorsToPass = null)
        {
            Request = request;
            Success = true;
            PathIndices = pathIndices;
            Waypoints = waypoints;
            DoorsToPass = doorsToPass ?? new List<Door>();
        }
        
        // Constructor for failed paths
        public PathResult(PathRequest request)
        {
            Request = request;
            Success = false;
            PathIndices = new List<int>();
            Waypoints = new List<Vector3>();
            DoorsToPass = new List<Door>();
        }
    }
    
    // Interface for A* heuristic calculation
    public interface IHeuristic
    {
        float EstimateDistance(int fromIndex, int toIndex, GridManager gridManager);
    }
    
    // Manhattan distance heuristic
    public class ManhattanHeuristic : IHeuristic
    {
        public float EstimateDistance(int fromIndex, int toIndex, GridManager gridManager)
        {
            Vector2Int fromPos = gridManager.IndexToGrid(fromIndex);
            Vector2Int toPos = gridManager.IndexToGrid(toIndex);
            
            return Mathf.Abs(fromPos.x - toPos.x) + Mathf.Abs(fromPos.y - toPos.y);
        }
    }
    
    // Euclidean distance heuristic
    public class EuclideanHeuristic : IHeuristic
    {
        public float EstimateDistance(int fromIndex, int toIndex, GridManager gridManager)
        {
            Vector2Int fromPos = gridManager.IndexToGrid(fromIndex);
            Vector2Int toPos = gridManager.IndexToGrid(toIndex);
            
            return Vector2Int.Distance(fromPos, toPos);
        }
    }
    
    // Chebyshev distance heuristic (allows diagonal movement)
    public class ChebyshevHeuristic : IHeuristic
    {
        public float EstimateDistance(int fromIndex, int toIndex, GridManager gridManager)
        {
            Vector2Int fromPos = gridManager.IndexToGrid(fromIndex);
            Vector2Int toPos = gridManager.IndexToGrid(toIndex);
            
            return Mathf.Max(Mathf.Abs(fromPos.x - toPos.x), Mathf.Abs(fromPos.y - toPos.y));
        }
    }
}