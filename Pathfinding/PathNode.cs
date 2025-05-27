using UnityEngine;

namespace RTS.Pathfinding
{
    public class PathNode
    {
        public Vector2Int Position { get; set; }
        public float GCost { get; set; }
        public float HCost { get; set; }
        public PathNode Parent { get; set; }

        public float FCost => GCost + HCost;

        public void Reset()
        {
            Position = Vector2Int.zero;
            GCost = float.MaxValue;
            HCost = 0;
            Parent = null;
        }
    }
}