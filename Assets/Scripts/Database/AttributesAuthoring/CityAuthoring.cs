using System;
using System.Collections.Generic;
using SparFlame.Components.General;
using SparFlame.Components.MainGameplay;
using SparFlame.Components.SubGameplay;
using SparFlame.Database;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics.Authoring;
using UnityEngine;

namespace GamePlaySystem.Functionality.MainGameplay.City
{
    public class CityAuthoring : MonoBehaviour
    {
        public int globalIdx;
        public List<LoadingGridInfo> nineGridInfos = new()
        {
           new LoadingGridInfo(),//0
           new LoadingGridInfo(),
           new LoadingGridInfo(),
           new LoadingGridInfo(),
           new LoadingGridInfo(),
           new LoadingGridInfo(),
           new LoadingGridInfo(),
           new LoadingGridInfo(),//7
        };
        
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
                AddComponent(entity, new BoxColliderSize
                {
                    Value = item.prefab.GetComponent<PhysicsShapeAuthoring>().m_PrimitiveSize
                });
                
                // City available grid numbers for army group to march in
                var loadingGridInfos = AddBuffer<LoadingGridInfo>(entity);
                foreach (var info in authoring.nineGridInfos)
                {
                    loadingGridInfos.Add(info);
                }
                
                // City future attackers
                AddBuffer<CityFutureInvaders>(entity);
                
                // City Tasks and Resources
                AddBuffer<CityTask>(entity);
                var resourceDatas = AddBuffer<CityResourceEntry>(entity);
                foreach (ResourceType type in Enum.GetValues(typeof(ResourceType)))
                {
                    var init = false;
                    foreach (var resourceData in item.initResources)
                    {
                        if (resourceData.resourceType == type)
                        {
                            init = true;
                            resourceDatas.Add(new CityResourceEntry
                            {
                                accumulatedHours = 0,
                                resourceData = resourceData,
                            });
                            break;
                        }
                    }
                    if (!init)
                    {
                        resourceDatas.Add(new CityResourceEntry
                        {
                            accumulatedHours = 0,
                            resourceData = new ResourceData
                            {
                                resourceType = type,
                                storage = 0,
                                availableAmount = 0,
                                amountPerHour = 0,
                            }
                        });
                    }
                }                
                
                // Garrison 
                AddBuffer<CityGarrisonEntity>(entity);

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