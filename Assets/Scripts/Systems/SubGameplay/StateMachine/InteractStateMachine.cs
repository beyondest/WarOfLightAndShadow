using System.Runtime.CompilerServices;
using SparFlame.Components.General;
using SparFlame.Components.SubGameplay;
using SparFlame.Components.VFX;
using SparFlame.Core.Utils;
using SparFlame.Systems.General.Audio;
using Unity.Entities;
using Unity.Collections;
using Unity.Transforms;
using SparFlame.Systems.SubGameplay.Interact;
using SparFlame.Systems.SubGameplay.Movement;
using Unity.Burst;
using Unity.Jobs;
using Unity.Mathematics;


namespace SparFlame.Systems.SubGameplay.StateMachine
{
    [BurstCompile]
    [UpdateAfter(typeof(BuffManageSystem))]
    [UpdateAfter(typeof(SightUpdateListSystem))]
    public partial struct InteractStateMachine : ISystem
    {
        private BufferLookup<InsightTarget> _insightTarget;

        private ComponentLookup<StatData> _stat;
        private ComponentLookup<LocalTransform> _localTransform;
        private ComponentLookup<SubGameplayGeneralAttr> _generalAttrLookup;
        private ComponentLookup<BoxColliderSize> _boxColliderSizeLookup;
        private ComponentLookup<MovableData> _movable;
        private ComponentLookup<BasicStateData> _basicState;
        private ComponentLookup<HealStateTag> _healState;
        private ComponentLookup<HarvestStateTag> _harvestState;
        private ComponentLookup<RegeneratingTag> _regeneratingTag;
        private ComponentLookup<AttackStateTag> _attackTag;
        private ComponentLookup<BuildingAttr> _buildingAttrLookup;
        private ComponentLookup<ExpData> _expDataLookup;
        private ComponentLookup<InteractAbilityBonus> _abilityBonusLookup;
        private ComponentLookup<DarkShieldTauntedBuff> _darkShieldTauntedBuffLookup;

        private EntityQuery _attackEntityQuery;
        private EntityQuery _healEntityQuery;
        private EntityQuery _harvestEntityQuery;


        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<GameTimeData>();
            state.RequireForUpdate<SightSystemConfig>();
            state.RequireForUpdate<InteractStateMachineConfig>();
            state.RequireForUpdate<EndSimulationEntityCommandBufferSystem.Singleton>();
            state.RequireForUpdate<SubGamingTag>();


            _attackEntityQuery = SystemAPI.QueryBuilder().WithAllRW<AttackAbility>().WithAllRW<BasicStateData>()
                .WithAll<AttackStateTag>().WithNone<UnitDeadTag>().Build();
            _healEntityQuery = SystemAPI.QueryBuilder().WithAllRW<HealAbility>().WithAllRW<BasicStateData>()
                .WithAll<HealStateTag>().WithNone<UnitDeadTag>().Build();
            _harvestEntityQuery = SystemAPI.QueryBuilder().WithAllRW<HarvestAbility>().WithAllRW<BasicStateData>()
                .WithAll<HarvestStateTag>().WithNone<UnitDeadTag>().Build();


