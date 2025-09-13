using SparFlame.Components.General;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Physics;
using Unity.Physics.Stateful;
using Unity.Physics.Systems;

namespace SparFlame.Systems.General.Battle
{
    
    [UpdateInGroup(typeof(PhysicsSystemGroup))]
    [UpdateAfter(typeof(StatefulTriggerEventBufferSystem))]
    public partial struct BattleCheckSightTriggerSystem : ISystem
    {
        private BufferLookup<BattleCheckSightTarget> _targetLookup;
        
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<SimulationSingleton>();
            state.RequireForUpdate<BattleCheckSightDataBelongsTo>();
            state.RequireForUpdate<GameStatusData>();
            _targetLookup = state.GetBufferLookup<BattleCheckSightTarget>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var gameStatusData = SystemAPI.GetSingleton<GameStatusData>();
            if(gameStatusData.Value != GameStatus.MainGaming && gameStatusData.Value != GameStatus.SubGaming)return;
            
            
            _targetLookup.Update(ref state);
            new BattleCheckSightTriggerJob
            {
                TargetLookup = _targetLookup
            }.ScheduleParallel();
        }

        [BurstCompile]
        public partial struct BattleCheckSightTriggerJob : IJobEntity
        {
            [NativeDisableParallelForRestriction] public BufferLookup<BattleCheckSightTarget> TargetLookup;
            private void Execute(ref DynamicBuffer<StatefulTriggerEvent> events, in BattleCheckSightDataBelongsTo triggerDataBelongsTo,
                Entity entity)
            {
                // This may happen when belongs to entity is dead but the sight not been removed by sight system yet
                if (!TargetLookup.TryGetBuffer(triggerDataBelongsTo.Value, out var targets)) return;
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
                            targets.Add(new BattleCheckSightTarget
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