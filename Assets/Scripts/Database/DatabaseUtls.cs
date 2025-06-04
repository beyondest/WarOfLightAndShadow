using GamePlaySystem.Database;
using SparFlame.GamePlaySystem.Building;
using SparFlame.GamePlaySystem.CameraControl;
using SparFlame.GamePlaySystem.CustomParticleSystem;
using SparFlame.GamePlaySystem.Fow;
using SparFlame.GamePlaySystem.General;
using SparFlame.GamePlaySystem.Interact;
using SparFlame.GamePlaySystem.Movement;
using SparFlame.GamePlaySystem.Ooc;
using SparFlame.GamePlaySystem.Resource;
using SparFlame.GamePlaySystem.State;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics.Authoring;
using UnityEngine;

namespace SparFlame.Database
{
    public class GeneralDataItemAuthoring : MonoBehaviour
    {
        [SerializeField] public int globalIdx;

        protected abstract class GeneralDataItemBaker<T> : Baker<T> where T : GeneralDataItemAuthoring
        {
            protected void BakeGeneralDataItem(Entity entity, GeneralDataItem item)
            {
                // General
                AddComponent(entity, new GeneralAttr
                {
                    BaseTag = item.baseTag,
                    FactionTag = item.factionTag,
                    ID = item.id,
                    BoxColliderSize = item.prefab.GetComponent<PhysicsShapeAuthoring>().m_PrimitiveSize
                });

                // Stat 
                AddComponent(entity, new StatData
                {
                    MaxValue = item.stat,
                    CurValue = item.stat
                });
                if (item.baseTag != BaseTag.Resources)
                {
                    AddComponent<OocTag>(entity);
                    SetComponentEnabled<OocTag>(entity, false);
                }

                // Screen Pos, both building, resource, unit can hide in fog of war, so this is needed;
                // Unit selection needs the screen pos too.
                AddComponent(entity, new ScreenPos
                {
                    ScreenPosition = float2.zero
                });
                AddComponent<InCameraView>(entity);
                AddComponent<InCameraExtendView>(entity);
                SetComponentEnabled<InCameraView>(entity, false);
                SetComponentEnabled<InCameraExtendView>(entity, false);

                // VFX Buffer and Buff buffer
                AddBuffer<TrackedByVFX>(entity);
                AddBuffer<TrackedByBuff>(entity);

                // Exp
                if (item.upgradable)
                {
                    AddComponent(entity, new ExpData
                    {
                        CurTier = item.curTier,
                        MaxTier = item.maxTier,
                        CurValue = 0,
                        MaxValue = item.statMaxValue,
                        NextTierPrefab = GetEntity(item.nextTierPrefab, TransformUsageFlags.Dynamic)
                    });
                }

                // Sight
                AddComponent(entity, new SightPriority
                {
                    Value = item.sightPriority
                });
                if (item.HasSight())
                {
                    AddBuffer<InsightTarget>(entity);
                    AddComponent(entity, new GenerateSightRequest
                    {
                        SightPrefab = GetEntity(item.sightPrefab, TransformUsageFlags.Dynamic),
                        // SightRange = item.sightRange,
                        // Filter = new CollisionFilter
                        // {
                        //     BelongsTo = item.sightBelongsTo.Value,
                        //     CollidesWith = item.sightCollidesWith.Value,
                        //     GroupIndex = 0
                        // }
                    });
                }

                // Interact Basic State
                if (item.HasSight())
                {
                    AddComponent(entity, new BasicStateData
                    {
                        CurState = InteractState.Idle,
                        Focus = false,
                        TargetEntity = Entity.Null,
                        TargetState = InteractState.Idle,
                        InteractCounter = 0
                    });
                    AddComponent<IdleStateTag>(entity);
                    SetComponentEnabled<IdleStateTag>(entity, true);
                }

                // Interact Ability 
                if (item.IsAttackable())
                {
                    AddComponent<AttackStateTag>(entity);
                    SetComponentEnabled<AttackStateTag>(entity, false);
                    AddComponent(entity, new AttackAbility
                    {
                        Amount = item.attackAmount,
                        Speed = item.attackSpeed,
                        RangeSq = item.attackRange * item.attackRange,
                        Targets = item.attackTargets,
                        InteractType = InteractType.Attack
                    });
                }

                if (item.IsHealable())
                {
                    AddComponent<HealStateTag>(entity);
                    SetComponentEnabled<HealStateTag>(entity, false);
                    AddComponent(entity, new HealAbility
                    {
                        Amount = item.healAmount,
                        Speed = item.healSpeed,
                        RangeSq = item.healRange * item.healRange,
                        Targets = item.healTargets,
                        InteractType = InteractType.Heal
                    });
                }

                if (item.IsHarvestable())
                {
                    AddComponent<HarvestStateTag>(entity);
                    SetComponentEnabled<HarvestStateTag>(entity, false);
                    AddComponent(entity, new HarvestAbility
                    {
                        Amount = item.harvestAmount,
                        Speed = item.harvestSpeed,
                        RangeSq = item.harvestRange * item.harvestRange,
                        Targets = item.harvestTargets,
                        InteractType = InteractType.Harvest
                    });
                }


                if (item.baseTag == BaseTag.Units || (item.baseTag == BaseTag.Buildings &&
                                                      item.GetGeneralTypeIndex() == (int)BuildingType.Ornaments
                                                      &&( item.GetSubtypeIndex() == (int)OrnamentType.Crystal ||
                                                      item.GetSubtypeIndex() == (int)OrnamentType.Beacon)))
                {
                    var fogOfWarSightRange = item.fogSightRange;
                    // Fog of War VFX
                    var fowAgentData = new FowAgentData
                    {
                        SightRange = fogOfWarSightRange,
                        SightCos = Mathf.Cos(item.fogSightAngle * 0.5f * Mathf.Deg2Rad),
                        DisappearAlphaThreshold = item.disappearAlphaThreshold,
                        IsInsight = true
                    };
                    AddComponent(entity, fowAgentData);
                    AddComponent<DisappearInFowTag>(entity);

                    AddComponent<InDarknessTag>(entity);
                    SetComponentEnabled<InDarknessTag>(entity, false);
                }
            }

            protected void BakeVolumeObstacleAttr(GeneralDataItem item, Entity entity)
            {
                // if (item.IsAttackable())
                // {
                //     volumeRadius = item.attackRange;
                //     areaType = (AreaType)((int)item.curTier + 10);
                // }
                // else
                // {
                const float volumeRadius = 0f;
                var areaType = (AreaType)item.curTier;
                // }
                var physicsShapeAuthoring = item.prefab.GetComponent<PhysicsShapeAuthoring>();
                AddComponent<VolumeObstacleTag>(entity);
                AddComponent(entity, new VolumeObstacleSpawnRequest
                {
                    Center = physicsShapeAuthoring.m_PrimitiveCenter,
                    Size = physicsShapeAuthoring.m_PrimitiveSize,
                    VolumeRadius = volumeRadius,
                    VolumeAreaType = areaType,
                    RequestFromFaction = item.factionTag,
                });
                SetComponentEnabled<VolumeObstacleSpawnRequest>(entity, true);
            }
        }
    }
}