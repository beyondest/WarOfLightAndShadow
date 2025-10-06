using System.Collections.Generic;
using System.Runtime.CompilerServices;
using SparFlame.Components.General;
using SparFlame.Components.SubGameplay;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace SparFlame.Systems.SubGameplay.Interact
{
    [BurstCompile]
    public partial struct SightUpdateListSystem : ISystem
    {
        private ComponentLookup<SubGameplayGeneralAttr> _interactableLookup;
        private ComponentLookup<StatData> _statDataLookup;
        private ComponentLookup<HealStateTag> _healLookup;
        private ComponentLookup<HarvestStateTag> _harvestLookup;
        private ComponentLookup<AttackStateTag> _attackLookup;
        private ComponentLookup<LocalTransform> _localTransformLookup;
        private ComponentLookup<SightPriority> _priorityLookup;
        private ComponentLookup<RegeneratingTag> _resourceAttrLookup;

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<EndSimulationEntityCommandBufferSystem.Singleton>();
            state.RequireForUpdate<SubGamingTag>();
            state.RequireForUpdate<SightSystemConfig>();
            state.RequireForUpdate<InsightTarget>();
            _interactableLookup = state.GetComponentLookup<SubGameplayGeneralAttr>(true);
            _statDataLookup = state.GetComponentLookup<StatData>(true);
            _localTransformLookup = state.GetComponentLookup<LocalTransform>(true);
            _healLookup = state.GetComponentLookup<HealStateTag>(true);
            _harvestLookup = state.GetComponentLookup<HarvestStateTag>(true);
            _attackLookup = state.GetComponentLookup<AttackStateTag>(true);
            _priorityLookup = state.GetComponentLookup<SightPriority>(true);
            _resourceAttrLookup = state.GetComponentLookup<RegeneratingTag>(true);
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var config = SystemAPI.GetSingleton<SightSystemConfig>();
            _statDataLookup.Update(ref state);
            _interactableLookup.Update(ref state);
            _localTransformLookup.Update(ref state);
            _healLookup.Update(ref state);
            _harvestLookup.Update(ref state);
            _priorityLookup.Update(ref state);
            _resourceAttrLookup.Update(ref state);
            _attackLookup.Update(ref state);
            state.Dependency = new UpdateTargetListJob
            {
                ECB = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>()
                    .CreateCommandBuffer(state.WorldUnmanaged).AsParallelWriter(),
                Config = config,
                GeneralAttrLookUp = _interactableLookup,
                StatDataLookup = _statDataLookup,
                HealLookup = _healLookup,
                HarvestLookup = _harvestLookup,
                AttackLookup = _attackLookup,
                TransformLookup = _localTransformLookup,
                PriorityLookup = _priorityLookup,
                RegeneratingTagLookup = _resourceAttrLookup
            }.ScheduleParallel(state.Dependency);
        }


        [BurstCompile]
        [WithNone(typeof(UnitDeadTag))]
        private partial struct UpdateTargetListJob : IJobEntity
        {
            public EntityCommandBuffer.ParallelWriter ECB;
            [ReadOnly] public ComponentLookup<SightPriority> PriorityLookup;
            [ReadOnly] public ComponentLookup<SubGameplayGeneralAttr> GeneralAttrLookUp;
            [ReadOnly] public ComponentLookup<StatData> StatDataLookup;
            [ReadOnly] public ComponentLookup<LocalTransform> TransformLookup;
            [ReadOnly] public ComponentLookup<HealStateTag> HealLookup;
            [ReadOnly] public ComponentLookup<HarvestStateTag> HarvestLookup;
            [ReadOnly] public ComponentLookup<AttackStateTag> AttackLookup;
            [ReadOnly] public ComponentLookup<RegeneratingTag> RegeneratingTagLookup;
            [ReadOnly] public SightSystemConfig Config;

            private void Execute([ChunkIndexInQuery] int index, ref DynamicBuffer<InsightTarget> targets,
                Entity selfEntity)
            {
                if (targets.IsEmpty)
                {
                    ECB.SetComponentEnabled<NeedTarget>(index, selfEntity, true);
                    return;
                }

                var selfFaction = GeneralAttrLookUp[selfEntity].Faction;
                var selfPos = TransformLookup[selfEntity].Position;

                var tempList = new NativeList<InsightTarget>(Allocator.Temp);
                var l = math.min(targets.Length, Config.ValidTargetRemainedCount);
                for (var i = 0; i < l; i++)
                {
                    var t = targets[i];
                    var insightTarget = t;
                    var target = insightTarget.Entity;
                    var canHarvest = HarvestLookup.HasComponent(selfEntity);
                    var canHeal = HealLookup.HasComponent(selfEntity);

                    if (!GeneralAttrLookUp.TryGetComponent(insightTarget.Entity, out var targetgeneralAttr)
                        || !StatDataLookup.TryGetComponent(insightTarget.Entity, out var targetStatData)
                        || !InteractUtils.IsTargetValid(in targetgeneralAttr, in selfFaction, in targetStatData,
                            canHeal, canHarvest, AttackLookup.HasComponent(selfEntity),
                            !RegeneratingTagLookup.HasComponent(target)))
                    {
                        continue;
                    }

                    if ((canHarvest || canHeal) && insightTarget.InteractOverride == 0f)
                    {
                        if (targetgeneralAttr.BaseTag == BaseTag.Resources)
                            insightTarget.InteractOverride = Config.HarvestAboveAttack;
                        else if (targetgeneralAttr.Faction == selfFaction)
                            insightTarget.InteractOverride = Config.HealAboveAttack;
                    }

                    if (insightTarget.PriorityValue == 0f)
                        insightTarget.PriorityValue = PriorityLookup[insightTarget.Entity].Value;

                    var targetPosition = TransformLookup[target].Position;
                    insightTarget.DisValue = CalDisPriority(ref targetPosition, ref selfPos, in Config);
                    UpdateTotalValue(ref insightTarget, in Config);

                    tempList.Add(insightTarget);
                }

                tempList.Sort(new TargetComparer()); // 自定义 comparer 排序

                targets.Clear();
                targets.AddRange(tempList.AsArray());
                tempList.Dispose();
            }

            private struct TargetComparer : IComparer<InsightTarget>
            {
                public int Compare(InsightTarget x, InsightTarget y)
                {
                    // 降序排列
                    return y.TotalValue.CompareTo(x.TotalValue);
                }
            }


            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            private static float CalDisPriority(ref float3 targetPosition, ref float3 selfPos,
                in SightSystemConfig config)
            {
                var disSq = math.distancesq(targetPosition, selfPos);
                return math.max(0f, config.BaseLineDistanceSq - disSq);
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            private static void UpdateTotalValue(ref InsightTarget insightTarget, in SightSystemConfig config)
            {
                insightTarget.TotalValue = insightTarget.DisValue * config.DisSqValueMultiplier
                                           + insightTarget.StatChangValue * config.StatValueChangeMultiplier
                                           + insightTarget.PriorityValue + insightTarget.InteractOverride +
                                           insightTarget.MemoryValue;
            }
        }
    }
}