using System.Collections.Generic;
using SparFlame.Components.General;
using SparFlame.Components.MainGameplay;
using SparFlame.Components.SubGameplay;
using SparFlame.Systems.General.BasicControl;
using Unity.AI.Navigation;
using Unity.Entities;
using UnityEngine;
using Unity.Collections;
using Unity.Transforms;
using UnityEngine.AI;

// ReSharper disable Unity.Entities.MustBeSurroundedWithRefRwRo

namespace SparFlame.Systems.SubGameplay.Movement
{
    public partial class VolumeObstacleSystem : SystemBase
    {
        private readonly Dictionary<Entity, (GameObject, GameObject)> _entityMap = new();
        private readonly Dictionary<Entity, GameObject> _neutralEntityMap = new();
        private Dictionary<FactionTag, GameObject> _obstacleTypePrefabMap;
        private Dictionary<FactionTag, GameObject> _volumeTypePrefabMap;
        private float _lastSyncTime;
        private float _syncTimeInterval;
        private float _allyAgentRadius;
        private float _enemyAgentRadius;
        private bool _initialized;
        
        protected override void OnCreate()
        {
            base.OnCreate();
            RequireForUpdate<SubGamingTag>();
            RequireForUpdate<VolumeObstacleSystemConfig>();
        }

        protected override void OnStartRunning()
        {
            if (!_initialized)
            {
                foreach (var config in SystemAPI.Query<VolumeObstacleSystemConfig>())
                {
                    _obstacleTypePrefabMap = config.ObstacleTypePrefabMap;
                    _volumeTypePrefabMap = config.VolumeTypePrefabMap;
                    _syncTimeInterval = config.SyncTimeInterval;
                    _allyAgentRadius = config.AllyAgentRadius;
                    _enemyAgentRadius = config.EnemyAgentRadius;
                }
                GameController.Instance.OnSwitchGameStatusForSystems += _=>
                {
                    ClearMappingWhenSwitchScene();
                };
                
                _initialized = true;
            }
          
        }
        
        protected override void OnUpdate()
        {
            var ecb = new EntityCommandBuffer(Allocator.Temp);
            var shouldUpdateAllyMesh = false;
            var shouldUpdateEnemyMesh = false;

            SpawnVolumeObstacle(ref shouldUpdateAllyMesh,  ref shouldUpdateEnemyMesh,ref ecb);

            DealDestroyVolumeObstacleRequest(ref shouldUpdateAllyMesh,  ref shouldUpdateEnemyMesh,ref ecb);

            SyncObstaclePosition(ecb, ref shouldUpdateAllyMesh,ref shouldUpdateEnemyMesh);

            HandleDoorControlRequest(ref shouldUpdateAllyMesh, ref shouldUpdateEnemyMesh, ref ecb);
            
            UpdateNavMeshSurface(shouldUpdateAllyMesh,  shouldUpdateEnemyMesh,ref ecb);
            ecb.Playback(EntityManager);
            ecb.Dispose();
        }

        private static void UpdateNavMeshSurface(bool shouldUpdateAllyMesh,  bool shouldUpdateEnemyMesh,ref EntityCommandBuffer ecb)
        {
            if (shouldUpdateAllyMesh)
            {
                var entity = ecb.CreateEntity();
                ecb.AddComponent(entity, new UpdateNavMeshRequest
                {
                    FactionTag = FactionTag.Light
                });
                ecb.AddComponent<SubGameplayEntityTag>(entity);

            }
            if (shouldUpdateEnemyMesh)
            {
                var entity2 = ecb.CreateEntity();
                ecb.AddComponent(entity2, new UpdateNavMeshRequest
                {
                    FactionTag = FactionTag.Dark
                });
                ecb.AddComponent<SubGameplayEntityTag>(entity2);

            }
        }

        private void SyncObstaclePosition(EntityCommandBuffer ecb, ref bool shouldUpdateAllyMesh , ref bool shouldUpdateEnemyMesh)
        {
            var curTime = SystemAPI.GetSingleton<GameTimeData>().ElapsedTime;
            if (!(curTime > _lastSyncTime)) return;
            _lastSyncTime = curTime + _syncTimeInterval;
            // Synchronize obstacle/volume position, only Sync non-neutral object
            foreach (var ( request, entity) in SystemAPI
                         .Query<RefRO<BuildingSyncVolumeRequest>>().WithEntityAccess())
            {
                var transform = SystemAPI.GetComponent<LocalTransform>(request.ValueRO.FromEntity);
                var (notWalkableVolume, highCostVolume) = _entityMap[request.ValueRO.FromEntity];
                notWalkableVolume.transform.position = transform.Position;
                notWalkableVolume.transform.rotation = transform.Rotation;
                highCostVolume.transform.position = transform.Position;
                highCostVolume.transform.rotation = transform.Rotation;
                shouldUpdateAllyMesh = true;
                shouldUpdateEnemyMesh = true;
                
                ecb.DestroyEntity(entity);
            }
        }

