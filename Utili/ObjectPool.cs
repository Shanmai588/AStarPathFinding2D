using System;
using System.Collections.Generic;

namespace RTS.Pathfinding
{
    // Generic object pool for reusing objects and avoiding GC allocations
    public class ObjectPool<T> where T : class
    {
        private readonly Func<T> createFunc;
        private readonly int maxSize;
        private readonly Stack<T> pool;
        private readonly Action<T> resetAction;

        public ObjectPool(Func<T> create, Action<T> reset, int maxPoolSize = 100)
        {
            pool = new Stack<T>();
            createFunc = create;
            resetAction = reset;
            maxSize = maxPoolSize;
        }

        public T Get()
        {
            if (pool.Count > 0)
            {
                var item = pool.Pop();
                return item;
            }

            return createFunc();
        }

        public void Return(T item)
        {
            if (item == null || pool.Count >= maxSize)
                return;

            resetAction?.Invoke(item);
            pool.Push(item);
        }

        public void PreWarm(int count)
        {
            for (var i = 0; i < count && pool.Count < maxSize; i++)
            {
                var item = createFunc();
                pool.Push(item);
            }
        }
    }
}