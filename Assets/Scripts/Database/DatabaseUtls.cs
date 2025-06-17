using GamePlaySystem.Database;
using SparFlame.GamePlaySystem.Building;
using SparFlame.GamePlaySystem.CameraControl;
using SparFlame.GamePlaySystem.CustomParticleSystem;
using SparFlame.GamePlaySystem.General;
using SparFlame.GamePlaySystem.Interact;
using SparFlame.GamePlaySystem.Movement;
using SparFlame.GamePlaySystem.Ooc;
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
                AddComponent(entity, new SubGameplayGeneralAttr
                {
                    BaseTag = item.baseTag,
                    FactionTag = item.factionTag,
                    ID = item.id,
                    BoxColliderSize = item.prefab.GetComponent<PhysicsShapeAuthoring>().m_PrimitiveSize
                });

                // Stat 
                AddComponent(entity, new StatData
                {
                    maxValue = item.stat,
                    curValue = item.stat
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
                if (item.IsUpgradable())
                {
                    AddComponent(entity, new ExpData
                    {
                        curTier = item.curTier,
                        curValue = 0,
                        maxValue = item.expMaxValue,
                        curLevel = 0
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
                    AddComponent<InteractAbilityBonus>(entity);
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
                        Range = item.attackRange ,
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
                        Range = item.healRange ,
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
                        Range = item.harvestRange ,
                        Targets = item.harvestTargets,
                        InteractType = InteractType.Harvest
                    });
                }


          
            }

            protected void BakeVolumeObstacleAttr(GeneralDataItem item, Entity entity)
            {
              
                const float volumeRadius = 0f;
                var areaType = (AreaType)item.curTier;
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