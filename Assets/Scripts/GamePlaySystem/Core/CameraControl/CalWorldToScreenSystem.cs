using Sirenix.OdinInspector;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Burst;
using Unity.Transforms;
using SparFlame.GamePlaySystem.General;
using SparFlame.GamePlaySystem.CameraControl;

namespace SparFlame.GamePlaySystem.UnitSelection
{
    public partial struct CalWorldToScreenSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<EndSimulationEntityCommandBufferSystem.Singleton>();
            state.RequireForUpdate<GameStatusData>();
            state.RequireForUpdate<ScreenPos>();
            state.RequireForUpdate<CameraData>();
            state.RequireForUpdate<CameraViewExtend>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var gameStatus = SystemAPI.GetSingleton<GameStatusData>().Value;
            if(gameStatus != GameStatus.MainGaming && gameStatus != GameStatus.SubGaming)return;
            var cameraData = SystemAPI.GetSingleton<CameraData>();
            // Calculate VP Matrix First
            var vpMatrix = math.mul(cameraData.ProjectionMatrix, cameraData.ViewMatrix);
            var ecb = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>()
                .CreateCommandBuffer(state.WorldUnmanaged).AsParallelWriter();
            var cameraViewExtend = SystemAPI.GetSingleton<CameraViewExtend>();
            var calculateWtsJob = new CalculateWtsJob
            {
                ECB = ecb,
                VpMatrix = vpMatrix,
                ScreenWidth = cameraData.ScreenSize.x,
                ScreenHeight = cameraData.ScreenSize.y,
                CameraViewExtend = cameraViewExtend.Value
            };
            calculateWtsJob.ScheduleParallel();
        }

        
        [BurstCompile]
        [WithNone(typeof(UnitDeadTag))]
        public partial struct CalculateWtsJob : IJobEntity
        {
            [ReadOnly]public float4x4 VpMatrix;
            [ReadOnly] public float2 CameraViewExtend;
            public float ScreenWidth;
            public float ScreenHeight;
            public EntityCommandBuffer.ParallelWriter ECB;

            private void Execute([ChunkIndexInQuery]int index, ref ScreenPos screenPos, in LocalTransform transform, Entity selfEntity)
            {
                var clipSpacePos = math.mul(VpMatrix, new float4(transform.Position, 1.0f));
                var ndcPos = clipSpacePos.xyz / clipSpacePos.w;
                screenPos.ScreenPosition = new float2(
                    //(ndcPos.x + 1.0f) * 0.5f * ScreenWidth 
                    //(1.0f + ndcPos.y ) * 0.5f  * ScreenHeight
                    // if use Camera Type to assign camera data, use + all; if use ecs type, use - all;
                    (1.0f + ndcPos.x) * 0.5f * ScreenWidth,
                    (1.0f + ndcPos.y) * 0.5f * ScreenHeight);
                if (screenPos.ScreenPosition is { x: > 0, y: > 0 }
                    && screenPos.ScreenPosition.x < ScreenWidth && screenPos.ScreenPosition.y < ScreenHeight)
                {
                    ECB.SetComponentEnabled<InCameraView>(index, selfEntity, true);
                    ECB.SetComponentEnabled<InCameraExtendView>(index, selfEntity, true);
                }
                else
                {
                    ECB.SetComponentEnabled<InCameraView>(index, selfEntity, false);
                    if (screenPos.ScreenPosition.x < ScreenWidth + CameraViewExtend.x
                        && screenPos.ScreenPosition.x > -CameraViewExtend.x
                        && screenPos.ScreenPosition.y < ScreenHeight + CameraViewExtend.y
                        && screenPos.ScreenPosition.y > -CameraViewExtend.y)
                    {
                        ECB.SetComponentEnabled<InCameraExtendView>(index, selfEntity, true);
                    }
                    else
                    {
                        ECB.SetComponentEnabled<InCameraExtendView>(index, selfEntity, false);
                    }
                }
                
            }
        }
    }
}