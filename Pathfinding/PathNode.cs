using System;
using UnityEngine;

namespace RTS.Pathfinding
{
    public class PathNode : IComparable<PathNode>
    {
        public PathNode(Vector2Int pos)
        {
            Position = pos;
            Reset();
        }

        public Vector2Int Position { get; private set; }
        public float GCost { get; set; }
        public float HCost { get; set; }
        public float FCost => GCost + HCost;
        public PathNode Parent { get; set; }

        public int CompareTo(PathNode other)
        {
            if (other == null) return -1;
            return FCost.CompareTo(other.FCost);
        }

        public void Reset()
        {
            GCost = 0;
            HCost = 0;
            Parent = null;
        }
    }
}