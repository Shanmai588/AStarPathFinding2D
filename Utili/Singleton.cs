using System;

namespace RTS.Pathfinding
{
    public sealed class Singleton<T> where T : class, new()
    {
        private static readonly Lazy<T> instance = new Lazy<T>(() => new T());

        // Private constructor to prevent external instantiation
        private Singleton() { }

        public static T Instance => instance.Value;
    }
}