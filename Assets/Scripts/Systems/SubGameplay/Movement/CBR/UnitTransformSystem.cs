using SparFlame.Components.General;
using SparFlame.Components.SubGameplay;
using SparFlame.Systems.SubGameplay.Movement.CBR;
using Unity.Burst;
using Unity.Entities;
using Unity.Jobs;
using Unity.Physics;
using Unity.Transforms;

namespace SparFlame.Systems.SubGameplay.Movement
{
    [BurstCompile]
    public partial struct UnitTransformSystem : ISystem
    {
        private ComponentLookup<MovingStateTag> _movingStateLookup;
        private ComponentLookup<PlayerTag> _playerTagLookup;

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<SeparationConfig>();
            state.RequireForUpdate<AvoidanceConfig>();
            state.RequireForUpdate<GetGroundNormalConfig>();
            state.RequireForUpdate<PhysicsWorldSingleton>();
            state.RequireForUpdate<MovementConfig>();
            state.RequireForUpdate<GameTimeData>();
            state.RequireForUpdate<CbrConfig>();
            state.RequireForUpdate<SubGamingTag>();
            _playerTagLookup = state.GetComponentLookup<PlayerTag>(true);
            _movingStateLookup = state.GetComponentLookup<MovingStateTag>(true);
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var debug = new MovementDebug();
            if (SystemAPI.HasSingleton<DebugTag>())
            {
                SystemAPI.TryGetSingleton(out debug);
            }
            _playerTagLookup.Update(ref state);
            _movingStateLookup.Update(ref state);
            var lookups = new SurroundingLookups
            {
                LocalTransformLookup = SystemAPI.GetComponentLookup<LocalTransform>(true),
                BoxColliderSizeLookup = SystemAPI.GetComponentLookup<BoxColliderSize>(true),
                VelocityLookup = SystemAPI.GetComponentLookup<Velocity>(true),
                // TriggerDataLookup = SystemAPI.GetComponentLookup<FakeCollisionTriggerData>(true),
                AttackStateLookup = SystemAPI.GetComponentLookup<AttackStateTag>(true),
                HealStateLookup = SystemAPI.GetComponentLookup<HealStateTag>(true),
                AutoGiveWayLookup = SystemAPI.GetComponentLookup<AutoGiveWayTag>(true),
                MovingStateLookup = SystemAPI.GetComponentLookup<MovingStateTag>(true),
                IdleStateLookup = SystemAPI.GetComponentLookup<IdleStateTag>(true),
                FormationMovingTagLookup = SystemAPI.GetComponentLookup<FormationMovingTag>(true),
            };
            
            var job1 =  new SeekTargetJobPlus
            {
                ElapsedTime = SystemAPI.GetSingleton<GameTimeData>().ElapsedTime,
                Config = SystemAPI.GetSingleton<MovementConfig>(),
                MovingStateLookup = _movingStateLookup,
            }.ScheduleParallel(state.Dependency);
            var job2= new GetGroundNormalJob
            {
                PhysicsWorld = SystemAPI.GetSingleton<PhysicsWorldSingleton>(),
                Config = SystemAPI.GetSingleton<GetGroundNormalConfig>(),
            }.ScheduleParallel(state.Dependency);
    
            var job3 = new SeparationSteeringJob
            {
                AvoidanceConfig = SystemAPI.GetSingleton<AvoidanceConfig>(),
                SeparationConfig = SystemAPI.GetSingleton<SeparationConfig>(),
                Lookups = lookups,
            }.ScheduleParallel(state.Dependency);
            
            state.Dependency = new UnitFinalTransformApplyJob
            {
                Debug = debug,
                Config = SystemAPI.GetSingleton<CbrConfig>(),
                DeltaTime = SystemAPI.GetSingleton<GameTimeData>().DeltaTime,
                PlayerTagLookup = _playerTagLookup
            }.ScheduleParallel(JobHandle.CombineDependencies(job1,job2,job3));
        }
    }

    

}