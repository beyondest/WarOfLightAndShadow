using SparFlame.GamePlaySystem.Units;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace SparFlame.GamePlaySystem.Garrison
{
    public class GarrisonAttributeAuthoring : MonoBehaviour
    {
        public float3 moveOutPositionBias;
        class GarrisonAttributeAuthoringBaker : Baker<GarrisonAttributeAuthoring>
        {
            public override void Bake(GarrisonAttributeAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.Dynamic);
                AddComponent(entity, new GarrisonAttr
                {
                    MoveOutPositionBias = authoring.moveOutPositionBias,
                });
                AddBuffer<GarrisonTypeData>(entity);
                AddBuffer<GarrisonEntity>(entity);
            }
        }
    }

    /// <summary>
    /// Only fortress and producer can be garrisoned
    /// </summary>
    public struct GarrisonAttr : IComponentData
    {
        public int MaxGarrisonCount;
        public float3 MoveOutPositionBias; // Only useful when building is alive
    }

    public struct AllowGarrisonUnit : IBufferElementData
    {
        public UnitType UnitType;
        public int SubTypeIndex;
    }
    
    public struct GarrisonTypeData : IBufferElementData
    {
        public UnitType UnitType; // For show sprite info in UI, actually only need id, but this makes it find faster
        public int ID;
        public int Count;
    }
    
    /// <summary>
    /// All entities with InGarrison Component
    /// </summary>
    public struct GarrisonEntity : IBufferElementData
    {
        public Entity Value;
        public int Id; // For fast filter
    }
    
}
