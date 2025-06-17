using SparFlame.GamePlaySystem.General;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Physics.Stateful;
using Unity.Physics.Systems;

namespace SparFlame.GamePlaySystem.EnemyAI
{
    [UpdateInGroup(typeof(PhysicsSystemGroup))]
    [UpdateAfter(typeof(StatefulTriggerEventBufferSystem))]
    public partial struct SurroundingTriggerSystem : ISystem
    {
        private BufferLookup<SurroundingData> _surroundingLookup;
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<SubGamingTag>();
            state.RequireForUpdate<MonitorData>();
            _surroundingLookup = state.GetBufferLookup<SurroundingData>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            _surroundingLookup.Update(ref state);
            new SurroundingMonitorTriggerJob
            {
                SurroundingDataLookup = _surroundingLookup
            }.ScheduleParallel();
        }
        
        [BurstCompile]
        public partial struct SurroundingMonitorTriggerJob : IJobEntity
        {
            [NativeDisableParallelForRestriction] public BufferLookup<SurroundingData> SurroundingDataLookup;
            private void Execute(ref DynamicBuffer<StatefulTriggerEvent> events, in MonitorData data,
                Entity entity)
            {
                // This may happen when belongs to entity is dead but the sight not been removed by sight system yet
                if (!SurroundingDataLookup.TryGetBuffer(data.BelongsTo, out var targets)) return;
                foreach (var triggerEvent in events)
                {
                    var target = triggerEvent.GetOtherEntity(entity);
                    switch (triggerEvent.State)
                    {
                        case StatefulEventState.Enter:
                            int j;
                            for ( j= 0; j < targets.Length; j++)
                            {
                                if(targets[j].Entity == target)break;
                            }
                            if (j != targets.Length) break;
                            targets.Add(new SurroundingData
                            {
                                Entity = target,
                            });
                            break;
                        case StatefulEventState.Exit:
                            for (var i = targets.Length - 1; i >= 0; i--)
                            {
                                if (targets[i].Entity == target)
                                {
                                    targets.RemoveAt(i);
                                    break;
                                }
                            }
                            break;
                        case StatefulEventState.Stay:
                            break;
                        case StatefulEventState.Undefined:
                            break;
                        default:
                            return;
                    }
                }
            }
        }
    }
}