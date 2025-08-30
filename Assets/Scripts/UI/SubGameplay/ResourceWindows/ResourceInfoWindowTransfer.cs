using System;
using SparFlame.Components.General;
using SparFlame.Components.SubGameplay;
using Unity.Entities;

namespace SparFlame.UI.SubGameplay
{
    public partial class ResourceInfoWindowTransfer : SystemBase
    {

        protected override void OnCreate()
        {
            RequireForUpdate<GameStatusData>();
            RequireForUpdate<ResourceTypeToAvailableAmount>();
            RequireForUpdate<UnitSelectionData>();
            RequireForUpdate<PlayerFactionData>();
        }

  
        

        protected override void OnUpdate()
        {
            var gameStatusData = SystemAPI.GetSingleton<GameStatusData>();
            var playerFaction = SystemAPI.GetSingleton<PlayerFactionData>().faction;
            var entity = playerFaction switch
            {
                FactionTag.Light =>
                    SystemAPI.GetSingletonEntity<LightResourceDataTag>(),
                FactionTag.Dark =>
                    SystemAPI.GetSingletonEntity<DarkResourceDataTag>(),
                FactionTag.Neutral => default,
                _ => throw new ArgumentOutOfRangeException()
            };
            var datas = SystemAPI.GetBuffer<ResourceTypeToAvailableAmount>(entity);
            if (gameStatusData.Value == GameStatus.Init)
            {
                ResourceInfoWindow.Instance.UpdateStaticData(datas);
            }
            if(gameStatusData.Value != GameStatus.MainGaming && gameStatusData.Value != GameStatus.SubGaming )return;
            
            var data = SystemAPI.GetComponent<PopulationSpecialData>(entity);
            ResourceInfoWindow.Instance.occupiedPopulationValue =data
                .OccupiedAmount;
            ResourceInfoWindow.Instance.totalAmount = data.TotalAmount;
            ResourceInfoWindow.Instance.UpdateDynamicData(datas);
        }
    }
}

