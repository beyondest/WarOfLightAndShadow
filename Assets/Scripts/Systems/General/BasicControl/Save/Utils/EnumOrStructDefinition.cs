using SparFlame.Components.General;

namespace SparFlame.Systems.General.BasicControl
{
    public struct RiftGameFileHeader
    {
        public SaveArcheType SaveArcheType;
        public int EntityCount;
    }

    public struct RiftGameFileQuickData
    {
        public FactionTag Faction;
        public SubFactionTag SubFaction;
        public int TotalHours;
        public int CityPrefabId;
    }
    public enum SaveLoadTaskType
    {
        GameMainData,
        ArmyGroupSubData,
        CitySubData
    }
    public enum SaveArcheType
    {
        GameMain = 0,
        CityUnit = 1,
        CityBuilding = 2,
        ArmyGroup = 3,
        City = 4,
        ArmyGroupUnit = 5,
    }

    public enum SaveEntityType
    {
        Prefab = 0,
        Singleton = 1,
    }
}