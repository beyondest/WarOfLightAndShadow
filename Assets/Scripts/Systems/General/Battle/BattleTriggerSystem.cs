using SparFlame.Components.General;
using SparFlame.Components.MainGameplay;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace SparFlame.Systems.General.Battle
{
    public partial struct BattleTriggerSystem : ISystem
    {
        private EntityQuery _requestQuery;

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<BattleTriggerConfig>();
            state.RequireForUpdate<PlayerFactionData>();
            state.RequireForUpdate<BattleTriggerRequest>();
            _requestQuery = SystemAPI.QueryBuilder().WithAll<BattleTriggerRequest>().Build();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            // If there are many requests at one time, trigger player's request first
            var findPlayerRequest = false;
            
            var entities = _requestQuery.ToEntityArray(Allocator.Temp);
            var requests = _requestQuery.ToComponentDataArray<BattleTriggerRequest>(Allocator.Temp);
            var playerFactionData = SystemAPI.GetSingleton<PlayerFactionData>();
            var request = requests[0];
            for (var i = 0; i < requests.Length; i++)
            {
                var req = requests[i];
                var generalAttr = SystemAPI.GetComponent<MainGameplayGeneralAttr>(req.Attacker);
                var relationShip =
                    FactionUtils.GetRelationship(playerFactionData.faction, playerFactionData.subFaction,
                        generalAttr.faction, generalAttr.subFaction);
                if (relationShip == Relationship.Self)
                {
                    findPlayerRequest = true;
                    var entity = entities[i];
                    request = requests[i];
                    state.EntityManager.DestroyEntity(entity);
                }
            }
            if (!findPlayerRequest)
            {
                state.EntityManager.DestroyEntity(entities[0]);
            }

            // Check if enemy army group attack support city. If so , only simulate the vfx 
            if (SystemAPI.HasComponent<SupportFightTag>(request.Defender)
                && SystemAPI.HasComponent<AITag>(request.Attacker))
            {
                state.EntityManager.AddComponent<InvadingSupportCityTag>(request.Attacker);
                return;
            }
            
            // Calculate battle center position
            var attackerPos = SystemAPI.GetComponent<LocalTransform>(request.Attacker).Position;
            var defenderPos = SystemAPI.GetComponent<LocalTransform>(request.Defender).Position;
            var targetPosition = (attackerPos + defenderPos) / 2;

            // Create battle check sight
            var sightTriggerEntity =
                state.EntityManager.Instantiate(SystemAPI.GetSingleton<BattleTriggerConfig>().BattleCheckSightPrefab);
            state.EntityManager.SetComponentData(sightTriggerEntity, new LocalTransform
            {
                Position = targetPosition,
                Rotation = quaternion.identity,
                Scale = 1f
            });

            // Create battle check sight data entity
            var sightDataEntity = state.EntityManager.CreateEntity();
            state.EntityManager.AddComponent<LocalTransform>(sightDataEntity);
            state.EntityManager.SetComponentData(sightDataEntity, new LocalTransform
            {
                Position = targetPosition,
                Rotation = quaternion.identity,
                Scale = 1f
            });
            state.EntityManager.AddBuffer<BattleCheckSightTarget>(sightDataEntity);
            state.EntityManager.AddComponent<BattleCheckSightData>(sightDataEntity);
            state.EntityManager.SetComponentData(sightDataEntity, new BattleCheckSightData
            {
                Value = sightTriggerEntity,
                TargetSubGameStatusData = new SubGameStatusData
                {
                    SubGameStatus = request.TargetSubGameStatus,
                    City = request.TargetSubGameStatus switch
                    {
                        SubGameStatus.Encounter => Entity.Null,
                        _ => request.Defender
                    }
                }
            });
            
            // Create connections
            state.EntityManager.AddComponent<BattleCheckSightTriggerBelongsTo>(sightTriggerEntity);
            state.EntityManager.SetComponentData(sightTriggerEntity, new BattleCheckSightTriggerBelongsTo
            {
                Value = sightDataEntity
            });

            
        }

        [BurstCompile]
        public void OnDestroy(ref SystemState state)
        {
        }
    }
}