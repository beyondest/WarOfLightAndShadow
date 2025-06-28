using System;
using SparFlame.Components.General;
using SparFlame.Components.MainGameplay;
using Unity.Collections;
using Unity.Entities;

namespace SparFlame.Systems.General.BasicControl.Battle
{
    public partial class BattleControlSystem : SystemBase
    {
        private bool _initialized;
        protected override void OnCreate()
        {
            RequireForUpdate<GameStatusData>();
        }


        protected override void OnStartRunning()
        {
            if (!_initialized)
            {
                GameController.Instance.OnEcsDealInSubGameTag += DealInSubGameTag;
                _initialized = true;
            }
        }

        protected override void OnUpdate()
        {
            
        }
        
        
        
        private void DealInSubGameTag(SubGameStatusData targetSubGameStatusData)
        {
            var ecb = new EntityCommandBuffer(Allocator.Temp);
            switch (targetSubGameStatusData.SubGameStatus)
            {
                case SubGameStatus.None:
                    foreach (var (_,entity) in SystemAPI.Query<RefRO<InSubGameTag>>().WithEntityAccess())
                    {
                        ecb.RemoveComponent<InSubGameTag>(entity);
                    }
                    break;
                case SubGameStatus.PlayerCity:
                    // Only need to load city garrison army groups
                    var garrisonArmyGroups = SystemAPI.GetBuffer<CityGarrisonEntity>(targetSubGameStatusData.City);
                    foreach (var cityGarrisonEntity in garrisonArmyGroups)
                    {
                        var armyGroup = cityGarrisonEntity.ArmyGroup;
                        ecb.AddComponent<InSubGameTag>(armyGroup);
                    }
                    break;
                case SubGameStatus.PlayerSiege:
                    // Load enemy garrison army groups
                    var enemyGarrisonArmyGroups = SystemAPI.GetBuffer<CityGarrisonEntity>(targetSubGameStatusData.City);
                    foreach (var cityGarrisonEntity in enemyGarrisonArmyGroups)
                    {
                        var armyGroup = cityGarrisonEntity.ArmyGroup;
                        ecb.AddComponent<InSubGameTag>(armyGroup);
                    }
                    // Add all combined player army groups
                    var playerAttackers = new NativeHashSet<Entity>(5,Allocator.Temp);
                    playerAttackers.Add(targetSubGameStatusData.BattleTriggerRequest.Attacker);
                    RecursivelyAddInSightArmyGroups(ref playerAttackers, targetSubGameStatusData.BattleTriggerRequest.Attacker);
                    break;
                case SubGameStatus.PlayerDefend:
                    // Add all player garrison army groups
                    var playerGarrisonArmyGroups = SystemAPI.GetBuffer<CityGarrisonEntity>(targetSubGameStatusData.City);
                    foreach (var cityGarrisonEntity in playerGarrisonArmyGroups)
                    {
                        var armyGroup = cityGarrisonEntity.ArmyGroup;
                        ecb.AddComponent<InSubGameTag>(armyGroup);
                    }
                    // Add all combined enemy army groups
                    var enemyAttackers = new NativeHashSet<Entity>(5, Allocator.Temp);
                    enemyAttackers.Add(targetSubGameStatusData.BattleTriggerRequest.Attacker);
                    RecursivelyAddInSightArmyGroups(ref enemyAttackers, targetSubGameStatusData.BattleTriggerRequest.Attacker);
                    foreach (var entity in enemyAttackers)
                    {
                        ecb.AddComponent<InSubGameTag>(entity);
                    }
                    break;
                case SubGameStatus.Encounter:
                    var armyGroups = new NativeHashSet<Entity>(5, Allocator.Temp);
                    // Add all combined player and enemy army groups
                    armyGroups.Add(targetSubGameStatusData.BattleTriggerRequest.Attacker);
                    RecursivelyAddInSightArmyGroups(ref armyGroups, targetSubGameStatusData.BattleTriggerRequest.Attacker);
                    armyGroups.Add(targetSubGameStatusData.BattleTriggerRequest.Defender);
                    RecursivelyAddInSightArmyGroups(ref armyGroups, targetSubGameStatusData.BattleTriggerRequest.Defender);
                    foreach (var entity in armyGroups)
                    {
                        ecb.AddComponent<InSubGameTag>(entity);
                    }
                    break;
                case SubGameStatus.Support:
                    var invaders = SystemAPI.GetBuffer<SupportFightInvader>(targetSubGameStatusData.City);
                    foreach (var invader in invaders)
                    {
                        ecb.AddComponent<InSubGameTag>(invader.Entity);
                    }
                    var garrisonEntities = SystemAPI.GetBuffer<CityGarrisonEntity>(targetSubGameStatusData.City);
                    foreach (var cityGarrisonEntity in garrisonEntities)
                    {
                        var armyGroup = cityGarrisonEntity.ArmyGroup;
                        ecb.AddComponent<InSubGameTag>(armyGroup);
                    }
                    var attackers = new NativeHashSet<Entity>(5, Allocator.Temp);
                    attackers.Add(targetSubGameStatusData.BattleTriggerRequest.Attacker);
                    RecursivelyAddInSightArmyGroups(ref attackers, targetSubGameStatusData.BattleTriggerRequest.Attacker);
                    foreach (var entity in attackers)
                    {
                        ecb.AddComponent<InSubGameTag>(entity);
                    }
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
            
            ecb.Playback(EntityManager);
            ecb.Dispose();
        }

        private void RecursivelyAddInSightArmyGroups(ref NativeHashSet<Entity> armyGroups, Entity triggerArmyGroup)
        {
            var triggerFaction = SystemAPI.GetComponent<MainGameplayGeneralAttr>(triggerArmyGroup).faction;
            var inSightArmyGroups = SystemAPI.GetBuffer<ArmyGroupSightTarget>(triggerArmyGroup);
            foreach (var armyGroupSightTarget in inSightArmyGroups)
            {
                var target = armyGroupSightTarget.Entity;
                if (SystemAPI.GetComponent<MainGameplayGeneralAttr>(target).faction == triggerFaction)
                {
                    if(armyGroups.Add(target))
                        RecursivelyAddInSightArmyGroups(ref armyGroups, target);
                }
            }
        }

    }
}