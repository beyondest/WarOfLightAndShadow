using Unity.Entities;
using Unity.Burst;
using Unity.Physics;
using SparFlame.GamePlaySystem.General;

// ReSharper disable UseIndexFromEndExpression


namespace SparFlame.GamePlaySystem.Movement
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
                DeltaTime = SystemAPI.GetSingleton<GameTimeData>().DeltaTime,
                ElapsedTime = SystemAPI.GetSingleton<GameTimeData>().ElapsedTime,
                Config = config,
                Debug = debug
            }.ScheduleParallel(state.Dependency);
            state.Dependency = new AIMovementJob
            {
                PhysicsWorld = physicsWorld,
                DeltaTime = SystemAPI.GetSingleton<GameTimeData>().DeltaTime,
                ElapsedTime = SystemAPI.GetSingleton<GameTimeData>().ElapsedTime,
                Config = config,
                Debug = debug
            }.ScheduleParallel(state.Dependency);
        }
    }



  
}