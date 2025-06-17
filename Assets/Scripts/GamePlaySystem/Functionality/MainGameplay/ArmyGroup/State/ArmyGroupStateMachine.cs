using System;
using GamePlaySystem.Functionality.MainGameplay.General;
using GamePlaySystem.Functionality.MainGameplay.War;
using SparFlame.GamePlaySystem.General;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace SparFlame.GamePlaySystem.Functionality.MainGameplay.ArmyGroup
{
    public partial struct ArmyGroupStateMachine : ISystem
    {
        private ComponentLookup<MainGameplayGeneralAttr> _generalAttrLookup;
        private ComponentLookup<LocalTransform> _localTransformLookup;

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<EndSimulationEntityCommandBufferSystem.Singleton>();
            state.RequireForUpdate<MainGamingTag>();
            state.RequireForUpdate<ArmyGroupSightTarget>();
            _generalAttrLookup = state.GetComponentLookup<MainGameplayGeneralAttr>(true);
            _localTransformLookup = state.GetComponentLookup<LocalTransform>(true);
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            _generalAttrLookup.Update(ref state);
            _localTransformLookup.Update(ref state);
            new ArmyGroupStateMachineJob
            {
                TransformLookup = _localTransformLookup,
                GeneralAttrLookup = _generalAttrLookup,
                ECB = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>()
                    .CreateCommandBuffer(state.WorldUnmanaged).AsParallelWriter(),
            }.ScheduleParallel();
        }


        [BurstCompile]
        public partial struct ArmyGroupStateMachineJob : IJobEntity
        {
            public EntityCommandBuffer.ParallelWriter ECB;
            [ReadOnly] public ComponentLookup<MainGameplayGeneralAttr> GeneralAttrLookup;
            [ReadOnly] public ComponentLookup<LocalTransform> TransformLookup;

            private void Execute([ChunkIndexInQuery] int index, ref DynamicBuffer<ArmyGroupSightTarget> targets,
                Entity selfEntity)
            {
                if (targets.IsEmpty) return;
                var finalTarget = targets[0].Entity;
                if (targets.Length > 1)
                {
                    var minDisSq = float.MaxValue;
                    for (int i = 0; i < targets.Length; i++)
                    {
                        var target = targets[i].Entity;
                        var dis = math.distancesq(TransformLookup[target].Position,
                            TransformLookup[selfEntity].Position);
                        if (dis < minDisSq)
                        {
                            minDisSq = dis;
                            finalTarget = target;
                        }
                    }
                }

                var targetGeneralAttr = GeneralAttrLookup[finalTarget];
                var warRequest = ECB.CreateEntity(index);
                ECB.AddComponent<MainGameplayEntityTag>(index, warRequest);
                ECB.AddComponent(index, warRequest, new WarTriggerRequest
                {
                    Attacker = selfEntity,
                    Defender = finalTarget,
                    Type = targetGeneralAttr.BaseTag == MainGameBaseTag.City ? WarType.Siege : WarType.Encounter
                });
                targets.Clear();
            }
        }
    }
}