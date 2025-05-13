using System;
using System.Collections.Generic;
using UnityEngine;

namespace RTS.Pathfinding 
{
    // Cache for storing and reusing path segments
    public class PathCache : MonoBehaviour 
    {
        [SerializeField] private int maxCachedPaths = 100;
        
        // References
        [SerializeField] private GridManager gridManager;
        
        // Cache for room-to-room paths
        private Dictionary<RoomPathKey, CachedRoomPath> roomPathCache = new Dictionary<RoomPathKey, CachedRoomPath>();
        
        // Cache for fine paths within rooms
        private Dictionary<TilePathKey, CachedTilePath> tilePathCache = new Dictionary<TilePathKey, CachedTilePath>();
        
        // Counters for statistics
        private int cacheHits = 0;
        private int cacheMisses = 0;
        
        private void Awake() 
        {
            if (gridManager == null)
                gridManager = FindObjectOfType<GridManager>();
            
            // Subscribe to tile change events
            gridManager.TileChanged += OnTileChanged;
        }
        
        private void OnDestroy() 
        {
            // Unsubscribe from tile change events
            if (gridManager != null)
                gridManager.TileChanged -= OnTileChanged;
        }
        
        // Handle tile changes
        private void OnTileChanged(int tileIndex) 
        {
            // Get the tile and its version
            Tile tile = gridManager.GetTile(tileIndex);
            ushort newVersion = tile.Version;
            
            // Find paths in the cache that include this tile
            List<TilePathKey> tileKeysToRemove = new List<TilePathKey>();
            
            foreach (var kvp in tilePathCache) 
            {
                // Check if the path includes the changed tile
                if (kvp.Value.PathIndices.Contains(tileIndex)) 
                {
                    tileKeysToRemove.Add(kvp.Key);
                }
            }
            
            // Remove invalidated paths
            foreach (var key in tileKeysToRemove) 
            {
                tilePathCache.Remove(key);
            }
            
            // We don't invalidate room paths because they don't depend on specific tiles
            // (only on door connectivity, which is handled separately)
        }
        
        // Try to get a cached path between rooms
        public bool TryGetRoomPath(Room fromRoom, Room toRoom, out List<Door> doors) 
        {
            RoomPathKey key = new RoomPathKey(fromRoom, toRoom);
            
            if (roomPathCache.TryGetValue(key, out CachedRoomPath cachedPath)) 
            {
                // Check if all doors are still open
                foreach (Door door in cachedPath.Doors) 
                {
                    if (!door.IsOpen) 
                    {
                        doors = null;
                        return false;
                    }
                }
                
                // Return the cached path
                doors = new List<Door>(cachedPath.Doors);
                cacheHits++;
                return true;
            }
            
            doors = null;
            cacheMisses++;
            return false;
        }
        
        // Cache a room-to-room path
        public void CacheRoomPath(Room fromRoom, Room toRoom, List<Door> doors) 
        {
            RoomPathKey key = new RoomPathKey(fromRoom, toRoom);
            
            // Create a deep copy of the door list
            List<Door> cachedDoors = new List<Door>(doors);
            
            // Store in cache
            roomPathCache[key] = new CachedRoomPath(cachedDoors);
            
            // Maintain cache size
            if (roomPathCache.Count > maxCachedPaths) 
            {
                // This is a simple approach - a more sophisticated cache eviction policy could be used
                foreach (var kvp in roomPathCache) 
                {
                    roomPathCache.Remove(kvp.Key);
                    break;
                }
            }
        }
        
        // Try to get a cached fine path within a room
        public bool TryGetTilePath(int fromIndex, int toIndex, ICostProvider costProvider, AStarNavAgent agent, out List<int> pathIndices, out List<Vector3> waypoints) 
        {
            TilePathKey key = new TilePathKey(fromIndex, toIndex, costProvider.GetType().Name, agent);
            
            if (tilePathCache.TryGetValue(key, out CachedTilePath cachedPath)) 
            {
                // Verify tile versions are still valid
                foreach (int index in cachedPath.PathIndices) 
                {
                    Tile tile = gridManager.GetTile(index);
                    if (tile.Version != cachedPath.TileVersions[index]) 
                    {
                        pathIndices = null;
                        waypoints = null;
                        return false;
                    }
                }
                
                // Return the cached path
                pathIndices = new List<int>(cachedPath.PathIndices);
                waypoints = new List<Vector3>(cachedPath.Waypoints);
                cacheHits++;
                return true;
            }
            
            pathIndices = null;
            waypoints = null;
            cacheMisses++;
            return false;
        }
        
