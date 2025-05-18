using System;
using SparFlame.GamePlaySystem.General;
using SparFlame.GamePlaySystem.Interact.GamePlaySystem.Functionality.Interact.Buff.Authoring;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;

namespace SparFlame.GamePlaySystem.Interact.AoeInteractBuff
{
    public partial struct AoeInteractBuffSystem : ISystem
    {
        private ComponentLookup<GeneralAttr> _generalAttrLookup;
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<EndSimulationEntityCommandBufferSystem.Singleton>();
            state.RequireForUpdate<GameTimeData>();
            state.RequireForUpdate<GamingTag>();
            state.RequireForUpdate<AoeInteractData>();
            _generalAttrLookup = state.GetComponentLookup<GeneralAttr>(true);
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            _generalAttrLookup.Update(ref state);
            var ecbSingleton = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>();
            var ecb = ecbSingleton.CreateCommandBuffer(state.WorldUnmanaged).AsParallelWriter();
            new AoeInteractJob
            {
                ECB = ecb,
                GeneralAttrLookup = _generalAttrLookup,
                CurTime = SystemAPI.GetSingleton<GameTimeData>().ElapsedTime
            }.ScheduleParallel();
        }

        [BurstCompile]
        public void OnDestroy(ref SystemState state)
        {

        }
    }
    
    [BurstCompile]
    public partial struct AoeInteractJob : IJobEntity
    {
        [ReadOnly] public ComponentLookup<GeneralAttr> GeneralAttrLookup;
        [ReadOnly] public float CurTime;
        public EntityCommandBuffer.ParallelWriter ECB;

        private void Execute([ChunkIndexInQuery]int index,ref AoeInteractData data, Entity selEntity, ref DynamicBuffer<AoeTarget> targets)
        {
            if (CurTime > data.TriggerTime)
            {
                for (var i = targets.Length - 1; i >= 0; i--)
                {
                    var target = targets[i];
                    // remove dead targets
                    if (!GeneralAttrLookup.TryGetComponent(target.Entity, out var attr))
                    {
                        targets.RemoveAt(i);
                        continue;
                    }
                    if(attr.FactionTag != data.TargetFaction)continue;
                    var request = ECB.CreateEntity(index);
                    ECB.AddComponent<GameplayEntityTag>(index,request);
                    var requestData = data.StatChangeRequest;
                    requestData.Interactee = target.Entity;
                    ECB.AddComponent(index, request,requestData);
                }
                data.CurrentTriggerCount++;
                data.TriggerTime =CurTime +  data.TriggerDuration;
            }

            if (data.CurrentTriggerCount >= data.MaxTriggerCount)
            {
                ECB.DestroyEntity(index, selEntity);
            }
        }
    }
}