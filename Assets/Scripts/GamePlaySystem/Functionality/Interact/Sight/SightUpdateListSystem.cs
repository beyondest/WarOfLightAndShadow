using System.Runtime.CompilerServices;
using SparFlame.GamePlaySystem.General;
using SparFlame.GamePlaySystem.Resource;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace SparFlame.GamePlaySystem.Interact
{
    [BurstCompile]
    public partial struct SightUpdateListSystem : ISystem
    {
        private ComponentLookup<GeneralAttr> _interactableLookup;
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
            state.RequireForUpdate<GamingTag>();
            state.RequireForUpdate<SightSystemConfig>();
            state.RequireForUpdate<InsightTarget>();
            _interactableLookup = state.GetComponentLookup<GeneralAttr>(true);
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
            new UpdateTargetListJob
            {
                Config = config,
                InteractableAttrLookup = _interactableLookup,
                StatDataLookup = _statDataLookup,
                HealLookup = _healLookup,
                HarvestLookup = _harvestLookup,
                AttackLookup = _attackLookup,
                TransformLookup = _localTransformLookup,
                PriorityLookup = _priorityLookup,
                RegeneratingTagLookup = _resourceAttrLookup
            }.ScheduleParallel();
        }


        [BurstCompile]
        private partial struct UpdateTargetListJob : IJobEntity
        {
            [ReadOnly] public ComponentLookup<SightPriority> PriorityLookup;
            [ReadOnly] public ComponentLookup<GeneralAttr> InteractableAttrLookup;
            [ReadOnly] public ComponentLookup<StatData> StatDataLookup;
            [ReadOnly] public ComponentLookup<LocalTransform> TransformLookup;
            [ReadOnly] public ComponentLookup<HealStateTag> HealLookup;
            [ReadOnly] public ComponentLookup<HarvestStateTag> HarvestLookup;
            [ReadOnly] public ComponentLookup<AttackStateTag> AttackLookup;
            [ReadOnly] public ComponentLookup<RegeneratingTag> RegeneratingTagLookup;
            [ReadOnly] public SightSystemConfig Config;

            private void Execute(ref DynamicBuffer<InsightTarget> targets, Entity selfEntity)
            {
                var selfFaction = InteractableAttrLookup[selfEntity].FactionTag;
                var selfPos = TransformLookup[selfEntity].Position;

                for (var i = targets.Length - 1; i >= 0; i--)
                {
                    var insightTarget = targets[i];
                    var target = insightTarget.Entity;
                    var canHarvest = HarvestLookup.HasComponent(selfEntity);
                    var canHeal = HealLookup.HasComponent(selfEntity);

                    // Remove invalid target
                    if (!InteractableAttrLookup.TryGetComponent(insightTarget.Entity, out var targetgeneralAttr)
                        || !StatDataLookup.TryGetComponent(insightTarget.Entity, out var targetStatData)
                        || !InteractUtils.IsTargetValid(in targetgeneralAttr, in selfFaction, in targetStatData,
                            canHeal,
                            canHarvest, AttackLookup.HasComponent(selfEntity),
                            !RegeneratingTagLookup.HasComponent(target)))
                    {
                        targets.RemoveAt(i);
                        continue;
                    }

                    // Update InteractOverride, which used for healer and farmer
                    if ((canHarvest || canHeal) && insightTarget.InteractOverride == 0f)
                    {
                        if (targetgeneralAttr.BaseTag == BaseTag.Resources)
                        {
                            // Harvest
                            insightTarget.InteractOverride = Config.HarvestAboveAttack;
                        }
                        else
                        {
                            // Heal
                            if (targetgeneralAttr.FactionTag == selfFaction)
                            {
                                insightTarget.InteractOverride = Config.HealAboveAttack;
                            }
                        }
                    }

                    if (insightTarget.PriorityValue == 0f)
                    {
                        var priority = PriorityLookup[insightTarget.Entity];
                        insightTarget.PriorityValue = priority.Value;
                    }

                    // Update value via position
                    var targetPosition = TransformLookup[target].Position;
                    insightTarget.DisValue = CalDisPriority(ref targetPosition, ref selfPos, in Config);
                    // Update total value
                    UpdateTotalValue(ref insightTarget, in Config);
                    targets[i] = insightTarget;
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