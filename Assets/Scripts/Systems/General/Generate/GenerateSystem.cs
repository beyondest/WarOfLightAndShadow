using SparFlame.Components.General;
using SparFlame.Components.SubGameplay;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
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
            state.RequireForUpdate<SubGameStatusData>();
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
            // var config = SystemAPI.GetSingleton<BuildingGenerateSystemConfig>();
            var ecbSingleton = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>();
            var ecb = ecbSingleton.CreateCommandBuffer(state.WorldUnmanaged).AsParallelWriter();
            new ResourceMineGenerateJob
            {
                ECB = ecb,
                GeneratingTagLookup = _generatingTagLookup,
                City = SystemAPI.GetSingleton<SubGameStatusData>().City
            }.ScheduleParallel();
        }


        [BurstCompile]
        [WithNone(typeof(ConstructingTimer))]
        [WithAll(typeof(PlayerTag))]
        private partial struct ResourceMineGenerateJob : IJobEntity
        {
            public EntityCommandBuffer.ParallelWriter ECB;

            [ReadOnly] public ComponentLookup<GeneratingTag> GeneratingTagLookup;

            [ReadOnly] public Entity City;

            private void Execute([ChunkIndexInQuery] int index, ref ResourceMineGenerateAttr resourceMineGenerateAttr,
                in DynamicBuffer<GarrisonEntity> entities, in BuildingAttr buildingAttr,
                in SubGameplayGeneralAttr subGameplayGeneralAttr,
                in LocalTransform transform,
                Entity entity)
            {
                // Not enough workers, decrease city generate speed if it has been already added.
                if (entities.Length < resourceMineGenerateAttr.MinCultivatorsRequireToGenerate)
                {
                    resourceMineGenerateAttr.GenerateSpeedHoursPerUnit = 0f;
                    if (GeneratingTagLookup.HasComponent(entity))
                    {
                        ECB.RemoveComponent<GeneratingTag>(index, entity);
                        var decreaseGenerateSpeedRequest = ECB.CreateEntity(index);
                        ECB.AddComponent<SubGameplayEntityTag>(index, decreaseGenerateSpeedRequest);
                        ECB.AddComponent(index, decreaseGenerateSpeedRequest, new ResourceChangeRequest
                        {
                            ResourceType = resourceMineGenerateAttr.GenerateResourceType,
                            RequestType = ResourceRequestType.DecreaseGenerateSpeedForResourceMine,
                            City = City,
                            HoursPerUnit = resourceMineGenerateAttr.GenerateSpeedHoursPerUnit
                        });
                    }
                    return;
                }

                // Increase city generate speed if it has not been added yet.
                if (!GeneratingTagLookup.HasComponent(entity))
                {
                    ECB.AddComponent<GeneratingTag>(index, entity);
                    var decreaseGenerateSpeedRequest = ECB.CreateEntity(index);
                    ECB.AddComponent<SubGameplayEntityTag>(index, decreaseGenerateSpeedRequest);
                    ECB.AddComponent(index, decreaseGenerateSpeedRequest, new ResourceChangeRequest
                    {
                        ResourceType = resourceMineGenerateAttr.GenerateResourceType,
                        RequestType = ResourceRequestType.DecreaseGenerateSpeedForResourceMine,
                        City = City,
                        HoursPerUnit = resourceMineGenerateAttr.GenerateSpeedHoursPerUnit
                    });
                }
            }
        }
    }
}