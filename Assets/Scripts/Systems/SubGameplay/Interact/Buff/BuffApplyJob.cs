using SparFlame.Components.General;
using SparFlame.Components.SubGameplay;
using SparFlame.Components.VFX;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

namespace SparFlame.Systems.SubGameplay.Interact
{
    // This job cannot parallel because bonus has to be applied in order
    [BurstCompile]
    public partial struct BuffApplyJob : IJobEntity
    {
        public EntityCommandBuffer.ParallelWriter ECB;

        [ReadOnly] public ComponentLookup<SubGameplayGeneralAttr> GeneralAttrLookup;
        [ReadOnly] public ComponentLookup<ExpData> ExpDataLookup;
        [ReadOnly] public ComponentLookup<StatData> StatDataLookup;
        [NativeDisableParallelForRestriction] public ComponentLookup<UnitAttr> UnitAttrLookup;

        [ReadOnly] public DynamicBuffer<DarkShieldBuffConfig> DarkShieldBuffConfigs;
        [ReadOnly] public DynamicBuffer<LightShieldBuffConfig> LightShieldBuffConfigs;
        [ReadOnly] public DynamicBuffer<LightArcherBuffConfig> LightArcherBuffConfigs;
        [ReadOnly] public DynamicBuffer<LightClericBuffConfig> LightClericBuffConfigs;
        [ReadOnly] public DynamicBuffer<DarkClericBuffConfig> DarkClericBuffConfigs;
        [ReadOnly] public DynamicBuffer<LightMagicDamageBuffConfig> LightMagicDamageBuffConfigs;
        [ReadOnly] public DynamicBuffer<DarkMagicDamageBuffConfig> DarkMagicDamageBuffConfigs;
        [ReadOnly] public CavalryMoveBuffConfig CavalryMoveBuffConfig;
        [ReadOnly] public GarrisonBuffConfig GarrisonBuffConfig;

        [ReadOnly] public ComponentLookup<LightShieldUnderDefend> LightShieldUnderDefendLookup;
        [ReadOnly] public ComponentLookup<LightShieldBuff> LightShieldBuffLookup;
        [ReadOnly] public ComponentLookup<DarkShieldTauntBuff> DarkShieldTauntBuffLookup;
        [ReadOnly] public ComponentLookup<DarkShieldTauntedBuff> DarkShieldTauntedBuffLookup;

        [ReadOnly] public ComponentLookup<LightArcherBuff> LightArcherBuffLookup;

        [ReadOnly] public ComponentLookup<DarkClericBuff> DarkClericBuffLookup;
        [ReadOnly] public ComponentLookup<LightClericBuff> LightClericBuffLookup;
        
        [ReadOnly] public ComponentLookup<LightMagicDamageBuff> LightMagicDamageBuffLookup;
        [ReadOnly] public ComponentLookup<DarkMagicDamageBuff> DarkMagicDamageBuffLookup;

        [ReadOnly] public ComponentLookup<CavalryMoveBuff> CavalryMoveBuffLookup;
        
        [ReadOnly] public ComponentLookup<BuildingGarrisonBuff> BuildingGarrisonBuffLookup;
        [ReadOnly] public ComponentLookup<UnitGarrisonBuff> UnitGarrisonBuffLookup;

        private void Execute([ChunkIndexInQuery] int index, ref StatChangeRequest request)
        {
            // Only apply buff when interactee and interactor are both valid

            if (!GeneralAttrLookup.TryGetComponent(request.Interactee, out var interacteeAttr)) return;
            if (!GeneralAttrLookup.HasComponent(request.Interactor)) return;

            if (request is { Type: StatChangeType.Attack, DamageType: DamageType.Magic or DamageType.Physical })
            {
                ApplyPhysicalMagicAttackRelativeBuff(ref request, index, interacteeAttr);
            }

            if (request.Type == StatChangeType.Attack)
            {
                ApplyGeneralAttackRelativeBuff(ref request, index);
            }

            if (request.Type == StatChangeType.Heal)
            {
                ApplyHealRelativeBuff(ref request, index, interacteeAttr);
            }
        }

