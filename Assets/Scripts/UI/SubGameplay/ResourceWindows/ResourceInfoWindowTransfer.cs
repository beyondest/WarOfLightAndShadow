using System;
using SparFlame.Components.General;
using SparFlame.Components.SubGameplay;
using Unity.Entities;

namespace SparFlame.UI.SubGameplay
{
    public partial class ResourceInfoWindowTransfer : SystemBase
    {
        private FactionTag _faction;

        protected override void OnCreate()
        {
            RequireForUpdate<SubGamingTag>();
            RequireForUpdate<ResourceTypeToAvailableAmount>();
            RequireForUpdate<UnitSelectionData>();
        }

        protected override void OnStartRunning()
        {
            _faction = FactionTag.Neutral;
        }

        protected override void OnUpdate()
        {
            var selectionData = SystemAPI.GetSingleton<UnitSelectionData>();
            var curFaction = selectionData.CurrentSelectFaction;
            var entity = curFaction switch
            {
                FactionTag.Light =>
                    SystemAPI.GetSingletonEntity<LightResourceDataTag>(),
                FactionTag.Dark =>
                    SystemAPI.GetSingletonEntity<DarkResourceDataTag>(),
                FactionTag.Neutral => default,
                _ => throw new ArgumentOutOfRangeException()
            };
            var datas = SystemAPI.GetBuffer<ResourceTypeToAvailableAmount>(entity);
            if (_faction != curFaction)
            {
                ResourceInfoWindow.Instance.UpdateStaticData(datas);
                _faction = curFaction;
            }

            var data = SystemAPI.GetComponent<PopulationSpecialData>(entity);
            ResourceInfoWindow.Instance.occupiedPopulationValue =data
                .OccupiedAmount;
            ResourceInfoWindow.Instance.totalAmount = data.TotalAmount;
            ResourceInfoWindow.Instance.UpdateDynamicData(datas);
        }
    }
}