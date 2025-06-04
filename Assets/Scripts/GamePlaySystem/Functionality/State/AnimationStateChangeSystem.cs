using System;
using SparFlame.GamePlaySystem.Animation;
using SparFlame.GamePlaySystem.General;
using SparFlame.GamePlaySystem.Interact;
using SparFlame.GamePlaySystem.Movement;
using SparFlame.GamePlaySystem.Units;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;

namespace SparFlame.GamePlaySystem.State
{
    public partial struct AnimationStateChangeSystem : ISystem
    {
        private ComponentLookup<AnimationStateData> _animationStateLookup;
        private ComponentLookup<AttackAbility> _attackLookup;
        private ComponentLookup<HealAbility> _healLookup;
        private ComponentLookup<HarvestAbility> _harvestLookup;

        private NativeParallelMultiHashMap<int, UnitTypeToAnimationStateToSpeedScalePair>
            _unitTypeToAnimationStateToSpeedScale;

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<GameTimeData>();
            state.RequireForUpdate<AnimationPlayConfig>();
            state.RequireForUpdate<GamingTag>();
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
            new AnimationStateChangeJob
            {
                AnimationLookup = _animationStateLookup,
                CurTime = SystemAPI.GetSingleton<GameTimeData>().ElapsedTime,
                Config = SystemAPI.GetSingleton<AnimationPlayConfig>(),
                AttackLookup = _attackLookup,
                HealLookup = _healLookup,
                HarvestLookup = _harvestLookup,
                Pairs = _unitTypeToAnimationStateToSpeedScale
            }.ScheduleParallel();
        }

        [BurstCompile]
        public void OnDestroy(ref SystemState state)
        {
            if (_unitTypeToAnimationStateToSpeedScale.IsCreated)
                _unitTypeToAnimationStateToSpeedScale.Dispose();
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
        }


        [BurstCompile]
        [WithNone(typeof(UnitDeadTag))]
        public partial struct AnimationStateChangeJob : IJobEntity
        {
            [NativeDisableParallelForRestriction] public ComponentLookup<AnimationStateData> AnimationLookup;
            [ReadOnly] public float CurTime;
            [ReadOnly] public AnimationPlayConfig Config;
            [ReadOnly] public ComponentLookup<AttackAbility> AttackLookup;
            [ReadOnly] public ComponentLookup<HealAbility> HealLookup;
            [ReadOnly] public ComponentLookup<HarvestAbility> HarvestLookup;
            [ReadOnly] public NativeParallelMultiHashMap<int, UnitTypeToAnimationStateToSpeedScalePair> Pairs;

            private void Execute(in BasicStateData basicStateData,
                in DynamicBuffer<InsightTarget> targets, in DynamicBuffer<LinkedEntityGroup> groups,
                Entity selfEntity, in MovableData movableData, in UnitAttr unitAttr)
            {
                for (int i = 1; i < groups.Length; i++)
                {
                    var model = groups[i].Value;
                    if (!AnimationLookup.HasComponent(model)) continue;
                    ref var data = ref AnimationLookup.GetRefRW(model).ValueRW;
                    var oriValue = data.State;
                    var targetPair = new UnitTypeToAnimationStateToSpeedScalePair
                    {
                        speedScale = 1f
                    };
                    foreach (var pair in Pairs.GetValuesForKey((int)unitAttr.Type))
                    {
                        if (pair.state == data.State)
                        {
                            targetPair = pair;
                            break;
                        }
                    }

                    switch (basicStateData.CurState)
                    {
                        case InteractState.Idle:
                            data.State = UnitAnimationState.Idle;

                            data.PlaySpeed = 1f * targetPair.speedScale;
                            break;
                        case InteractState.Attacking:
                            data.State = UnitAnimationState.Attack;

                            var attackAbility = AttackLookup[selfEntity];
                            data.PlaySpeed = attackAbility.Speed * targetPair.speedScale;
                            break;
                        case InteractState.Moving:
                            data.State = targets.IsEmpty ? UnitAnimationState.March : UnitAnimationState.AlertMarch;

                            data.PlaySpeed = movableData.MoveSpeed * targetPair.speedScale;
                            break;
                        case InteractState.Garrison:
                            data.State = UnitAnimationState.Idle;

                            data.PlaySpeed = 1f * targetPair.speedScale;
                            break;
                        case InteractState.Harvesting:
                            data.State = UnitAnimationState.Attack;

                            data.PlaySpeed = HarvestLookup[selfEntity].Speed * targetPair.speedScale;
                            break;
                        case InteractState.Healing:
                            data.State = UnitAnimationState.Attack;

                            data.PlaySpeed = HealLookup[selfEntity].Speed * targetPair.speedScale;
                            break;
                        default:
                            throw new ArgumentOutOfRangeException();
                    }


                    if (data.State != oriValue)
                    {
                        // data.Blending = true;
                        data.ClipAIndex = (int)oriValue;
                        data.ClipAIndex = (int)data.State;
                        data.ClipBIndex = (int)data.State;
                        data.ClipAWeight = 1f;
                        data.ClipBWeight = 0f;
                        data.ClipAStartTime = CurTime;
                        data.ClipBStartTime = CurTime;
                        // data.PlaySpeed = 1f;
                        return;
                    }

                    if (data.Blending)
                    {
                        var t = CurTime - data.ClipBStartTime;
                        if (t > Config.blendDuration)
                        {
                            data.ClipAIndex = data.ClipBIndex;
                            data.ClipAStartTime = data.ClipBStartTime;
                            data.ClipAWeight = 1f;
                            data.Blending = false;
                            return;
                        }

                        var weightB = t / Config.blendDuration;
                        data.ClipAWeight = 1 - weightB;
                        data.ClipBWeight = weightB;
                    }
                }
            }
        }
    }
}