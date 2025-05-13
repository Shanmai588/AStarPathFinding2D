using System;
using System.Collections.Generic;
using UnityEngine;

namespace RTS.Pathfinding 
{
    // Generic object pool for reusing objects and avoiding GC allocations
    public class ObjectPool<T> where T : class
    {
        // Stack of inactive objects
        private readonly Stack<T> pool = new Stack<T>();
        
        // Factory function for creating new objects
        private readonly Func<T> createFunc;
        
        // Optional reset action to prepare objects for reuse
        private readonly Action<T> resetAction;
        
        // Capacity and total created object count for tracking
        private readonly int capacity;
        private int totalCreated;
        
        public int AvailableCount => pool.Count;
        public int TotalCreated => totalCreated;
        
        public ObjectPool(Func<T> createFunc, Action<T> resetAction = null, int initialCapacity = 32)
        {
            this.createFunc = createFunc ?? throw new ArgumentNullException(nameof(createFunc));
            this.resetAction = resetAction;
            this.capacity = initialCapacity;
            
            // Pre-create objects up to initial capacity
            for (int i = 0; i < initialCapacity; i++)
            {
                T obj = createFunc();
                pool.Push(obj);
                totalCreated++;
            }
        }
        
        // Get an object from the pool or create a new one
        public T Get()
        {
            T obj;
            
            if (pool.Count > 0)
            {
                obj = pool.Pop();
            }
            else
            {
                obj = createFunc();
                totalCreated++;
            }
            
            return obj;
        }
        
        // Return an object to the pool
        public void Return(T obj)
        {
            if (obj == null)
                return;
                
            // Reset the object if a reset action is provided
            resetAction?.Invoke(obj);
            
            // Return to pool
            pool.Push(obj);
        }
        
        // Clear the pool
        public void Clear()
        {
            pool.Clear();
            totalCreated = 0;
        }
    }
    
    // Helper class for common object pool types used in pathfinding
    public static class PathfindingPools
    {
        // Pool for List<int> objects
        private static ObjectPool<List<int>> intListPool;
        
        // Pool for List<Vector3> objects
        private static ObjectPool<List<Vector3>> vector3ListPool;
        
        // Pool for Priority Queue objects
        private static ObjectPool<PathNodePriorityQueue> queuePool;
        
        // Initialize pools
        static PathfindingPools()
        {
            intListPool = new ObjectPool<List<int>>(
                () => new List<int>(64),
                list => list.Clear(),
                16
            );
            
            vector3ListPool = new ObjectPool<List<Vector3>>(
                () => new List<Vector3>(64),
                list => list.Clear(),
                16
            );
            
            queuePool = new ObjectPool<PathNodePriorityQueue>(
                () => new PathNodePriorityQueue(256),
                queue => queue.Clear(),
                8
            );
        }
        
        // Get a List<int> from the pool
        public static List<int> GetIntList()
        {
            return intListPool.Get();
        }
        
        // Return a List<int> to the pool
        public static void ReturnIntList(List<int> list)
        {
            intListPool.Return(list);
        }
        
        // Get a List<Vector3> from the pool
        public static List<Vector3> GetVector3List()
        {
            return vector3ListPool.Get();
        }
        
        // Return a List<Vector3> to the pool
        public static void ReturnVector3List(List<Vector3> list)
        {
            vector3ListPool.Return(list);
        }
        
        // Get a PathNodePriorityQueue from the pool
        public static PathNodePriorityQueue GetQueue()
        {
            return queuePool.Get();
        }
        
        // Return a PathNodePriorityQueue to the pool
        public static void ReturnQueue(PathNodePriorityQueue queue)
        {
            queuePool.Return(queue);
        }
    }
}