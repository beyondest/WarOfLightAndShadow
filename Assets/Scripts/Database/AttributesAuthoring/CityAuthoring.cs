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
                if(DatabaseManager.CityDatabaseSo == null)return;
                var items = DatabaseManager.CityDatabaseSo.items;  
                var entity = GetEntity(TransformUsageFlags.Dynamic);
                var item  = items[authoring.globalIdx];
                AddComponent(entity, new MainGameplayGeneralAttr
                {
                    Faction = item.faction,
                    BaseTag = MainGameBaseTag.City
                });
                AddComponent(entity, new CityAttr
                {
                    ID = authoring.globalIdx,
                });
                
            }
        }
    }

    
    
    
}