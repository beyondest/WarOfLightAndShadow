using System;
using SparFlame.Components.General;
using SparFlame.Components.SubGameplay;
using SparFlame.Core.Utils;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine.AI;
using Random = Unity.Mathematics.Random;

namespace SparFlame.Database
{
    public class GeneralUnitAttributesAuthoring : GeneralDataItemAuthoring
    {
        private class Baker : GeneralDataItemBaker<GeneralUnitAttributesAuthoring>
        {
            public override void Bake(GeneralUnitAttributesAuthoring authoring)
            {
                if (authoring.globalIdx == 0) return;

                var item = DatabaseManager.UnitDatabaseSo.GetItemById(authoring.globalIdx);

                var entity = GetEntity(TransformUsageFlags.Dynamic);
                AddComponent<NeedSaveTag>(entity);
                SetComponentEnabled<NeedSaveTag>(entity, false);
                AddComponent<AssignRandomRequest>(entity);
                AddComponent(entity, new Rnd { value = new Random((uint)DateTime.Now.Ticks) });
                BakeGeneralDataItem(entity, item);
                AddComponent<GarrisonStateTag>(entity);
                SetComponentEnabled<GarrisonStateTag>(entity, false);
                AddComponent<CastSkillStateTag>(entity);
                SetComponentEnabled<CastSkillStateTag>(entity, false);
                AddComponent(entity, new UnitAttr
                {
                    type = item.type,
                    subTypeIndex = item.GetSubtypeIndex(),
                    conjureSpeedHoursPerUnit = item.conjureSpeedHoursPerUnit == 0 ? 1f : item.conjureSpeedHoursPerUnit,
                });
                switch (item.type)
                {
                    case UnitType.Shield:
                        AddComponent<ShieldTag>(entity);
                        break;
                    case UnitType.Archer:
                        AddComponent<ArcherTag>(entity);
                        break;
                    case UnitType.Cleric:
                        if (item.GetSubtypeIndex() == (int)MagicType.Cleric)
                            AddComponent<ClericTag>(entity);
                        // if(item.GetSubtypeIndex() == (int)MagicType.Mage)
                        //     AddComponent<MageTag>(entity);
                        break;
                    case UnitType.DualSpear:
                        AddComponent<DualSpearTag>(entity);
                        break;
                    case UnitType.Worker:
                        AddComponent<WorkerTag>(entity);
                        break;
                    case UnitType.SpellSword:
                        AddComponent<SpellSwordTag>(entity);
                        break;
                    case UnitType.GreatSword:
                        AddComponent<GreatSwordTag>(entity);
                        break;
                    case UnitType.Mage:
                        AddComponent<MageTag>(entity);
                        break;
                    default:
                        BurstSafe.UnexpectedEnum(item.type);
                        break;
                }

                var buffer = AddBuffer<CostList>(entity);
                foreach (var cost in item.costs)
                {
                    buffer.Add(new CostList
                    {
                        Amount = cost.amount,
                        Type = cost.type
                    });
                }

                BakeMovementAttr(item, entity, authoring);
                BakeSelectableAttr(entity);
                BakeBuff(item, entity);
                AddComponent(entity, new FormationTransform
                {
                    Transform = new LocalTransform
                    {
                        Position = float3.zero,
                        Rotation = quaternion.identity,
                        Scale = 1f
                    }
                });
                AddComponent(entity, new AutoGiveWayData
                {
                    State = AutoGiveWayState.None,
                    OriPosition = float3.zero,
                });
            }

            private void BakeMovementAttr(UnitDataItem item, Entity entity, GeneralUnitAttributesAuthoring authoring)
            {
                AddComponent(entity, new NavAgentComponent
                {
                    targetPosition = float3.zero,
                    calculateInterval = item.movementCalculationInterval,
                    extents = float3.zero,
                    enableCalculation = false,
                    calculationComplete = false,
                    currentWaypoint = 0,
                    forceCalculate = false,
                    agentId = authoring.GetComponent<NavMeshAgent>().agentTypeID
                });

                AddComponent(entity, new MovableData
                {
                    MoveSpeed = item.moveSpeed,
                    TargetCenterPos = float3.zero,
                    TargetColliderShape = float3.zero,
                    MovementCommandType = MovementCommandType.None,
                    InteractRange = 0f,
                    DetailInfo = DetailInfo.None,
                    MovementState = MovementState.NotMoving,
                    ForceCalculate = false,
                    // SelfColliderShapeXz = new float2(physicsShape.m_PrimitiveSize.x,physicsShape.m_PrimitiveSize.z),
                });
                AddComponent(entity, new Surroundings
                {
                    MoveSuccess = true,
                    FrontEntity = Entity.Null,
                    LeftEntity = Entity.Null,
                    RightEntity = Entity.Null,
                });
                AddComponent(entity, new Separation());
                AddComponent(entity, new GroundInfo());
                AddComponent(entity, new SeekTarget());
                AddComponent(entity, new Velocity());
                AddComponent(entity, new Avoidance());
                AddComponent(entity, new Alignment());
                AddBuffer<WaypointBuffer>(entity);
                AddComponent<MovingStateTag>(entity);
                SetComponentEnabled<MovingStateTag>(entity, false);
                AddComponent<FormationMovingTag>(entity);
                SetComponentEnabled<FormationMovingTag>(entity, false);
                AddComponent<AutoGiveWayTag>(entity);
                SetComponentEnabled<AutoGiveWayTag>(entity, false);
            }

