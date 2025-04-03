using System.Collections.Generic;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace SparFlame.GamePlaySystem.Building
{
    public class ConstructSystemAuthoring : MonoBehaviour
    {
       
        public BuildingDatabaseSo buildingDatabase;
        public float3 hideBuildingLocation  = new float3(0, -100, 0);
        
  
        private class Baker : Baker<ConstructSystemAuthoring>
        {
            public override void Bake(ConstructSystemAuthoring dataBaseAuthoring)
            {
                var entity = GetEntity(TransformUsageFlags.None);
                AddComponent(entity, new ConstructSystemConfig
                {
                    HideBuildingLocation = dataBaseAuthoring.hideBuildingLocation,
                });
                var entity2 = CreateAdditionalEntity(TransformUsageFlags.None);
                var buffer = AddBuffer<BuildingSlot>(entity2);
                foreach (var buildingData in dataBaseAuthoring.buildingDatabase.buildingsData)
                {
                    buffer.Add(new BuildingSlot
                    {
                        Type = buildingData.BuildingType,
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
        public float3 HideBuildingLocation;
    }
}