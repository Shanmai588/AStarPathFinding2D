using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace RTS.Pathfinding
{
    public class PathCacheKey
    {
        public Vector2Int start, end;
        public int roomId;
        public string costProviderType;

        public override int GetHashCode()
        {
            return HashCode.Combine(start, end, roomId, costProviderType);
        }

        public override bool Equals(object obj)
        {
            if (obj is PathCacheKey other)
            {
                return start == other.start && end == other.end && 
                       roomId == other.roomId && costProviderType == other.costProviderType;
            }
            return false;
        }
    }

    public class PathCache
    {
        private Dictionary<PathCacheKey, Path> cache = new Dictionary<PathCacheKey, Path>();
        private int maxCacheSize = 1000;

        public Path GetCachedPath(PathCacheKey key)
        {
            return cache.ContainsKey(key) ? cache[key] : null;
        }

        public void CachePath(PathCacheKey key, Path path)
        {
            if (cache.Count >= maxCacheSize)
            {
                // Simple cache eviction - remove first entry
                var firstKey = cache.Keys.First();
                cache.Remove(firstKey);
            }
            cache[key] = path;
        }

        public void InvalidateCache(int roomId)
        {
            var keysToRemove = cache.Keys.Where(k => k.roomId == roomId).ToList();
            foreach (var key in keysToRemove)
                cache.Remove(key);
        }
    }
}