using System.Collections.Generic;
using SparFlame.GamePlaySystem.Building;
using SparFlame.GamePlaySystem.Interact;
using SparFlame.GamePlaySystem.General;
using SparFlame.GamePlaySystem.Resource;
using Unity.Entities;

namespace SparFlame.UI.GamePlay
{
    public partial class BuildingExtensionTransfer :  SystemBase
    {
        private bool _isEventInit;
        private FactionTag _playerFaction;
        protected override void OnCreate()
        {
            RequireForUpdate<GamingTag>();
        }

        protected override void OnStartRunning()
        {
            if (!_isEventInit)
            {
                BuildingDetailWindow.Instance.EcsRecycleTarget += RecycleBuilding;
                BuildingUpgradePopUpWindow.Instance.EcsUpgradeBuilding += UpgradeBuilding;
            }

            _playerFaction = SystemAPI.GetSingleton<PlayerFactionData>().Value;

        }

        protected override void OnUpdate()
        {
            var entity = _playerFaction == FactionTag.Ally
                ? SystemAPI.GetSingletonEntity<AllyResourceDataTag>()
                : SystemAPI.GetSingletonEntity<EnemyResourceDataTag>();
            var playerResources = SystemAPI.GetBuffer<ResourceTypeToAvailableAmount>(entity);
            BuildingDetailWindow.Instance.UpDatePlayerGlobalResourceData(playerResources);
        }

     

        private void RecycleBuilding(Entity entity)
        {
            if(!SystemAPI.HasBuffer<CostList>(entity))
                return;
            var buffer = SystemAPI.GetBuffer<CostList>(entity);
            var scale = SystemAPI.GetSingleton<ConstructSystemConfig>().RecycleScale;
            foreach (var cost in buffer )
            {
                var request = EntityManager.CreateEntity();
                EntityManager.AddComponent<GameplayEntityTag>(request);
                EntityManager.AddComponent<ResourceChangeRequest>(request);
                EntityManager.SetComponentData(request, new ResourceChangeRequest
                {
                    FromFaction =_playerFaction,
                    Type = cost.Type,
                    AbsAmount = (int)(cost.Amount * scale),
                    RequestType = ResourceRequestType.Generate
                });
            }
        }

        private void UpgradeBuilding(List<CostList> costs, Entity entity)
        {
            foreach (var cost in costs)
            {
                var request = EntityManager.CreateEntity();
                EntityManager.AddComponent<GameplayEntityTag>(request);
                EntityManager.AddComponent<ResourceChangeRequest>(request);
                EntityManager.SetComponentData(request, new ResourceChangeRequest
                {
                    FromFaction =_playerFaction,
                    Type = cost.Type,
                    AbsAmount = cost.Amount,
                    RequestType = ResourceRequestType.Consume
                });
            }
            var request2 = EntityManager.CreateEntity();
            EntityManager.AddComponent<GameplayEntityTag>(request2);
            EntityManager.AddComponent<UpgradeRequest>(request2);
            EntityManager.SetComponentData(request2,new UpgradeRequest
            {
                FromEntity = entity
            });
            
        }
    }
    
    
}