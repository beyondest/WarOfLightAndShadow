using GamePlaySystem.Functionality.MainGameplay.General;
using SparFlame.GamePlaySystem.CustomInput;
using SparFlame.GamePlaySystem.General;
using SparFlame.GamePlaySystem.Movement;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace SparFlame.GamePlaySystem.Functionality.MainGameplay.ArmyGroup
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

        public static bool IsSettingTarget(in InputArmyGroupControlData data, in InputMouseData inputMouseData,
            EntityManager entityManager)
        {
            return data.SetTarget && !inputMouseData.IsOverUI  &&
                   entityManager.HasComponent<ArmyGroupWalkableTag>(inputMouseData.HitEntity);
        }
    }
}