        private void DealDestroyVolumeObstacleRequest(ref bool shouldUpdateAllyMesh,ref bool shouldUpdateEnemyMesh,ref EntityCommandBuffer ecb)
        {
            // Destroy game object correspond with destroyed entity
            foreach (var (request, entity) in SystemAPI
                         .Query<RefRO<VolumeObstacleDestroyRequest>>()
                         .WithEntityAccess())
            {
                var destroyReq = request.ValueRO;
                if (destroyReq.RequestFromFaction == FactionTag.Neutral)
                {
                    var obstacle = _neutralEntityMap[destroyReq.FromEntity];
                    _neutralEntityMap.Remove(destroyReq.FromEntity);
                    Object.Destroy(obstacle);
                }
                else
                {
                    var (obstacle, volume) = _entityMap[destroyReq.FromEntity];
                    _entityMap.Remove(destroyReq.FromEntity);
                    if (volume)
                    {
                        // request from ally or enemy, then both need to update, because one is not walkable, one is high cost volume
                        shouldUpdateAllyMesh = true;
                        shouldUpdateEnemyMesh = true;
                        Object.Destroy(volume);
                    }

                    if (obstacle)
                    {
                        shouldUpdateAllyMesh = true;
                        shouldUpdateEnemyMesh = true;
                        Object.Destroy(obstacle);
                    }
                }
                ecb.DestroyEntity(entity);
            }
        }

        private void SpawnVolumeObstacle(ref bool shouldUpdateAllyMesh,ref bool shouldUpdateEnemyMesh,ref EntityCommandBuffer ecb )
        {
            // Spawn Main scene obstacle/volume so that navmesh can recognize it
            foreach (var (localTransform, request, entity) in SystemAPI
                         .Query<RefRO<LocalTransform>, RefRO<VolumeObstacleSpawnRequest>>()
                         .WithNone<CityAttr>()
                         .WithEntityAccess())
            {
                var req = request.ValueRO;
                var transform = localTransform.ValueRO;
                // If neutral, generate obstacle for all agent 
                if (req.RequestFromFaction == FactionTag.Neutral)
                {
                    var obstacle = Object.Instantiate(_obstacleTypePrefabMap[req.RequestFromFaction],
                        transform.Position,
                        transform.Rotation
                    );
                    // Since default prefab is for all factions, then don't need to change the modifier
                    var navMeshObstacle = obstacle.GetComponent<NavMeshObstacle>();
                    navMeshObstacle.size = req.Size;
                    navMeshObstacle.center = req.Center;
                    _neutralEntityMap.Add(entity, obstacle);
                }
                // Ally or Enemy
                else
                {
                    GameObject volumeNotWalkable;
                    GameObject volumeHighCost;
                    // if (!req.NotGenerateNotWalkableVolume)
                    {
                        // request from ally, then this building is ally, then this building is not walkable volume for ally
                        volumeNotWalkable = Object.Instantiate(_obstacleTypePrefabMap[req.RequestFromFaction],
                            transform.Position,
                            transform.Rotation
                        );
                        var navMeshVolume0 = volumeNotWalkable.GetComponent<NavMeshModifierVolume>();
                        var size0 = req.Size;
                        if (req.RequestFromFaction == FactionTag.Light)
                        {
                            size0.x += _allyAgentRadius; // Ally building is not walkable for ally unit and should plus radius to prevent stuck
                            size0.z += _allyAgentRadius;
                            shouldUpdateAllyMesh = true;
                        }
                        else
                        {
                            size0.x += _enemyAgentRadius;
                            size0.z += _enemyAgentRadius;
                            shouldUpdateEnemyMesh = true;
                        }
                        navMeshVolume0.size = size0;
                        navMeshVolume0.center = req.Center;
                    }

                    // if (!req.NotGenerateHighCostVolume)
                    {
                        // If request from ally, then the building is high cost volume for enemy so we use ~
                        volumeHighCost = Object.Instantiate(_volumeTypePrefabMap[~req.RequestFromFaction],
                            transform.Position,
                            transform.Rotation
                        );
                        var navMeshVolume = volumeHighCost.GetComponent<NavMeshModifierVolume>();
                        var size = req.Size;
                        // If request from ally, then this is the area high cost for enemy
                        if (~req.RequestFromFaction == FactionTag.Light)
                        {
                            size.x += _allyAgentRadius;
                            size.z += _allyAgentRadius;
                            shouldUpdateAllyMesh = true;
                        }
                        else
                        {
                            size.x += _enemyAgentRadius;
                            size.z += _enemyAgentRadius;
                            shouldUpdateEnemyMesh = true;
                        }
                        // If this is an attackable building, should mark attack range at higher cost
                        if (req.VolumeRadius != 0) 
                        {
                            size.x += req.VolumeRadius - req.Size.x;
                            size.z += req.VolumeRadius - req.Size.z;
                        }
                        navMeshVolume.size = size;
                        navMeshVolume.center = req.Center;
                        navMeshVolume.area = (int)req.VolumeAreaType;
                        
                    }
                    _entityMap.Add(entity, (volumeNotWalkable, volumeHighCost));
                }
                
                ecb.RemoveComponent<VolumeObstacleSpawnRequest>(entity);
            }
        }


        private void HandleDoorControlRequest(ref bool shouldUpdateAllyMesh, ref bool shouldUpdateEnemyMesh,
            ref EntityCommandBuffer ecb)
        {
            foreach (var doorControl in SystemAPI
                         .Query<RefRO<DoorControlRequest>>())
            {
                var (obstacle, volume) = _entityMap[doorControl.ValueRO.FromEntity];
                if (obstacle)
                {
                    obstacle.SetActive(doorControl.ValueRO.OpenOrClose);
                }
                if (volume)
                {
                    volume.SetActive(doorControl.ValueRO.OpenOrClose);
                    if(doorControl.ValueRO.RequestFromFaction == FactionTag.Light)
                        shouldUpdateAllyMesh = true;
                    if (doorControl.ValueRO.RequestFromFaction == FactionTag.Dark)
                        shouldUpdateEnemyMesh = true;
                }
            }
        }

        private void ClearMappingWhenSwitchScene()
        {
            _entityMap.Clear();
            _neutralEntityMap.Clear();
        }
    }
}