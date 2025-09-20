using System;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using SparFlame.Components.General;
using SparFlame.Components.MainGameplay;
using SparFlame.Components.SubGameplay;
using SparFlame.Components.VFX;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.AI;

namespace SparFlame.Systems.MainGameplay.ArmyGroup
{
    [RequireComponent(typeof(NavMeshAgent))]
    public class ArmyGroupAuthoring : MonoBehaviour
    {
        [Header("General")] public FactionTag faction;
        public ArmyGroupIconType iconType;
        public SubFactionTag initialSubFactionTag;

        public string gameplayName = "New Army Group";
        // public int initialId;
        [Header("Moving config")]
        public float movementInitialSpeed;
        
        [Header("Sight config"),AssetsOnly] public GameObject armyGroupSightPrefab;

        [Header("Enemy AI Config")] public bool ifEnemyArmyGroup;
        [ShowIf(nameof(ifEnemyArmyGroup))]
        public List<UnitCompositionData> enemyArmyGroupCompositionDatas; 

        
        private class ArmyGroupAuthoringBaker : Baker<ArmyGroupAuthoring>
        {
            public override void Bake(ArmyGroupAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.Dynamic);
                
                // General
                AddComponent<GlobalSingleId>(entity);
                AddComponent<AssignGlobalSingleIDRequest>(entity);
                AddComponent(entity, new MainGameplayGeneralAttr
                {
                    faction = authoring.faction,
                    baseTag = MainGameBaseTag.ArmyGroup,
                    subFaction = authoring.initialSubFactionTag,
                });
                AddComponent(entity, new ArmyGroupAttr
                {
                    iconType = authoring.iconType,
                    gameplayName = authoring.gameplayName,
                });
                AddComponent(entity, new ArmyGroupStatData());
                AddComponent(entity, new ArmyGroupSkillTimer());
                AddComponent(entity, new HpRegenerateTimer());
                
                // Moving 
                AddBuffer<ArmyGroupMovingTarget>(entity);
                AddComponent(entity, new ArmyGroupMovableData
                {
                    minUnitMoveSpeed = authoring.movementInitialSpeed,
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
                    calculationInfo = ArmyGroupPathCalculationInfo.None,
                    boxColliderSizeXz = float2.zero,
                });
                AddComponent<ArmyGroupCalculateEnable>(entity);
                SetComponentEnabled<ArmyGroupCalculateEnable>(entity, false);
                
              
                // Path visualizer  
                AddComponent<PathVisualizeEnabled>(entity);
                SetComponentEnabled<PathVisualizeEnabled>(entity, false);
                AddComponent(entity, new PathVisualizeData
                {
                    preWaypoint = 0
                });
                
                // Sight
                AddBuffer<ArmyGroupSightTarget>(entity);
                AddComponent(entity, new ArmyGroupSightRequest
                {
                    Prefab = GetEntity(authoring.armyGroupSightPrefab,TransformUsageFlags.Dynamic)
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
                    mainUnitType = UnitType.Magic,
                    totalThreatenValue = 0
                });
                
                if (authoring.ifEnemyArmyGroup)
                {
                    var buffer = AddBuffer<EnemyArmyGroupCompositionData>(entity);
                    foreach (var data in authoring.enemyArmyGroupCompositionDatas)
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



    [Serializable]
    public class UnitCompositionData
    {
        public GameObject unitPrefab;
        public int count;
        public int level;
    }
    


    
    
    
}