using System.Runtime.CompilerServices;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

namespace SparFlame.GamePlaySystem.Interact
{
    public partial struct StatSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<AutoChooseTargetSystemConfig>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var autoConfig = SystemAPI.GetSingleton<AutoChooseTargetSystemConfig>();
        }

        [BurstCompile]
        public void OnDestroy(ref SystemState state)
        {
        }


        [BurstCompile]
        public partial struct CheckStatChangeRequest : IJobEntity
        {
            public EntityCommandBuffer.ParallelWriter ECB;
            
            [NativeDisableParallelForRestriction] public BufferLookup<InsightTarget> TargetListLookup;
            [NativeDisableParallelForRestriction] public ComponentLookup<StatData> StatLookup;


            [ReadOnly] public AutoChooseTargetSystemConfig Config;

            private void Execute(in StatChangeRequest request, Entity entity)
            {
                // Handle Stat Change Request. This request is destroyed other place, like pop number system
                ref var stat = ref StatLookup.GetRefRW(request.Interactee).ValueRW;
                stat.CurValue = request.InteractType == InteractType.Heal
                    ? math.min(stat.MaxValue, stat.CurValue + request.Amount)
                    : math.max(0, stat.CurValue - request.Amount);
                
                
                // Only attacker will raise statChangeValue in interactee's target list
                if (request.InteractType == InteractType.Attack && stat.CurValue > 0)
                {
                    TargetListLookup.TryGetBuffer(request.Interactee, out var targetsBuffer);
                    for (var i = 0; i < targetsBuffer.Length; i++)
                    {
                        var target = targetsBuffer[i];
                        if (target.Entity == request.Interactor)
                        {
                            var statChangeValue = CalStatChangeValue(request.Amount, ref Config);
                            target.StatChangValue += statChangeValue;
                            targetsBuffer[i] = target;
                        }
                    }
                }
            }

            private void RemoveAndSendRequest(Entity entity)
            {
                
            }
            

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            private static float CalStatChangeValue(float requestAmount, ref AutoChooseTargetSystemConfig config)
            {
                return requestAmount * config.StatValueChangeMultiplier;
            }
        }
    }
}