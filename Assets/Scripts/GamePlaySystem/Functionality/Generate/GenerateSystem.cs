using SparFlame.GamePlaySystem.Building;
using SparFlame.GamePlaySystem.Garrison;
using SparFlame.GamePlaySystem.General;
using SparFlame.GamePlaySystem.Ooc;
using SparFlame.GamePlaySystem.Resource;
using SparFlame.GamePlaySystem.Units;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

namespace SparFlame.GamePlaySystem.Generate
{
    public partial struct GenerateSystem : ISystem
    {
        private ComponentLookup<GeneratingTag> _generatingTagLookup;
        private ComponentLookup<AttunerAttr> _attunerAttrLookup;
        
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<EndSimulationEntityCommandBufferSystem.Singleton>();
            state.RequireForUpdate<BuildingGenerateSystemConfig>();
            state.RequireForUpdate<GamingTag>();
            _generatingTagLookup = state.GetComponentLookup<GeneratingTag>();
            _attunerAttrLookup = state.GetComponentLookup<AttunerAttr>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            _generatingTagLookup.Update(ref state);
            _attunerAttrLookup.Update(ref state);
            var config = SystemAPI.GetSingleton<BuildingGenerateSystemConfig>();
            var ecbSingleton = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>();
            var ecb = ecbSingleton.CreateCommandBuffer(state.WorldUnmanaged).AsParallelWriter();
            new GenerateJob
            {
                ECB = ecb,
                GeneratingTagLookup = _generatingTagLookup,
                AttunerAttrLookup = _attunerAttrLookup,
                Config = config,
                ElapsedTime = (float)SystemAPI.Time.ElapsedTime
            }.ScheduleParallel();
        }

  

        [BurstCompile]
        [WithNone(typeof(OocTag))]
        [WithNone(typeof(ConstructingTag))]
        private partial struct GenerateJob : IJobEntity
        {
            public EntityCommandBuffer.ParallelWriter ECB;
            [ReadOnly] public ComponentLookup<GeneratingTag> GeneratingTagLookup;
            [ReadOnly] public ComponentLookup<AttunerAttr> AttunerAttrLookup;
            [ReadOnly] public float ElapsedTime;
            [ReadOnly] public BuildingGenerateSystemConfig Config;

            private void Execute([ChunkIndexInQuery] int index, ref GenerateAttr generateAttr, ref GenerateData data,
                in DynamicBuffer<GarrisonEntity> entities, in BuildingAttr buildingAttr, in GeneralAttr generalAttr,
                Entity entity)
            {
                // Not enough workers
                if (entities.Length < generateAttr.MinCultivatorsRequireToGenerate  )
                {
                    generateAttr.CurGenerateSpeed = 0f;
                    if (GeneratingTagLookup.HasComponent(entity))
                        ECB.RemoveComponent<GeneratingTag>(index, entity);
                    return;
                }
                // Generating
                if (!GeneratingTagLookup.HasComponent(entity))
                    ECB.AddComponent<GeneratingTag>(index, entity);
                // Calculate speed
                var speedMultiplier = 1f;
                foreach (var garrisonEntity in entities)
                {
                    var attunerAttr = AttunerAttrLookup[garrisonEntity.Value];
                    speedMultiplier += attunerAttr.GenerateSpeedBonus;
                }

                generateAttr.CurGenerateSpeed = math.clamp(speedMultiplier * generateAttr.GenerateInitialSpeed,
                    generateAttr.GenerateInitialSpeed, generateAttr.MaxGenerateSpeed);
                // Generate resource
                if (ElapsedTime > data.GenerateTime)
                {
                    data.GenerateTime = ElapsedTime + Config.GenerateIntervalSeconds;
                    var request = ECB.CreateEntity(index);
                    ECB.AddComponent(index,request, new ResourceChangeRequest
                    {
                        Type = generateAttr.GenerateResourceType,
                        FromFaction = generalAttr.FactionTag,
                        AbsAmount = math.abs((int)generateAttr.CurGenerateSpeed),
                        RequestType = ResourceRequestType.Generate
                    });
                }
            }
        }
    }
}