            _stat = state.GetComponentLookup<StatData>(true);
            _generalAttrLookup = state.GetComponentLookup<SubGameplayGeneralAttr>(true);
            _boxColliderSizeLookup = state.GetComponentLookup<BoxColliderSize>(true);
            _insightTarget = state.GetBufferLookup<InsightTarget>(true);
            _healState = state.GetComponentLookup<HealStateTag>(true);
            _harvestState = state.GetComponentLookup<HarvestStateTag>(true);
            _attackTag = state.GetComponentLookup<AttackStateTag>(true);
            _localTransform = state.GetComponentLookup<LocalTransform>();
            _movable = state.GetComponentLookup<MovableData>();
            _basicState = state.GetComponentLookup<BasicStateData>();
            _regeneratingTag = state.GetComponentLookup<RegeneratingTag>();
            _buildingAttrLookup = state.GetComponentLookup<BuildingAttr>(true);
            _expDataLookup = state.GetComponentLookup<ExpData>(true);
            _abilityBonusLookup = state.GetComponentLookup<InteractAbilityBonus>(true);
            _darkShieldTauntedBuffLookup = state.GetComponentLookup<DarkShieldTauntedBuff>(true);
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var curTime = SystemAPI.GetSingleton<GameTimeData>().ElapsedTime;
            var ecbSingleton = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>();
            var ecb = ecbSingleton.CreateCommandBuffer(state.WorldUnmanaged).AsParallelWriter();
            var config = SystemAPI.GetSingleton<InteractStateMachineConfig>();
            var sightSystemConfig = SystemAPI.GetSingleton<SightSystemConfig>();
            _stat.Update(ref state);
            _localTransform.Update(ref state);
            _generalAttrLookup.Update(ref state);
            _boxColliderSizeLookup.Update(ref state);
            _movable.Update(ref state);
            _insightTarget.Update(ref state);
            _basicState.Update(ref state);
            _healState.Update(ref state);
            _regeneratingTag.Update(ref state);
            _harvestState.Update(ref state);
            _attackTag.Update(ref state);
            _buildingAttrLookup.Update(ref state);
            _expDataLookup.Update(ref state);
            _abilityBonusLookup.Update(ref state);
            _darkShieldTauntedBuffLookup.Update(ref state);
            var attackAbilities = _attackEntityQuery.ToComponentDataArray<AttackAbility>(Allocator.TempJob);
            var attackEntities = _attackEntityQuery.ToEntityArray(Allocator.TempJob);
            state.Dependency.Complete();
            var deltaTime = SystemAPI.GetSingleton<GameTimeData>().DeltaTime;
            var attackJob = new InteractStateJob<AttackAbility>
            {
                CurTime = curTime,
                Ability = attackAbilities,
                Entities = attackEntities,
                StatDataLookup = _stat,
                TransformLookup = _localTransform,
                GeneralAttrLookup = _generalAttrLookup,
                BoxColliderSizeLookup = _boxColliderSizeLookup,
                InsightTarget = _insightTarget,
                RegeneratingTagLookup = _regeneratingTag,
                ECB = ecb,
                MovableLookup = _movable,
                BasicStateData = _basicState,
                HealLookup = _healState,
                HarvestLookup = _harvestState,
                AttackLookup = _attackTag,
                DeltaTime = deltaTime,
                InteractTurnSpeed = config.InteractTurnSpeed,
                BuildingAttrLookup = _buildingAttrLookup,
                Config = sightSystemConfig,
                ExpDataLookup = _expDataLookup,
                AbilityBonusLookup = _abilityBonusLookup,
                DarkShieldTauntedBuffLookup = _darkShieldTauntedBuffLookup

            }.Schedule(attackEntities.Length, config.AttackJobBatchCount, state.Dependency);
            state.Dependency = attackJob;

            var healAbilities = _healEntityQuery.ToComponentDataArray<HealAbility>(Allocator.TempJob);
            var healEntities = _healEntityQuery.ToEntityArray(Allocator.TempJob);
            var healJob = new InteractStateJob<HealAbility>
            {
                CurTime = curTime,

                Ability = healAbilities,
                Entities = healEntities,
                StatDataLookup = _stat,
                TransformLookup = _localTransform,
                GeneralAttrLookup = _generalAttrLookup,
                BoxColliderSizeLookup = _boxColliderSizeLookup,
                InsightTarget = _insightTarget,
                RegeneratingTagLookup = _regeneratingTag,
                ECB = ecb,
                MovableLookup = _movable,
                BasicStateData = _basicState,
                HealLookup = _healState,
                HarvestLookup = _harvestState,
                AttackLookup = _attackTag,
                DeltaTime = deltaTime,
                InteractTurnSpeed = config.InteractTurnSpeed,
                Config = sightSystemConfig,
                BuildingAttrLookup = _buildingAttrLookup,
                ExpDataLookup = _expDataLookup,
                AbilityBonusLookup = _abilityBonusLookup,
                DarkShieldTauntedBuffLookup = _darkShieldTauntedBuffLookup

            }.Schedule(healEntities.Length, config.HealJobBatchCount, state.Dependency);
            state.Dependency = healJob;

