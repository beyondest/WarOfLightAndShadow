using SparFlame.Components.General;
using SparFlame.Components.SubGameplay;
using SparFlame.Components.VFX;
using SparFlame.Core.Utils;
using SparFlame.Systems.SubGameplay.Interact;
using SparFlame.Systems.SubGameplay.StateMachine;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace SparFlame.Systems.SubGameplay
{
    [BurstCompile]
    [WithAll(typeof(CastSkillStateTag))]
    [WithNone(typeof(UnitDeadTag))]
    [WithNone(typeof(ArcherTag))] // Archer skill is different from others
    public partial struct CastSkillJob : IJobEntity
    {
        public EntityCommandBuffer.ParallelWriter ECB;
        [ReadOnly] public float ElapsedTime;
        [ReadOnly] public AnimationEventTriggerModelIndex AnimationEventModelIndexConfig;
        [ReadOnly] public CastSkillConfig CastSkillConfig;
        [ReadOnly] public DamageReduceShieldBuffConfig DamageReduceShieldBuffConfig;
        [ReadOnly] public BlessingBuffGeneralConfig BlessingBuffGeneralConfig;
        [ReadOnly] public DynamicBuffer<BlessingBuffAoeTriggerPrefabs> BlessingBuffAoeTriggerPrefabs;
        [ReadOnly] public LightShieldBuffGeneralConfig LightShieldBuffGeneralConfig;
        [ReadOnly] public DarkShieldBuffGeneralConfig DarkShieldBuffGeneralConfig;
        [ReadOnly] public DarkCavalryBuffGeneralConfig DarkCavalryBuffGeneralConfig;
        [ReadOnly] public ComponentLookup<HealAbility> HealAbilityLookup;
        [ReadOnly] public ComponentLookup<AttackAbility> AttackAbilityLookup;
        // This component is only written to self
        [NativeDisableParallelForRestriction] public BufferLookup<AnimationEventData> EventsLookup;

        private void Execute([ChunkIndexInQuery] int index, ref BasicStateData stateData, Entity selfEntity,
            in DynamicBuffer<LinkedEntityGroup> children, in UnitAttr unitAttr, in ExpData expData,
            in LocalTransform transform,
            in SubGameplayGeneralAttr subGameplayGeneralAttr)
        {
            if (stateData.CurState != InteractState.CastSkill) return;
            var modelIndex = AnimationEventModelIndexConfig.value;
            var model = children[modelIndex].Value;
            var events = EventsLookup[model];
            if (events.Length == 0) return;
            var eventData = events[0];
            if (eventData.NameHash == CastSkillConfig.EndEventNameHash)
            {
                stateData.TargetState = InteractState.Idle;
                StateUtils.SwitchState(ref stateData, ECB, selfEntity, index);
                stateData.TargetState = InteractState.Idle;
            }
            else
            {
                CastUnitSkillByUnitType(index, selfEntity, subGameplayGeneralAttr, expData, transform, unitAttr,
                    eventData);
            }

            events.Clear();
        }

        private void CastUnitSkillByUnitType(int index,
            Entity selfEntity, in SubGameplayGeneralAttr subGameplayGeneralAttr,
            in ExpData expData, in LocalTransform transform,
            in UnitAttr unitAttr, in AnimationEventData eventData)
        {
            switch (unitAttr.type)
            {
                case UnitType.Shield:
                    if (subGameplayGeneralAttr.Faction == FactionTag.Light)
                    {
                        BuffUtils.AddLightShieldBuff(ECB, index, selfEntity,
                            BlessingBuffAoeTriggerPrefabs[(int)expData.curTier - 3].Prefab,
                            LightShieldBuffGeneralConfig.SelfDuration, ElapsedTime
                        );
                    }
                    else if (subGameplayGeneralAttr.Faction == FactionTag.Dark)
                    {
                        BuffUtils.AddDarkShieldBuff(ECB, index, selfEntity, DarkShieldBuffGeneralConfig.SelfDuration,
                            ElapsedTime
                        );
                    }

                    break;

                case UnitType.Cleric:
                    CastClericCircleSkill(index, selfEntity, subGameplayGeneralAttr, expData, transform);
                    break;
                case UnitType.DualSpear:
                    if (subGameplayGeneralAttr.Faction == FactionTag.Light)
                    {
                        BuffUtils.AddBlessingBuff(ECB, index, selfEntity,
                            BlessingBuffAoeTriggerPrefabs[(int)expData.curTier - 3].Prefab,
                            BlessingBuffGeneralConfig.BlessingBuffDuration, ElapsedTime, expData.curTier);
                    }
                    else if (subGameplayGeneralAttr.Faction == FactionTag.Dark)
                    {
                        BuffUtils.AddDarkCavalryBuff(ECB, index, selfEntity, DarkCavalryBuffGeneralConfig.selfDuration,
                            ElapsedTime,
                            expData.curTier);
                    }

                    break;
                case UnitType.SpellSword:
                    CastSpellSwordSkill(index, selfEntity, subGameplayGeneralAttr, expData, transform);
                    break;
                case UnitType.GreatSword:
                    BuffUtils.AddDamageReduceBuff(ECB, index, selfEntity, subGameplayGeneralAttr.Faction,
                        DamageReduceShieldBuffConfig.keepDuration,
                        ElapsedTime
                    );
                    break;
                case UnitType.Mage:
                    break;
                // Archer will not stop by event, this should never happen
                case UnitType.Archer:
                case UnitType.Worker:
                default:
                    BurstSafe.UnexpectedEnum(unitAttr.type);
                    break;
            }
        }

        private void CastClericCircleSkill(int index, Entity selfEntity,
            in SubGameplayGeneralAttr subGameplayGeneralAttr,
            in ExpData expData, in LocalTransform transform)
        {
            var statChangeRequest = new StatChangeRequest
            {
                Interactor = selfEntity,
                Interactee = Entity.Null,
                AbsAmount = (int)(HealAbilityLookup[selfEntity].Amount * CastSkillConfig.ClericSkillAmountScale),
                DamageType = DamageType.None,
                InteractorSubGameplayGeneralAttr = subGameplayGeneralAttr,
                Type = StatChangeType.Heal
            };

            var vfxRequest = ECB.CreateEntity(index);
            ECB.AddComponent<SubGameplayEntityTag>(index, vfxRequest);
            var vfx = new VFXRequest
            {
                // If attacking, then vfx starts from attacker, otherwise starts from target position
                SpawnPosition = transform.Position,
                Filter = new VFXSubFilter
                {
                    FactionFilterEnable = true,
                    Faction = subGameplayGeneralAttr.Faction,
                    Tier = expData.curTier,
                    TierFilterEnable = true,
                },
                KeepDuration = CastSkillConfig.ClericSkillLastDuration,
                StatChangeRequest = statChangeRequest,
                RequestType = VFXRequestType.Spawn,
                VFXName = VFXName.ClericHealField,
                VFXTrackTarget = Entity.Null,
                ParabolaTargetPosition = transform.Position
            };
            ECB.AddComponent(index, vfxRequest, vfx);


            var aoeBuffRequest = ECB.CreateEntity(index);
            ECB.AddComponent<SubGameplayEntityTag>(index, aoeBuffRequest);
            ECB.AddComponent(index, aoeBuffRequest, new AoeInteractData
            {
                TriggerTime = ElapsedTime + CastSkillConfig.ClericAoeTriggerDelay,
                StatChangeRequest = statChangeRequest,
                TargetFaction = subGameplayGeneralAttr.Faction,
            });
            ECB.AddComponent(index, aoeBuffRequest, new BuffRequest
            {
                SpawnPosition = transform.Position,
                SpawnRotation = transform.Rotation,
                TrackTarget = Entity.Null,
                Name = BuffName.ClericSkillCircle,
                Filter = new BuffFilter
                {
                    factionFilterEnabled = true,
                    faction = subGameplayGeneralAttr.Faction,
                    tier = expData.curTier,
                    tierFilterEnabled = true
                }
            });
        }

        private void CastSpellSwordSkill(int index, Entity selfEntity, in SubGameplayGeneralAttr subGameplayGeneralAttr,
            in ExpData expData, in LocalTransform transform)
        {
            var statChangeRequest = new StatChangeRequest
            {
                Interactor = selfEntity,
                Interactee = Entity.Null,
                AbsAmount = (int)(AttackAbilityLookup[selfEntity].Amount * CastSkillConfig.SpellSwordSkillAmountScale),
                DamageType = DamageType.Magic,
                InteractorSubGameplayGeneralAttr = subGameplayGeneralAttr,
                Type = StatChangeType.Attack
            };

            var vfxRequest = ECB.CreateEntity(index);
            ECB.AddComponent<SubGameplayEntityTag>(index, vfxRequest);
            var vfx = new VFXRequest
            {
                SpawnPosition = transform.Position,
                Filter = new VFXSubFilter
                {
                    FactionFilterEnable = true,
                    Faction = subGameplayGeneralAttr.Faction,
                    Tier = expData.curTier,
                    TierFilterEnable = true,
                },
                KeepDuration = CastSkillConfig.SpellSwordSkillLastDuration,
                StatChangeRequest = statChangeRequest,
                RequestType = VFXRequestType.Spawn,
                VFXName = VFXName.SpellSwordSkill,
                VFXTrackTarget = Entity.Null,
                ParabolaTargetPosition = transform.Position
            };
            ECB.AddComponent(index, vfxRequest, vfx);


            var aoeBuffRequest = ECB.CreateEntity(index);
            ECB.AddComponent<SubGameplayEntityTag>(index, aoeBuffRequest);
            ECB.AddComponent(index, aoeBuffRequest, new AoeInteractData
            {
                TriggerTime = ElapsedTime + CastSkillConfig.SpellSwordSkillTriggerDelay,
                StatChangeRequest = statChangeRequest,
                TargetFaction = ~subGameplayGeneralAttr.Faction,
            });
            ECB.AddComponent(index, aoeBuffRequest, new BuffRequest
            {
                SpawnPosition = transform.Position,
                SpawnRotation = transform.Rotation,
                TrackTarget = Entity.Null,
                Name = BuffName.SpellSwordSkill,
                Filter = new BuffFilter
                {
                    factionFilterEnabled = true,
                    faction = subGameplayGeneralAttr.Faction,
                    tier = expData.curTier,
                    tierFilterEnabled = true
                }
            });
        }
    }
    
    
    [BurstCompile]
    [WithAll(typeof(ArcherTag))]
    [WithNone(typeof(UnitDeadTag))]
    public partial struct CastArcherSkillJob : IJobEntity
    {
        public EntityCommandBuffer.ParallelWriter ECB;
        [ReadOnly] public PlayerFactionData PlayerFactionData;
        [ReadOnly] public ArcherSkillConfig Config;
        [ReadOnly] public PlayerArcherSkill PlayerArcherSkill;
        [ReadOnly] public EnemyArcherSkill EnemyArcherSkill;
        [ReadOnly] public float DeltaTime;

        private void Execute([ChunkIndexInQuery] int index, ref BasicStateData stateData, ref LocalTransform transform,
            ref Rnd rnd, in SubGameplayGeneralAttr subGameplayGeneralAttr, in GroundInfo groundInfo, in ExpData expData,
            Entity selfEntity)
        {
            if (stateData.CurState != InteractState.CastSkill) return;
            var relationShip =
                FactionUtils.GetRelationshipSimple(subGameplayGeneralAttr.Faction, PlayerFactionData.faction);
            float3 targetPos;
            bool fire;
            if (relationShip is Relationship.Ally or Relationship.Self)
            {
                targetPos = PlayerArcherSkill.TargetPosition;
                fire = PlayerArcherSkill.Fire;
            }
            else
            {
                targetPos = EnemyArcherSkill.TargetPosition;
                fire = EnemyArcherSkill.Fire;
            }

            var direction = math.normalizesafe(targetPos.xz - transform.Position.xz);
            var targetRotation = quaternion.LookRotation(new float3(direction.x, 0, direction.y), groundInfo.Normal);
            transform.Rotation = math.slerp(
                transform.Rotation.value,
                targetRotation,
                DeltaTime * Config.RotationSpeedWhileAiming);
            if (fire)
            {
                stateData.TargetState = InteractState.Idle;
                StateUtils.SwitchState(ref stateData, ECB, selfEntity, index);
                stateData.TargetState = InteractState.Idle;
                
                var vfxRequest = ECB.CreateEntity(index);
                ECB.AddComponent<SubGameplayEntityTag>(index, vfxRequest);
                var statChangeRequest = new StatChangeRequest
                {
                    Interactor = selfEntity,
                    Interactee = Entity.Null,
                    AbsAmount = 0,
                    DamageType = DamageType.Physical,
                    InteractorSubGameplayGeneralAttr = subGameplayGeneralAttr,
                    Type = StatChangeType.Attack
                };
                var vfx = new VFXRequest
                {
                    // Vfx will start from attacker position, and reach target position by parabola system
                    SpawnPosition = transform.Position,
                    Filter = new VFXSubFilter
                    {
                        FactionFilterEnable = true,
                        Faction = subGameplayGeneralAttr.Faction,
                        Tier = expData.curTier,
                        TierFilterEnable = true,
                    },
                    KeepDuration = float.MaxValue,
                    StatChangeRequest = statChangeRequest,
                    RequestType = VFXRequestType.Spawn,
                    VFXName = VFXName.RangedUnitProjectile,
                    VFXTrackTarget = Entity.Null,
                    ParabolaTargetPosition =
                        MathUtils.GetRandomPointInRange(targetPos, 0, Config.ArrowRainRadius, ref rnd.value)
                };
                ECB.AddComponent(index, vfxRequest, vfx);
                
            }
        }
    }
    
}