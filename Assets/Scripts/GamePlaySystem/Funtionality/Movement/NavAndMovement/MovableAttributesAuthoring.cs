using System;
using Unity.Collections;
using UnityEngine;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine.AI;
using UnityEngine.Experimental.AI;
using UnityEngine.Serialization;

namespace SparFlame.GamePlaySystem.Movement
{
    public class MovableAttributesAuthoring : MonoBehaviour
    {

        [Tooltip("This interval determines how long the path calculation is executed once")]
        public float calculateInterval = 1.0f;
         public float moveSpeed = 5f;

        
        private class Baker : Baker<MovableAttributesAuthoring>
        {
            public override void Bake(MovableAttributesAuthoring authoring)
            {
                if (!authoring.TryGetComponent<BoxCollider>(out var collider))
                {
                    Debug.LogError("Movable unit must have a Box Collider");
                    return;
                }
                if (!authoring.TryGetComponent(out NavMeshAgent agent))
                {
                    Debug.Log("Movable unit requires NavMeshAgent component");
                    return;
                }
                
                
                var entity = GetEntity(TransformUsageFlags.Dynamic);
                
                AddComponent(entity, new NavAgentComponent
                {
                    TargetPosition = float3.zero,
                    CalculateInterval = authoring.calculateInterval,
                    Extents = float3.zero,
                    EnableCalculation = false,
                    CalculationComplete = false,
                    CurrentWaypoint = 0,
                    IsNavQuerySet = false,
                    ForceCalculate = false,
                    AgentId = agent.agentTypeID
                });
                
                AddComponent(entity, new MovableData
                {
                    MoveSpeed = authoring.moveSpeed,
                    TargetCenterPos = float3.zero,
                    TargetColliderShapeXZ = float2.zero,
                    MovementCommandType = MovementCommandType.None,
                    InteractiveRangeSq = 0f,
                    DetailInfo = DetailInfo.None,
                    MovementState = MovementState.NotMoving,
                    ForceCalculate = false,
                    SelfColliderShapeXz = new float2(collider.size.x, collider.size.z),
                });
                AddComponent(entity, new Surroundings
                {
                    MoveSuccess = true,
                    FrontEntity = Entity.Null,
                    LeftEntity = Entity.Null,
                    RightEntity = Entity.Null,
                });
                
                AddBuffer<WaypointBuffer>(entity);
                AddComponent<MovingStateTag>(entity);
                SetComponentEnabled<MovingStateTag>(entity, false);
            }
        }
    }


    public struct MovableData : IComponentData
    {
        public float MoveSpeed;
        public float3 TargetCenterPos;
        /// <summary>
        /// Target collider shape is used for calculating
        /// the extents of nav agent, extra radius for reachable check
        /// </summary>
        public float2 TargetColliderShapeXZ;
        public MovementCommandType MovementCommandType;
        public MovementState MovementState;
        public DetailInfo DetailInfo;
        /// <summary>
        /// This range is attack range for attack movement, garrison range for garrison movement...
        /// </summary>
        public float InteractiveRangeSq;
        public bool ForceCalculate;
        /// <summary>
        /// This is the collider of object itself, used for raycast for obstacle avoidance 
        /// </summary>
        public float2 SelfColliderShapeXz;
    }

    public struct Surroundings : IComponentData
    {
        public bool MoveSuccess;
        public Entity FrontEntity;
        public Entity LeftEntity;
        public Entity RightEntity;
        public Entity LeftTailEntity;
        public Entity RightTailEntity;
        public int CompromiseTimes;
        public float3 IdealDirection;
        public bool ChooseRight;
        public int SlideTimes;
    }
    
    public struct NavAgentComponent : IComponentData
    {
        public bool EnableCalculation;
        public float3 TargetPosition;
        public bool CalculationComplete;
        public int CurrentWaypoint;
        public float NextPathCalculateTime;
        public bool IsNavQuerySet;
        public float CalculateInterval;
        public float3 Extents;
        public bool ForceCalculate;
        public int AgentId;
        public int QueryIndex;
    }

    public struct WaypointBuffer : IBufferElementData
    {
        public float3 WayPoint;
    }



    public struct MovingStateTag : IComponentData, IEnableableComponent
    {
        
    }
    

    
    public enum MovementCommandType
    {
        None,
        /// <summary>
        /// Interactive includes attack, heal, garrison, harvest
        /// </summary>
        Interactive,
        March,
    }

    public enum MovementState
    {
        NotMoving ,
        /// <summary>
        /// Is moving 
        /// </summary>
        IsMoving ,
        /// <summary>
        /// Target reachable and reach
        /// </summary>
        MovementComplete ,
        /// <summary>
        /// Only reach the closest point , cause target not reachable
        /// </summary>
        MovementPartialComplete,
    }

    public enum DetailInfo
    {
        None,
        Reachable,
        NotReachable,
        /// <summary>
        /// Calculation not complete or fail will return this
        /// </summary>
        CalculationNotComplete,
        AutoGiveWay,
        Stuck
    }


}