        private void ApplyGeneralAttackRelativeBuff(ref StatChangeRequest request, int index)
        {
            // Apply cavalry move reduce damage buff
            if (CavalryMoveBuffLookup.HasComponent(request.Interactee) &&
                CavalryMoveBuffLookup.IsComponentEnabled(request.Interactee))
            {
                request.AbsAmount = (int)(request.AbsAmount *
                                          (1 - CavalryMoveBuffConfig.damageReduceScale)); 
            }

            // Apply building garrison reduce damage buff
            if (BuildingGarrisonBuffLookup.HasComponent(request.Interactee) &&
                BuildingGarrisonBuffLookup.IsComponentEnabled(request.Interactee))
            {
                request.AbsAmount = (int)(request.AbsAmount *
                                          (1 - GarrisonBuffConfig.buildingDamageReduceScale));
            }

            // Apply unit garrison reduce damage buff
            if (UnitGarrisonBuffLookup.HasComponent(request.Interactee)
                && UnitGarrisonBuffLookup.IsComponentEnabled(request.Interactee))
            {
                request.AbsAmount = (int)(request.AbsAmount *
                                          (1 - GarrisonBuffConfig.unitDamageReduceScale));
            }
        }
        
        private void ApplyHealRelativeBuff(ref StatChangeRequest request, int index,
            in SubGameplayGeneralAttr interacteeAttr)
        {
            if (DarkMagicDamageBuffLookup.TryGetComponent(request.Interactee, out var buff) &&
                DarkMagicDamageBuffLookup.IsComponentEnabled(request.Interactee))
            {
                request.AbsAmount = (int)(request.AbsAmount *
                                          (1 - buff.HealingReductionPercent)); // Apply healing reduction percent
            }

            if (DarkClericBuffLookup.TryGetComponent(request.Interactee, out var darkClericBuff) &&
                request.InteractorSubGameplayGeneralAttr.FactionTag == FactionTag.Enemy)
            {
                var expData = ExpDataLookup[request.Interactor];
                var config = DarkClericBuffConfigs[(int)expData.curTier - 3];
                ECB.SetComponentEnabled<DarkClericBuff>(index, request.Interactee, true);
                ECB.SetComponent(index, request.Interactee, new DarkClericBuff
                {
                    AttackAmountBonusScale = math.max(config.attackAmountBonusScale, darkClericBuff.AttackAmountBonusScale),
                    LastTime = math.max(config.lastTime, darkClericBuff.LastTime),
                });
                if (!DarkClericBuffLookup.IsComponentEnabled(request.Interactee))
                {
                    var vfxRequest = ECB.CreateEntity(index);
                    ECB.AddComponent<SubGameplayEntityTag>(index, vfxRequest);
                    ECB.AddComponent(index, vfxRequest, new VFXRequest
                    {
                        RequestType = VFXRequestType.Spawn,
                        VFXName = VFXName.DarkClericAttackGain,
                        VFXTrackTarget = request.Interactee,
                        KeepDuration = float.MaxValue,
                    });
                }
            }

            if (LightClericBuffLookup.HasComponent(request.Interactor))
            {
                var expData = ExpDataLookup[request.Interactor];
                var config = LightClericBuffConfigs[(int)expData.curTier - 3];
                request.AbsAmount = (int)(request.AbsAmount *
                                          (1 + config.healingBonusScale)); // Apply to heal amount bonus scale
            }
        }

