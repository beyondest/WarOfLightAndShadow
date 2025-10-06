using SparFlame.Components.General;
using SparFlame.Components.SubGameplay;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;

namespace SparFlame.Systems.SubGameplay.StateMachine
{
    [UpdateInGroup(typeof(InitializationSystemGroup))]
    public partial struct UnitDeadStateMachine : ISystem
    {
        private BufferLookup<AnimationEventData> _eventsLookup;
        private ComponentLookup<AnimationStateData> _animationStateLookup;

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<GameTimeData>();
            state.RequireForUpdate<AnimationEventTriggerModelIndex>();
            state.RequireForUpdate<EndInitializationEntityCommandBufferSystem.Singleton>();
            _eventsLookup = state.GetBufferLookup<AnimationEventData>();
            _animationStateLookup = state.GetComponentLookup<AnimationStateData>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var ecb = SystemAPI.GetSingleton<EndInitializationEntityCommandBufferSystem.Singleton>()
                .CreateCommandBuffer(state.WorldUnmanaged).AsParallelWriter();
            var config = SystemAPI.GetSingleton<AnimationEventTriggerModelIndex>();
            var curTime = SystemAPI.GetSingleton<GameTimeData>().ElapsedTime;
            _eventsLookup.Update(ref state);
            _animationStateLookup.Update(ref state);
            state.Dependency = new UnitDeadJob
            {
                ECB = ecb,
                Config = config,
                EventsLookup = _eventsLookup,
                AnimationStateLookup = _animationStateLookup,
                CurTime = curTime,
            }.ScheduleParallel(state.Dependency);
        }

        [BurstCompile]
        [WithAll(typeof(UnitDeadTag))]
        public partial struct UnitDeadJob : IJobEntity
        {
            [ReadOnly] public float CurTime;
            [ReadOnly] public AnimationEventTriggerModelIndex Config;

            // These components are only written to self
            [NativeDisableParallelForRestriction] public BufferLookup<AnimationEventData> EventsLookup;
            [NativeDisableParallelForRestriction] public ComponentLookup<AnimationStateData> AnimationStateLookup;
            public EntityCommandBuffer.ParallelWriter ECB;
            private void Execute([ChunkIndexInQuery] int index, Entity selfEntity,
                in DynamicBuffer<LinkedEntityGroup> children
            )
            {
                var modelIndex = Config.value;
                var child = children[modelIndex].Value;

                if (EventsLookup.TryGetBuffer(child,
                        out var buffer)) // When unit with dead tag raise events, it must be dead event
                {
                    ref var stateData = ref AnimationStateLookup.GetRefRW(child).ValueRW;
                    if (stateData.State != UnitAnimationState.Die)
                    {
                        stateData.State = UnitAnimationState.Die;
                        stateData.ClipAIndex = (int)UnitAnimationState.Die;
                        stateData.ClipBIndex = stateData.ClipAIndex;
                        stateData.Blending = false;
                        stateData.ClipAWeight = 1f;
                        stateData.ClipBWeight = 0f;
                        stateData.ClipAStartTime = CurTime;
                        stateData.ClipBStartTime = CurTime;
                        stateData.PlaySpeed = 1f;
                        buffer.Clear();

                        return;
                    }

                    if (buffer.Length == 0) return;
                    ECB.DestroyEntity(index, selfEntity);
                }
            }
        }
    }
}