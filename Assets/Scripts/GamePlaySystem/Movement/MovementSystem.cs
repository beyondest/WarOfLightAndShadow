using SparFlame.GamePlaySystem.General;
using Unity.Entities;
using Unity.Burst;
using Unity.Collections;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

namespace SparFlame.GamePlaySystem.Movement
{
    public partial struct MovementSystem : ISystem
    {
        private BufferLookup<WaypointBuffer> _waypointLookup;

        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<BeginSimulationEntityCommandBufferSystem.Singleton>();
            state.RequireForUpdate<NotPauseTag>();
            state.RequireForUpdate<MovementConfig>();
            _waypointLookup = state.GetBufferLookup<WaypointBuffer>(true);
        }


        public void OnUpdate(ref SystemState state)
        {
            var config = SystemAPI.GetSingleton<MovementConfig>();
            var ecbSingleton = SystemAPI.GetSingleton<BeginSimulationEntityCommandBufferSystem.Singleton>();
            _waypointLookup.Update(ref state);
            // PathCalculated is set to true only if calculation is done successfully
            new MoveJob
            {
                DeltaTime = SystemAPI.Time.DeltaTime,
                ECB = ecbSingleton.CreateCommandBuffer(state.WorldUnmanaged).AsParallelWriter(),
                WayPointsLookup = _waypointLookup,
                WayPointDistanceSq = config.WayPointDistanceSq
            }.ScheduleParallel();
        }
    }


    [BurstCompile]
    public partial struct MoveJob : IJobEntity
    {
        public EntityCommandBuffer.ParallelWriter ECB;
        [ReadOnly] public BufferLookup<WaypointBuffer> WayPointsLookup;
        [ReadOnly] public float DeltaTime;
        [ReadOnly] public float WayPointDistanceSq;

        private void Execute([ChunkIndexInQuery] int chunkIndex, ref MovableData movableData, ref NavAgentComponent navAgent, ref LocalTransform transform,
             Entity entity)
        {
            if(!(movableData.ShouldMove && navAgent.PathCalculated) )return;
            // Check if entity is already reaching the current waypoint
            if (WayPointsLookup.TryGetBuffer(entity, out var waypointBuffer)
                && math.distancesq(transform.Position, waypointBuffer[navAgent.CurrentWaypoint].WayPoint) < WayPointDistanceSq
                )
            {
                if (navAgent.CurrentWaypoint + 1 < waypointBuffer.Length)
                {
                    navAgent.CurrentWaypoint += 1;
                }

                ECB.SetComponent(chunkIndex,entity, navAgent);
            }

            var direction = waypointBuffer[navAgent.CurrentWaypoint].WayPoint - transform.Position;
            var angle = math.degrees(math.atan2(direction.z, direction.x));

            transform.Rotation = math.slerp(
                transform.Rotation,
                quaternion.Euler(new float3(0, angle, 0)),
                DeltaTime);

            transform.Position +=
                math.normalize(direction) * DeltaTime * movableData.MoveSpeed;
            ECB.SetComponent(chunkIndex,entity, transform);
        }
    }
}


