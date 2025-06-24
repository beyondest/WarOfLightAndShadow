using SparFlame.Components.MainGameplay;
using SparFlame.Database;
using Unity.Entities;
using UnityEngine;

namespace GamePlaySystem.Functionality.MainGameplay.City
{
    public class CityAuthoring : MonoBehaviour
    {
        public int globalIdx = -1;
        private class CityAuthoringBaker : Baker<CityAuthoring>
        {
            public override void Bake(CityAuthoring authoring)
            {
                if(authoring.globalIdx == -1)return;
                var items = DatabaseManager.CityDatabaseSo.items;  
                var entity = GetEntity(TransformUsageFlags.Dynamic);
                var item  = items[authoring.globalIdx];
                // General
                AddComponent(entity, new MainGameplayGeneralAttr
                {
                    Faction = item.faction,
                    BaseTag = MainGameBaseTag.City,
                    SubFaction = item.subFaction,
                    
                });
                AddComponent(entity, new CityAttr
                {
                    ID = authoring.globalIdx,
                    MaxGarrisonCount = item.maxGarrisonArmyCount,
                    
                });
                
                // Garrison 
                AddBuffer<CityGarrisonEntity>(entity);
                AddBuffer<CityGarrisonTypeData>(entity);

            }
        }
    }

    
    
    
}