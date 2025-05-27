using System;
using System.Collections.Generic;
using UnityEngine;

namespace RTS.Pathfinding
{
    public class PathCache : ITileChangeListener
    {
        private readonly Dictionary<PathCacheKey, Path> cache = new();
        private readonly LRUCache<PathCacheKey> lruTracker;
        private readonly int maxCacheSize = 100;

        public PathCache()
        {
            lruTracker = new LRUCache<PathCacheKey>(maxCacheSize);
        }

        public void OnTileChanged(TileChangedEvent eventData)
        {
            InvalidateCache(eventData.RoomId);
        }

        public void OnEvent(TileChangedEvent eventData)
        {
            OnTileChanged(eventData);
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
            if (cache.Count >= maxCacheSize)
            {
                var lru = lruTracker.GetLeastRecentlyUsed();
                cache.Remove(lru);
            }

            cache[key] = path;
            lruTracker.Access(key);
        }

        public void InvalidateCache(int roomId)
        {
            var keysToRemove = new List<PathCacheKey>();

            foreach (var key in cache.Keys)
                if (key.RoomId == roomId)
                    keysToRemove.Add(key);

            foreach (var key in keysToRemove)
            {
                cache.Remove(key);
                lruTracker.Remove(key);
            }
        }
    }

    public struct PathCacheKey : IEquatable<PathCacheKey>
    {
        public Vector2Int Start { get; set; }
        public Vector2Int End { get; set; }
        public int RoomId { get; set; }
        public string CostProviderType { get; set; }

        public override int GetHashCode()
        {
            unchecked
            {
                var hash = 17;
                hash = hash * 23 + Start.GetHashCode();
                hash = hash * 23 + End.GetHashCode();
                hash = hash * 23 + RoomId;
                hash = hash * 23 + (CostProviderType?.GetHashCode() ?? 0);
                return hash;
            }
        }

        public bool Equals(PathCacheKey other)
        {
            return Start == other.Start &&
                   End == other.End &&
                   RoomId == other.RoomId &&
                   CostProviderType == other.CostProviderType;
        }

        public override bool Equals(object obj)
        {
            return obj is PathCacheKey key && Equals(key);
        }
    }

// Simple LRU Cache implementation
    public class LRUCache<T>
    {
        private readonly int capacity;
        private readonly Node head;

        private readonly Dictionary<T, Node> nodeMap = new();
        private readonly Node tail;

        public LRUCache(int capacity)
        {
            this.capacity = capacity;
            head = new Node();
            tail = new Node();
            head.Next = tail;
            tail.Prev = head;
        }

        public void Access(T value)
        {
            if (nodeMap.TryGetValue(value, out var node))
            {
                RemoveNode(node);
                AddToHead(node);
            }
            else
            {
                node = new Node { Value = value };
                nodeMap[value] = node;
                AddToHead(node);

                if (nodeMap.Count > capacity)
                {
                    var lru = tail.Prev;
                    RemoveNode(lru);
                    nodeMap.Remove(lru.Value);
                }
            }
        }

        public T GetLeastRecentlyUsed()
        {
            return tail.Prev.Value;
        }

        public void Remove(T value)
        {
            if (nodeMap.TryGetValue(value, out var node))
            {
                RemoveNode(node);
                nodeMap.Remove(value);
            }
        }

        private void RemoveNode(Node node)
        {
            node.Prev.Next = node.Next;
            node.Next.Prev = node.Prev;
        }

        private void AddToHead(Node node)
        {
            node.Prev = head;
            node.Next = head.Next;
            head.Next.Prev = node;
            head.Next = node;
        }

        private class Node
        {
            public T Value { get; set; }
            public Node Prev { get; set; }
            public Node Next { get; set; }
        }
    }
}