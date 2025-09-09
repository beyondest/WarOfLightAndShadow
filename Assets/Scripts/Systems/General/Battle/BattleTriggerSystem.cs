using SparFlame.Components.General;
using SparFlame.Components.MainGameplay;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;

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
            for (var i = 0; i < requests.Length; i++)
            {
                var request = requests[i];
                var generalAttr = SystemAPI.GetComponent<MainGameplayGeneralAttr>(request.Attacker);
                var relationShip =
                    FactionUtils.GetRelationship(playerFactionData, generalAttr.faction, generalAttr.subFaction);
                if (relationShip == Relationship.Player)
                {
                    findPlayerRequest = true;
                    var entity = entities[i];
                    state.EntityManager.DestroyEntity(entity);
                }
            }

            if (!findPlayerRequest)
            {
                state.EntityManager.DestroyEntity(entities[0]);
            }

            // Create battle check sight
            state.EntityManager.Instantiate(SystemAPI.GetSingleton<BattleTriggerConfig>().BattleCheckSightPrefab);
            

        }

        [BurstCompile]
        public void OnDestroy(ref SystemState state)
        {
            
        }
    }
}