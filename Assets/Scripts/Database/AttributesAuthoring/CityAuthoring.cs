using GamePlaySystem.Functionality.MainGameplay.General;
using SparFlame.Database;
using SparFlame.GamePlaySystem.General;
using Unity.Entities;
using UnityEngine;

namespace GamePlaySystem.Functionality.MainGameplay.City
{
    public class CityAuthoring : MonoBehaviour
    {
        public int globalIdx;
        private class CityAuthoringBaker : Baker<CityAuthoring>
        {
            public override void Bake(CityAuthoring authoring)
            {
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