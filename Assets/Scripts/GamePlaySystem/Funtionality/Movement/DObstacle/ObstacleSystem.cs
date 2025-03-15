using System.Collections.Generic;
using SparFlame.GamePlaySystem.General;
using Unity.Entities;
using UnityEngine;
using Unity.Collections;
using Unity.Transforms;
using UnityEngine.AI;

namespace SparFlame.GamePlaySystem.Movement
{
    public partial class ObstacleSystem : SystemBase
    {
        private Dictionary<Entity, GameObject> _obstacleMap = new Dictionary<Entity, GameObject>();
        private Dictionary<ObstacleShapeType, GameObject> _typePrefabMap;
        private float _lastSyncTime;
        private float _syncTimeInterval;

        protected override void OnCreate()
        {
            base.OnCreate();
            RequireForUpdate<NotPauseTag>();
            RequireForUpdate<ObstacleSystemConfig>();
        }

        protected override void OnStartRunning()
        {
            base.OnStartRunning();
            foreach (var config in SystemAPI.Query<ObstacleSystemConfig>())
            {
                _typePrefabMap = config.TypePrefabMap;
                _syncTimeInterval = config.SyncTimeInterval;
            }
        }

        protected override void OnUpdate()
        {
            var ecb = new EntityCommandBuffer(Allocator.Temp);
            var curTime = SystemAPI.Time.ElapsedTime;
            // Spawn Main scene obstacle so that navmesh can recognize it
            foreach (var (localTransform, request, entity) in SystemAPI
                         .Query<RefRO<LocalTransform>, RefRO<ObstacleSpawnRequest>>()
                         .WithEntityAccess())
            {
                var go = Object.Instantiate(_typePrefabMap[request.ValueRO.ObstacleShapeType],
                    localTransform.ValueRO.Position,
                    localTransform.ValueRO.Rotation
                    );
                _obstacleMap.Add(entity, go);
                var navMeshObstacle = go.GetComponent<NavMeshObstacle>();
                navMeshObstacle.size = request.ValueRO.Size;
                navMeshObstacle.center = request.ValueRO.Center;
                ecb.RemoveComponent<ObstacleSpawnRequest>(entity);
            }


            // Destroy game object correspond with destroyed entity
            foreach (var (obstacleDestroyRequest, entity) in SystemAPI.Query<RefRO<ObstacleDestroyRequest>>()
                         .WithEntityAccess())
            {
                _obstacleMap.Remove(obstacleDestroyRequest.ValueRO.FromEntity);
                var go = _obstacleMap[obstacleDestroyRequest.ValueRO.FromEntity];
                Object.Destroy(go);
                ecb.DestroyEntity(entity);
            }


            if (_lastSyncTime > curTime) return;
            _lastSyncTime = (float)curTime + _syncTimeInterval;
            // Synchronize obstacle position
            foreach (var (localTransform, dynamicObstacleData, entity) in SystemAPI
                         .Query<RefRO<LocalTransform>, RefRW<DynamicObstacleData>>().WithEntityAccess())
            {
                if (dynamicObstacleData.ValueRW.SyncTime > curTime) return;
                dynamicObstacleData.ValueRW.SyncTime =
                    (float)curTime + dynamicObstacleData.ValueRW.SyncPositionInterval;
                var go = _obstacleMap[entity];
                go.transform.position = localTransform.ValueRO.Position;
                go.transform.rotation = localTransform.ValueRO.Rotation;
            }

            // Test delete destroy entity to remove obstacle
            // if (Input.GetKeyDown(KeyCode.D))
            // {
            //     foreach (var (_, entity) in SystemAPI.Query<ObstacleTag>().WithEntityAccess())
            //     {
            //         var e = ecb.CreateEntity();
            //         ecb.AddComponent<ObstacleDestroyRequest>(e, new ObstacleDestroyRequest
            //         {
            //             Entity = entity
            //         });
            //         ecb.DestroyEntity(entity);
            //     }
            // };
            ecb.Playback(EntityManager);
            ecb.Dispose();
        }
    }
}