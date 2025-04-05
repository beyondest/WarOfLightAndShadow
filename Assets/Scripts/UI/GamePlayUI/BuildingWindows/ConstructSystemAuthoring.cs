using System.Collections.Generic;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace SparFlame.GamePlaySystem.Building
{
    public class ConstructSystemAuthoring : MonoBehaviour
    {
       
        public BuildingDatabaseSo buildingDatabase;
        [Tooltip("This location is used for hiding building when enter movement ghost show")]
        public float3 hideBuildingLocation  = new float3(0, -100, 0);

        public float rotateSpeed = 2f;
        
        private class Baker : Baker<ConstructSystemAuthoring>
        {
            public override void Bake(ConstructSystemAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.None);
                AddComponent(entity, new ConstructSystemConfig
                {
                    HideBuildingLocation = authoring.hideBuildingLocation,
                    RotateSpeed = authoring.rotateSpeed,
                });
                var entity2 = CreateAdditionalEntity(TransformUsageFlags.None);
                var buffer = AddBuffer<BuildingSlot>(entity2);
                foreach (var buildingData in authoring.buildingDatabase.buildingsData)
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
        public float RotateSpeed;
    }
}