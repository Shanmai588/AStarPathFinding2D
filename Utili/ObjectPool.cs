using System;
using System.Collections.Generic;

namespace RTS.Pathfinding
{
    // Generic object pool for reusing objects and avoiding GC allocations
    public class ObjectPool<T>
    {
        private readonly Func<T> createFunc;
        private readonly Stack<T> pool = new();
        private readonly Action<T> resetAction;

        public ObjectPool(Func<T> create, Action<T> reset)
        {
            createFunc = create;
            resetAction = reset;
        }

        public T Get()
        {
            if (pool.Count > 0)
                return pool.Pop();

            return createFunc();
        }

        public void Return(T item)
        {
            resetAction?.Invoke(item);
            pool.Push(item);
        }

        public void PreWarm(int count)
        {
            for (var i = 0; i < count; i++) pool.Push(createFunc());
        }
    }
}