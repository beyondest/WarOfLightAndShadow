using System.Collections.Generic;
using SparFlame.BootStrapper;
using SparFlame.GamePlaySystem.Building;
using SparFlame.GamePlaySystem.Interact;
using SparFlame.GamePlaySystem.General;
using SparFlame.GamePlaySystem.Resource;
using Unity.Collections;
using Unity.Entities;
using Unity.Transforms;
using UnityEngine;

namespace SparFlame.UI.SubGameplay
{
    public partial class BuildingExtensionTransfer :  SystemBase
    {
        private bool _isEventInit;
        private FactionTag _playerFaction;
        private NativeHashMap<int, ExpStaticConfig> _expDatabase;
        protected override void OnCreate()
        {
            RequireForUpdate<SubGamingTag>();
            // RequireForUpdate<ExpStaticConfig>();
        }

        protected override void OnDestroy()
        {
            if(_expDatabase.IsCreated)
                _expDatabase.Dispose();
        }

        protected override void OnStartRunning()
        {
            if (!_isEventInit)
            {
                BuildingDetailWindow.Instance.EcsRecycleTarget += RecycleBuilding;
                BuildingUpgradePopUpWindow.Instance.EcsUpgradeBuilding += UpgradeBuilding;
                BuildingDetailWindow.Instance.EcsGetExpStaticConfig += UpdateExpStaticConfig;
            }

            if (!_expDatabase.IsCreated)
            {
                _expDatabase = new NativeHashMap<int, ExpStaticConfig>(5, Allocator.Persistent);
                var buffer = SystemAPI.GetSingletonBuffer<ExpStaticConfig>();
                foreach (var config in buffer)
                {
                    _expDatabase.Add(config.GlobalIdx, config);
                }
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
            if(!SystemAPI.HasBuffer<CostList>(entity) || SystemAPI.HasComponent<LightSingleCrystalTag>(entity))
                return;
            AudioUtils.PlayAudioClip(AudioName.Recycle, SystemAPI.GetComponent<LocalTransform>(entity).Position,EntityManager);
            var buffer = SystemAPI.GetBuffer<CostList>(entity);
            var list = new NativeList<CostList>(Allocator.Temp);    
            list.AddRange(buffer.AsNativeArray());
            var scale = SystemAPI.GetSingleton<ConstructSystemConfig>().RecycleScale;
            foreach (var cost in list )
            {
                var request = EntityManager.CreateEntity();
                EntityManager.AddComponent<SubGameplayEntityTag>(request);
                EntityManager.AddComponent<ResourceChangeRequest>(request);
                EntityManager.SetComponentData(request, new ResourceChangeRequest
                {
                    FromFaction =_playerFaction,
                    Type = cost.Type,
                    AbsAmount = (int)(cost.Amount * scale),
                    RequestType = ResourceRequestType.Generate
                });
            }

            var fakeKillRequest = EntityManager.CreateEntity();
            EntityManager.AddComponent<StatChangeRequest>(fakeKillRequest);
            EntityManager.AddComponent<SubGameplayEntityTag>(fakeKillRequest);
            EntityManager.SetComponentData(fakeKillRequest, new StatChangeRequest
            {
                Type = StatChangeType.SimpleCleanUsedAsUpgrade,
                AbsAmount = 0,
                Interactee = entity,
                Interactor = Entity.Null,
                InteractorSubGameplayGeneralAttr = new SubGameplayGeneralAttr()
            });
            list.Dispose();

        }

        private void UpgradeBuilding(List<CostList> costs, Entity entity)
        {
            foreach (var cost in costs)
            {
                var request = EntityManager.CreateEntity();
                EntityManager.AddComponent<SubGameplayEntityTag>(request);
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
            EntityManager.AddComponent<SubGameplayEntityTag>(request2);
            EntityManager.AddComponent<UpgradeRequest>(request2);
            EntityManager.SetComponentData(request2,new UpgradeRequest
            {
                FromEntity = entity
            });
        }

        private void UpdateExpStaticConfig(Entity targetEntity)
        {
            var generalAttr = SystemAPI.GetComponent<SubGameplayGeneralAttr>(targetEntity);
            BuildingDetailWindow.Instance.ExpStaticConfig = _expDatabase[generalAttr.ID];
        }
    }
    
    
}