using SparFlame.Components.General;
using SparFlame.Components.MainGameplay;
using SparFlame.Components.SubGameplay;
using SparFlame.Components.VFX;
using SparFlame.Database;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.AI;

namespace SparFlame.Systems.MainGameplay.ArmyGroup
{
    [RequireComponent(typeof(NavMeshAgent))]
    public class ArmyGroupAuthoring : MonoBehaviour
    {
        public int globalIdx;
        
        private class ArmyGroupAuthoringBaker : Baker<ArmyGroupAuthoring>
        {
            public override void Bake(ArmyGroupAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.Dynamic);
                var item = DatabaseManager.ArmyGroupDatabaseSo.GetItemById(authoring.globalIdx);
                // General
                AddComponent(entity, new PrefabId {value = authoring.globalIdx});
                AddComponent<GlobalSingleId>(entity);
                AddComponent<AssignGlobalSingleIDRequest>(entity);
                AddComponent(entity, new MainGameplayGeneralAttr
                {
                    faction = item.faction,
                    baseTag = MainGameBaseTag.ArmyGroup,
                    subFaction = item.subFaction
                });
                AddComponent(entity, new ArmyGroupAttr
                {
                    iconType = item.iconType,
                    gameplayName = item.gameplayName,
                });
                AddComponent(entity, new ArmyGroupStatData());
                AddComponent(entity, new ArmyGroupSkillTimer());
                AddComponent(entity, new HpRegenerateTimer());
                
                // Moving 
                AddBuffer<ArmyGroupMovingTarget>(entity);
                AddComponent(entity, new ArmyGroupMovableData
                {
                    minUnitMoveSpeed = item.initSpeed,
                    curWaypoint = 0,
                    isTargetReachable = true
                });
                AddComponent<ArmyGroupMovingTag>(entity);
                SetComponentEnabled<ArmyGroupMovingTag>(entity, false);
                
                
                
                // Navigation path
                AddComponent(entity, new NavAgentComponent
                {
                    targetPosition = float3.zero,
                    extents = float3.zero,
                    enableCalculation = false,
                    calculationComplete = true,
                    currentWaypoint = 0,
                    forceCalculate = false,
                    agentId = authoring.GetComponent<NavMeshAgent>().agentTypeID
                });
                AddBuffer<WaypointBuffer>(entity);
                AddBuffer<ArmyGroupFinalWayPoint>(entity);
                AddComponent(entity, new ArmyGroupCalculatePathData
                {
                    curTargetIndex = -1,
                    startPosition = float3.zero,
                    boxColliderSizeXz = float2.zero,
                });
                AddComponent<ArmyGroupCalculateEnable>(entity);
                SetComponentEnabled<ArmyGroupCalculateEnable>(entity, false);
                
              
                // Path visualizer  
                AddComponent<ArmyGroupPathVisualizeEnabled>(entity);
                SetComponentEnabled<ArmyGroupPathVisualizeEnabled>(entity, false);
                AddComponent(entity, new ArmyGroupPathVisualizeData
                {
                    preWaypoint = 0
                });
                
                // Sight
                AddBuffer<ArmyGroupSightTarget>(entity);
                AddComponent(entity, new ArmyGroupGenerateSightRequest
                {
                    Prefab = GetEntity(item.sightPrefab,TransformUsageFlags.Dynamic)
                });
                
                
                // Selected
                AddComponent<ArmyGroupSelected>(entity);
                SetComponentEnabled<ArmyGroupSelected>(entity, false);
                AddComponent<LockArmyGroupSelectedWorkForDrag>(entity);
                SetComponentEnabled<LockArmyGroupSelectedWorkForDrag>(entity, false);
                
                // Camera view
                AddComponent(entity, new ScreenPos
                {
                    ScreenPosition = float2.zero
                });
                AddComponent<InCameraView>(entity);
                AddComponent<InCameraExtendView>(entity);
                SetComponentEnabled<InCameraView>(entity, false);
                SetComponentEnabled<InCameraExtendView>(entity, false);
                
                // VFX
                AddBuffer<TrackedByVFX>(entity);
                
                // Unit management
                AddBuffer<ArmyGroupUnit>(entity);
                AddBuffer<ArmyGroupUnitTypeData>(entity);
                
                // Passing Data
                AddComponent<LastPassingByCity>(entity);
                
                // State data
                AddComponent(entity, new ArmyGroupStateData
                {
                    Target =  Entity.Null,
                    TargetState = ArmyGroupState.Idle,
                    CurState = ArmyGroupState.Idle
                });

                // Enemy AI
                AddComponent(entity, new ArmyGroupThreatenData
                {
                    mainUnitType = UnitType.Cleric,
                    totalThreatenValue = 0
                });
                AddComponent(entity, new ArmyGroupAIData());
                AddBuffer<SubGameplayArmyGroupWaypointData>(entity);
                if (item.isEnemyArmyGroup)
                {
                    var buffer = AddBuffer<EnemyArmyGroupCompositionData>(entity);
                    foreach (var data in item.enemyArmyGroupCompositionDatas)
                    {
                        buffer.Add(new EnemyArmyGroupCompositionData
                        {
                            Count = data.count,
                            UnitPrefab = GetEntity(data.unitPrefab, TransformUsageFlags.Dynamic),
                            Level = data.level
                        });
                    }
                    AddComponent<ArmyGroupCommandData>(entity);
                    AddComponent<ArmyGroupCommandUpdate>(entity);
                    SetComponentEnabled<ArmyGroupCommandUpdate>(entity,false);
                }
            }
        }
    }





    
    
    
}