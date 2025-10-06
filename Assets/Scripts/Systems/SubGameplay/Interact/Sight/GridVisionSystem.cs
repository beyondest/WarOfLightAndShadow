using SparFlame.Components.General;
using SparFlame.Components.MainGameplay;
using SparFlame.Components.SubGameplay;
using SparFlame.Core.Utils;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace SparFlame.Systems.SubGameplay.Interact
{
    public partial struct GridVisionSystem : ISystem
    {
        public struct EntityPos
        {
            public Entity Entity;
            public float3 Position;
        }

        private NativeParallelMultiHashMap<int3, EntityPos> _lightEntities;
        private NativeParallelMultiHashMap<int3, EntityPos> _darkEntities;
        private ComponentLookup<AttackAbility> _attackAbilityLookup;
        private EntityQuery _collectPosQuery;

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<EndSimulationEntityCommandBufferSystem.Singleton>();
            state.RequireForUpdate<NeedTarget>();
            state.RequireForUpdate<GridVisionConfig>();
            state.RequireForUpdate<SubGamingTag>();
            _attackAbilityLookup = state.GetComponentLookup<AttackAbility>(true);
            _collectPosQuery = SystemAPI.QueryBuilder().WithAll<SubGameplayGeneralAttr>().WithAll<LocalTransform>()
                .WithNone<FakeUnitTag>()
                .WithNone<UnitDeadTag>().Build();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var config = SystemAPI.GetSingleton<GridVisionConfig>();
            var count = _collectPosQuery.CalculateEntityCount();
            if (count == 0) return;
            _lightEntities =
                new NativeParallelMultiHashMap<int3, EntityPos>(count + (int)(config.overInitRatio * count),
                    Allocator.TempJob);
            _darkEntities =
                new NativeParallelMultiHashMap<int3, EntityPos>(count + (int)(config.overInitRatio * count),
                    Allocator.TempJob);
            var job1 = new CollectPosJob
            {
                LightEntities = _lightEntities.AsParallelWriter(),
                DarkEntities = _darkEntities.AsParallelWriter(),
                Config = config,
            }.ScheduleParallel(state.Dependency);
            _attackAbilityLookup.Update(ref state);
            state.Dependency = new AddInsightTargetJob
            {
                LightEntities = _lightEntities,
                DarkEntities = _darkEntities,
                ECB = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>()
                    .CreateCommandBuffer(state.WorldUnmanaged).AsParallelWriter(),
                Config = config,
                AttackAbilityLookup = _attackAbilityLookup,
            }.ScheduleParallel(job1);
            _lightEntities.Dispose(state.Dependency);
            _darkEntities.Dispose(state.Dependency);
        }

        public void OnDestroy(ref SystemState state)
        {
            if (_lightEntities.IsCreated)
                _lightEntities.Dispose();
            if (_darkEntities.IsCreated)
                _darkEntities.Dispose();
        }
    }

    [BurstCompile]
    [WithNone(typeof(UnitDeadTag))]
    [WithNone(typeof(FakeUnitTag))]
    public partial struct CollectPosJob : IJobEntity
    {
        [NativeDisableParallelForRestriction]
        public NativeParallelMultiHashMap<int3, GridVisionSystem.EntityPos>.ParallelWriter LightEntities;

        [NativeDisableParallelForRestriction]
        public NativeParallelMultiHashMap<int3, GridVisionSystem.EntityPos>.ParallelWriter DarkEntities;

        [ReadOnly] public GridVisionConfig Config;

        private void Execute(in LocalTransform transform, in SubGameplayGeneralAttr generalAttr, Entity selfEntity)
        {
            var cell = (int3)(transform.Position / Config.gridSize);
            if (generalAttr.Faction == FactionTag.Dark)
            {
                DarkEntities.Add(cell, new GridVisionSystem.EntityPos
                {
                    Entity = selfEntity,
                    Position = transform.Position,
                });
            }
            else if (generalAttr.Faction == FactionTag.Light)
            {
                LightEntities.Add(cell, new GridVisionSystem.EntityPos
                {
                    Entity = selfEntity,
                    Position = transform.Position,
                });
            }
        }
    }

    [BurstCompile]
    [WithAll(typeof(NeedTarget))]
    public partial struct AddInsightTargetJob : IJobEntity
    {
        public EntityCommandBuffer.ParallelWriter ECB;
        [ReadOnly] public NativeParallelMultiHashMap<int3, GridVisionSystem.EntityPos> LightEntities;
        [ReadOnly] public NativeParallelMultiHashMap<int3, GridVisionSystem.EntityPos> DarkEntities;
        [ReadOnly] public GridVisionConfig Config;
        [ReadOnly] public ComponentLookup<AttackAbility> AttackAbilityLookup;

        private void Execute([ChunkIndexInQuery] int index, Entity selfEntity, in SubGameplayGeneralAttr generalAttr,
            in SightRange sightRange, in LocalTransform transform, in DynamicBuffer<InsightTarget> targets)
        {
            ECB.SetComponentEnabled<NeedTarget>(index, selfEntity, false);
            if (!targets.IsEmpty) return;
            var canAttack = AttackAbilityLookup.HasComponent(selfEntity);
            var map = (generalAttr.Faction, canAttack) switch
            {
                (FactionTag.Dark, false) => DarkEntities,
                (FactionTag.Dark, true) => LightEntities,
                (FactionTag.Light, false) => LightEntities,
                (FactionTag.Light, true) => DarkEntities,
                _ => BurstSafe.UnexpectedEnum(generalAttr.Faction, LightEntities)
            };
            var radius = (int)math.ceil(sightRange.Dis / Config.gridSize);
            var centerCell = (int3)(transform.Position / Config.gridSize);
            // iterate neighbor cells in cubic box (you can optimize to circle cells if desired)
            for (var z = -radius; z <= radius; ++z)
            for (var y = -radius; y <= radius; ++y)
            for (var x = -radius; x <= radius; ++x)
            {
                var c = centerCell + new int3(x, y, z);
                // iterate map entries for this cell
                foreach (var target in map.GetValuesForKey(c))
                {
                    var disSq = math.distancesq(transform.Position, target.Position);
                    if (disSq < sightRange.DisSq)
                    {
                        ECB.AppendToBuffer(index, selfEntity, new InsightTarget
                        {
                            Entity = target.Entity,
                        });
                    }
                }
            }
        }
    }
}