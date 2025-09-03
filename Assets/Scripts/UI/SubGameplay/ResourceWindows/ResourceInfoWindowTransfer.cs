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
                foreach (var cityEntry in cityEntries)
                {
                    datas.Add(cityEntry.resourceData);
                }
            }

            ResourceInfoWindow.Instance.UpdateDynamicData(datas);
        }
    }
}

