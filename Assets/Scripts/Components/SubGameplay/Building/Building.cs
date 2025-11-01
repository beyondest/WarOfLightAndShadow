using System;
using SparFlame.Components.General;
using Unity.Entities;

namespace SparFlame.Components.SubGameplay
{
    public enum BuildingState
    {
        Idle = 0,
        Constructing = 1,
        Working = 2,
        UnderAttack = 3,
    }


    [Serializable]
    public struct ConstructingTimer : IComponentData
    {
        public float builtUpTargetTotalHours;
    }

    public struct BuildingAttr : IComponentData
    {
        public float ConstructTimeHours;
        public int SubTypeIndex;
        public BuildingType Type;
    }

    public enum BuildingType
    {
        Fortifications = 0,
        Generators = 1,
        ConjuringShrines = 2,
        CapacityBuildings = 3,
        Ornaments = 4,
    }



    public enum CapacityBuildingType
    {
        Dwelling = 0,
        ManaPool = 1,
        CrystalStoreHouse = 2,
    }


    public enum FortificationType
    {
        Wall = 0, // Fence（木栅栏）, Rampart（防坡墙）, Bastion（棱堡）
        Tower = 1, // Watchpost（瞭望哨）, Guard Tower（守卫塔）, Keep（主堡楼）
        BigTower = 2,
    }


    public enum GeneratorType
    {
        ResourceMine = 0,
        PlantGenerator = 1,
    }

    public enum ResourceMineType
    {
        Mine0 = 0,
        Mine1 = 1,
        Mine2 = 2,
    }

    public enum PlantGeneratorType
    {
        Plant0 = 0,
        Plant1 = 1,
        Plant2 = 2,
    }


    public enum OrnamentType
    {
        Crystal = 0, // Shard（水晶碎片）, Cluster（水晶簇）, Monolith（晶体巨柱）
        Beacon = 1,
        RetreatPortal = 2,
        Others = 3

    }


    public struct BuildingEntityPrefabData : IEntityPrefabData<BuildingType>
    {
        public Entity Prefab { get; set; }
        public BuildingType Type { get; set; }
        public int PrefabId { get; set; }
    }
    public struct CapacityBuildingAttr : IComponentData
    {
        public int StorageAmount;
        public ResourceType ResourceType;
    }

    public struct RetreatPortalTag : IComponentData
    {
        
    }


    public struct BuildingUtils
    {
        public static BuildingState GetBuildingState(bool underAttack, bool constructing, bool conjuring,
            bool generating)
        {
            if (underAttack) return BuildingState.UnderAttack;
            if (constructing) return BuildingState.Constructing;
            if (conjuring || generating) return BuildingState.Working;
            return BuildingState.Idle;
        }
    }
}