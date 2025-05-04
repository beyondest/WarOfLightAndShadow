using GamePlaySystem.Database;
using SparFlame.GamePlaySystem.Building;
using SparFlame.GamePlaySystem.CameraControl;
using SparFlame.GamePlaySystem.Exp;
using SparFlame.GamePlaySystem.Fow;
using SparFlame.GamePlaySystem.General;
using SparFlame.GamePlaySystem.Interact;
using SparFlame.GamePlaySystem.Movement;
using SparFlame.GamePlaySystem.State;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics.Authoring;
using Unity.Rendering;
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

                // Screen Pos, both building, resource, unit can hide in fog of war, so this is needed;
                // Unit selection needs the screen pos too.
                AddComponent(entity, new ScreenPos
                {
                    ScreenPosition = float2.zero
                });
                AddComponent<InCameraView>(entity);
                AddComponent<InCameraExtendView>(entity);
                SetComponentEnabled<InCameraView>(entity, false);
                SetComponentEnabled<InCameraExtendView>(entity,false);

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

                // Fog of War VFX
                var fowAgentData = new FowAgentData
                {
                    ContributeToFOV = item.factionTag == FactionTag.Ally,
                    DisappearInFow =  item.factionTag != FactionTag.Ally,
                    SightRange = item.fogSightRange,
                    SightCos = Mathf.Cos(item.fogSightAngle * 0.5f * Mathf.Deg2Rad),
                    DisappearAlphaThreshold = item.disappearAlphaThreshold,
                    IsInsight = item.factionTag == FactionTag.Ally, 
                };
                AddComponent(entity, fowAgentData);
                if (fowAgentData.ContributeToFOV)
                {
                    AddComponent<ContributeSightTag>(entity);
                    
                }

                if (fowAgentData.DisappearInFow)
                {
                    AddComponent<DisappearInFowTag>(entity);
                    AddComponent(entity, new HideFowAgentRequest
                    {
                        Hide = true
                    });
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
            }
           
        }
    }


    
   
}