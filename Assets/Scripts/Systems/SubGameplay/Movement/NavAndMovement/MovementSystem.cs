using SparFlame.Components.General;
using SparFlame.Components.SubGameplay;
using Unity.Entities;
using Unity.Burst;
using Unity.Mathematics;
using Unity.Physics;

// ReSharper disable UseIndexFromEndExpression


namespace SparFlame.Systems.SubGameplay.Movement
{
    [BurstCompile]
    public partial struct MovementSystem : ISystem
    {

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<GameTimeData>();
            state.RequireForUpdate<PhysicsWorldSingleton>();
            state.RequireForUpdate<SubGamingTag>();
            state.RequireForUpdate<MovableData>();
            state.RequireForUpdate<MovementConfig>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var config = SystemAPI.GetSingleton<MovementConfig>();
            var physicsWorld = SystemAPI.GetSingleton<PhysicsWorldSingleton>();

            if (!(SystemAPI.HasSingleton<DebugTag>() && SystemAPI.TryGetSingleton(out MovementDebug debug)))
            {
                debug = new MovementDebug
                {
                    enabled = false
                };
            } 
            state.Dependency = new PlayerMovementJob
            {
                PhysicsWorld = physicsWorld,
                ElapsedTime = SystemAPI.GetSingleton<GameTimeData>().ElapsedTime,
                Config = config,
            }.ScheduleParallel(state.Dependency);
            state.Dependency = new AIMovementJob
            {
                PhysicsWorld = physicsWorld,
                ElapsedTime = SystemAPI.GetSingleton<GameTimeData>().ElapsedTime,
                Config = config,
            }.ScheduleParallel(state.Dependency);
            new NotMovementStateJob().ScheduleParallel();
        }
    }
    [WithDisabled(typeof(MovingStateTag))]
    public partial struct NotMovementStateJob : IJobEntity
    {
        private void Execute(ref SeekTarget seekTarget)
        {
            seekTarget.Direction = float3.zero;
        }
    }
}