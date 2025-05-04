using SparFlame.GamePlaySystem.Units;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace SparFlame.GamePlaySystem.Garrison
{
    public class GarrisonSystemAuthoring : MonoBehaviour
    {
        public int minCountToTriggerDefenceBuff;
        public float3 hidePositionBias;
        public float garrisonRadius;
        private class GarrisonSystemAuthoringBaker : Baker<GarrisonSystemAuthoring>
        {
            public override void Bake(GarrisonSystemAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.None);
                AddComponent(entity, new GarrisonSystemConfig
                {
                    MinCountToTriggerDefenceBuff = authoring.minCountToTriggerDefenceBuff,
                    HidePositionBias = authoring.hidePositionBias,
                    GarrisonRadiusSq = authoring.garrisonRadius * authoring.garrisonRadius,
                });
            }
        }
    }
   
    
    public struct GarrisonSystemConfig : IComponentData
    {
        public int MinCountToTriggerDefenceBuff;
        public float3 HidePositionBias;
        public float GarrisonRadiusSq;
    }

    /// <summary>
    /// The only way to Garrison Count ++
    /// This request needs to be dealt in main thread, cause add buffer cannot parallel
    /// 
    /// </summary>
    public struct GarrisonInBuildingRequest : IComponentData
    {
        public Entity BuildingEntity;
        public UnitType UnitType;
        public int Id;
        public Entity UnitEntity;
    }

    /// <summary>
    /// All units with this tag will show shield VFX, and gain buff
    /// </summary>
    public struct InGarrison : IComponentData
    {
        public Entity BuildingEntity;
        public bool InBuilding;
        public float PriorMass;
    }

    /// <summary>
    /// All buildings with this tag will radiate buff to fortifications around it
    /// </summary>
    public struct UnderDefence : IComponentData
    {
        public float RangeSq;
    }

    /// <summary>
    /// The only way to Garrison count--
    /// This command is for player control move out from building detail window, and for ai move out unit when base is under attack
    /// </summary>
    public struct GarrisonMoveOutCommand : IComponentData
    {
        public Entity BuildingEntity;
        public int MoveOutUnitId;
        public bool MoveOutAllSameId;
        public bool MoveOutAll;
    }

    public struct GarrisonUnitDieRequest : IComponentData
    {
        public Entity BuildingEntity;
        public Entity UnitEntity;
        public int Id;
    }


    public struct GarrisonGetOut : IComponentData
    {
        
    }

    // public struct GarrisonAiHpRegeneratingTag : IComponentData
    // {
    //     
    // }
}