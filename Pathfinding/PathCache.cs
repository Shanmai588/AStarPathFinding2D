using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace RTS.Pathfinding
{
    public class PathCacheKey : IEquatable<PathCacheKey>
    {
        public PathCacheKey(Vector2Int start, Vector2Int end, int roomId, string providerType)
        {
            Start = start;
            End = end;
            RoomId = roomId;
            CostProviderType = providerType;
        }

        public Vector2Int Start { get; }
        public Vector2Int End { get; }
        public int RoomId { get; }
        public string CostProviderType { get; }

        public bool Equals(PathCacheKey other)
        {
            if (other == null) return false;
            return Start == other.Start &&
                   End == other.End &&
                   RoomId == other.RoomId &&
                   CostProviderType == other.CostProviderType;
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hash = 17;
                hash = hash * 31 + Start.GetHashCode();
                hash = hash * 31 + End.GetHashCode();
                hash = hash * 31 + RoomId.GetHashCode();
                hash = hash * 31 + (CostProviderType?.GetHashCode() ?? 0);
                return hash;
            }
        }

        public override bool Equals(object obj)
        {
            return Equals(obj as PathCacheKey);
        }
    }

    public class LRUCache<T>
    {
        private readonly LinkedList<T> accessOrder;
        private readonly int capacity;
        private readonly Dictionary<T, LinkedListNode<T>> nodeMap;

        public LRUCache(int cap)
        {
            capacity = cap;
            accessOrder = new LinkedList<T>();
            nodeMap = new Dictionary<T, LinkedListNode<T>>();
        }

        public void Access(T key)
        {
            if (nodeMap.TryGetValue(key, out var node))
            {
                accessOrder.Remove(node);
                accessOrder.AddFirst(node);
            }
            else
            {
                if (nodeMap.Count >= capacity)
                {
                    var lru = accessOrder.Last;
                    accessOrder.RemoveLast();
                    nodeMap.Remove(lru.Value);
                }

                var newNode = accessOrder.AddFirst(key);
                nodeMap[key] = newNode;
            }
        }

        public T GetLeastRecentlyUsed()
        {
            return accessOrder.Count > 0 ? accessOrder.Last.Value : default;
        }

        public void Remove(T key)
        {
            if (nodeMap.TryGetValue(key, out var node))
            {
                accessOrder.Remove(node);
                nodeMap.Remove(key);
            }
        }
    }

    public class PathCache : ITileChangeListener
    {
        private readonly Dictionary<PathCacheKey, Path> cache;
        private readonly LRUCache<PathCacheKey> lruTracker;
        private readonly int maxCacheSize;

        public PathCache(int maxSize = 1000)
        {
            cache = new Dictionary<PathCacheKey, Path>();
            lruTracker = new LRUCache<PathCacheKey>(maxSize);
            maxCacheSize = maxSize;
        }

        public void OnTileChanged(TileChangedEvent eventData)
        {
            // Invalidate paths that might be affected by this tile change
            InvalidateCache(eventData.RoomId);
        }

        public Path GetCachedPath(PathCacheKey key)
        {
            if (cache.TryGetValue(key, out var path))
            {
                lruTracker.Access(key);
                return path;
            }

            return null;
        }

        public void CachePath(PathCacheKey key, Path path)
        {
            if (path == null || !path.IsValid)
                return;

            if (cache.Count >= maxCacheSize)
            {
                var lru = lruTracker.GetLeastRecentlyUsed();
                cache.Remove(lru);
                lruTracker.Remove(lru);
            }

            cache[key] = path;
            lruTracker.Access(key);
        }

        public void InvalidateCache(int roomId)
        {
            var keysToRemove = cache.Keys.Where(k => k.RoomId == roomId).ToList();
            foreach (var key in keysToRemove)
            {
                cache.Remove(key);
                lruTracker.Remove(key);
            }
        }
    }
}