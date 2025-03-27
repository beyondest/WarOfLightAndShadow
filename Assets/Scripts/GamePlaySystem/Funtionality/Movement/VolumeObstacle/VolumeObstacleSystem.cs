using System.Collections.Generic;
using SparFlame.GamePlaySystem.General;
using Unity.AI.Navigation;
using Unity.Entities;
using UnityEngine;
using Unity.Collections;
using Unity.Transforms;
using UnityEngine.AI;

// ReSharper disable Unity.Entities.MustBeSurroundedWithRefRwRo

namespace SparFlame.GamePlaySystem.Movement
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
        
        protected override void OnCreate()
        {
            base.OnCreate();
            RequireForUpdate<NotPauseTag>();
            RequireForUpdate<VolumeObstacleSystemConfig>();
        }

        protected override void OnStartRunning()
        {
            foreach (var config in SystemAPI.Query<VolumeObstacleSystemConfig>())
            {
                _obstacleTypePrefabMap = config.ObstacleTypePrefabMap;
                _volumeTypePrefabMap = config.VolumeTypePrefabMap;
                _syncTimeInterval = config.SyncTimeInterval;
                _allyAgentRadius = config.AllyAgentRadius;
                _enemyAgentRadius = config.EnemyAgentRadius;
            }
        }
        
        // TODO : Add job parallel support
        protected override void OnUpdate()
        {
            var ecb = new EntityCommandBuffer(Allocator.Temp);
            var shouldUpdateAllyMesh = false;
            var shouldUpdateEnemyMesh = false;

            SpawnVolumeObstacleInMainScene(ref shouldUpdateAllyMesh,  ref shouldUpdateEnemyMesh,ref ecb);

            DestroyVolumeObstacleInMainScene(ref shouldUpdateAllyMesh,  ref shouldUpdateEnemyMesh,ref ecb);

            SyncObstaclePosition();

            HandleDoorControlRequest(ref shouldUpdateAllyMesh, ref shouldUpdateEnemyMesh, ref ecb);
            
            UpdateNavMeshSurface(shouldUpdateAllyMesh,  shouldUpdateEnemyMesh,ref ecb);

            //Test delete destroy entity to remove obstacle
            // if (Input.GetKeyDown(KeyCode.D))
            // {
            //     foreach (var (_, entity) in SystemAPI.Query<VolumeObstacleTag>().WithEntityAccess())
            //     {
            //         var e = ecb.CreateEntity();
            //         ecb.AddComponent(e, new VolumeObstacleDestroyRequest
            //         {
            //             FromEntity = entity,
            //             RequestFromFaction = FactionTag.Ally
            //         });
            //         ecb.DestroyEntity(entity);
            //     }
            // }
            
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
                    FactionTag = FactionTag.Ally
                });
            }
            if (shouldUpdateEnemyMesh)
            {
                var entity2 = ecb.CreateEntity();
                ecb.AddComponent(entity2, new UpdateNavMeshRequest
                {
                    FactionTag = FactionTag.Enemy
                });
            }
        }

        private void SyncObstaclePosition()
        {
            var curTime = (float)SystemAPI.Time.ElapsedTime;
            if (!(curTime > _lastSyncTime)) return;
            _lastSyncTime = curTime + _syncTimeInterval;
            // Synchronize obstacle/volume position, only Sync non-neutral object
            foreach (var (localTransform, syncObstacleData, entity) in SystemAPI
                         .Query<RefRO<LocalTransform>, RefRW<SyncObstacleData>>().WithEntityAccess())
            {
                var transform = localTransform.ValueRO;
                if (curTime < syncObstacleData.ValueRW.SyncTime ) continue;
                syncObstacleData.ValueRW.SyncTime =
                    curTime + syncObstacleData.ValueRW.SyncPositionInterval;
                var (obstacle, _) = _entityMap[entity];
                obstacle.transform.position = transform.Position;
                obstacle.transform.rotation = transform.Rotation;
            }
        }

        private void DestroyVolumeObstacleInMainScene(ref bool shouldUpdateAllyMesh,ref bool shouldUpdateEnemyMesh,ref EntityCommandBuffer ecb)
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
                    if (volume != null)
                    {
                        // request from ally or enemy, then both need to update, because one is not walkable, one is high cost volume
                        shouldUpdateAllyMesh = true;
                        shouldUpdateEnemyMesh = true;
                        Object.Destroy(volume);
                    }

                    if (obstacle != null)
                    {
                        shouldUpdateAllyMesh = true;
                        shouldUpdateEnemyMesh = true;
                        Object.Destroy(obstacle);
                    }
                }
                ecb.DestroyEntity(entity);
            }
        }

        private void SpawnVolumeObstacleInMainScene(ref bool shouldUpdateAllyMesh,ref bool shouldUpdateEnemyMesh,ref EntityCommandBuffer ecb )
        {
            // Spawn Main scene obstacle/volume so that navmesh can recognize it
            foreach (var (localTransform, request, entity) in SystemAPI
                         .Query<RefRO<LocalTransform>, RefRO<VolumeObstacleSpawnRequest>>()
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
                    GameObject navMeshVolumeNotWalkable = null;
                    GameObject volume = null;
                    // Actually here generates volume not walkable, because obstacle is not walkable for everyone everytime
                    if (!req.NotGenerateObstacle)
                    {
                        // request from ally, then this building is ally, then this building is obstacle for ally
                        navMeshVolumeNotWalkable = Object.Instantiate(_obstacleTypePrefabMap[req.RequestFromFaction],
                            transform.Position,
                            transform.Rotation
                        );
                        var navMeshVolume0 = navMeshVolumeNotWalkable.GetComponent<NavMeshModifierVolume>();
                        var size0 = req.Size;
                        if (req.RequestFromFaction == FactionTag.Ally)
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

                    if (!req.NotGenerateVolume)
                    {
                        // If request from ally, then this volume should affect enemy so we use ~
                        volume = Object.Instantiate(_volumeTypePrefabMap[~req.RequestFromFaction],
                            transform.Position,
                            transform.Rotation
                        );
                        var navMeshVolume = volume.GetComponent<NavMeshModifierVolume>();
                        var size = req.Size;
                        // If request from ally, then this is the area high cost for enemy
                        if (~req.RequestFromFaction == FactionTag.Ally)
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
                        // If this is interactable building such as archer tower
                        
                    }
                    _entityMap.Add(entity, (navMeshVolumeNotWalkable, volume));
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
                if (obstacle != null)
                {
                    obstacle.SetActive(doorControl.ValueRO.OpenOrClose);
                }
                if (volume != null)
                {
                    volume.SetActive(doorControl.ValueRO.OpenOrClose);
                    if(doorControl.ValueRO.RequestFromFaction == FactionTag.Ally)
                        shouldUpdateAllyMesh = true;
                    if (doorControl.ValueRO.RequestFromFaction == FactionTag.Enemy)
                        shouldUpdateEnemyMesh = true;
                }
            }
        }
    }
}