using System;
using SparFlame.Components.General;
using SparFlame.Components.MainGameplay;
using SparFlame.Core.Utils;
using Unity.Collections;
using Unity.Entities;

namespace SparFlame.Systems.General.BasicControl.Battle
{
    public partial class InSubGameTagManageSystem : SystemBase
    {
        private bool _initialized;
        protected override void OnCreate()
        {
            RequireForUpdate<GameStatusData>();
        }


        protected override void OnStartRunning()
        {
            if (!_initialized && GameController.Instance)
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
                // Back to main world
                case SubGameStatus.None:
                    foreach (var (_,entity) in SystemAPI.Query<RefRO<InSubGameTag>>().WithEntityAccess())
                    {
                        ecb.RemoveComponent<InSubGameTag>(entity);
                    }
                    break;
                // Enter player city
                case SubGameStatus.PlayerCity:
                    // Only need to load city garrison army groups
                    var garrisonArmyGroups = SystemAPI.GetBuffer<CityGarrisonEntity>(targetSubGameStatusData.City);
                    foreach (var cityGarrisonEntity in garrisonArmyGroups)
                    {
                        var armyGroup = cityGarrisonEntity.ArmyGroup;
                        ecb.AddComponent<InSubGameTag>(armyGroup);
                    }
                    break;

                // Enter War
                case SubGameStatus.PlayerSiege:
                case SubGameStatus.PlayerDefend:
                case SubGameStatus.Encounter:
                case SubGameStatus.Support:

                    foreach (var (_, entity) in SystemAPI.Query<RefRO<BeforeBattleArmyGroupSnapShot>>().WithEntityAccess())
                    {
                        ecb.AddComponent<InSubGameTag>(entity);
                    }
                    break;
                default:
                    BurstSafe.UnexpectedEnum(targetSubGameStatusData.SubGameStatus);
                    break;
            }
            
            ecb.Playback(EntityManager);
            ecb.Dispose();
        }



    }
}