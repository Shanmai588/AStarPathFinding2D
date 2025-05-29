using System;
using System.Collections.Generic;

namespace RTS.Pathfinding
{
    public interface IEventListener<T>
    {
        void OnEvent(T eventData);
    }

    public class EventBus
    {
        private readonly Dictionary<Type, List<object>> listeners = new();

        public void Subscribe<T>(IEventListener<T> listener)
        {
            var type = typeof(T);
            if (!listeners.ContainsKey(type))
                listeners[type] = new List<object>();

            listeners[type].Add(listener);
        }

        public void Unsubscribe<T>(IEventListener<T> listener)
        {
            var type = typeof(T);
            if (listeners.ContainsKey(type))
                listeners[type].Remove(listener);
        }

        public void Publish<T>(T eventData)
        {
            var type = typeof(T);
            if (listeners.ContainsKey(type))
                foreach (var listener in listeners[type])
                    ((IEventListener<T>)listener).OnEvent(eventData);
        }
    }
}