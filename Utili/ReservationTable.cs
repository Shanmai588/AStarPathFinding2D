using System.Collections.Generic;
using UnityEngine;

namespace RTS.Pathfinding 
{
    // Cooperative A* reservation table to prevent agent collisions
    public class ReservationTable : MonoBehaviour 
    {
        // The maximum number of timesteps to track
        [SerializeField] private int maxTimesteps = 32;
        
        // Grid manager reference
        [SerializeField] private GridManager gridManager;
        
        // Structure to hold timed reservations
        private Dictionary<int, Dictionary<int, AStarNavAgent>> reservations = new Dictionary<int, Dictionary<int, AStarNavAgent>>();
        
        private void Awake()
        {
            if (gridManager == null)
                gridManager = FindObjectOfType<GridManager>();
                
            // Initialize timestep dictionaries
            for (int t = 0; t < maxTimesteps; t++)
            {
                reservations[t] = new Dictionary<int, AStarNavAgent>();
            }
        }
        
        // Check if a tile is reserved at a specific timestep
        public bool IsReserved(int tileIndex, int timestep)
        {
            if (timestep < 0 || timestep >= maxTimesteps)
                return false;
                
            return reservations[timestep].ContainsKey(tileIndex);
        }
        
        // Get the agent that has reserved a tile at a specific timestep
        public AStarNavAgent GetReservation(int tileIndex, int timestep)
        {
            if (timestep < 0 || timestep >= maxTimesteps)
                return null;
                
            if (reservations[timestep].TryGetValue(tileIndex, out AStarNavAgent agent))
                return agent;
                
            return null;
        }
        
        // Reserve a tile for an agent at a specific timestep
        public bool Reserve(int tileIndex, int timestep, AStarNavAgent agent)
        {
            if (timestep < 0 || timestep >= maxTimesteps)
                return false;
                
            // Check if already reserved
            if (IsReserved(tileIndex, timestep))
            {
                // Don't allow reservation if it's by a different agent
                if (GetReservation(tileIndex, timestep) != agent)
                    return false;
            }
            
            // Add the reservation
            reservations[timestep][tileIndex] = agent;
            return true;
        }
        
        // Reserve a sequence of tiles along a path for an agent
        public bool ReserveSequence(List<int> path, int startTimestep, AStarNavAgent agent)
        {
            if (path == null || path.Count == 0)
                return true;
                
            // Check if the entire path can be reserved
            for (int i = 0; i < path.Count; i++)
            {
                int timestep = startTimestep + i;
                if (timestep >= maxTimesteps)
                    break;
                    
                // If any tile is already reserved by another agent, fail
                if (IsReserved(path[i], timestep) && GetReservation(path[i], timestep) != agent)
                    return false;
            }
            
            // Reserve the entire path
            for (int i = 0; i < path.Count; i++)
            {
                int timestep = startTimestep + i;
                if (timestep >= maxTimesteps)
                    break;
                    
                reservations[timestep][path[i]] = agent;
            }
            
            // Additionally, reserve the final position for some time
            int finalIndex = path[path.Count - 1];
            for (int t = startTimestep + path.Count; t < Mathf.Min(startTimestep + path.Count + 5, maxTimesteps); t++)
            {
                reservations[t][finalIndex] = agent;
            }
            
            return true;
        }
        
        // Release all reservations for an agent
        public void ReleaseAll(AStarNavAgent agent)
        {
            for (int t = 0; t < maxTimesteps; t++)
            {
                List<int> tilesToRelease = new List<int>();
                
                foreach (var kvp in reservations[t])
                {
                    if (kvp.Value == agent)
                    {
                        tilesToRelease.Add(kvp.Key);
                    }
                }
                
                // Remove the reservations
                foreach (int tileIndex in tilesToRelease)
                {
                    reservations[t].Remove(tileIndex);
                }
            }
        }
        

        
        // Advance time by shifting reservations
        public void AdvanceTime()
        {
            // Move all reservations one timestep earlier
            for (int t = 0; t < maxTimesteps - 1; t++)
            {
                reservations[t] = reservations[t + 1];
            }
            
            // Create a new empty dictionary for the last timestep
            reservations[maxTimesteps - 1] = new Dictionary<int, AStarNavAgent>();
        }
        
        // Find a free tile near the given position within a specific timestep
        public int FindNearestFreePosition(int tileIndex, int timestep, AStarNavAgent agent, int maxRadius = 5)
        {
            // If the tile itself is free, return it
            if (!IsReserved(tileIndex, timestep) || GetReservation(tileIndex, timestep) == agent)
                return tileIndex;
                
            Vector2Int startPos = gridManager.IndexToGrid(tileIndex);
            
            // Search in expanding squares around the start position
            for (int radius = 1; radius <= maxRadius; radius++)
            {
                for (int dx = -radius; dx <= radius; dx++)
                {
                    for (int dy = -radius; dy <= radius; dy++)
                    {
                        // Only check tiles exactly at the current radius
                        if (Mathf.Abs(dx) != radius && Mathf.Abs(dy) != radius)
                            continue;
                            
                        Vector2Int checkPos = new Vector2Int(startPos.x + dx, startPos.y + dy);
                        
                        // Skip if out of bounds
                        if (checkPos.x < 0 || checkPos.x >= gridManager.Width || checkPos.y < 0 || checkPos.y >= gridManager.Height)
                            continue;
                            
                        int checkIndex = gridManager.GridToIndex(checkPos);
                        
                        // Check if the tile is walkable and not reserved
                        if (gridManager.GetTile(checkIndex).Walkable && 
                            (!IsReserved(checkIndex, timestep) || GetReservation(checkIndex, timestep) == agent))
                        {
                            return checkIndex;
                        }
                    }
                }
            }
            
            // No free position found
            return -1;
        }
    }
}