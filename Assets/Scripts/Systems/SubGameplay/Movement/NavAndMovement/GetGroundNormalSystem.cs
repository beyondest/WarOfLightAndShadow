using SparFlame.Components.General;
using SparFlame.Components.SubGameplay;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Transforms;

namespace SparFlame.Systems.SubGameplay.Movement
{
    public partial struct GetGroundNormalSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<GetGroundNormalConfig>();
            state.RequireForUpdate<PhysicsWorldSingleton>();
            state.RequireForUpdate<GroundInfo>();
            state.RequireForUpdate<SubGamingTag>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            new GetGroundNormalJob
            {
                PhysicsWorld = SystemAPI.GetSingleton<PhysicsWorldSingleton>(),
                Config = SystemAPI.GetSingleton<GetGroundNormalConfig>(),
            }.ScheduleParallel();
        }

    }

    [BurstCompile]
    public partial struct GetGroundNormalJob : IJobEntity
    {
        [ReadOnly] public PhysicsWorldSingleton PhysicsWorld;
        [ReadOnly] public GetGroundNormalConfig Config;

        private void Execute(ref GroundInfo groundInfo, in LocalTransform transform)
        {
            var b = MovementUtils.RayCastToTerrainToGetNormal(ref PhysicsWorld,
                transform.Position + new float3(0, Config.LiftDistance, 0),
                Config.DownDistance, Config.TerrainLayerMask, Config.DetectTerrainRaycastBelongsTo, out var hit);
            groundInfo.Normal = hit.SurfaceNormal;
            groundInfo.HitPosition = hit.Position;
            groundInfo.Hit = b;
        }
    }
}