        // Cache a fine path within a room
        public void CacheTilePath(int fromIndex, int toIndex, ICostProvider costProvider, AStarNavAgent agent, List<int> pathIndices, List<Vector3> waypoints) 
        {
            TilePathKey key = new TilePathKey(fromIndex, toIndex, costProvider.GetType().Name, agent);
            
            // Create a deep copy of the path
            List<int> cachedIndices = new List<int>(pathIndices);
            List<Vector3> cachedWaypoints = new List<Vector3>(waypoints);
            
            // Record tile versions at the time of caching
            Dictionary<int, ushort> tileVersions = new Dictionary<int, ushort>();
            foreach (int index in pathIndices) 
            {
                tileVersions[index] = gridManager.GetTile(index).Version;
            }
            
            // Store in cache
            tilePathCache[key] = new CachedTilePath(cachedIndices, cachedWaypoints, tileVersions);
            
            // Maintain cache size
            if (tilePathCache.Count > maxCachedPaths) 
            {
                // This is a simple approach - a more sophisticated cache eviction policy could be used
                foreach (var kvp in tilePathCache) 
                {
                    tilePathCache.Remove(kvp.Key);
                    break;
                }
            }
        }
        
        // Clear the cache
        public void ClearCache() 
        {
            roomPathCache.Clear();
            tilePathCache.Clear();
            cacheHits = 0;
            cacheMisses = 0;
        }
        
        // Get cache statistics
        public (int hits, int misses, float hitRate) GetStats() 
        {
            int total = cacheHits + cacheMisses;
            float hitRate = total > 0 ? (float)cacheHits / total : 0f;
            return (cacheHits, cacheMisses, hitRate);
        }
        
        // Key for room-to-room paths
        private struct RoomPathKey : IEquatable<RoomPathKey> 
        {
            public readonly int FromRoomId;
            public readonly int ToRoomId;
            
            public RoomPathKey(Room fromRoom, Room toRoom) 
            {
                FromRoomId = fromRoom.RoomId;
                ToRoomId = toRoom.RoomId;
            }
            
            public bool Equals(RoomPathKey other) 
            {
                return FromRoomId == other.FromRoomId && ToRoomId == other.ToRoomId;
            }
            
            public override bool Equals(object obj) 
            {
                if (obj is RoomPathKey other)
                    return Equals(other);
                return false;
            }
            
            public override int GetHashCode() 
            {
                return (FromRoomId << 16) | ToRoomId;
            }
        }
        
        // Key for tile-to-tile paths
        private struct TilePathKey : IEquatable<TilePathKey> 
        {
            public readonly int FromIndex;
            public readonly int ToIndex;
            public readonly string CostProviderName;
            public readonly int AgentId;
            
            public TilePathKey(int fromIndex, int toIndex, string costProviderName, AStarNavAgent agent) 
            {
                FromIndex = fromIndex;
                ToIndex = toIndex;
                CostProviderName = costProviderName;
                AgentId = agent != null ? agent.GetInstanceID() : 0;
            }
            
            public bool Equals(TilePathKey other) 
            {
                return FromIndex == other.FromIndex && 
                       ToIndex == other.ToIndex && 
                       CostProviderName == other.CostProviderName &&
                       AgentId == other.AgentId;
            }
            
            public override bool Equals(object obj) 
            {
                if (obj is TilePathKey other)
                    return Equals(other);
                return false;
            }
            
            public override int GetHashCode() 
            {
                unchecked 
                {
                    int hash = 17;
                    hash = hash * 23 + FromIndex;
                    hash = hash * 23 + ToIndex;
                    hash = hash * 23 + (CostProviderName != null ? CostProviderName.GetHashCode() : 0);
                    hash = hash * 23 + AgentId;
                    return hash;
                }
            }
        }
        
        // Cached data for room paths
        private class CachedRoomPath 
        {
            public List<Door> Doors;
            public float CreationTime;
            
            public CachedRoomPath(List<Door> doors) 
            {
                Doors = doors;
                CreationTime = Time.time;
            }
        }
        
        // Cached data for tile paths
        private class CachedTilePath 
        {
            public List<int> PathIndices;
            public List<Vector3> Waypoints;
            public Dictionary<int, ushort> TileVersions;
            public float CreationTime;
            
            public CachedTilePath(List<int> pathIndices, List<Vector3> waypoints, Dictionary<int, ushort> tileVersions) 
            {
                PathIndices = pathIndices;
                Waypoints = waypoints;
                TileVersions = tileVersions;
                CreationTime = Time.time;
            }
        }
    }
}