            var harvestAbilities = _harvestEntityQuery.ToComponentDataArray<HarvestAbility>(Allocator.TempJob);
            var harvestEntities = _harvestEntityQuery.ToEntityArray(Allocator.TempJob);
            var harvestJob = new InteractStateJob<HarvestAbility>
            {
                CurTime = curTime,

                Ability = harvestAbilities,
                Entities = harvestEntities,
                StatDataLookup = _stat,
                TransformLookup = _localTransform,
                GeneralAttrLookup = _generalAttrLookup,
                BoxColliderSizeLookup = _boxColliderSizeLookup,
                InsightTarget = _insightTarget,
                RegeneratingTagLookup = _regeneratingTag,
                ECB = ecb,
                MovableLookup = _movable,
                BasicStateData = _basicState,
                HealLookup = _healState,
                HarvestLookup = _harvestState,
                AttackLookup = _attackTag,
                DeltaTime = deltaTime,
                InteractTurnSpeed = config.InteractTurnSpeed,
                Config = sightSystemConfig,
                BuildingAttrLookup = _buildingAttrLookup,
                ExpDataLookup = _expDataLookup,
                AbilityBonusLookup = _abilityBonusLookup,
                DarkShieldTauntedBuffLookup = _darkShieldTauntedBuffLookup
            }.Schedule(harvestEntities.Length, config.HarvestJobBatchCount, state.Dependency);
            state.Dependency = harvestJob;

            attackJob.Complete();
            healJob.Complete();
            harvestJob.Complete();

            attackEntities.Dispose();
            attackAbilities.Dispose();
            healEntities.Dispose();
            healAbilities.Dispose();
            harvestEntities.Dispose();
            harvestAbilities.Dispose();
        }


