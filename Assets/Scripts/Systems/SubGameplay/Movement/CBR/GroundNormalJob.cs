using SparFlame.Components.SubGameplay;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Transforms;

namespace SparFlame.Systems.SubGameplay.Movement.CBR
{
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