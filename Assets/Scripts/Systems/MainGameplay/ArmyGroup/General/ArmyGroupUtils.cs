using SparFlame.Components.General;
using SparFlame.Components.Input;
using SparFlame.Components.MainGameplay;
using SparFlame.Components.SubGameplay;
using Unity.Entities;
using Unity.Mathematics;

namespace SparFlame.Systems.MainGameplay.ArmyGroup
{
    public struct ArmyGroupUtils
    {
        public static void ResetArmyGroupMovableData(ref ArmyGroupMovableData movableData,
            ref ArmyGroupCalculatePathData pathData,
            ref DynamicBuffer<ArmyGroupFinalWayPoint> finalWaypoints,
            ref PathVisualizeData visualizeData,
            ref NavAgentComponent navAgent,
            EntityCommandBuffer.ParallelWriter ecb, int index, Entity selfEntity)
        {
            movableData.CurWaypoint = 0;
            pathData.CurTargetIndex = -1;
            visualizeData.PreWaypoint = 0;
            finalWaypoints.Clear();
            movableData.IsTargetReachable = true;
            navAgent.CalculationComplete = true;
            ecb.SetComponentEnabled<ArmyGroupCalculateEnable>(index, selfEntity, false);
            ecb.SetComponentEnabled<ArmyGroupMovingTag>(index, selfEntity, false);
        }
        public static void ResetArmyGroupMovableData(ref ArmyGroupMovableData movableData,
            ref ArmyGroupCalculatePathData pathData,
            ref DynamicBuffer<ArmyGroupFinalWayPoint> finalWaypoints,
            ref PathVisualizeData visualizeData,
            ref NavAgentComponent navAgent,
            EntityCommandBuffer ecb, Entity selfEntity)
        {
            movableData.CurWaypoint = 0;
            pathData.CurTargetIndex = -1;
            visualizeData.PreWaypoint = 0;
            movableData.IsTargetReachable = true;
            navAgent.CalculationComplete = true;
            ecb.SetComponentEnabled<ArmyGroupCalculateEnable>(selfEntity, false);
            finalWaypoints.Clear();
        }
        public static bool IsSelectable(EntityManager entityManager, Entity entity,
            FactionTag playerFaction)
        {
            return entityManager.HasComponent<ArmyGroupSelected>(entity)
                   && entityManager.GetComponentData<MainGameplayGeneralAttr>(entity).Faction == playerFaction;
        }

        // public static bool IsSettingTarget(in InputArmyGroupControlData data, in InputMouseData inputMouseData,
        //     EntityManager entityManager)
        // {
        //     return data.SetTarget && !inputMouseData.IsOverUI  &&
        //            entityManager.HasComponent<ArmyGroupWalkableTag>(inputMouseData.HitEntity);
        // }
    }
}