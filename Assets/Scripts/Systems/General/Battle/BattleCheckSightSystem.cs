using System;
using SparFlame.Components.General;
using SparFlame.Components.MainGameplay;
using SparFlame.Core.Utils;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;

namespace SparFlame.Systems.General.Battle
{
    public partial struct BattleCheckSightSystem : ISystem
    {
        private EntityQuery _battleCheckSightQuery;

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<PlayerFactionData>();
            state.RequireForUpdate<BattleCheckSightData>();
            _battleCheckSightQuery = SystemAPI.QueryBuilder().WithAll<BattleCheckSightData>().Build();
        }

        public void OnUpdate(ref SystemState state)
        {
            var entities = _battleCheckSightQuery.ToEntityArray(Allocator.Temp);
            var entity = entities[0];
            var targets = SystemAPI.GetBuffer<BattleCheckSightTarget>(entity);
            if (targets.Length == 0) return;

            var playerArmyGroups = new NativeList<Entity>();
            var allyArmyGroups = new NativeList<Entity>();
            var enemyArmyGroups = new NativeList<Entity>();
            var targetSubGameStatus = SubGameStatus.Encounter;
            var playerFactionData = SystemAPI.GetSingleton<PlayerFactionData>();
            var city = Entity.Null;
            foreach (var target in targets)
            {
                var generalAttr = SystemAPI.GetComponent<MainGameplayGeneralAttr>(target.Entity);
                var relationship =
                    FactionUtils.GetRelationship(playerFactionData, generalAttr.faction, generalAttr.subFaction);
                // If battle includes a city
                if (generalAttr.baseTag == MainGameBaseTag.City)
                {
                    switch (relationship)
                    {
                        case Relationship.Neutral:
                            targetSubGameStatus = SubGameStatus.Encounter;
                            break;
                        case Relationship.Ally:
                            city = target.Entity;
                            targetSubGameStatus = SubGameStatus.Support;
                            break;
                        case Relationship.Hostile:
                            city = target.Entity;
                            targetSubGameStatus = SubGameStatus.PlayerSiege;
                            break;
                        case Relationship.Player:
                            city = target.Entity;
                            targetSubGameStatus = SubGameStatus.PlayerDefend;
                            break;
                        default:
                            BurstSafe.UnexpectedEnum(relationship);
                            break;
                    }

                    continue;
                }

                // Army group
                switch (relationship)
                {
                    case Relationship.Neutral:
                        break;
                    case Relationship.Ally:
                        allyArmyGroups.Add(target.Entity);
                        break;
                    case Relationship.Hostile:
                        enemyArmyGroups.Add(target.Entity);
                        break;
                    case Relationship.Player:
                        playerArmyGroups.Add(target.Entity);
                        break;
                    default:
                        BurstSafe.UnexpectedEnum(relationship);
                        break;
                }
            }


            if (targetSubGameStatus == SubGameStatus.PlayerSiege)
            {
                var notReachedPlayerArmyGroups = new NativeList<Entity>();
                var cityFutureAttackers = SystemAPI.GetBuffer<CityFutureAttackers>(city);
                for (int i = cityFutureAttackers.Length - 1; i >= 0; i--)
                {
                    var cityFutureAttacker = cityFutureAttackers[i];
                    // Check if this army group died on the way
                    if (!SystemAPI.HasComponent<MainGameplayGeneralAttr>(cityFutureAttacker.ArmyGroup))
                    {
                        cityFutureAttackers.RemoveAt(i);
                        continue;
                    }

                    var generalAttr = SystemAPI.GetComponent<MainGameplayGeneralAttr>(cityFutureAttacker.ArmyGroup);
                    var relationShip =
                        FactionUtils.GetRelationship(playerFactionData, generalAttr.faction,
                            generalAttr.subFaction);
                    if (relationShip == Relationship.Player &&
                        !NativeContainerUtils.ContainsEq(playerArmyGroups, cityFutureAttacker.ArmyGroup))
                    {
                        notReachedPlayerArmyGroups.Add(cityFutureAttacker.ArmyGroup);
                    }
                }

                if (notReachedPlayerArmyGroups.Length != 0)
                {
                    // Enter ask window : should player siege at once or wait for other army groups to reach city?
                }
                else
                {
                    // Destroy the battle check sight
                    state.EntityManager.DestroyEntity(entity);
                    ShowPreBattleWindow(ref state, playerArmyGroups, allyArmyGroups, enemyArmyGroups, city);
                }
            }

            // else if (targetSubGameStatus == SubGameStatus.PlayerDefend)
            // {
            //     // Enemy AI may choose to wait, but that is not considered for now 
            // }
            state.EntityManager.DestroyEntity(entity);
            ShowPreBattleWindow(ref state, playerArmyGroups, allyArmyGroups, enemyArmyGroups, city);
        }

        private void ShowPreBattleWindow(
            ref SystemState state,
            NativeList<Entity> playerArmyGroups, NativeList<Entity> allyArmyGroups, NativeList<Entity> enemyArmyGroups,
            Entity city)
        {
            
            
        }


        [BurstCompile]
        public void OnDestroy(ref SystemState state)
        {
        }
    }
}