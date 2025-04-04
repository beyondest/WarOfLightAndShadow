using Unity.Entities;
using Unity.Mathematics;
using Unity.Burst;
using Unity.Transforms;
using SparFlame.GamePlaySystem.General;
using SparFlame.GamePlaySystem.CameraControl;
namespace SparFlame.GamePlaySystem.UnitSelection
{
    [UpdateAfter(typeof(CameraInfoUpdateSystem))]
    public partial struct CalWorldToScreenSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<NotPauseTag>();
            state.RequireForUpdate<ScreenPos>();
            state.RequireForUpdate<CameraData>();
        }
        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var cameraData = SystemAPI.GetSingleton<CameraData>();
            // Calculate VP Matrix First
            var vpMatrix = math.mul(cameraData.ProjectionMatrix, cameraData.ViewMatrix);
            var calculateWtsJob = new CalculateWtsJob
            {
                VpMatrix = vpMatrix,
                ScreenWidth = cameraData.ScreenSize.x,
                ScreenHeight = cameraData.ScreenSize.y
            };
            calculateWtsJob.ScheduleParallel();
        }

        [BurstCompile]
        public partial struct CalculateWtsJob : IJobEntity
        {
            public float4x4 VpMatrix;
            public float ScreenWidth;
            public float ScreenHeight;

            // ReSharper disable once MemberCanBePrivate.Global
            public void Execute(ref ScreenPos screenPos, in LocalTransform transform)
            {
                var clipSpacePos = math.mul(VpMatrix, new float4(transform.Position, 1.0f));
                var ndcPos = clipSpacePos.xyz / clipSpacePos.w;
                screenPos.ScreenPosition = new float2(
                //(ndcPos.x + 1.0f) * 0.5f * ScreenWidth 
                //(1.0f + ndcPos.y ) * 0.5f  * ScreenHeight
                // if use Camera Type to asign camera data, use + all; if use ecs type, use - all;
                (1.0f + ndcPos.x) * 0.5f *  ScreenWidth,
                (1.0f + ndcPos.y ) * 0.5f  * ScreenHeight);
            }
        }
    }
}
