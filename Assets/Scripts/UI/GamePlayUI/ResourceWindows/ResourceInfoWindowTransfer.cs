using System;
using SparFlame.GamePlaySystem.General;
using SparFlame.GamePlaySystem.Resource;
using SparFlame.GamePlaySystem.UnitSelection;
using Unity.Entities;

namespace SparFlame.UI.GamePlay
{
    public partial class ResourceInfoWindowTransfer : SystemBase
    {
        private FactionTag _faction;

        protected override void OnCreate()
        {
            RequireForUpdate<NotPauseTag>();
            RequireForUpdate<ResourceData>();
            RequireForUpdate<UnitSelectionData>();
        }

        protected override void OnStartRunning()
        {
            _faction = FactionTag.Neutral;
        }

        protected override void OnUpdate()
        {
            if (ResourceInfoWindow.Instance == null || !ResourceInfoWindow.Instance.IsResourceLoaded()
                                                    || BasicResourceManager.Instance == null
                                                    || !BasicResourceManager.Instance.IsResourceLoaded()) return;
            var selectionData = SystemAPI.GetSingleton<UnitSelectionData>();
            var curFaction = selectionData.CurrentSelectFaction;
            var entity = curFaction switch
            {
                FactionTag.Ally =>
                    SystemAPI.GetSingletonEntity<AllyResourceDataTag>(),
                FactionTag.Enemy =>
                    SystemAPI.GetSingletonEntity<EnemyResourceDataTag>(),
                FactionTag.Neutral => default,
                _ => throw new ArgumentOutOfRangeException()
            };
            var datas = SystemAPI.GetBuffer<ResourceData>(entity);
            if (_faction != curFaction)
            {
                ResourceInfoWindow.Instance.UpdateStaticData(datas);
                _faction = curFaction;
            }

            ResourceInfoWindow.Instance.occupiedPopulationValue =
                SystemAPI.GetComponent<PopulationOccupiedData>(entity).Value;

            ResourceInfoWindow.Instance.UpdateDynamicData(datas);
        }
    }
}