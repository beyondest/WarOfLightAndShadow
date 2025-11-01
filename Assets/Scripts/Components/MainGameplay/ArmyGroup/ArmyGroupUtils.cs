using System.Runtime.CompilerServices;
using SparFlame.Components.General;
using SparFlame.Components.SubGameplay;
using SparFlame.Components.VFX;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace SparFlame.Components.MainGameplay
{
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
            }

            armyGroupAttr.avgLevel = unitCount == 0 ? 0 : totalLevel / unitCount;
            armyGroupMovableData.minUnitMoveSpeed = unitCount == 0 ? 0 : minSpeed;

            armyGroupStatData.totalCurrentHp = currentHp;
            armyGroupStatData.totalMaxHp = maxHp;

            em.SetComponentData(armyGroup, armyGroupAttr);
            em.SetComponentData(armyGroup, armyGroupMovableData);
            em.SetComponentData(armyGroup, armyGroupStatData);
        }


        public static void ResetArmyGroupMovableData(ref ArmyGroupMovableData movableData,
            ref ArmyGroupCalculatePathData pathData,
            ref DynamicBuffer<ArmyGroupFinalWayPoint> finalWaypoints,
            ref ArmyGroupPathVisualizeData visualizeData,
            ref NavAgentComponent navAgent,
            EntityCommandBuffer.ParallelWriter ecb, int index, Entity selfEntity)
        {
            movableData.curWaypoint = 0;
            pathData.curTargetIndex = -1;
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
            ref ArmyGroupPathVisualizeData visualizeData,
            ref NavAgentComponent navAgent,
            EntityCommandBuffer ecb, Entity selfEntity)
        {
            movableData.curWaypoint = 0;
            pathData.curTargetIndex = -1;
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
            NativeHashMap<Entity, int> armyGroupToUnitCount, float spacing, float3 firstPointPosition
        )
        {
            var armyGroupToSquarePositions =
                new NativeHashMap<Entity, NativeList<float3>>(armyGroupToUnitCount.Capacity, Allocator.Temp);

            var keys = armyGroupToUnitCount.GetKeyArray(Allocator.Temp);
            foreach (var army in keys)
            {
                var unitCount = armyGroupToUnitCount[army];
                var positions = new NativeList<float3>(Allocator.Temp);
                GetSquareFormationPositions(spacing, firstPointPosition, unitCount, positions);
                armyGroupToSquarePositions.TryAdd(army, positions);
            }

            keys.Dispose();
            return armyGroupToSquarePositions;
        }

        public static void GetSquareFormationPositions(float spacing, float3 firstPointPosition, int unitCount,
            NativeList<float3> positions)
        {
            var side = (int)math.ceil(math.sqrt(unitCount));
            var added = 0;
            for (var z = 0; z < side && added < unitCount; z++)
            {
                for (var x = 0; x < side && added < unitCount; x++)
                {
                    positions.Add(new float3(x * spacing, 0, z * spacing) + firstPointPosition);
                    added++;
                }
            }
        }
        public static void DestroyArmyGroup(Entity armyGroup, EntityCommandBuffer ecb, EntityManager em)
        {
            ecb.DestroyEntity(armyGroup);

            var generalAttr = em.GetComponentData<MainGameplayGeneralAttr>(armyGroup);
            var transform = em.GetComponentData<LocalTransform>(armyGroup);
            
            var vfx = ecb.CreateEntity();
            ecb.AddComponent(vfx, new VFXRequest
            {
                Filter = new VFXSubFilter
                {
                    FactionFilterEnable = true,
                    Faction = generalAttr.faction
                },
                KeepDuration = 0,
                RequestType = VFXRequestType.Spawn,
                SpawnPosition = transform.Position,
                VFXName = VFXName.ArmyGroupDestroyed,
            });
            ecb.AddComponent<MainGameplayEntityTag>(vfx);

            if (em.HasComponent<ArmyGroupInGarrison>(armyGroup))
            {
                var inGarrison = em.GetComponentData<ArmyGroupInGarrison>(armyGroup);
                var garrisonRequest = ecb.CreateEntity();
                ecb.AddComponent(garrisonRequest, new ArmyGroupGarrisonRequest
                {
                    ArmyGroup = armyGroup,
                    IfGarrisonIn = false,
                    City = inGarrison.City
                });
                ecb.AddComponent<MainGameplayEntityTag>(garrisonRequest);
            }
        }
        
        
        
    }
}