using Unity.Entities;
using Unity.Physics;
using Unity.Transforms;
using UnityEngine;

namespace SparFlame.GamePlaySystem.Garrison
{
    public struct GarrisonUtils
    {
        public static void PosGetOut(
            ref InGarrison inGarrison,
            ref LocalTransform selfTransform,
            in LocalTransform buildingTransform,
            in GarrisonAttr garrisonAttr,
            ref PhysicsMass physicsMass,
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

                // selfTransform.Position = garrisonAttr.MoveOutPositionBias +
                //                      buildingTransform.Position;
            }
            inGarrison.InBuilding = false;
            physicsMass.InverseMass = inGarrison.PriorMass;
        }
        
        public static void PosGetIn(
            ref InGarrison inGarrison,ref LocalTransform selfTransform,
            in LocalTransform buildingTransform,
            ref PhysicsMass physicsMass,
             in GarrisonSystemConfig config)
        {
            var targetPos = buildingTransform.Position + config.HidePositionBias;
            selfTransform.Position = targetPos;
            inGarrison.InBuilding = true;
            inGarrison.PriorMass = physicsMass.InverseMass;
            physicsMass.InverseMass = 0;
        }

        
        // public static void PosGetOutWhenBuildingDestroyed(Entity entity,
        //     ref ComponentLookup<LocalTransform> transformLookup, in GarrisonStateMachineConfig config)
        // {
        //     ref var transform = ref transformLookup.GetRefRW(entity).ValueRW;
        //     transform.Position -= config.HidePositionBias;
        // }
    }
}