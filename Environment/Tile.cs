using System;

namespace RTS.Pathfinding 
{
    // Immutable runtime data for one grid cell
    public struct Tile 
    {
        public bool Walkable;      // Can be traversed by an agent
        public float BaseCost;     // Base cost to traverse this tile
        public ushort Version;     // Used for detecting changes and cache invalidation
        public byte Flags;         // Bit flags for special tile properties (mud, poison, etc.)
        
        // Constructor with default values
        public Tile(bool walkable = true, float baseCost = 1.0f, ushort version = 0, byte flags = 0) 
        {
            Walkable = walkable;
            BaseCost = baseCost;
            Version = version;
            Flags = flags;
        }
        
        // Common flag masks for easy access
        public const byte FLAG_MUD = 1;        // 00000001 - Slow movement
        public const byte FLAG_POISON = 2;     // 00000010 - Health damage
        public const byte FLAG_WATER = 4;      // 00000100 - Might restrict some units
        public const byte FLAG_ELEVATION = 8;  // 00001000 - Height differences
        
        // Helper methods to check if a specific flag is set
        public bool HasFlag(byte flag) => (Flags & flag) != 0;
        
        // Create a new tile with updated version (for change tracking)
        public Tile WithIncrementedVersion() => new Tile(Walkable, BaseCost, (ushort)(Version + 1), Flags);
        
        // Create a new tile with modified walkable state
        public Tile WithWalkable(bool walkable) => new Tile(walkable, BaseCost, (ushort)(Version + 1), Flags);
        
        // Create a new tile with modified flags
        public Tile WithFlags(byte newFlags) => new Tile(Walkable, BaseCost, (ushort)(Version + 1), newFlags);
    }
}