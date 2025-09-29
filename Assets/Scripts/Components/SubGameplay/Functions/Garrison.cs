using System;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace SparFlame.Components.SubGameplay
{
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
    
    [Serializable]
    public struct GarrisonTypeData : IBufferElementData
    {
        // For show sprite info in UI, actually only need id, but this makes it find faster
        public UnitType unitType;
        public int id;
        public int count;
    }
    
    /// <summary>
    /// All entities with InGarrison Component
    /// </summary>
    public struct GarrisonEntity : IBufferElementData
    {
        public Entity Unit;
        public int PrefabId; // For fast filter
        public long SingleId; // For saving
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
        public long SingleId; // For saving
    }

   


    // public struct UnderDefence : IComponentData
    // {
    //     public float RangeSq;
    // }

    /// <summary>
    /// The only way to Garrison count--
    /// This command is for player control move out from building detail window, and for AI move out unit when base is under attack
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
    
    public struct GarrisonSystemConfig : IComponentData
    {
        public float3 HidePositionBias;
        public float GarrisonRadiusSq;
    }
    
    public struct GarrisonUtils
    {
        public static void PosGetOut(
            ref InGarrison inGarrison,
            ref LocalTransform selfTransform,
            in LocalTransform buildingTransform,
            in GarrisonAttr garrisonAttr,
            in GarrisonSystemConfig config,
            bool isBuildingDead)
        {
            // Building is dead, move out by bias
            if (isBuildingDead)
            {
                selfTransform.Position += config.HidePositionBias;
            }
            else
            {
                var buildingTransformCopied = buildingTransform;
                selfTransform.Position = buildingTransformCopied.TransformPoint(garrisonAttr.MoveOutPositionBias);
            }
            inGarrison.InBuilding = false;
        }
        
        public static void PosGetIn(
            ref InGarrison inGarrison,ref LocalTransform selfTransform,
            in LocalTransform buildingTransform,
            in GarrisonSystemConfig config)
        {
            var targetPos = buildingTransform.Position + config.HidePositionBias;
            selfTransform.Position = targetPos;
            inGarrison.InBuilding = true;
        }

    }

}