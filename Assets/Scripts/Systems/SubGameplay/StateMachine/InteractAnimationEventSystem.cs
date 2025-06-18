using System;
using SparFlame.Components.General;
using SparFlame.Components.SubGameplay;
using SparFlame.Components.VFX;
using SparFlame.Core.Utils;
using SparFlame.Systems.General.Audio;
using SparFlame.Systems.SubGameplay.Interact;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace SparFlame.Systems.SubGameplay.StateMachine
{
    [UpdateAfter(typeof(StatSystem))]
    [UpdateBefore(typeof(TransformSystemGroup))]
    public partial struct InteractAnimationEventSystem : ISystem
    {
        private NativeParallelMultiHashMap<int, AnimationEventInfo> _hashStringToEventInfos;
        private ComponentLookup<AttackAbility> _attackLookup;
        private ComponentLookup<HealAbility> _healLookup;
        private ComponentLookup<HarvestAbility> _harvestLookup;
        private BufferLookup<AnimationEventData> _eventsLookup;
        private ComponentLookup<LocalTransform> _transformLookup;
        private ComponentLookup<AnimationStateData> _animationStateLookup;
        private ComponentLookup<DarkArcherBuff> _darkArcherBuffLookup;

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<GameTimeData>();
            state.RequireForUpdate<EndSimulationEntityCommandBufferSystem.Singleton>();
            state.RequireForUpdate<SubGamingTag>();
            state.RequireForUpdate<AnimationEventInfo>();

            _attackLookup = state.GetComponentLookup<AttackAbility>(true);
            _healLookup = state.GetComponentLookup<HealAbility>(true);
            _harvestLookup = state.GetComponentLookup<HarvestAbility>(true);
            _eventsLookup = state.GetBufferLookup<AnimationEventData>();
            _transformLookup = state.GetComponentLookup<LocalTransform>(true);
            _animationStateLookup = state.GetComponentLookup<AnimationStateData>();
            _darkArcherBuffLookup = state.GetComponentLookup<DarkArcherBuff>(true);
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            if (!_hashStringToEventInfos.IsCreated)
                Initialize();
            var ecbSingleton = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>();
            var ecb = ecbSingleton.CreateCommandBuffer(state.WorldUnmanaged).AsParallelWriter();
            _attackLookup.Update(ref state);
            _healLookup.Update(ref state);
            _harvestLookup.Update(ref state);
            _eventsLookup.Update(ref state);
            _transformLookup.Update(ref state);
            _animationStateLookup.Update(ref state);
            _darkArcherBuffLookup.Update(ref state);
            var curTime = SystemAPI.GetSingleton<GameTimeData>().ElapsedTime;
            new CheckAnimationEventJob
            {
                ECB = ecb,
                CurTime = curTime,
                AttackLookup = _attackLookup,
                HealLookup = _healLookup,
                HarvestLookup = _harvestLookup,
                EventsLookup = _eventsLookup,
                HashStringToEventInfos = _hashStringToEventInfos,
                LocalTransformLookup = _transformLookup,
                DarkArcherBuffConfigs = SystemAPI.GetSingletonBuffer<DarkArcherBuffConfig>(),
                DarkArcherBuffLookup = _darkArcherBuffLookup
            }.ScheduleParallel();
            new UnitDeadJob
            {
                ECB = ecb,
                EventsLookup = _eventsLookup,
                AnimationStateLookup = _animationStateLookup,
                CurTime = curTime,
            }.ScheduleParallel();
        }

        [BurstCompile]
        public void OnDestroy(ref SystemState state)
        {
            if (_hashStringToEventInfos.IsCreated)
                _hashStringToEventInfos.Dispose();
        }

        private void Initialize()
        {
            var buffer = SystemAPI.GetSingletonBuffer<AnimationEventInfo>();
            _hashStringToEventInfos = new NativeParallelMultiHashMap<int, AnimationEventInfo>(5, Allocator.Persistent);
            foreach (var info in buffer)
            {
                _hashStringToEventInfos.Add(info.eventName.GetHashCode(), info);
            }
        }

        [BurstCompile]
        [WithAll(typeof(UnitDeadTag))]
        public partial struct UnitDeadJob : IJobEntity
        {
            [ReadOnly] public float CurTime;
            [NativeDisableParallelForRestriction] public BufferLookup<AnimationEventData> EventsLookup;
            [NativeDisableParallelForRestriction] public ComponentLookup<AnimationStateData> AnimationStateLookup;
            public EntityCommandBuffer.ParallelWriter ECB;

            private void Execute([ChunkIndexInQuery] int index, Entity selfEntity,
                in DynamicBuffer<LinkedEntityGroup> groups
            )
            {
                for (int i = 1; i < groups.Length; i++)
                {
                    var child = groups[i].Value;
                    if (EventsLookup.TryGetBuffer(child,
                            out var buffer)) // When unit with dead tag raise events, it must be dead event
                    {
                        ref var stateData = ref AnimationStateLookup.GetRefRW(child).ValueRW;
                        if (stateData.State != UnitAnimationState.Die)
                        {
                            stateData.State = UnitAnimationState.Die;
                            stateData.ClipAIndex = (int)UnitAnimationState.Die;
                            stateData.ClipBIndex = stateData.ClipAIndex;
                            stateData.Blending = false;
                            stateData.ClipAWeight = 1f;
                            stateData.ClipBWeight = 0f;
                            stateData.ClipAStartTime = CurTime;
                            stateData.ClipBStartTime = CurTime;
                            stateData.PlaySpeed = 1f;
                            buffer.Clear();

                            return;
                        }

                        if (buffer.Length == 0) continue;
                        ECB.DestroyEntity(index, selfEntity);
                        break;
                    }
                }
            }
        }


        [BurstCompile]
        [WithNone(typeof(IdleStateTag))]
        [WithNone(typeof(MovingStateTag))]
        [WithNone(typeof(GarrisonStateTag))]
        [WithNone(typeof(UnitDeadTag))]
        public partial struct CheckAnimationEventJob : IJobEntity
        {
            [ReadOnly] public float CurTime;
            [NativeDisableParallelForRestriction] public BufferLookup<AnimationEventData> EventsLookup;
            [ReadOnly] public NativeParallelMultiHashMap<int, AnimationEventInfo> HashStringToEventInfos;
            [ReadOnly] public ComponentLookup<AttackAbility> AttackLookup;
            [ReadOnly] public ComponentLookup<HealAbility> HealLookup;
            [ReadOnly] public ComponentLookup<HarvestAbility> HarvestLookup;
            [ReadOnly] public ComponentLookup<LocalTransform> LocalTransformLookup;
            [ReadOnly] public DynamicBuffer<DarkArcherBuffConfig> DarkArcherBuffConfigs;
            [ReadOnly] public ComponentLookup<DarkArcherBuff> DarkArcherBuffLookup;

            public EntityCommandBuffer.ParallelWriter ECB;

            private void Execute([ChunkIndexInQuery] int index, in DynamicBuffer<LinkedEntityGroup> children,
                in BasicStateData stateData, in LocalTransform transform,
                in SubGameplayGeneralAttr subGameplayGeneralAttr, ref UnitAttr unitAttr, in ExpData expData, Entity selfEntity,
                in InteractAbilityBonus bonus, in DynamicBuffer<InsightTarget> targets
            )
            {
                if (stateData.CurState != InteractState.Attacking
                    && stateData.CurState != InteractState.Healing
                    && stateData.CurState != InteractState.Harvesting) return;

                if (!LocalTransformLookup.TryGetComponent(stateData.TargetEntity, out var targetTransform)) return;
                var selfTransform = LocalTransformLookup[selfEntity];
                // Check model animation events buffer to raise interact stat change 
                for (int i = 1; i < children.Length; i++)
                {
                    if (!EventsLookup.TryGetBuffer(children[i].Value, out var events)) continue;
                    if (events.Length == 0) continue;
                    // We only suggest that there is only one event happen 
                    var e = events[0];
                    events.Clear();
                    if (NativeContainerUtils.TryGetValueAt(HashStringToEventInfos, e.NameHash, e.Parameter - 1,
                            out var eventInfo))
                    {
                        // Generate VFX and stat change request
                        var statChangeRequest = new StatChangeRequest
                        {
                            Interactor = selfEntity,
                            Interactee = stateData.TargetEntity,
                            AbsAmount = stateData.CurState switch
                            {
                                InteractState.Attacking => AttackLookup[selfEntity].Amount + bonus.AmountBonus,
                                InteractState.Healing => HealLookup[selfEntity].Amount + bonus.AmountBonus,
                                InteractState.Harvesting => HarvestLookup[selfEntity].Amount + bonus.AmountBonus,
                                _ => 0 // This should never happen
                            },
                            Type = stateData.CurState switch
                            {
                                InteractState.Attacking => StatChangeType.Attack,
                                InteractState.Healing => StatChangeType.Heal,
                                InteractState.Harvesting => StatChangeType.Harvest,
                                _ => StatChangeType.None // This should never happen
                            },
                            InteractorSubGameplayGeneralAttr = subGameplayGeneralAttr,
                            DamageType = unitAttr.Type switch
                            {
                                UnitType.Cavalry when stateData.CurState == InteractState.Attacking => DamageType
                                    .Physical,
                                UnitType.Shield when stateData.CurState == InteractState.Attacking => DamageType
                                    .Physical,
                                UnitType.Ranged when stateData.CurState == InteractState.Attacking => DamageType
                                    .Physical,
                                UnitType.Magic when stateData.CurState == InteractState.Attacking => DamageType.Magic,
                                UnitType.Worker when stateData.CurState == InteractState.Attacking => DamageType
                                    .Physical,
                                _ => DamageType.None
                            }
                        };

                        statChangeRequest.AbsAmount = (int)(statChangeRequest.AbsAmount * eventInfo.amountMultiplier);

                        if (eventInfo.sendVfxName != VFXName.None)
                        {
                            var vfxRequest = ECB.CreateEntity(index);
                            ECB.AddComponent<SubGameplayEntityTag>(index, vfxRequest);
                            if (eventInfo.animationInteractType != AnimationInteractType.VfxChangeStat)
                                statChangeRequest.Interactee = Entity.Null;
                            var vfx = new VFXRequest
                            {
                                // If attacking, then vfx starts from attacker, otherwise starts from target position
                                SpawnPosition = stateData.CurState == InteractState.Healing
                                    ? targetTransform.Position
                                    : selfTransform.Position,
                                Filter = new VFXSubFilter
                                {
                                    FactionFilterEnable = true,
                                    Faction = subGameplayGeneralAttr.FactionTag,
                                    Tier = expData.curTier,
                                    TierFilterEnable = true,
                                },
                                KeepDuration =
                                    4, // This is cleric healing circle duration; Other interact effect is projectile and its lifetime not handled by this variable
                                StatChangeRequest = statChangeRequest,
                                RequestType = VFXRequestType.Spawn,
                                VFXName = eventInfo.sendVfxName,
                                VFXTrackTarget = Entity.Null,
                                ParabolaTargetPosition = targetTransform.Position
                            };
                            ECB.AddComponent(index, vfxRequest, vfx);

                            // Apply dark archer buff
                            if (DarkArcherBuffLookup.HasComponent(selfEntity))
                            {
                                var config = DarkArcherBuffConfigs[(int)expData.curTier - 3];
                                var count = 0;
                                if (unitAttr.Rnd.NextFloat(0f, 1f) < config.extraArrowTriggerChance)
                                {
                                    foreach (var target in targets)
                                    {
                                        if (count >= config.extraArrowCount) break;
                                        // Target is main target, skip
                                        if (target.Entity == stateData.TargetEntity) continue;
                                        // Target is invalid, skip
                                        if (!LocalTransformLookup.TryGetComponent(target.Entity,
                                                out var newTargetTransform)) continue;
                                        // Target not in range, skip
                                        if (math.distance(newTargetTransform.Position, selfTransform.Position) >
                                            bonus.RangeBonus + AttackLookup[selfEntity].Range) continue;
                                        count++;
                                        var newStatChangeRequest = statChangeRequest;
                                        newStatChangeRequest.Interactee = target.Entity;
                                        newStatChangeRequest.AbsAmount =
                                            (int)(statChangeRequest.AbsAmount * config.extraArrowDamageScale);

                                        var extraArrowVfxRequest = ECB.CreateEntity(index);
                                        ECB.AddComponent<SubGameplayEntityTag>(index, extraArrowVfxRequest);
                                        var extraArrowVfx = new VFXRequest
                                        {
                                            SpawnPosition = selfTransform.Position,
                                            Filter = vfx.Filter,
                                            KeepDuration = vfx.KeepDuration,
                                            StatChangeRequest = newStatChangeRequest,
                                            RequestType = VFXRequestType.Spawn,
                                            VFXName = eventInfo.sendVfxName,
                                            VFXTrackTarget = Entity.Null,
                                            ParabolaTargetPosition = newTargetTransform.Position
                                        };
                                        ECB.AddComponent(index, extraArrowVfxRequest, extraArrowVfx);
                                    }
                                    for (int j = 0; j < config.extraArrowCount - count; j++)
                                    {
                                        var extraArrowVfxRequest = ECB.CreateEntity(index);
                                        ECB.AddComponent<SubGameplayEntityTag>(index, extraArrowVfxRequest);
                                        var extraArrowVfx = new VFXRequest
                                        {
                                            SpawnPosition = selfTransform.Position,
                                            Filter = vfx.Filter,
                                            KeepDuration = vfx.KeepDuration,
                                            StatChangeRequest = statChangeRequest,
                                            RequestType = VFXRequestType.Spawn,
                                            VFXName = eventInfo.sendVfxName,
                                            VFXTrackTarget = Entity.Null,
                                            ParabolaTargetPosition = targetTransform.Position
                                        };
                                        ECB.AddComponent(index, extraArrowVfxRequest, extraArrowVfx);
                                    }
                                }
                            }
                        }

                        if (eventInfo.animationInteractType == AnimationInteractType.AoeBuffChangeStat)
                        {
                            var targetFaction = stateData.CurState switch
                            {
                                InteractState.Attacking => ~subGameplayGeneralAttr.FactionTag,
                                InteractState.Healing => subGameplayGeneralAttr.FactionTag,
                                InteractState.Harvesting => FactionTag.Neutral,
                                _ => FactionTag.Neutral // This should never happen
                            };
                            statChangeRequest.Interactee = stateData.TargetEntity;
                            var aoeBuffRequest = ECB.CreateEntity(index);
                            ECB.AddComponent<SubGameplayEntityTag>(index, aoeBuffRequest);
                            ECB.AddComponent(index, aoeBuffRequest, new AoeInteractData
                            {
                                TriggerTime = CurTime + eventInfo.sendStatChangeRequestDelay,
                                StatChangeRequest = statChangeRequest,
                                TargetFaction = targetFaction,
                            });

                            ECB.AddComponent(index, aoeBuffRequest, new BuffRequest
                            {
                                SpawnPosition = selfTransform.Position,
                                SpawnRotation = selfTransform.Rotation,
                                TrackTarget = Entity.Null,
                                // Only magic unit has aoe attack, others only has vfx
                                Name = stateData.CurState == InteractState.Healing
                                    ? BuffName.ClericHealCircle
                                    : BuffName.MagicSwordSplash,
                                Filter = new BuffFilter
                                {
                                    factionFilterEnabled = true,
                                    faction = subGameplayGeneralAttr.FactionTag,
                                    tier = expData.curTier,
                                    tierFilterEnabled = true
                                }
                            });
                        }

                        if (eventInfo.animationInteractType == AnimationInteractType.DirectlyChangeStat)
                        {
                            var statChangeRequestEntity = ECB.CreateEntity(index);
                            statChangeRequest.Interactee = stateData.TargetEntity;
                            ECB.AddComponent<SubGameplayEntityTag>(index, statChangeRequestEntity);
                            ECB.AddComponent(index, statChangeRequestEntity, statChangeRequest);
                        }

                        // Generate audio request
                        var name = AudioName.None;
                        switch (unitAttr.Type)
                        {
                            case UnitType.Shield:
                                break;
                            case UnitType.Ranged:
                                name = AudioName.ArrowShoot;
                                break;
                            case UnitType.Magic:
                                name = stateData.CurState == InteractState.Healing
                                    ? AudioName.ClericHealCircle
                                    : AudioName.MagicSwordSplash;
                                break;
                            case UnitType.Cavalry:
                                name = AudioName.Spear;
                                break;
                            case UnitType.Worker:
                                name = AudioName.WorkerHarvest;
                                break;
                            default:
                                throw new ArgumentOutOfRangeException();
                        }

                        if (name != AudioName.None)
                        {
                            AudioUtils.PlayAudioClip(name, transform.Position, ECB, index);
                        }
                    }

                    // Only check one event in models
                    break;
                }
            }
        }
    }
}