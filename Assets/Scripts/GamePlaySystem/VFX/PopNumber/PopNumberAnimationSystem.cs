using Unity.Burst;
using Unity.Entities;
using Unity.Transforms;
using System.Runtime.CompilerServices;
using SparFlame.GamePlaySystem.CameraControl;
using SparFlame.GamePlaySystem.General;
using Unity.Collections;
using Unity.Mathematics;

namespace SparFlame.GamePlaySystem.PopNumber
{

    [UpdateAfter(typeof(PopNumberSpawnSystem))]
    public partial struct PopNumberAnimationSystem : ISystem
    {
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<CameraData>();
            state.RequireForUpdate<EndSimulationEntityCommandBufferSystem.Singleton>();
            state.RequireForUpdate<GamingTag>();
            state.RequireForUpdate<PopNumberConfig>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var ecbSingleton = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>();
            var config = SystemAPI.GetSingleton<PopNumberConfig>();
            var cameraData = SystemAPI.GetSingleton<CameraData>();
            new MoveJob
            {
                ElapsedTime = (float)SystemAPI.GetSingleton<GameTimeData>().ElapsedTime,
                ECBWriter = ecbSingleton.CreateCommandBuffer(state.WorldUnmanaged).AsParallelWriter(),
                LifeTime = config.MovementTime,
                VerticalMovementOffset = config.VerticalMovementOffset,
                ZMovementOffset = config.ZMovementOffset,
                ScaleOffset = config.ScaleOffset,
                CameraData = cameraData
            }.ScheduleParallel();
        }
        
        [BurstCompile]
        public partial struct MoveJob : IJobEntity
        {
            public float ElapsedTime;
            public EntityCommandBuffer.ParallelWriter ECBWriter;
            public float LifeTime;
            public float VerticalMovementOffset;
            public float ZMovementOffset;
            public float ScaleOffset;
            [ReadOnly]public CameraData CameraData;

            private void Execute(Entity entity, [ChunkIndexInQuery] int chunkIndex, ref LocalTransform transform,
                in PopNumberData data)
            {
                var timeAlive = ElapsedTime - data.SpawnTime;
                if (timeAlive > LifeTime)
                {
                    ECBWriter.DestroyEntity(chunkIndex, entity);
                    return;
                }

                var easing = EaseOutQuad(timeAlive / LifeTime);
                // transform.Position.y = data.OriginalY + VerticalMovementOffset * easing;
                transform.Position = data.OriginalPosition + easing * VerticalMovementOffset * CameraData.CameraUp;
                // transform.Position.z +=ZMovementOffset * easing ;
                // var forward =math.normalizesafe( data.OriginalPosition - CameraData.CameraPosition);
                transform.Position +=easing * ZMovementOffset *CameraData.CameraForward;
                transform.Scale *= 1 + ScaleOffset * easing;
            }
            /// <summary>
            /// Provide Ease Movement Effect
            /// </summary>
            /// <param name="number"></param>
            /// <returns></returns>
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            private static float EaseOutQuad(float number)
            {
                return 1 - (1 - number) * (1 - number);
            }
        }
    }
}
    