            private void BakeSelectableAttr(Entity entity)
            {
                AddComponent<Selected>(entity);
                SetComponentEnabled<Selected>(entity, false);
                AddComponent<LockSelectedWorkForDrag>(entity);
                SetComponentEnabled<LockSelectedWorkForDrag>(entity, false);
            }

            // Buff can only be added to units that is not tier 1, debuff can be added to all
            private void BakeBuff(UnitDataItem item, Entity entity)
            {
                // Bake light circle
                if (item.HasLightGroupBuff() && item.curTier != Tier.Tier1)
                {
                    // AddComponent(entity, new AoeTriggerRequest
                    // {
                    //     Prefab = GetEntity(item.lightGroupAoeTrigger,TransformUsageFlags.Dynamic)
                    // });
                    // AddBuffer<AoeTarget>(entity);
                }
                AddComponent<SprintBuff>(entity);
                SetComponentEnabled<SprintBuff>(entity, false);
                if (item.type == UnitType.Archer)
                {
                    AddComponent<UnitGarrisonBuff>(entity);
                    SetComponentEnabled<UnitGarrisonBuff>(entity, false);
                }
                if (item.factionTag == FactionTag.Light)
                {
                    // Bake dark debuffs
                    // AddComponent<DarkMagicDamageBuff>(entity);
                    // SetComponentEnabled<DarkMagicDamageBuff>(entity, false);
                    if (item.IsAttackable() && item.attackAmount != 0)
                    {
                        AddComponent<DarkShieldTauntedBuff>(entity);
                        SetComponentEnabled<DarkShieldTauntedBuff>(entity, false);
                    }

                    // Bake light shield buff
                    if (item.type == UnitType.Shield && item.curTier != Tier.Tier1)
                    {
                        AddComponent<LightShieldBuff>(entity);
                    }

                    AddComponent<LightShieldUnderDefend>(entity);
                    SetComponentEnabled<LightShieldUnderDefend>(entity, false);

                    if (item.type != UnitType.DualSpear)
                    {
                        AddComponent<UnderBlessingBonus>(entity);
                        SetComponentEnabled<UnderBlessingBonus>(entity, false);
                    }

                    // Bake light cleric buff
                    // if (item.type == UnitType.Cleric && item.GetSubtypeIndex() == (int)MagicType.Cleric &&
                    //     item.curTier != Tier.Tier1)
                    // {
                    //     AddComponent<LightClericBuff>(entity);
                    // }

                    // Bake light archer buff
                    // if (item.type == UnitType.Archer && item.curTier != Tier.Tier1)
                    // {
                    //     AddComponent<LightArcherBuff>(entity);
                    // }

                    // Bake Unit Garrison Buff
              

                   
                }

                else if (item.factionTag == FactionTag.Dark)
                {
                    // Bake light debuffs
                    // AddComponent<LightMagicDamageBuff>(entity);
                    // SetComponentEnabled<LightMagicDamageBuff>(entity, false);
                    //
                    // Bake dark cavalry buff
                    // if (item.type == UnitType.DualSpear && item.curTier != Tier.Tier1)
                    // {
                    //     AddComponent<DarkCavalryBuff>(entity);
                    // }

                    // Bake dark cleric buff
                    // if (item.IsAttackable() && item.type != UnitType.DualSpear && item.type != UnitType.Shield)
                    // {
                    //     AddComponent<DarkClericBuff>(entity);
                    //     SetComponentEnabled<DarkClericBuff>(entity, false);
                    // }

                    // Bake dark archer buff
                    // if (item.type == UnitType.Archer && item.curTier != Tier.Tier1)
                    // {
                    //     AddComponent<DarkArcherBuff>(entity);
                    // }
                }

                // Bake neutral unit buffs
                // else if (item.factionTag == FactionTag.Neutral)
                // {
                //     AddComponent<LightMagicDamageBuff>(entity);
                //     SetComponentEnabled<LightMagicDamageBuff>(entity, false);
                //     AddComponent<DarkMagicDamageBuff>(entity);
                //     SetComponentEnabled<DarkMagicDamageBuff>(entity, false);
                //     if (item.IsAttackable() && item.attackAmount != 0)
                //     {
                //         AddComponent<DarkShieldTauntedBuff>(entity);
                //         SetComponentEnabled<DarkShieldTauntedBuff>(entity, false);
                //     }
                // }
            }
        }
    }
}