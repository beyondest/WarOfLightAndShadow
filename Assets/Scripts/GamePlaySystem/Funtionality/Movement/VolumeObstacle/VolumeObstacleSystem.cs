using System.Collections.Generic;
using SparFlame.GamePlaySystem.General;
using Unity.AI.Navigation;
using Unity.Entities;
using UnityEngine;
using Unity.Collections;
using Unity.Mathematics;
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
        private int _allyAgentTypeId;
        private int _enemyAgentTypeId;

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
            if (Input.GetKeyDown(KeyCode.D))
            {
                foreach (var (_, entity) in SystemAPI.Query<VolumeObstacleTag>().WithEntityAccess())
                {
                    var e = ecb.CreateEntity();
                    ecb.AddComponent(e, new VolumeObstacleDestroyRequest
                    {
                        FromEntity = entity,
                        RequestFromFaction = FactionTag.Ally
                    });
                    ecb.DestroyEntity(entity);
                }
            }
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
                if (curTime < syncObstacleData.ValueRW.SyncTime ) continue;
                syncObstacleData.ValueRW.SyncTime =
                    curTime + syncObstacleData.ValueRW.SyncPositionInterval;
                var (obstacle, _) = _entityMap[entity];
                obstacle.transform.position = localTransform.ValueRO.Position;
                obstacle.transform.rotation = localTransform.ValueRO.Rotation;
            }
        }

        private void DestroyVolumeObstacleInMainScene(ref bool shouldUpdateAllyMesh,ref bool shouldUpdateEnemyMesh,ref EntityCommandBuffer ecb)
        {
            // Destroy game object correspond with destroyed entity
            foreach (var (volumeObstacleDestroyRequest, entity) in SystemAPI
                         .Query<RefRO<VolumeObstacleDestroyRequest>>()
                         .WithEntityAccess())
            {
                if (volumeObstacleDestroyRequest.ValueRO.RequestFromFaction == FactionTag.Neutral)
                {
                    var obstacle = _neutralEntityMap[volumeObstacleDestroyRequest.ValueRO.FromEntity];
                    _neutralEntityMap.Remove(volumeObstacleDestroyRequest.ValueRO.FromEntity);
                    Object.Destroy(obstacle);
                }
                else
                {
                    var (obstacle, volume) = _entityMap[volumeObstacleDestroyRequest.ValueRO.FromEntity];
                    _entityMap.Remove(volumeObstacleDestroyRequest.ValueRO.FromEntity);
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
                // If this is neutral , such as resources
                if (request.ValueRO.RequestFromFaction == FactionTag.Neutral)
                {
                    var obstacle = Object.Instantiate(_obstacleTypePrefabMap[request.ValueRO.RequestFromFaction],
                        localTransform.ValueRO.Position,
                        localTransform.ValueRO.Rotation
                    );
                    // Since default prefab is for all factions, then don't need to change the modifier
                    var navMeshObstacle = obstacle.GetComponent<NavMeshObstacle>();
                    navMeshObstacle.size = request.ValueRO.Size;
                    navMeshObstacle.center = request.ValueRO.Center;
                    _neutralEntityMap.Add(entity, obstacle);
                }
                // Ally or Enemy
                else
                {
                    GameObject obstacle = null;
                    GameObject volume = null;
                    // Actually here generates volume not walkable, because obstacle is not walkable for everyone everytime
                    if (!request.ValueRO.NotGenerateObstacle)
                    {
                        // request from ally, then this building is ally, then this building is obstacle for ally
                        obstacle = Object.Instantiate(_obstacleTypePrefabMap[request.ValueRO.RequestFromFaction],
                            localTransform.ValueRO.Position,
                            localTransform.ValueRO.Rotation
                        );
                        var navMeshVolume0 = obstacle.GetComponent<NavMeshModifierVolume>();
                        navMeshVolume0.size = request.ValueRO.Size;
                        navMeshVolume0.center = request.ValueRO.Center;
                        if (request.ValueRO.RequestFromFaction == FactionTag.Ally)
                        {
                            shouldUpdateAllyMesh = true;
                        }
                        else
                        {
                            shouldUpdateEnemyMesh = true;
                        }
                    }

                    if (!request.ValueRO.NotGenerateVolume)
                    {
                        // If request from ally, then this volume should affect enemy so we use ~
                        volume = Object.Instantiate(_volumeTypePrefabMap[~request.ValueRO.RequestFromFaction],
                            localTransform.ValueRO.Position,
                            localTransform.ValueRO.Rotation
                        );
                        var navMeshVolume = volume.GetComponent<NavMeshModifierVolume>();
                        navMeshVolume.size = request.ValueRO.Size;
                        navMeshVolume.center = request.ValueRO.Center;
                        navMeshVolume.area = (int)request.ValueRO.VolumeAreaType;
                        // If this is interactable building such as archer tower
                        if (request.ValueRO.VolumeRadius != 0) 
                        {
                            navMeshVolume.size = new float3(request.ValueRO.VolumeRadius, request.ValueRO.Size.y, request.ValueRO.VolumeRadius);
                        }
                        // If request from ally, then this is the area high cost for enemy
                        if (request.ValueRO.RequestFromFaction == FactionTag.Ally)
                        {
                            shouldUpdateAllyMesh = true;
                        }
                        else
                        {
                            shouldUpdateEnemyMesh = true;
                        }
                    }
                    _entityMap.Add(entity, (obstacle, volume));
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