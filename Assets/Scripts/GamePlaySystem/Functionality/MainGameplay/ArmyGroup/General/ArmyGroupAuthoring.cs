using GamePlaySystem.Functionality.MainGameplay.General;
using Sirenix.OdinInspector;
using SparFlame.GamePlaySystem.CameraControl;
using SparFlame.GamePlaySystem.CustomParticleSystem;
using SparFlame.GamePlaySystem.General;
using SparFlame.GamePlaySystem.Movement;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.AI;

namespace SparFlame.GamePlaySystem.Functionality.MainGameplay.ArmyGroup
{
    [RequireComponent(typeof(NavMeshAgent))]
    public class ArmyGroupAuthoring : MonoBehaviour
    {
        [Header("General")] public FactionTag faction;
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
                    Faction = authoring.faction,
                    BaseTag = MainGameBaseTag.Army
                });
                
                // Moving 
                AddBuffer<ArmyGroupMovingTarget>(entity);
                AddComponent(entity, new ArmyGroupMovableData
                {
                    Speed = authoring.movementInitialSpeed,
                    CurWaypoint = 0,
                    IsTargetReachable = true
                });
                AddComponent<ArmyGroupMovingTag>(entity);
                SetComponentEnabled<ArmyGroupMovingTag>(entity, false);
                
                
                
                // Navigation path
                AddComponent(entity, new NavAgentComponent
                {
                    TargetPosition = float3.zero,
                    Extents = float3.zero,
                    EnableCalculation = false,
                    CalculationComplete = true,
                    CurrentWaypoint = 0,
                    ForceCalculate = false,
                    AgentId = authoring.GetComponent<NavMeshAgent>().agentTypeID
                });
                AddBuffer<WaypointBuffer>(entity);
                AddBuffer<ArmyGroupFinalWayPoint>(entity);
                AddComponent(entity, new ArmyGroupCalculatePathData
                {
                    CurTargetIndex = -1,
                    StartPosition = float3.zero
                });
                AddComponent<ArmyGroupCalculateEnable>(entity);
                SetComponentEnabled<ArmyGroupCalculateEnable>(entity, false);
                
                // Path visualizer  
                AddComponent<PathVisualizeEnabled>(entity);
                SetComponentEnabled<PathVisualizeEnabled>(entity, false);
                AddComponent(entity, new PathVisualizeData
                {
                    PreWaypoint = 0
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
            }
        }
    }

    public struct ArmyGroupMovableData : IComponentData
    {
        public float Speed;
        public int CurWaypoint;
        public bool IsTargetReachable;
    }

    public struct ArmyGroupCalculatePathData : IComponentData
    {
        public int CurTargetIndex;
        public float3 StartPosition;
    }
    public struct ArmyGroupCalculateEnable : IComponentData, IEnableableComponent{}

    public struct ArmyGroupFinalWayPoint : IBufferElementData
    {
        public float3 Position;
    }

   
    public struct ArmyGroupMovingTarget : IBufferElementData
    {
        public float3 Position;
    }
    public struct ArmyGroupSightTarget : IBufferElementData
    {
        public Entity Entity;
    }

    public struct ArmyGroupMovingTag : IComponentData, IEnableableComponent{}

    public struct PathVisualizer : IComponentData
    {
    }
    public struct PathVisualizeEnabled : IComponentData, IEnableableComponent{}

    public struct PathVisualizeData : IComponentData
    {
        public int PreWaypoint;
    }
    
}