using System;
using Unity.Collections;
using Unity.Entities;

namespace SparFlame.Components.SubGameplay
{
    public struct TerrainTag : IComponentData
    {
        
    }
    
    [Serializable]
    public struct EnvTileTypeSpecialData : IBufferElementData
    {
        public EnvType type;
        public FixedList128Bytes<TileTypeToSpawnWeight> spawnableTiles;
    }
    public enum TileType
    {
        None = 0,
        GrassLike = 1,
        WoodsLike = 2,
        SandLike = 3,
        RockLike = 4,
        Five = 5,
        Six = 6,
        Seven = 7,
        Eight = 8,
    }
    public struct EnvSpawnTypeTotalAmount : IBufferElementData
    {
        public EnvType Type;
        public int Amount;
    }

    public struct EnvSpawnPrefabData : IBufferElementData
    {
        public EnvType Type;
        public float Prob;
        public int Amount;
        public Entity Prefab;
    }

    public enum EnvType
    {
        // Grass types
        GrayGrass = 0,
        BlueGrass = 1,
        PurpleGrass = 2,
        YellowGrass = 3,
        RedGrass = 4,

        // Water types
        BlueWater = 5,
        Lava = 6,

        // Rock types
        GrayRock = 7,
        BlueRock = 8,
        PurpleRock = 9,
        YellowRock = 10,
        RedMagma = 11,
        GreenMagma = 12,

        // Tree types, only spawn in elder grove
        Bark = 13,
        Root = 14,
        Bloom = 15,
        Vine = 16,

        // Flame types
        ObsidianFlame = 17,
        RiftFlame = 18,

        // Extension
        StoneWood = 19
    }

    [Serializable]
    public struct TileTypeToSpawnWeight
    {
        public TileType tileType;
        public float weight;
    }
}