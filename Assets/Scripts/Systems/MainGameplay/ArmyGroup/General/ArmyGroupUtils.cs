using SparFlame.Components.ComponentUtils;
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
            movableData.curWaypoint = 0;
            pathData.curTargetIndex = -1;
            visualizeData.preWaypoint = 0;
            finalWaypoints.Clear();
            movableData.isTargetReachable = true;
            navAgent.calculationComplete = true;
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
            movableData.curWaypoint = 0;
            pathData.curTargetIndex = -1;
            visualizeData.preWaypoint = 0;
            movableData.isTargetReachable = true;
            navAgent.calculationComplete = true;
            ecb.SetComponentEnabled<ArmyGroupCalculateEnable>(selfEntity, false);
            finalWaypoints.Clear();
        }
        public static bool IsSelectable(EntityManager entityManager, Entity entity,
           in PlayerFactionData playerFactionData)
        {
            if (!entityManager.HasComponent<ArmyGroupSelected>(entity)) return false;
            if(!entityManager.HasComponent<ArmyGroupInGarrison>(entity))return false;
            var generalAttr = entityManager.GetComponentData<MainGameplayGeneralAttr>(entity);
            var relationship =
                FactionUtils.GetRelationship(playerFactionData, generalAttr.faction, generalAttr.subFaction);
            return relationship == Relationship.Player;
        }

        // public static bool IsSettingTarget(in InputArmyGroupControlData data, in InputMouseData inputMouseData,
        //     EntityManager entityManager)
        // {
        //     return data.SetTarget && !inputMouseData.IsOverUI  &&
        //            entityManager.HasComponent<ArmyGroupWalkableTag>(inputMouseData.HitEntity);
        // }
    }
}