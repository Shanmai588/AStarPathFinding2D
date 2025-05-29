namespace RTS.Pathfinding
{
    public class TileChangeListenerAdapter : IEventListener<TileChangedEvent>
    {
        private readonly ITileChangeListener listener;

        public TileChangeListenerAdapter(ITileChangeListener l)
        {
            listener = l;
        }

        public void OnEvent(TileChangedEvent eventData)
        {
            listener.OnTileChanged(eventData);
        }
    }
}