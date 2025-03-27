using System.Runtime.CompilerServices;
using SparFlame.GamePlaySystem.General;
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
        private ComponentLookup<InteractableAttr> _interactableLookup;
        private ComponentLookup<StatData> _statDataLookup;
        private ComponentLookup<HealStateTag> _healLookup;
        private ComponentLookup<HarvestStateTag> _harvestLookup;
        private ComponentLookup<LocalTransform> _localTransformLookup;
        private ComponentLookup<InteractPriority> _priorityLookup;
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<NotPauseTag>();
            state.RequireForUpdate<SightSystemConfig>();
            _interactableLookup = state.GetComponentLookup<InteractableAttr>(true);
            _statDataLookup = state.GetComponentLookup<StatData>(true);
            _localTransformLookup = state.GetComponentLookup<LocalTransform>(true);
            _healLookup = state.GetComponentLookup<HealStateTag>(true);
            _harvestLookup = state.GetComponentLookup<HarvestStateTag>(true);
            _priorityLookup = state.GetComponentLookup<InteractPriority>(true);
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
            new UpdateTargetListJob
            {
                Config = config,
                InteractableAttrLookup = _interactableLookup,
                StatDataLookup = _statDataLookup,
                HealLookup = _healLookup,
                HarvestLookup = _harvestLookup,
                TransformLookup = _localTransformLookup,
                PriorityLookup = _priorityLookup,
            }.ScheduleParallel();
        }

   
        [BurstCompile]
        private partial struct UpdateTargetListJob : IJobEntity
        {
            [ReadOnly] public ComponentLookup<InteractPriority> PriorityLookup;
            [ReadOnly] public ComponentLookup<InteractableAttr> InteractableAttrLookup;
            [ReadOnly] public ComponentLookup<StatData> StatDataLookup;
            [ReadOnly] public ComponentLookup<LocalTransform> TransformLookup;
            [ReadOnly] public ComponentLookup<HealStateTag> HealLookup;
            [ReadOnly] public ComponentLookup<HarvestStateTag> HarvestLookup;
            [ReadOnly] public SightSystemConfig Config;

            private void Execute(ref DynamicBuffer<InsightTarget> targets, Entity entity)
            {
                var selfFaction = InteractableAttrLookup[entity].FactionTag;
                var selfPos = TransformLookup[entity].Position;

                for (var i = targets.Length - 1; i >=0 ; i--)
                {
                    var insightTarget = targets[i];
                    var target = insightTarget.Entity;
                    var canHarvest = HarvestLookup.HasComponent(entity);
                    var canHeal = HealLookup.HasComponent(entity);
                    // Remove invalid target
                    if (!InteractableAttrLookup.TryGetComponent(insightTarget.Entity, out var targetInteractAttr)
                        ||!StatDataLookup.TryGetComponent(insightTarget.Entity, out var targetStatData)
                        ||!InteractUtils.IsTargetValid(in targetInteractAttr,in selfFaction,in targetStatData,canHeal ,
                           canHarvest))
                    {
                        targets.RemoveAt(i);
                        continue;
                    }
                    // Update InteractOverride, which used for healer and farmer
                    if ((canHarvest || canHeal) && insightTarget.InteractOverride == 0f )
                    {
                        if (targetInteractAttr.BaseTag == BaseTag.Resources)
                        {   // Harvest
                            insightTarget.InteractOverride = Config.HarvestAboveAttack;
                        }
                        else
                        {   // Heal
                            if (targetInteractAttr.FactionTag == selfFaction)
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
                    insightTarget.DisValue = CalDisPriority(ref targetPosition,ref selfPos,in Config);
                    // Update total value
                    UpdateTotalValue(ref insightTarget, in Config);
                    targets[i] = insightTarget;
                }
            }
            
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            private static float CalDisPriority(ref float3 targetPosition, ref float3 selfPos, in SightSystemConfig config)
            {
                var disSq = math.distancesq(targetPosition, selfPos);
                return math.max(0f, config.BaseLineDistanceSq - disSq);
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            private static void UpdateTotalValue(ref InsightTarget insightTarget, in SightSystemConfig config)
            {
                insightTarget.TotalValue = insightTarget.DisValue * config.DisSqValueMultiplier
                                           + insightTarget.StatChangValue * config.StatValueChangeMultiplier
                                           + insightTarget.PriorityValue + insightTarget.InteractOverride + insightTarget.MemoryValue;
            }
        }
    }
}