        [BurstCompile]
        private struct InteractStateJob<TInteractAbility> : IJobParallelFor
            where TInteractAbility : struct, IInteractAbility
        {
            [ReadOnly] public float CurTime;
            public NativeArray<TInteractAbility> Ability;
            public NativeArray<Entity> Entities;

            [ReadOnly] public ComponentLookup<StatData> StatDataLookup;
            [ReadOnly] public ComponentLookup<SubGameplayGeneralAttr> GeneralAttrLookup;
            [ReadOnly] public ComponentLookup<BoxColliderSize> BoxColliderSizeLookup;
            [ReadOnly] public ComponentLookup<HealStateTag> HealLookup;
            [ReadOnly] public ComponentLookup<HarvestStateTag> HarvestLookup;
            [ReadOnly] public ComponentLookup<AttackStateTag> AttackLookup;
            [ReadOnly] public ComponentLookup<RegeneratingTag> RegeneratingTagLookup;
            [ReadOnly] public BufferLookup<InsightTarget> InsightTarget;
            [ReadOnly] public ComponentLookup<BuildingAttr> BuildingAttrLookup;
            [ReadOnly] public ComponentLookup<ExpData> ExpDataLookup;
            [ReadOnly] public ComponentLookup<InteractAbilityBonus> AbilityBonusLookup;
            [ReadOnly] public ComponentLookup<DarkShieldTauntedBuff> DarkShieldTauntedBuffLookup;
            [ReadOnly] public float InteractTurnSpeed;

            // Turn rotation to target
            [NativeDisableParallelForRestriction] public ComponentLookup<LocalTransform> TransformLookup;

            // Change self state
            [NativeDisableParallelForRestriction] public ComponentLookup<MovableData> MovableLookup;

            // Change self state
            [NativeDisableParallelForRestriction] public ComponentLookup<BasicStateData> BasicStateData;

            [ReadOnly] public float DeltaTime;
            [ReadOnly] public SightSystemConfig Config;
            public EntityCommandBuffer.ParallelWriter ECB;

            public void Execute(int index)
            {
                var selfEntity = Entities[index];
                var ability = Ability[index];
                ref var selfStateData = ref BasicStateData.GetRefRW(selfEntity).ValueRW;
                var selfGeneralAttr = GeneralAttrLookup[selfEntity];
                var selfFactionTag = selfGeneralAttr.Faction;

                // Pre Check 
                // This should check in every state machine, because switch state tag only happens in next frame dur to ecb playback
                if (selfStateData.CurState != InteractState.Attacking
                    && selfStateData.CurState != InteractState.Healing
                    && selfStateData.CurState != InteractState.Harvesting) return;

                if (!InsightTarget.TryGetBuffer(selfEntity, out var targetList)) return; // This should never return

                // Check if target is valid

                /*Target is dead and removed. This may happen when it kills target after exactly this attack, but the entity is removed next frame end
                This may happen due to truly switch state always happen in te end of frame(ECB Playback) .
                Fake Interact State , next frame will turn to another state*/
                bool isTargetValid;
                if (!GeneralAttrLookup.TryGetComponent(selfStateData.TargetEntity, out var targetGeneralAttr)
                    || !StatDataLookup.TryGetComponent(selfStateData.TargetEntity, out var targetStat))
                {
                    selfStateData.TargetEntity = Entity.Null;
                    isTargetValid = false;
                }
                else
                {
                    isTargetValid = InteractUtils.IsTargetValid(in targetGeneralAttr, in selfFactionTag,
                        in targetStat, HealLookup.HasComponent(selfEntity), HarvestLookup.HasComponent(selfEntity),
                        AttackLookup.HasComponent(selfEntity),
                        !RegeneratingTagLookup.HasComponent(selfStateData.TargetEntity));
                }

                if (!isTargetValid) // Current target is invalid
                {
                    // Focus interact state but target is invalid, exit focus state
                    selfStateData.Focus = false;
                    // No enemy around , turn to idle
                    if (targetList.IsEmpty)
                    {
                        selfStateData.TargetState = InteractState.Idle;
                    }
                    // Target insight, choose the highest value target
                    else
                    {
                        selfStateData.TargetEntity = InteractUtils.ChooseTarget(in targetList);
                        StateUtils.SetTargetStateViaTargetType(in selfFactionTag,
                            GeneralAttrLookup[selfStateData.TargetEntity],
                            ref selfStateData);
                    }

                    StateUtils.SwitchState(ref selfStateData, ECB, selfEntity, index);
                    return;
                }

                // Player can decide whether unit can switch target during interact state
                if (ShouldChangeTarget(ref selfStateData, HealLookup.HasComponent(selfEntity),
                        in targetList, selfEntity))
                {
                    if (GeneralAttrLookup.TryGetComponent(selfStateData.TargetEntity, out var newTargetGeneralAttr))
                    {
                        StateUtils.SetTargetStateViaTargetType(in selfFactionTag,
                            newTargetGeneralAttr, ref selfStateData);
                        StateUtils.SwitchState(ref selfStateData, ECB, selfEntity, index);
                    }
                    return;
                }


                ref var transform = ref TransformLookup.GetRefRW(selfEntity).ValueRW;
                var curPos = transform.Position;
                var targetPos = TransformLookup[selfStateData.TargetEntity].Position;
                var targetBoxColliderSize = BoxColliderSizeLookup[selfStateData.TargetEntity];
                // Check if target in range
                /*As long as target is valid, movable unit will never change target in interact state.
                 The target can only be changed while moving*/
                if (!IsTargetInRange(math.square(ability.Range + AbilityBonusLookup[selfEntity].RangeBonus), in curPos, in targetPos, in targetBoxColliderSize))
                {
                    // Interacter is movable
                    if (MovableLookup.HasComponent(selfEntity))
                    {
                        ref var movableData = ref MovableLookup.GetRefRW(selfEntity).ValueRW;
                        InteractMoveToTarget(ref selfStateData, ref movableData, in ability, selfEntity, index);
                        return;
                    }

                    // Interacter is not movable, like building
                    if (targetList.IsEmpty)
                    {
                        // No enemy around, turn to idle
                        selfStateData.TargetState = InteractState.Idle;
                        StateUtils.SwitchState(ref selfStateData, ECB, selfEntity, index);
                    }
                    else
                    {
                        // Enemy in sight, choose the highest value target
                        selfStateData.TargetEntity = InteractUtils.ChooseTarget(in targetList);
                    }

                    return;
                }

                // Units Look at target, attack by animation event   
                if (selfGeneralAttr.BaseTag != BaseTag.Buildings)
                {
                    var targetRotation = quaternion.LookRotationSafe(-(targetPos - curPos), math.up());
                    transform.Rotation =
                        math.slerp(transform.Rotation.value, targetRotation, DeltaTime * InteractTurnSpeed);
                    return;
                }

                // Only building attack target here
                var counter = 1 / DeltaTime / ability.Speed;
                if (++selfStateData.InteractCounter > (int)counter)
                {
                    selfStateData.InteractCounter = 0;
                    // Calculate True Interact AbsAmount
                    BuildingAttack(selfStateData.TargetEntity, ability.Amount, index, selfEntity,
                        ability.InteractType, selfGeneralAttr, transform.Position);
                }
            }

            #region Interact Target Analyse Methods

            private bool ShouldChangeTarget(ref BasicStateData selfStateData, bool heal,
                in DynamicBuffer<InsightTarget> targets, Entity selfEntity)
            {
                var originalTarget = selfStateData.TargetEntity;

                if (DarkShieldTauntedBuffLookup.TryGetComponent(selfEntity, out var taunted) &&
                    DarkShieldTauntedBuffLookup.IsComponentEnabled(selfEntity))
                {
                    selfStateData.TargetEntity = taunted.TauntedBy;
                    return originalTarget != selfStateData.TargetEntity;
                }
                // Focus mode cannot switch target unless this is a healer and healer heal first
                if (selfStateData.Focus && (!heal || !Config.HealerAlwaysHealFirst)) return false;

                var selfStatData = StatDataLookup[selfEntity];
                // Heal self first
                if (heal && selfStatData.curValue < selfStatData.maxValue + selfStatData.bonus)
                {
                    selfStateData.TargetEntity = selfEntity;
                    return originalTarget != selfStateData.TargetEntity;
                }

                // If dynamic choose target
                if (Config.DynamicChooseTargetInInteract && !targets.IsEmpty)
                {
                    selfStateData.TargetEntity = InteractUtils.ChooseTarget(in targets);
                    return originalTarget != selfStateData.TargetEntity;
                }

                return false;
            }


            private void InteractMoveToTarget(ref BasicStateData stateData, ref MovableData movableData,
                in TInteractAbility ability, Entity entity, int index)
            {
                var tarPos = TransformLookup[stateData.TargetEntity].Position;
                var tarColliderShape = BoxColliderSizeLookup[stateData.TargetEntity].Box;
                MovementUtils.SetMoveTarget(ref movableData, tarPos, tarColliderShape,
                    MovementCommandType.Interactive,
                    ability.Range
                );
                stateData.TargetState = InteractState.Moving;
                StateUtils.SwitchState(ref stateData, ECB, entity, index);
                stateData.TargetState = ability.InteractType switch
                {
                    InteractType.Attack => InteractState.Attacking,
                    InteractType.Heal => InteractState.Healing,
                    InteractType.Harvest => InteractState.Harvesting,
                    _ => BurstSafe.UnexpectedEnum(ability.InteractType, InteractState.Idle)
                };
            }


            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            private static bool IsTargetInRange(float rangeSq, in float3 curPos, in float3 targetPos,
                in BoxColliderSize targetSubGameplayGeneralAttr)
            {
                var curPos2 = new float2(curPos.x, curPos.z);
                var targetPos2 = new float2(targetPos.x, targetPos.z);
                var targetColliderSizeXz = new float2(targetSubGameplayGeneralAttr.Box.x,
                    targetSubGameplayGeneralAttr.Box.z);
                var disSqPointToRect = MovementUtils.DistanceSqPointToRect(targetPos2, targetColliderSizeXz, curPos2);
                return disSqPointToRect < rangeSq;
            }


            private void BuildingAttack(Entity targetEntity, int amount, int index,
                Entity selfEntity, InteractType interactType, in SubGameplayGeneralAttr selfSubGameplayGeneralAttr,
                in float3 selfPos)
            {
                var targetPos = TransformLookup[targetEntity].Position;
                var tier = ExpDataLookup[selfEntity].curTier;
                var statChangeRequest = new StatChangeRequest
                {
                    Interactor = selfEntity,
                    Interactee = targetEntity,
                    AbsAmount = amount,
                    Type = interactType switch
                    {
                        InteractType.Attack => StatChangeType.Attack,
                        InteractType.Heal => StatChangeType.Heal,
                        InteractType.Harvest => StatChangeType.Harvest,
                        _ => StatChangeType.None // This should never happen,
                    },
                    InteractorSubGameplayGeneralAttr = selfSubGameplayGeneralAttr,
                    DamageType = DamageType.Magic
                };

                var buildingAttr = BuildingAttrLookup[selfEntity];
                var vfxName = VFXName.TowerProjectile;
                var damageDelayTime = math.distance(selfPos, targetPos) / 6f;// Tower Projectile speed
                
                
                // Spawn vfx request
                var vfxRequest = ECB.CreateEntity(index);
                ECB.AddComponent<SubGameplayEntityTag>(index, vfxRequest);
                
                // Crystal, beacon, tower tier 4 not has expData
                if (buildingAttr.Type == BuildingType.Ornaments)
                {
                    // Tower tier 4 is not support, so tier 4 tower is considered as special tower using magic circle attack
                    // Crystal and beacon use projectile tier 1
                    vfxName  = VFXName.TowerProjectile;
                }
                else
                {
                    if (buildingAttr.SubTypeIndex == (int)FortificationType.Tower)
                    {
                        vfxName = VFXName.TowerProjectile;
                    }
                    else if(buildingAttr.SubTypeIndex == (int) FortificationType.BigTower)
                    {
                        vfxName = VFXName.TowerCircleAttack;
                    }
                }

                if (vfxName == VFXName.TowerCircleAttack)
                    damageDelayTime = 0.2f;
                
                statChangeRequest.Interactee = Entity.Null;
                ECB.AddComponent(index, vfxRequest, new VFXRequest
                {
                    Filter = new VFXSubFilter
                    {
                        TierFilterEnable = true,
                        Tier =tier,
                        FactionFilterEnable = true,
                        Faction = selfSubGameplayGeneralAttr.Faction
                    },
                    StatChangeRequest = statChangeRequest,
                    VFXName = vfxName,
                    SpawnPosition = vfxName == VFXName.TowerCircleAttack ? targetPos : selfPos,
                    RequestType = VFXRequestType.Spawn,
                    KeepDuration = 0,
                    VFXTrackTarget = Entity.Null,
                    ParabolaTargetPosition = targetPos
                });

                
                // Spawn buff trigger to deal damage
                statChangeRequest.Interactee = targetEntity;

                var aoeBuffRequest = ECB.CreateEntity(index);
                ECB.AddComponent<SubGameplayEntityTag>(index, aoeBuffRequest);
                ECB.AddComponent(index, aoeBuffRequest, new AoeInteractData
                {
                    TriggerTime = CurTime + damageDelayTime, 
                    StatChangeRequest = statChangeRequest,
                    TargetFaction = ~selfSubGameplayGeneralAttr.Faction,
                });
                ECB.AddComponent(index, aoeBuffRequest, new BuffRequest
                {
                    SpawnPosition = targetPos,
                    SpawnRotation = quaternion.identity,
                    TrackTarget = Entity.Null,
                    // Only magic unit has aoe attack, others only has vfx
                    Name = vfxName == VFXName.TowerProjectile
                        ? BuffName.MagicTowerProjectile
                        : BuffName.MagicTowerCircle,
                    Filter = new BuffFilter
                    {
                    factionFilterEnabled = true,
                    faction = selfSubGameplayGeneralAttr.Faction,
                    tier = tier,
                    tierFilterEnabled = true
                }
                });
                
                // Spawn audio request
                var audioName = vfxName == VFXName.TowerProjectile
                    ? AudioName.TowerMagicBallStart
                    : AudioName.TowerMagicCircle;
                AudioUtils.PlayAudioClip(audioName, selfPos,ECB, index);
            }
            #endregion
        }
    }
}