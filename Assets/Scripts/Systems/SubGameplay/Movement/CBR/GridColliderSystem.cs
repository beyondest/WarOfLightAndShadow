using SparFlame.Components.General;
using SparFlame.Components.MainGameplay;
using SparFlame.Components.SubGameplay;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace SparFlame.Systems.SubGameplay.Movement.CBR
{
    public partial struct GridColliderSystem : ISystem
    {
        public struct EntityPos
        {
            public Entity Entity;
            public float3 Position;
        }

        private NativeParallelMultiHashMap<int3, EntityPos> _entities;
        private EntityQuery _collectPosQuery;

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<EndSimulationEntityCommandBufferSystem.Singleton>();
            state.RequireForUpdate<GridColliderConfig>();
            state.RequireForUpdate<SubGamingTag>();
            _collectPosQuery = SystemAPI.QueryBuilder().WithAll<SubGameplayGeneralAttr>().WithAll<LocalTransform>()
                .WithNone<FakeUnitTag>()
                .WithNone<UnitDeadTag>().Build();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var config = SystemAPI.GetSingleton<GridColliderConfig>();
            var count = _collectPosQuery.CalculateEntityCount();
            if (count == 0) return;
            _entities =
                new NativeParallelMultiHashMap<int3, EntityPos>(count + (int)(config.overInitRatio * count),
                    Allocator.TempJob);
            var job1 = new CollectPosJob
            {
                Map = _entities.AsParallelWriter(),
                Config = config,
            }.ScheduleParallel(state.Dependency);
            state.Dependency = new AddGridColliderTargetJob
            {
                Map = _entities,
                ECB = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>()
                    .CreateCommandBuffer(state.WorldUnmanaged).AsParallelWriter(),
                Config = config,
            }.ScheduleParallel(job1);
            _entities.Dispose(state.Dependency);
        }

        public void OnDestroy(ref SystemState state)
        {
            if (_entities.IsCreated)
                _entities.Dispose();
        }
    }

    [BurstCompile]
    [WithNone(typeof(UnitDeadTag))]
    [WithNone(typeof(FakeUnitTag))]
    [WithAll(typeof(SubGameplayGeneralAttr))]
    public partial struct CollectPosJob : IJobEntity
    {
        [NativeDisableParallelForRestriction]
        public NativeParallelMultiHashMap<int3, GridColliderSystem.EntityPos>.ParallelWriter Map;
        [ReadOnly] public GridColliderConfig Config;
        private void Execute(in LocalTransform transform, Entity selfEntity)
        {
            var cell = (int3)(transform.Position / Config.gridSize);
            Map.Add(cell, new GridColliderSystem.EntityPos
            {
                Entity = selfEntity,
                Position = transform.Position,
            });
        }
    }

    [BurstCompile]
    public partial struct AddGridColliderTargetJob : IJobEntity
    {
        public EntityCommandBuffer.ParallelWriter ECB;
        [ReadOnly] public NativeParallelMultiHashMap<int3, GridColliderSystem.EntityPos> Map;
        [ReadOnly] public GridColliderConfig Config;

        private void Execute([ChunkIndexInQuery] int index, Entity selfEntity,
            in BoxColliderSize boxColliderSize, in LocalTransform transform, ref DynamicBuffer<FakeColliderTarget> targets)
        {
            targets.Clear();
            // ECB.SetComponentEnabled<NeedTarget>(index, selfEntity, false);
            if (!targets.IsEmpty) return;
            var radius = (int)math.ceil(boxColliderSize.Radius / Config.gridSize);
            var centerCell = (int3)(transform.Position / Config.gridSize);
            // iterate neighbor cells in cubic box (you can optimize to circle cells if desired)
            for (var z = -radius; z <= radius; ++z)
            for (var y = -radius; y <= radius; ++y)
            for (var x = -radius; x <= radius; ++x)
            {
                var c = centerCell + new int3(x, y, z);
                // iterate map entries for this cell
                foreach (var target in Map.GetValuesForKey(c))
                {
                    if(target.Entity == selfEntity)continue;
                    var disSq = math.distancesq(transform.Position, target.Position);
                    if (disSq < boxColliderSize.RadiusSq)
                    {
                        targets.Add(new FakeColliderTarget
                        {
                            Entity = target.Entity,
                        });
                    }
                }
            }
        }
    }
}