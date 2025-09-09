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
        // public int initialId;
        [Header("Moving config")]
        public float movementInitialSpeed;
        
        [Header("Sight config"),AssetsOnly] public GameObject armyGroupSightPrefab;
        

        private class ArmyGroupAuthoringBaker : Baker<ArmyGroupAuthoring>
        {
            public override void Bake(ArmyGroupAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.Dynamic);
                
                // General
                AddComponent(entity, new MainGameplayGeneralAttr
                {
                    faction = authoring.faction,
                    baseTag = MainGameBaseTag.ArmyGroup,
                    subFaction = authoring.initialSubFactionTag,
                });
                AddComponent(entity, new ArmyGroupAttr
                {
                    iconType = authoring.iconType,
                    saveId = 0,
                    gameplayName = "New Army Group",
                });
                AddComponent(entity, new ArmyGroupStatData());
                AddComponent(entity, new ArmyGroupSkillTimer());
                
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
                    startPosition = float3.zero
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
                AddComponent<LastPassingByPlayerCity>(entity);
                
                // State data
                AddComponent(entity, new ArmyGroupStateData
                {
                    Target =  Entity.Null,
                    TargetState = ArmyGroupState.Idle,
                    CurState = ArmyGroupState.Idle
                });
            }
        }
    }
   


    
    public struct PathVisualizer : IComponentData
    {
    }
    public struct PathVisualizeEnabled : IComponentData, IEnableableComponent{}


    
    
    
}