using SparFlame.Components.MainGameplay;
using SparFlame.Components.SubGameplay;
using SparFlame.Database;
using Unity.Entities;
using Unity.Physics.Authoring;
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
                if(authoring.globalIdx <= 0)return;
                var items = DatabaseManager.CityDatabaseSo.items;  
                var entity = GetEntity(TransformUsageFlags.Dynamic);
                var item  = items[authoring.globalIdx - DatabaseManager.CityDatabaseSo.idStart];
                // General
                AddComponent(entity, new MainGameplayGeneralAttr
                {
                    faction = item.faction,
                    baseTag = MainGameBaseTag.City,
                    subFaction = item.subFactionTag,
                    
                });
                AddComponent(entity, new CityAttr
                {
                    globalId = authoring.globalIdx,
                    maxGarrisonCount = item.maxGarrisonArmyCount,
                    
                });
                
                
                // Garrison 
                AddBuffer<CityGarrisonEntity>(entity);
                AddBuffer<CityGarrisonTypeData>(entity);

                // Volume obstacle 
                const float volumeRadius = 0f;
                var physicsShapeAuthoring = item.prefab.GetComponent<PhysicsShapeAuthoring>();
                AddComponent<VolumeObstacleTag>(entity);
                AddComponent(entity, new VolumeObstacleSpawnRequest
                {
                    Center = physicsShapeAuthoring.m_PrimitiveCenter,
                    Size = physicsShapeAuthoring.m_PrimitiveSize,
                    VolumeRadius = volumeRadius,
                    VolumeAreaType = AreaType.NotWalkable,
                    RequestFromFaction = item.faction,
                });
                SetComponentEnabled<VolumeObstacleSpawnRequest>(entity, true);
            }
        }
    }

    
    
    
}