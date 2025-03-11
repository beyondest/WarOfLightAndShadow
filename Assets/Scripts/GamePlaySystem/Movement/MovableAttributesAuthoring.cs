using UnityEngine;
using Unity.Entities;
using Unity.Mathematics;

namespace SparFlame.GamePlaySystem.Movement
{
    public class MovableAttributesAuthoring : MonoBehaviour
    {

        [Tooltip("This interval determines how long the path calculation is executed once")]
        public float calculateInterval = 1.0f;

        
        private class Baker : Baker<MovableAttributesAuthoring>
        {
            public override void Bake(MovableAttributesAuthoring authoring)
            {
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
                });
                
                AddComponent(entity, new MovableData
                {
                    MoveSpeed = 0f,
                    TargetCenterPos = float3.zero,
                    TargetColliderShapeXZ = float2.zero,
                    MovementCommandType = MovementCommandType.None,
                    InteractiveRangeSq = 0f,
                    DetailInfo = DetailInfo.None,
                    MovementState = MovementState.NotMoving
                });
                AddBuffer<WaypointBuffer>(entity);
                AddComponent<HaveTarget>(entity);
                SetComponentEnabled<HaveTarget>(entity, false);
                
            }
        }
    }


    public struct MovableData : IComponentData
    {
        public float MoveSpeed;
        public float3 TargetCenterPos;
        public float2 TargetColliderShapeXZ;
        public MovementCommandType MovementCommandType;
        public MovementState MovementState;
        public DetailInfo DetailInfo;
        /// <summary>
        /// This range is attack range for attack movement, garrison range for garrison movement...
        /// </summary>
        public float InteractiveRangeSq;
        
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
    }

    public struct WaypointBuffer : IBufferElementData
    {
        public float3 WayPoint;
    }



    public struct HaveTarget : IComponentData, IEnableableComponent
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
        MovementPartialComplete
    }

    public enum DetailInfo
    {
        None,
        Reachable,
        NotReachable,
        /// <summary>
        /// Calculation not complete or fail will return this
        /// </summary>
        CalculationFailed
    }

    
}