using System.Collections.Generic;
using Unity.Entities;
using UnityEngine;

namespace SparFlame.GamePlaySystem.Building
{
    public class ConstructSystemAuthoring : MonoBehaviour
    {
        public int maxUniqueBuildingNums = 100;
        public List<BuildingData> buildingDatabase;

        private class BuildingsPrefabAuthoringBaker : Baker<ConstructSystemAuthoring>
        {
            public override void Bake(ConstructSystemAuthoring dataBaseAuthoring)
            {
                var entity = GetEntity(TransformUsageFlags.None);
                AddComponent(entity, new ConstructSystemConfig
                {
                    MaxUniqueBuildingNums = dataBaseAuthoring.maxUniqueBuildingNums,
                });
                var buffer = AddBuffer<BuildingSlot>(entity);
                foreach (var buildingData in dataBaseAuthoring.buildingDatabase)
                {
                    buffer.Add(new BuildingSlot
                    {
                        Type = buildingData.buildingType,
                        Entity = GetEntity(buildingData.prefab, TransformUsageFlags.Dynamic)
                    });
                }
            }
        }
    }

   
    
    public struct BuildingSlot : IBufferElementData
    {
        public Entity Entity;
        public BuildingType Type;
    }

    public struct ConstructSystemConfig : IComponentData
    {
        public int MaxUniqueBuildingNums;
    }
}