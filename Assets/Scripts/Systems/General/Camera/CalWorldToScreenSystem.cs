using SparFlame.Components.General;
using SparFlame.Components.MainGameplay;
using SparFlame.Components.SubGameplay;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Burst;
using Unity.Transforms;
using Unity.Collections;
namespace SparFlame.Systems.General.Camera
{
    public partial struct CalWorldToScreenSystem : ISystem
    {
        private ComponentLookup<InCameraView> _inCameraViewLookup;
        private ComponentLookup<InCameraExtendView> _inCameraExtendViewLookup;
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<WaitInfo>();
            state.RequireForUpdate<EndSimulationEntityCommandBufferSystem.Singleton>();
            state.RequireForUpdate<GameStatusData>();
            state.RequireForUpdate<ScreenPos>();
            state.RequireForUpdate<CameraData>();
            state.RequireForUpdate<CameraViewExtendConfig>();
            _inCameraViewLookup = state.GetComponentLookup<InCameraView>();
            _inCameraExtendViewLookup = state.GetComponentLookup<InCameraExtendView>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            _inCameraViewLookup.Update(ref state);
            _inCameraExtendViewLookup.Update(ref state);
            var gameStatus = SystemAPI.GetSingleton<GameStatusData>().Value;
            var waitInfo = SystemAPI.GetSingleton<WaitInfo>();
            if (gameStatus != GameStatus.MainGaming && gameStatus != GameStatus.SubGaming) return;
            if(waitInfo.WaitType != WaitType.None)return;
            
            
            var cameraData = SystemAPI.GetSingleton<CameraData>();
            // Calculate VP Matrix First
            var vpMatrix = math.mul(cameraData.ProjectionMatrix, cameraData.ViewMatrix);
            var ecb = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>()
                .CreateCommandBuffer(state.WorldUnmanaged).AsParallelWriter();
            var cameraViewExtend = SystemAPI.GetSingleton<CameraViewExtendConfig>();
            var calculateWtsJob = new CalculateWtsJob
            {
                // ECB = ecb,
                VpMatrix = vpMatrix,
                ScreenWidth = cameraData.ScreenSize.x,
                ScreenHeight = cameraData.ScreenSize.y,
                CameraViewExtend = cameraViewExtend.Value,
                InCameraExtendLookup = _inCameraExtendViewLookup,
                InCameraViewLookup = _inCameraViewLookup
            };
            calculateWtsJob.ScheduleParallel();
        }


        [BurstCompile]
        [WithNone(typeof(UnitDeadTag))]
        public partial struct CalculateWtsJob : IJobEntity
        {
            [ReadOnly] public float4x4 VpMatrix;
            [ReadOnly] public float2 CameraViewExtend;

            [ReadOnly] public float ScreenWidth;
            [ReadOnly] public float ScreenHeight;
            [NativeDisableParallelForRestriction] public ComponentLookup<InCameraView> InCameraViewLookup;
            [NativeDisableParallelForRestriction] public ComponentLookup<InCameraExtendView> InCameraExtendLookup;
            // public EntityCommandBuffer.ParallelWriter ECB;

            private void Execute(ref ScreenPos screenPos, in LocalTransform transform,
                Entity selfEntity)
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
                    if (InCameraViewLookup.HasComponent(selfEntity))
                        InCameraViewLookup.SetComponentEnabled(selfEntity, true);
                    if (InCameraExtendLookup.HasComponent(selfEntity))
                        InCameraExtendLookup.SetComponentEnabled(selfEntity, true);
                    // ECB.SetComponentEnabled<InCameraView>(index, selfEntity, true);
                    // ECB.SetComponentEnabled<InCameraExtendView>(index, selfEntity, true);
                }
                else
                {
                    if (InCameraViewLookup.HasComponent(selfEntity))
                        InCameraViewLookup.SetComponentEnabled(selfEntity, false);
                    // ECB.SetComponentEnabled<InCameraView>(index, selfEntity, false);
                    if (screenPos.ScreenPosition.x < ScreenWidth + CameraViewExtend.x
                        && screenPos.ScreenPosition.x > -CameraViewExtend.x
                        && screenPos.ScreenPosition.y < ScreenHeight + CameraViewExtend.y
                        && screenPos.ScreenPosition.y > -CameraViewExtend.y)
                    {
                        if (InCameraExtendLookup.HasComponent(selfEntity))
                            InCameraExtendLookup.SetComponentEnabled(selfEntity, true);
                        // ECB.SetComponentEnabled<InCameraExtendView>(index, selfEntity, true);
                    }
                    else
                    {
                        if (InCameraExtendLookup.HasComponent(selfEntity))
                            InCameraExtendLookup.SetComponentEnabled(selfEntity, false);
                    }
                }
            }
        }
    }
}