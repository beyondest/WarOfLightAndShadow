using SparFlame.Components.General;
using SparFlame.Components.SubGameplay;
using SparFlame.Core.Utils;
using SparFlame.Systems.General.Animation;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

namespace SparFlame.Systems.SubGameplay.StateMachine
{
    public partial struct AnimationStateChangeSystem : ISystem
    {
        private ComponentLookup<AnimationStateData> _animationStateLookup;
        private ComponentLookup<AttackAbility> _attackLookup;
        private ComponentLookup<HealAbility> _healLookup;
        private ComponentLookup<HarvestAbility> _harvestLookup;

        private NativeParallelMultiHashMap<int, UnitTypeToAnimationStateToSpeedScalePair>
            _unitTypeToAnimationStateToSpeedScale;

        private NativeParallelMultiHashMap<int, int> _modelIndices;

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<GameTimeData>();
            state.RequireForUpdate<AnimationPlayConfig>();
            state.RequireForUpdate<SubGamingTag>();
            state.RequireForUpdate<AnimationStateData>();
            state.RequireForUpdate<BasicStateData>();
            _animationStateLookup = state.GetComponentLookup<AnimationStateData>();
            _attackLookup = state.GetComponentLookup<AttackAbility>(true);
            _healLookup = state.GetComponentLookup<HealAbility>(true);
            _harvestLookup = state.GetComponentLookup<HarvestAbility>(true);
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            if (!_unitTypeToAnimationStateToSpeedScale.IsCreated)
                Initialize();

            _animationStateLookup.Update(ref state);
            _attackLookup.Update(ref state);
            _healLookup.Update(ref state);
            _harvestLookup.Update(ref state);
            state.Dependency = new AnimationStateChangeJob
            {
                AnimationDataLookup = _animationStateLookup,
                CurTime = SystemAPI.GetSingleton<GameTimeData>().ElapsedTime,
                Config = SystemAPI.GetSingleton<AnimationPlayConfig>(),
                AttackLookup = _attackLookup,
                HealLookup = _healLookup,
                HarvestLookup = _harvestLookup,
                Pairs = _unitTypeToAnimationStateToSpeedScale,
                ModelIndices = _modelIndices,
            }.ScheduleParallel(state.Dependency);
        }

        [BurstCompile]
        public void OnDestroy(ref SystemState state)
        {
            if (_unitTypeToAnimationStateToSpeedScale.IsCreated)
                _unitTypeToAnimationStateToSpeedScale.Dispose();
            if (_modelIndices.IsCreated)
                _modelIndices.Dispose();
        }

        private void Initialize()
        {
            var buffer = SystemAPI.GetSingletonBuffer<UnitTypeToAnimationStateToSpeedScalePair>();
            _unitTypeToAnimationStateToSpeedScale =
                new NativeParallelMultiHashMap<int, UnitTypeToAnimationStateToSpeedScalePair>(5, Allocator.Persistent);

            foreach (var pair in buffer)
            {
                _unitTypeToAnimationStateToSpeedScale.Add((int)pair.unitType, pair);
            }

            var configs = SystemAPI.GetSingletonBuffer<AnimationModelIndices>();
            _modelIndices = new NativeParallelMultiHashMap<int, int>(configs.Length, Allocator.Persistent);
            foreach (var config in configs)
            {
                _modelIndices.Add((int)config.unitType, config.modelIndex);
            }
        }


        [BurstCompile]
        [WithNone(typeof(UnitDeadTag))]
        public partial struct AnimationStateChangeJob : IJobEntity
        {
            // This component is only written to self children and children not cross
            [NativeDisableParallelForRestriction] public ComponentLookup<AnimationStateData> AnimationDataLookup;
            [ReadOnly] public float CurTime;
            [ReadOnly] public AnimationPlayConfig Config;
            [ReadOnly] public ComponentLookup<AttackAbility> AttackLookup;
            [ReadOnly] public ComponentLookup<HealAbility> HealLookup;
            [ReadOnly] public ComponentLookup<HarvestAbility> HarvestLookup;
            [ReadOnly] public NativeParallelMultiHashMap<int, UnitTypeToAnimationStateToSpeedScalePair> Pairs;
            [ReadOnly] public NativeParallelMultiHashMap<int, int> ModelIndices;