        private void ApplyPhysicalMagicAttackRelativeBuff(ref StatChangeRequest request, int index,
            in SubGameplayGeneralAttr interacteeAttr)
        {

            // Apply light shield damage reduction
            if (LightShieldUnderDefendLookup.TryGetComponent(request.Interactee, out var underDefend)
                && LightShieldUnderDefendLookup.IsComponentEnabled(request.Interactee))
            {
                if (LightShieldBuffLookup.HasComponent(underDefend.DefendBy))
                {
                    var expData = ExpDataLookup[underDefend.DefendBy];
                    var selfGetMagicDamageScale =
                        LightShieldBuffConfigs[(int)expData.curTier - 3].selfGetMagicDamageScale;
                    var selfGetPhysicalDamageScale =
                        LightShieldBuffConfigs[(int)expData.curTier - 3].selfGetPhysicalDamageScale;
                    var shieldGetMagicDamageScale =
                        LightShieldBuffConfigs[(int)expData.curTier - 3].shieldGetMagicDamageScale;
                    var shieldGetPhysicalDamageScale =
                        LightShieldBuffConfigs[(int)expData.curTier - 3].shieldGetPhysicalDamageScale;

                    var selfScale = request.DamageType == DamageType.Magic
                        ? selfGetMagicDamageScale
                        : selfGetPhysicalDamageScale;
                    var shieldScale = request.DamageType == DamageType.Magic
                        ? shieldGetMagicDamageScale
                        : shieldGetPhysicalDamageScale;

                    var lightShieldPassDamage = ECB.CreateEntity(index);
                    ECB.AddComponent(index, lightShieldPassDamage, new StatChangeRequest
                    {
                        AbsAmount = (int)(request.AbsAmount * shieldScale),
                        Type = StatChangeType.Attack,
                        Interactee = underDefend.DefendBy,
                        Interactor = request.Interactor,
                        InteractorSubGameplayGeneralAttr = request.InteractorSubGameplayGeneralAttr,
                        DamageType = DamageType.BuffDamage
                    });
                    ECB.AddComponent<SubGameplayEntityTag>(index, lightShieldPassDamage);
                    request.AbsAmount = (int)(request.AbsAmount * selfScale);
                }
            }

            // Apply dark shield reflect damage
            if (DarkShieldTauntedBuffLookup
                    .HasComponent(request
                        .Interactor) && // Only attackers that have taunted buff will get reflected damage
                DarkShieldTauntedBuffLookup.IsComponentEnabled(request.Interactor) &&
                DarkShieldTauntBuffLookup.HasComponent(request.Interactee) &&
                request is { Type: StatChangeType.Attack, AbsAmount: > 0 } &&
                GeneralAttrLookup.TryGetComponent(request.Interactee, out var shieldUnit)) // Only apply to units
            {
                var expData = ExpDataLookup[request.Interactee];

                var reflectDamageScale = request.DamageType == DamageType.Magic
                    ? DarkShieldBuffConfigs[(int)expData.curTier - 3].reflectMagicDamageScale
                    : DarkShieldBuffConfigs[(int)expData.curTier - 3].reflectPhysicalDamageScale;
                var reflectDamage = ECB.CreateEntity(index);
                ECB.AddComponent<SubGameplayEntityTag>(index, reflectDamage);
                ECB.AddComponent(index, reflectDamage, new StatChangeRequest
                {
                    Type = StatChangeType.Attack,
                    AbsAmount = (int)(request.AbsAmount * reflectDamageScale),
                    Interactee = request.Interactor,
                    Interactor = request.Interactee,
                    InteractorSubGameplayGeneralAttr = shieldUnit,
                    DamageType = DamageType.BuffDamage,
                });
            }

            // Apply light archer buff
            if (LightArcherBuffLookup.HasComponent(request.Interactor) && interacteeAttr.FactionTag == FactionTag.Enemy)
            {
                var unitAttr = UnitAttrLookup.GetRefRW(request.Interactor);
                var expData = ExpDataLookup[request.Interactor];
                var buffConfig = LightArcherBuffConfigs[(int)expData.curTier - 3];
                if (unitAttr.ValueRW.Rnd.NextFloat(0f, 1f) < buffConfig.bonusTriggerChance)
                {
                    var stat = StatDataLookup[request.Interactee];
                    var damageBonus = interacteeAttr.BaseTag == BaseTag.Buildings
                        ? buffConfig.bonusMaxHpScaleBuilding * stat.maxValue
                        : buffConfig.bonusMaxHpScaleUnit * stat.maxValue;
                    var extraDamage = ECB.CreateEntity(index);
                    ECB.AddComponent<SubGameplayEntityTag>(index, extraDamage);
                    ECB.AddComponent(index, extraDamage, new StatChangeRequest
                    {
                        Type = StatChangeType.Attack,
                        AbsAmount = (int)damageBonus,
                        Interactee = request.Interactee,
                        Interactor = request.Interactor,
                        InteractorSubGameplayGeneralAttr = request.InteractorSubGameplayGeneralAttr,
                        DamageType = DamageType.BuffDamage
                    });
                }

               
            }

            // Apply Light magic damage buff
            if (request.DamageType == DamageType.Magic && LightMagicDamageBuffLookup.TryGetComponent(request.Interactee,out var lightMagicDamageBuff)
                                                       && request.InteractorSubGameplayGeneralAttr.FactionTag == FactionTag.Ally)
            {
                // When attacker is tier4 magic tower, should be considered as tier3
                if (!ExpDataLookup.TryGetComponent(request.Interactor, out var expData))
                    expData.curTier = Tier.Tier3;
                var config = LightMagicDamageBuffConfigs[(int)expData.curTier - 3];
                ECB.SetComponentEnabled<LightMagicDamageBuff>(index, request.Interactee, true);
                ECB.SetComponent(index, request.Interactee, new LightMagicDamageBuff
                {
                    LastTime = math.max(config.lastTime, lightMagicDamageBuff.LastTime),
                    MoveSpeedNegativeBonusScale = math.min(config.moveSpeedNegativeBonus, lightMagicDamageBuff.MoveSpeedNegativeBonusScale),
                    SpeedNegativeBonus = math.min(config.speedNegativeBonus, lightMagicDamageBuff.SpeedNegativeBonus),
                });
                if (!LightMagicDamageBuffLookup.IsComponentEnabled(request.Interactee))
                {
                    var vfxRequest = ECB.CreateEntity(index);
                    ECB.AddComponent<SubGameplayEntityTag>(index, vfxRequest);
                    ECB.AddComponent(index, vfxRequest, new VFXRequest
                    {
                        RequestType = VFXRequestType.Spawn,
                        VFXName = VFXName.LightMagicDamageDebuff,
                        VFXTrackTarget = request.Interactee,
                        KeepDuration = float.MaxValue,
                    });
                }
            }

            // Apply dark magic damage buff
            if (request.DamageType == DamageType.Magic && DarkMagicDamageBuffLookup.TryGetComponent(request.Interactee, out var darkMagicDamageBuff)
                                                       && request.InteractorSubGameplayGeneralAttr.FactionTag ==
                                                       FactionTag.Enemy)
            {
                // When attacker is tier4 magic tower, should be considered as tier3
                if (!ExpDataLookup.TryGetComponent(request.Interactor, out var expData))
                    expData.curTier = Tier.Tier3;
                var config = DarkMagicDamageBuffConfigs[(int)expData.curTier - 3];
                ECB.SetComponentEnabled<DarkMagicDamageBuff>(index, request.Interactee, true);
                ECB.SetComponent(index, request.Interactee, new DarkMagicDamageBuff
                {
                    HealingReductionPercent =math.max( config.healingReductionPercent,darkMagicDamageBuff.HealingReductionPercent),
                    LastTime = math.max(config.lastTime, darkMagicDamageBuff.LastTime),
                });
                if (!DarkMagicDamageBuffLookup.IsComponentEnabled(request.Interactee))
                {
                    var vfxRequest = ECB.CreateEntity(index);
                    ECB.AddComponent<SubGameplayEntityTag>(index, vfxRequest);
                    ECB.AddComponent(index, vfxRequest, new VFXRequest
                    {
                        RequestType = VFXRequestType.Spawn,
                        VFXName = VFXName.DarkMagicDamageDebuff,
                        VFXTrackTarget = request.Interactee,
                        KeepDuration = float.MaxValue,
                    });
                }
               
            }
            
           
        }
    }
}