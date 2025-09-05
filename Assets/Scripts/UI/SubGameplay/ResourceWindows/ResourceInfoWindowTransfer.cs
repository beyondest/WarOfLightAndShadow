using System.Collections.Generic;
using System.Linq;
using SparFlame.Components.General;
using SparFlame.Components.MainGameplay;
using SparFlame.Components.SubGameplay;
using Unity.Entities;
// ReSharper disable ForeachCanBeConvertedToQueryUsingAnotherGetEnumerator

namespace SparFlame.UI.SubGameplay
{
    public partial class ResourceInfoWindowTransfer : SystemBase
    {

        protected override void OnCreate()
        {
            RequireForUpdate<GameStatusData>();
            RequireForUpdate<ResourceData>();
            RequireForUpdate<UnitSelectionData>();
            RequireForUpdate<PlayerFactionData>();
        }

  
        

        protected override void OnUpdate()
        {
            var gameStatusData = SystemAPI.GetSingleton<GameStatusData>();
         
            var generalResourceDatas = SystemAPI.GetSingletonBuffer<ResourceData>();
            var populationResourceData = SystemAPI.GetSingleton<PopulationResourceData>();
            var datas = new List<ResourceData>();
            if (gameStatusData.Value == GameStatus.Init)
            {
                foreach (var resourceData in generalResourceDatas)
                {
                    datas.Add(resourceData);
                }
                ResourceInfoWindow.Instance.UpdateStaticData(datas);
            }
            if(gameStatusData.Value != GameStatus.MainGaming && gameStatusData.Value != GameStatus.SubGaming )return;

            if (gameStatusData.Value == GameStatus.MainGaming)
            {
                foreach (var resourceData in generalResourceDatas)
                {
                    datas.Add(resourceData);
                }
            }
            else
            {
                var subGameStatusData = SystemAPI.GetSingleton<SubGameStatusData>();
                var cityEntries = SystemAPI.GetBuffer<CityResourceEntry>(subGameStatusData.City);
                for (var i = 0; i < cityEntries.Length; i++)
                {
                    var cityEntry = cityEntries[i];
                    datas.Add(cityEntry.resourceData.resourceType is ResourceType.Mana or ResourceType.Crystal
                        ? cityEntry.resourceData
                        : generalResourceDatas[i]);
                }
            }

            ResourceInfoWindow.Instance.UpdateDynamicData(datas, populationResourceData);
        }
    }
}

