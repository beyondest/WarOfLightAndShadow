using System;
using System.Collections.Generic;
using SparFlame.Components.General;
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
        public List<int> availableGridNums = new()
        {
            0,
            1,
            2,
            3,
            4,
            5,
            6,
            7,
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
                    gridSize = item.gridSize,
                });
                
                // City available grid numbers for army group to march in
                var numBuffer = AddBuffer<CityAvailableGridNumber>(entity);
                foreach (var num in authoring.availableGridNums)
                {
                    numBuffer.Add(new CityAvailableGridNumber { value = num });
                }
                
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