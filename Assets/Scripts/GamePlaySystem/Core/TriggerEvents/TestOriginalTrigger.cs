using SparFlame.GamePlaySystem.State;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Physics.Systems;
using UnityEngine;

namespace Unity.Physics.Stateful
{
    [UpdateInGroup(typeof(PhysicsSystemGroup))]
    [BurstCompile]
    public partial struct TestOriginalTrigger : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<SimulationSingleton>();
            state.RequireForUpdate<TestTriggerAuthoring.EnableOriginalEvent>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            new test
            {
                ElapsedTime = (float)SystemAPI.Time.ElapsedTime
            }.Schedule(SystemAPI.GetSingleton<SimulationSingleton>(), state.Dependency);
        }
    }
    [BurstCompile]
    public struct test : ITriggerEventsJob
    {
        public float ElapsedTime;
        public void Execute(TriggerEvent triggerEvent)
        {
            Debug.Log($"Time : {ElapsedTime}");
            Debug.Log($"A : {triggerEvent.EntityA}, {triggerEvent.BodyIndexA}, {triggerEvent.ColliderKeyA}");
            Debug.Log($"B : {triggerEvent.EntityB}, {triggerEvent.BodyIndexB}, {triggerEvent.ColliderKeyB}");
        }
    }

}