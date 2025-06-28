using SparFlame.Components.General;
using SparFlame.Components.SubGameplay;
using SparFlame.Components.VFX;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace SparFlame.Systems.Generate
{
    public partial struct GenerateSystem : ISystem
    {
        private ComponentLookup<GeneratingTag> _generatingTagLookup;
        private ComponentLookup<HarvestAbility> _harvestAbilityLookup;

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<GameTimeData>();
            state.RequireForUpdate<EndSimulationEntityCommandBufferSystem.Singleton>();
            state.RequireForUpdate<BuildingGenerateSystemConfig>();
            state.RequireForUpdate<SubGamingTag>();
            _generatingTagLookup = state.GetComponentLookup<GeneratingTag>();
            _harvestAbilityLookup = state.GetComponentLookup<HarvestAbility>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            _generatingTagLookup.Update(ref state);
            _harvestAbilityLookup.Update(ref state);
            var config = SystemAPI.GetSingleton<BuildingGenerateSystemConfig>();
            var ecbSingleton = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>();
            var ecb = ecbSingleton.CreateCommandBuffer(state.WorldUnmanaged).AsParallelWriter();
             new ResourceMineGenerateJob
            {
                ECB = ecb,
                GeneratingTagLookup = _generatingTagLookup,
                HarvestAbilityLookup = _harvestAbilityLookup,
                Config = config,
                ElapsedTime = SystemAPI.GetSingleton<GameTimeData>().ElapsedTime
            }.ScheduleParallel();

            new PlantGenerateJob
            {
                ECB = ecb,
                Config = config,
                ElapsedTime = SystemAPI.GetSingleton<GameTimeData>().ElapsedTime,
            }.ScheduleParallel();
        }


        [BurstCompile]
        [WithNone(typeof(OocTag))]
        [WithNone(typeof(ConstructingData))]
        [WithAll(typeof(PlayerTag))]
        private partial struct PlantGenerateJob : IJobEntity
        {
            public EntityCommandBuffer.ParallelWriter ECB;
            [ReadOnly] public float ElapsedTime;
            [ReadOnly] public BuildingGenerateSystemConfig Config;

            private void Execute([ChunkIndexInQuery] int index, ref PlantGenerateAttr plantGenerateAttr, ref GenerateData data,
               in BuildingAttr buildingAttr, in SubGameplayGeneralAttr subGameplayGeneralAttr, in LocalTransform transform)
            {
                // Generate resource
                if (ElapsedTime > data.GenerateTime)
                {
                    data.GenerateTime = ElapsedTime + Config.GenerateIntervalSeconds;
                    // Generate resource
                    var request = ECB.CreateEntity(index);
                    ECB.AddComponent<SubGameplayEntityTag>(index, request);
                    ECB.AddComponent(index, request, new ResourceChangeRequest
                    {
                        Type = plantGenerateAttr.GenerateResourceType,
                        FromFaction = subGameplayGeneralAttr.Faction,
                        AbsAmount = math.abs((int)plantGenerateAttr.GenerateSpeed),
                        RequestType = ResourceRequestType.Generate
                    });
                    // Generate Pop Number VFX
                    var popNumberRequest = ECB.CreateEntity(index);
                    ECB.AddComponent<SubGameplayEntityTag>(index, popNumberRequest);
                    ECB.AddComponent(index, popNumberRequest, new PopNumberRequest
                    {
                        ColorId = subGameplayGeneralAttr.Faction == FactionTag.Light
                            ? (int)PopNumberType.LightGenerate
                            : (int)PopNumberType.DarkGenerate,
                        Position = transform.Position,
                        Scale = 1f,
                        Value = math.abs((int)plantGenerateAttr.GenerateSpeed),
                    });
                }
            }
        }
         [BurstCompile]
        [WithNone(typeof(OocTag))]
        [WithNone(typeof(ConstructingData))]
        [WithAll(typeof(PlayerTag))]
        private partial struct ResourceMineGenerateJob : IJobEntity
        {
            public EntityCommandBuffer.ParallelWriter ECB;
            [ReadOnly] public ComponentLookup<GeneratingTag> GeneratingTagLookup;
            [ReadOnly] public ComponentLookup<HarvestAbility> HarvestAbilityLookup;
            [ReadOnly] public float ElapsedTime;
            [ReadOnly] public BuildingGenerateSystemConfig Config;

            private void Execute([ChunkIndexInQuery] int index, ref ResourceMineGenerateAttr resourceMineGenerateAttr, ref GenerateData data,
                in DynamicBuffer<GarrisonEntity> entities, in BuildingAttr buildingAttr, in SubGameplayGeneralAttr subGameplayGeneralAttr,
                in LocalTransform transform,
                Entity entity)
            {
         
                // Not enough workers
                if (entities.Length < resourceMineGenerateAttr.MinCultivatorsRequireToGenerate)
                {
                    resourceMineGenerateAttr.CurGenerateSpeed = 0f;
                    if (GeneratingTagLookup.HasComponent(entity))
                        ECB.RemoveComponent<GeneratingTag>(index, entity);
                    return;
                }

                // Generating
                if (!GeneratingTagLookup.HasComponent(entity))
                    ECB.AddComponent<GeneratingTag>(index, entity);
                // Calculate speed
                var speed = 0f;
                foreach (var garrisonEntity in entities)
                {
                    var harvestAbility = HarvestAbilityLookup[garrisonEntity.Value];
                    speed += harvestAbility.Amount * harvestAbility.Speed;
                }
                resourceMineGenerateAttr.CurGenerateSpeed = speed;
                
                // Generate resource
                if (ElapsedTime > data.GenerateTime)
                {
                    data.GenerateTime = ElapsedTime + Config.GenerateIntervalSeconds;
                    // Generate resource
                    var request = ECB.CreateEntity(index);
                    ECB.AddComponent<SubGameplayEntityTag>(index, request);
                    ECB.AddComponent(index, request, new ResourceChangeRequest
                    {
                        Type = resourceMineGenerateAttr.GenerateResourceType,
                        FromFaction = subGameplayGeneralAttr.Faction,
                        AbsAmount = math.abs((int)resourceMineGenerateAttr.CurGenerateSpeed),
                        RequestType = ResourceRequestType.Generate
                    });
                    // Generate Pop Number VFX
                    var popNumberRequest = ECB.CreateEntity(index);
                    ECB.AddComponent<SubGameplayEntityTag>(index, popNumberRequest);
                    ECB.AddComponent(index, popNumberRequest, new PopNumberRequest
                    {
                        ColorId = subGameplayGeneralAttr.Faction == FactionTag.Light
                            ? (int)PopNumberType.LightGenerate
                            : (int)PopNumberType.DarkGenerate,
                        Position = transform.Position,
                        Scale = 1f,
                        Value = math.abs((int)resourceMineGenerateAttr.CurGenerateSpeed),
                    });
                }
            }
        }
    }
}