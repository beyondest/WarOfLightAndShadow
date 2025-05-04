using System;
using SparFlame.GamePlaySystem.General;
using SparFlame.GamePlaySystem.Map.GamePlaySystem.Core.Map;
using SparFlame.GamePlaySystem.Resource;
using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace SparFlame.GamePlaySystem.RandomSpawn.GamePlaySystem.Functionality.RandomSpawn
{
    
    [UpdateInGroup(typeof(InitializationSystemGroup))]
    [UpdateAfter(typeof(MapInitializeSystem))]
    public partial struct PlayerFirstBaseSpawnSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<PlayerFactionData>();
            state.RequireForUpdate<PlayerFirstBaseEntity>();
            state.RequireForUpdate<MapInfo>();
            state.RequireForUpdate<GameStatusData>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var gameStatusData = SystemAPI.GetSingleton<GameStatusData>();
            if (gameStatusData.Value == GameStatus.Init)
            {
                var mapInfo = SystemAPI.GetSingleton<MapInfo>();
                var playerFaction = SystemAPI.GetSingleton<PlayerFactionData>();
                var playerPos = float2.zero;
                ref var rnd = ref SystemAPI.GetSingletonRW<GeneralRandom>().ValueRW;
                var data = SystemAPI.GetSingleton<PlayerFirstBaseEntity>();
                Entity basePrefab = Entity.Null;
                switch (playerFaction.Value)
                {
                    case FactionTag.Enemy:
                        playerPos = MapUtils.SampleSquareRing(mapInfo.OuterSquareSize, mapInfo.InnerSquareSize,
                            ref rnd.Rnd);
                        basePrefab = data.DarkPrefab;
                        break;
                    case FactionTag.Ally:
                    {
                        var sub = mapInfo.OuterSquareSize - mapInfo.InnerSquareSize;
                        playerPos = rnd.Rnd.NextFloat2(new float2(sub,sub), new float2(sub + mapInfo.InnerSquareSize, sub + mapInfo.InnerSquareSize));
                        basePrefab = data.LightPrefab;
                        break;
                    }
                    case FactionTag.Neutral:
                        break;
                }
                var entity = state.EntityManager.Instantiate(basePrefab);
                SystemAPI.SetComponent(entity, new LocalTransform
                {
                    Position = new float3(playerPos.x, 0f, playerPos.y),
                    Rotation = quaternion.identity,
                    Scale = 1f
                });
                
                var changeOccupiedTagRequest = state.EntityManager.CreateEntity();
                state.EntityManager.AddComponent<ChangeOccupiedTagRequest>(changeOccupiedTagRequest);
                state.EntityManager.SetComponentData(changeOccupiedTagRequest, new ChangeOccupiedTagRequest
                {
                    CrystalFaction = playerFaction.Value,
                    CrystalPos =  new float3(playerPos.x, 0f, playerPos.y),
                    IsDestroyed = false
                });
            }
        }

        [BurstCompile]
        public void OnDestroy(ref SystemState state)
        {

        }
    }
}