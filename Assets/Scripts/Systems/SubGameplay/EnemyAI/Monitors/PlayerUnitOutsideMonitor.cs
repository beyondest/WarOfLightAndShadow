using SparFlame.Components.ComponentUtils;
using SparFlame.Components.General;
using SparFlame.Components.SubGameplay;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace SparFlame.Systems.SubGameplay.EnemyAI
{
    public partial struct PlayerUnitOutsideMonitor : ISystem
    {
        private ComponentLookup<OutsideTag> _outsideTagLookup;
        private EntityQuery _crystalQuery;
        private NativeList<float3> _playerSideCrystalPositions;

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<PlayerFactionData>();
            state.RequireForUpdate<PlayerUnitOutsideMonitorConfig>();
            state.RequireForUpdate<EndSimulationEntityCommandBufferSystem.Singleton>();
            state.RequireForUpdate<SubGamingTag>();
            _crystalQuery = SystemAPI.QueryBuilder().WithAll<CrystalDef>().WithAll<LocalTransform>().WithAll<SubGameplayGeneralAttr>().Build();
            _outsideTagLookup = state.GetComponentLookup<OutsideTag>(true);
            _playerSideCrystalPositions = new NativeList<float3>(Allocator.Persistent);
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var ecbSingleton = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>();
            var config = SystemAPI.GetSingleton<PlayerUnitOutsideMonitorConfig>();
            var playerFactionData = SystemAPI.GetSingleton<PlayerFactionData>();
            _outsideTagLookup.Update(ref state);
            if (_crystalQuery.IsEmpty) return;
            _playerSideCrystalPositions.Clear();
            var generalAttrs = _crystalQuery.ToComponentDataArray<SubGameplayGeneralAttr>(Allocator.Temp);
            var locations = _crystalQuery.ToComponentDataArray<LocalTransform>(Allocator.Temp);
            for (var i = 0; i < generalAttrs.Length; i++)
            {
                var relationship = FactionUtils.GetRelationship(playerFactionData, generalAttrs[i].Faction, generalAttrs[i].SubFaction);
                if (relationship == Relationship.Player || relationship == Relationship.Ally)
                {
                    _playerSideCrystalPositions.Add(locations[i].Position);
                }
            }
            if (_playerSideCrystalPositions.Length == 0) return;
            new PlayerSideUnitOutsideMonitorJob
            {
                Config = config,
                ECB = ecbSingleton.CreateCommandBuffer(state.WorldUnmanaged).AsParallelWriter(),
                OutsideTagLookup = _outsideTagLookup,
                PlayerCrystalLocs = _playerSideCrystalPositions,
            }.ScheduleParallel();
        }


        [BurstCompile]
        [WithAll(typeof(UnitAttr))]
        [WithAll(typeof(PlayerTag))]
        public partial struct PlayerSideUnitOutsideMonitorJob : IJobEntity
        {
            public EntityCommandBuffer.ParallelWriter ECB;
            [ReadOnly] public NativeList<float3> PlayerCrystalLocs;
            [ReadOnly] public PlayerUnitOutsideMonitorConfig Config;
            [ReadOnly] public ComponentLookup<OutsideTag> OutsideTagLookup;

            private void Execute([ChunkIndexInQuery] int chunkIndex, in SubGameplayGeneralAttr subGameplayGeneralAttr, Entity selfEntity,
                in LocalTransform transform)
            {
                var unitHasOutsideTag = OutsideTagLookup.HasComponent(selfEntity);
                var unitPos = transform.Position;
                var minDisSq = float.MaxValue;

                foreach (var crystalLoc in PlayerCrystalLocs)
                {
                    var disSq = math.distancesq(unitPos, crystalLoc);
                    if (disSq < minDisSq)
                    {
                        minDisSq = disSq;
                    }
                }

                if (minDisSq > Config.OutSideDisThresholdSq && !unitHasOutsideTag)
                {
                    ECB.AddComponent(chunkIndex, selfEntity, new OutsideTag { OutSideDisSq = minDisSq });
                }

                if (minDisSq <= Config.OutSideDisThresholdSq && unitHasOutsideTag)
                {
                    ECB.RemoveComponent<OutsideTag>(chunkIndex, selfEntity);
                }
            }
        }

        public void OnDestroy(ref SystemState state)
        {
            if (_playerSideCrystalPositions.IsCreated)
                _playerSideCrystalPositions.Dispose();
        }
    }
}