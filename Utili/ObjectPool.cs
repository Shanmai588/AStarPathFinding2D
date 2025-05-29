using System;
using System.Collections.Generic;

namespace RTS.Pathfinding
{
    // Generic object pool for reusing objects and avoiding GC allocations
    public class ObjectPool<T> where T : class, new()
    {
        private Stack<T> pool = new Stack<T>();
        private Func<T> createFunc;
        private Action<T> resetAction;

        public ObjectPool(Func<T> create = null, Action<T> reset = null)
        {
            createFunc = create ?? (() => new T());
            resetAction = reset;
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
            resetAction?.Invoke(item);
            pool.Push(item);
        }

        public void PreWarm(int count)
        {
            for (int i = 0; i < count; i++)
            {
                pool.Push(createFunc());
            }
        }
    }
}