            private void Execute(in BasicStateData basicStateData,
                in DynamicBuffer<InsightTarget> targets, in DynamicBuffer<LinkedEntityGroup> groups,
                Entity selfEntity, in MovableData movableData, in UnitAttr unitAttr, in InteractAbilityBonus bonus)
            {
                var key = (int)unitAttr.type;

                foreach (var modelIndex in ModelIndices.GetValuesForKey(key))
                {
                    var model = groups[modelIndex].Value;
                    if (!AnimationDataLookup.HasComponent(model)) continue;
                    ref var animationData = ref AnimationDataLookup.GetRefRW(model).ValueRW;
                    var oriValue = animationData.State;

                    float animationPlaySpeedScale = 1f;
                    switch (basicStateData.CurState)
                    {
                        case InteractState.Idle:
                            animationData.State = UnitAnimationState.Idle;
                            animationPlaySpeedScale *= 1f;
                            break;
                        case InteractState.Moving:
                            animationData.State =
                                targets.IsEmpty ? UnitAnimationState.March : UnitAnimationState.AlertMarch;
                            animationPlaySpeedScale *=
                                movableData.MoveSpeed + bonus.MoveSpeedBonus;
                            break;
                        case InteractState.Garrison:
                            animationData.State = UnitAnimationState.Idle;
                            animationPlaySpeedScale *= 1f;
                            break;
                        case InteractState.Attacking:
                            animationData.State = UnitAnimationState.Attack;

                            var attackAbility = AttackLookup[selfEntity];
                            animationPlaySpeedScale *= attackAbility.Speed + bonus.SpeedBonus;
                            break;
                        case InteractState.Harvesting:
                            animationData.State = UnitAnimationState.Attack;

                            animationPlaySpeedScale *= HarvestLookup[selfEntity].Speed + bonus.SpeedBonus;
                            break;
                        case InteractState.Healing:
                            animationData.State = UnitAnimationState.Attack;

                            animationPlaySpeedScale *= HealLookup[selfEntity].Speed + bonus.SpeedBonus;
                            break;
                        case InteractState.CastSkill:
                            animationData.State = UnitAnimationState.CastSkill;
                            animationPlaySpeedScale *= 1f;
                            break;
                        default:
                            BurstSafe.UnexpectedEnum(basicStateData.CurState);
                            break;
                    }

                    if (animationData.State != oriValue)
                    {
                        // data.Blending = true;
                        animationData.ClipAIndex = (int)oriValue;
                        animationData.ClipAIndex = (int)animationData.State;
                        animationData.ClipBIndex = (int)animationData.State;
                        animationData.ClipAWeight = 1f;
                        animationData.ClipBWeight = 0f;
                        animationData.ClipAStartTime = CurTime;
                        animationData.ClipBStartTime = CurTime;
                        // data.PlaySpeed = 1f;
                        return;
                    }

                    // Set play speed, need to calculate event state not changed, because interact speed or moving speed may change
                    var animationInitSpeedScale = 1f;
                    foreach (var pair in Pairs.GetValuesForKey(key))
                    {
                        if (pair.state == animationData.State)
                        {
                            animationInitSpeedScale = pair.speedScale;
                            break;
                        }
                    }

                    animationData.PlaySpeed = animationPlaySpeedScale * animationInitSpeedScale;

                    /*if (animationData.Blending)
                    {
                        var t = CurTime - animationData.ClipBStartTime;
                        if (t > Config.blendDuration)
                        {
                            animationData.ClipAIndex = animationData.ClipBIndex;
                            animationData.ClipAStartTime = animationData.ClipBStartTime;
                            animationData.ClipAWeight = 1f;
                            animationData.Blending = false;
                            return;
                        }

                        var weightB = t / Config.blendDuration;
                        animationData.ClipAWeight = 1 - weightB;
                        animationData.ClipBWeight = weightB;
                    }*/
                }
            }
        }
    }
}