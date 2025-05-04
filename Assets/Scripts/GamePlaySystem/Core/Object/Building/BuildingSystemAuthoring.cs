using SparFlame.GamePlaySystem.General;
using UnityEngine;
using Unity.Entities;

namespace SparFlame.GamePlaySystem.Building
{
    public class BuildingSystemAuthoring : MonoBehaviour
    {
        private class BuildingSystemAuthoringBaker : Baker<BuildingSystemAuthoring>
        {
            public override void Bake(BuildingSystemAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.None);
                AddComponent(entity, new BuildingSystemConfig());
            }
        }
    }



    public struct BuildingSystemConfig : IComponentData
    {
    }

    public enum BuildingType 
    {
        Fortifications = 0,
        Generators = 1,
        ConjuringShrines = 2,
        Dwellings = 3,
        Ornaments = 4,
    }
    
    public enum ConjuringShrineType
    {
        AegisShrine = 0,    // Stone Sigil（石之符印）, Ward Circle（守护法阵）, Bulwark Core（壁垒核心）
        StormSpire = 1,     // Wind Glyph（风之印记）, Arrow Rift（箭矢裂隙）, Tempest Spire（风暴尖塔）
        EldritchSeal = 2,   // Mana Ring（法力之环）, Arcane Core（奥术核心）, Eldritch Pillar（神秘石柱）
        PhantomGate = 3,    // Wild Rift（野性裂隙）, Charge Beacon（充能灯塔）, Thunder Gate（雷霆之门）
        TerraNexus = 4      // Rune Pad（符文阵盘）, Soul Anchor（灵魂锚点）, Golem Crucible（魔像熔炉）
    }

    public enum DwellingType
    {
        CommonDwelling = 0, // Hut（小屋）, Lodge（山屋）, Hall（大厅）
        FlameDwelling = 1,  // Hearth（炉台）, Crucible（熔炉）, Pyrelord Hall（火主大厅）
        MysticDwelling = 2, // Chapel（礼拜堂）, Sanctum（密室）, Sanctuary（圣域）
    }

    
    public enum FortificationType
    {
        Wall = 0,          // Fence（木栅栏）, Rampart（防坡墙）, Bastion（棱堡）
        Tower = 1,         // Watchpost（瞭望哨）, Guard Tower（守卫塔）, Keep（主堡楼）
    }


    public enum GeneratorType
    {
        Converter = 0,     // Seedling Converter（幼芽转化器）, Core Converter（核心转化器）, Arcane Forge（奥术熔炉）
        BloomSpire = 1,    // Bloom Pod（花蕾囊）, Bloom Spire（绽放尖塔）, Bloom Throne（盛放王座）
    }




    public enum OrnamentType
    {
        Crystal = 0,       // Shard（水晶碎片）, Cluster（水晶簇）, Monolith（晶体巨柱）
        StorableOrnament = 1,      // Crate（储物箱）, Depot（储存站）, Warehouse（仓库）
        UnStorableOrnament = 2,    // Relic（遗物）, Totem（图腾柱）, Monument（纪念碑）
    }

    
    public enum AreaType
    {
        Walkable = 0,
        NotWalkable = 1,
        Jump = 2,
        
        // Not attackable cost
        Cost00 = 3,
        Cost01 = 4,
        Cost02 = 5,
        Cost03 = 6,
        Cost04 = 7,
        
        // Attackable cost
        Cost10 = 13,
        Cost11 = 14,
        Cost12 = 15,
        Cost13 = 16,
        Cost14 = 17,
        Cost15 = 18,
    }
    public struct BuildingEntityPrefabData : IEntityPrefabData<BuildingType>
    {
        public Entity Prefab { get; set; }
        public BuildingType Type { get; set; }
    }


}

