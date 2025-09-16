using System;
using System.Runtime.CompilerServices;
using SparFlame.Components.General;
using SparFlame.Components.SubGameplay;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

namespace SparFlame.Components.MainGameplay
{
    // Army Group Attr

    public enum ArmyGroupIconType
    {
        Bear,
        Butterfly,
        Dragon,
        Deer,
        Horse,
        Lion,
        Rabbit,
        Scorpion,
        Snake,
        Wolf
    }

    [Serializable]
    public struct ArmyGroupAttr : IComponentData
    {
        // Static data
        public ArmyGroupIconType iconType;
        public FixedString32Bytes gameplayName;
        public long saveId; // This id is generated when new an army group, and will never duplicate nor change.
        public float createTimeInTotalHours;

        // Unit data
        public int avgLevel;
        // public int tier1UnitCount;
        // public int tier2UnitCount;
        // public int tier3UnitCount;

        // This is used to record units formation info
        public float2 boundingBoxDelta;

        public float3
            loadingCenter; // This value should be set when an army group garrisons a city or leaves a garrisoned city

        public float loadingScale;
    }

    [Serializable]
    public struct ArmyGroupStatData : IComponentData
    {
        public float totalMaxHp;
        public float totalCurrentHp;
        public float recoveredHpRatio;

        public int
            lastCheckTotalHours; // Used for hp auto recovery, reset to current total hours immediately when army group is in garrison state
    }


    public struct LastPassingByCity : IComponentData
    {
        public Entity City;
    }


    public struct ArmyGroupUtils
    {
        /// <summary>
        /// This function can only be called in sub gameplay, use it to update army group info when composition may be changed.
        /// </summary>
        /// <param name="em"></param>
        /// <param name="armyGroup"></param>
        public static void UpdateArmyGroupInfoForCompoChanged(EntityManager em, Entity armyGroup)
        {
            var armyGroupAttr = em.GetComponentData<ArmyGroupAttr>(armyGroup);
            var armyGroupMovableData = em.GetComponentData<ArmyGroupMovableData>(armyGroup);
            var armyGroupUnits = em.GetBuffer<ArmyGroupUnit>(armyGroup);
            var armyGroupStatData = em.GetComponentData<ArmyGroupStatData>(armyGroup);


            var unitCount = armyGroupUnits.Length;
            var totalLevel = 0;
            // var totalTier1Count = 0;
            // var totalTier2Count = 0;
            // var totalTier3Count = 0;
            var minSpeed = float.MaxValue;
            var currentHp = 0f;
            var maxHp = 0f;


            for (var i = 0; i < unitCount; i++)
            {
                var unit = armyGroupUnits[i].Unit;
                var expData = em.GetComponentData<ExpData>(unit);
                var movableData = em.GetComponentData<MovableData>(unit);
                var statData = em.GetComponentData<StatData>(unit);
                if (movableData.MoveSpeed < minSpeed) minSpeed = movableData.MoveSpeed;
                totalLevel += expData.curLevel;
                currentHp += statData.curValue;
                maxHp += statData.maxValue;


                // switch (expData.curTier)
                // {
                //     case Tier.Tier1:
                //         totalTier1Count++;
                //         break;
                //     case Tier.Tier2:
                //         totalTier2Count++;
                //         break;
                //     case Tier.Tier3:
                //         totalTier3Count++;
                //         break;
                //     default:
                //         BurstSafe.UnexpectedEnum(expData.curTier);
                //         break;
                // }
            }

            armyGroupAttr.avgLevel = unitCount == 0 ? 0 : totalLevel / unitCount;
            armyGroupMovableData.minUnitMoveSpeed = unitCount == 0 ? 0 : minSpeed;

            //
            // armyGroupAttr.tier1UnitCount = totalTier1Count;
            // armyGroupAttr.tier2UnitCount = totalTier2Count;
            // armyGroupAttr.tier3UnitCount = totalTier3Count;
            //
            //
            armyGroupStatData.totalCurrentHp = currentHp;
            armyGroupStatData.totalMaxHp = maxHp;

            em.SetComponentData(armyGroup, armyGroupAttr);
            em.SetComponentData(armyGroup, armyGroupMovableData);
            em.SetComponentData(armyGroup, armyGroupStatData);
        }


        public static void ResetArmyGroupMovableData(ref ArmyGroupMovableData movableData,
            ref ArmyGroupCalculatePathData pathData,
            ref DynamicBuffer<ArmyGroupFinalWayPoint> finalWaypoints,
            ref PathVisualizeData visualizeData,
            ref NavAgentComponent navAgent,
            EntityCommandBuffer.ParallelWriter ecb, int index, Entity selfEntity)
        {
            movableData.curWaypoint = 0;
            pathData.curTargetIndex = -1;
            pathData.calculationInfo = ArmyGroupPathCalculationInfo.None;
            pathData.boxColliderSizeXz = float2.zero;
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
            pathData.calculationInfo = ArmyGroupPathCalculationInfo.None;
            pathData.boxColliderSizeXz = float2.zero;
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
            if (entityManager.HasComponent<ArmyGroupInGarrison>(entity)) return false;
            var generalAttr = entityManager.GetComponentData<MainGameplayGeneralAttr>(entity);
            var relationship =
                FactionUtils.GetRelationship(playerFactionData.faction, playerFactionData.subFaction,
                    generalAttr.faction, generalAttr.subFaction);
            return relationship == Relationship.Self;
        }

        /// <summary>
        /// 计算 pos 到以 centerPos 为中心，rect 为长宽的矩形的最近点（2D XZ 平面）。
        /// </summary>
        /// <param name="centerPos">矩形中心 (x,z)</param>
        /// <param name="rect">矩形尺寸 (width, height)</param>
        /// <param name="pos">待投影点 (x,z)</param>
        /// <returns>矩形上的最近点 (x,z)</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float3 GetNearestPointOnRect(float3 centerPos, float2 rect, float3 pos)
        {
            var halfX = rect.x * 0.5f;
            var halfZ = rect.y * 0.5f;

            var left = centerPos.x - halfX;
            var right = centerPos.x + halfX;
            var bottom = centerPos.z - halfZ;
            var top = centerPos.z + halfZ;

            var clampedX = math.clamp(pos.x, left, right);
            var clampedZ = math.clamp(pos.z, bottom, top);

            return new float3(clampedX, centerPos.y, clampedZ);
        }

        public static NativeHashMap<Entity, NativeList<float3>> GenerateArmyGroupSquareFormations(
            NativeHashMap<Entity, int> armyGroupToUnitCount, float intervalLength, float3 firstBias
        )
        {
            var armyGroupToSquarePositions =
                new NativeHashMap<Entity, NativeList<float3>>(armyGroupToUnitCount.Capacity, Allocator.Temp);

            var keys = armyGroupToUnitCount.GetKeyArray(Allocator.Temp);
            foreach (var army in keys)
            {
                var unitCount = armyGroupToUnitCount[army];

                // 决定方阵边长
                var side = (int)math.ceil(math.sqrt(unitCount));

                // 创建 NativeList
                var positions = new NativeList<float3>(unitCount, Allocator.Temp);

                var added = 0;
                for (var z = 0; z < side && added < unitCount; z++)
                {
                    for (var x = 0; x < side && added < unitCount; x++)
                    {
                        positions.Add(new float3(x * intervalLength, 0, z * intervalLength) + firstBias);
                        added++;
                    }
                }

                armyGroupToSquarePositions.TryAdd(army, positions);
            }

            keys.Dispose();
            return armyGroupToSquarePositions;
        }
    }
}