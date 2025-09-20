using System;
using System.Collections.Generic;
using SparFlame.Components.General;
using SparFlame.Components.MainGameplay;
using SparFlame.Components.SubGameplay;
using SparFlame.Database;
using Unity.Entities;
using Unity.Physics.Authoring;
using UnityEngine;
using Random = Unity.Mathematics.Random;

namespace GamePlaySystem.Functionality.MainGameplay.City
{
    public class CityAuthoring : MonoBehaviour
    {
        public int globalIdx;
        public List<LoadingGridInfo> nineGridInfos = new();

        private class CityAuthoringBaker : Baker<CityAuthoring>
        {
            public override void Bake(CityAuthoring authoring)
            {
                if (authoring.globalIdx <= 0) return;
                var items = DatabaseManager.CityDatabaseSo.items;
                var entity = GetEntity(TransformUsageFlags.Dynamic);
                var item = items[authoring.globalIdx - DatabaseManager.CityDatabaseSo.idStart];
                // General
                AddComponent<GlobalSingleId>(entity);
                AddComponent<AssignGlobalSingleIDRequest>(entity);
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
                    lightModelIndex = item.lightModelIndex,
                    darkModelIndex = item.darkModelIndex,
                });
                AddComponent<CityNeedInitModelTag>(entity);


                AddComponent(entity, new BoxColliderSize
                {
                    Value = item.prefab.GetComponent<PhysicsShapeAuthoring>().m_PrimitiveSize
                });

                // City hp regeneration timer
                AddComponent(entity, new HpRegenerateTimer());

                // Map info
                AddComponent(entity, new MapInfo
                {
                    CameraMaxCoordinate = item.camMaxCoordinate,
                    CameraMinCoordinate = item.camMinCoordinate,
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

                // Enemy AI
                // Defend and attack army groups
                var defendBuffer = AddBuffer<DefendArmyGroupPrefab>(entity);
                var attackBuffer = AddBuffer<AttackArmyGroupPrefab>(entity);
                foreach (var prefabData in item.defendArmyGroupPrefabs)
                {
                    defendBuffer.Add(new DefendArmyGroupPrefab
                    {
                        ArmyGroupPrefab = GetEntity(prefabData.prefab, TransformUsageFlags.Dynamic),
                        NeedHours = prefabData.conjureTotalHours
                    });
                }
                foreach (var prefabData in item.attackArmyGroupPrefabs)
                {
                    attackBuffer.Add(new AttackArmyGroupPrefab
                    {
                        ArmyGroupPrefab = GetEntity(prefabData.prefab, TransformUsageFlags.Dynamic),
                        NeedHours = prefabData.conjureTotalHours
                    });
                }
                AddBuffer<ExtraArmyGroup>(entity);
                AddBuffer<AttackArmyGroup>(entity);
                AddBuffer<DefendArmyGroup>(entity);
                AddBuffer<InvadingArmyGroup>(entity);
                // Check city list
                var checkCityBuffer = AddBuffer<CheckCity>(entity);
                foreach (var checkCityId in item.checkCityIds)
                {
                    checkCityBuffer.Add(new CheckCity
                    {
                        CityId = checkCityId,
                    });
                }
                
                AddComponent<FocusOnPlayerTag>(entity);
                SetComponentEnabled<FocusOnPlayerTag>(entity, item.isFocusOnPlayerAtBeginning);
                AddBuffer<InvadeTarget>(entity);
                if(item.isSupportCity)
                    AddComponent<SupportFightTag>(entity);
                AddBuffer<ArmyGroupConjureStack>(entity);
                AddComponent(entity, new CityAIData
                {
                    Strategy = item.strategy,
                    StartConjuringTotalHours = 0f,
                    Rnd = new Random( (uint)DateTime.Now.Ticks )
                });
                
            }
        }
    }
}