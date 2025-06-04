using SparFlame.GamePlaySystem.General;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Transforms;

namespace SparFlame.GamePlaySystem.EnemyAI
{
    public partial struct SurroundingMonitorManageSystem : ISystem
    {
        private ComponentLookup<LocalTransform> _transformLookup;
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<MonitorPrefabData>();
            state.RequireForUpdate<PlayerFactionData>();
            state.RequireForUpdate<EndSimulationEntityCommandBufferSystem.Singleton>();
            state.RequireForUpdate<GamingTag>();
            _transformLookup = state.GetComponentLookup<LocalTransform>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            _transformLookup.Update(ref state);
            var ecbSingleton = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>();
            var ecb = ecbSingleton.CreateCommandBuffer(state.WorldUnmanaged).AsParallelWriter();
            new GenerateSurroundingMonitorJob
            {
                PlayerFaction = SystemAPI.GetSingleton<PlayerFactionData>().Value,
                TransformLookup = _transformLookup,
                ECB = ecb,
                MonitorPrefabData = SystemAPI.GetSingleton<MonitorPrefabData>()
            }.ScheduleParallel();
            new SyncMonitorJob
            {
                LocalTransformLookup = _transformLookup,
                ECB = ecb,
            }.ScheduleParallel();
            
        }



        
        [BurstCompile]
        public partial struct GenerateSurroundingMonitorJob : IJobEntity
        {
            public EntityCommandBuffer.ParallelWriter ECB;
            [NativeDisableParallelForRestriction] public ComponentLookup<LocalTransform> TransformLookup;
            [ReadOnly] public FactionTag PlayerFaction;
            [ReadOnly] public MonitorPrefabData MonitorPrefabData;

            private void Execute([ChunkIndexInQuery] int index, Entity selfEntity,in GenerateMonitorRequest request)
            {
                // Safety check
                if(!TransformLookup.TryGetComponent(request.TargetToMonitor, out var transform))return;
                
                // This should not happen, only for safety
                var prefab = PlayerFaction == FactionTag.Ally
                    ? MonitorPrefabData.LightMonitorPrefab
                    : MonitorPrefabData.DarkMonitorPrefab;
                var monitor = ECB.Instantiate(index,prefab);
                ECB.AddComponent<GameplayEntityTag>(index,monitor);
                ECB.AddComponent(index, monitor, new MonitorData
                {
                    BelongsTo = request.TargetToMonitor
                });
                ECB.SetComponent(index, monitor, transform);
                ECB.AddComponent<UnderMonitorTag>(index, request.TargetToMonitor);
                ECB.DestroyEntity(index,selfEntity);
                
            }
        }
        
        
        [BurstCompile]
        public partial struct SyncMonitorJob : IJobEntity
        {
            [NativeDisableParallelForRestriction] public ComponentLookup<LocalTransform> LocalTransformLookup;
            public EntityCommandBuffer.ParallelWriter ECB;
            private void Execute([ChunkIndexInQuery] int index, in MonitorData data, Entity entity)
            {
                // Entity dead and sight should remove
                if (!LocalTransformLookup.TryGetComponent(data.BelongsTo, out var localTransform))
                {
                    ECB.DestroyEntity(index, entity);
                    return;
                }
                ref var transform = ref LocalTransformLookup.GetRefRW(entity).ValueRW;
                transform.Position = localTransform.Position;
                transform.Rotation = localTransform.Rotation;
            }
        }
    }
}