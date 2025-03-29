using UnityEngine;
using Unity.Entities;
using UnityEngine.AI;

namespace SparFlame.GamePlaySystem.Building
{
    public class BuildingSystemAuthoring : MonoBehaviour
    {
        public float buildingGarrisonRadius = 1f;
        
        private class BuildingSystemAuthoringBaker : Unity.Entities.Baker<BuildingSystemAuthoring>
        {
            public override void Bake(BuildingSystemAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.None);
                AddComponent(entity, new BuildingConfig
                {
                    BuildingGarrisonRadiusSq = authoring.buildingGarrisonRadius * authoring.buildingGarrisonRadius,
                });
            }
        }
    }

    public struct BuildingConfig : IComponentData
    {
        public float BuildingGarrisonRadiusSq;
    }

    public enum BuildingType
    {
        Walls,
        Decoration,
        DefenseTower,
        ResourceGenerator,
        UnitSummoning,
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


    public enum Tier
    {
        Tier1 = 3,
        Tier2 = 4,
        Tier3 = 5,
        Tier4 = 6,
        Tier5 = 7,
    }
}