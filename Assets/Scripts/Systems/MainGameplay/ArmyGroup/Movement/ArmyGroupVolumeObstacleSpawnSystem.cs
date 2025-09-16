using System.Collections.Generic;
using SparFlame.Components.General;
using SparFlame.Components.MainGameplay;
using SparFlame.Components.SubGameplay;
using Unity.AI.Navigation;
using Unity.Collections;
using Unity.Entities;
using Unity.Transforms;
using UnityEngine;

namespace SparFlame.Systems.MainGameplay.ArmyGroup
{
    public partial class ArmyGroupVolumeObstacleSpawnSystem : SystemBase
    {
        private readonly Dictionary<Entity, GameObject> _entityMap = new();
        private readonly Dictionary<FactionTag, GameObject> _obstacleTypePrefabMap = new();
        
        
        protected override void OnCreate()
        {
            RequireForUpdate<ArmyGroupVolumeObstacleConfig>();
            RequireForUpdate<UpdateCityNavMeshRequest>();
        }

        protected override void OnStartRunning()
        {
            // Init prefab dictionary
            if (_obstacleTypePrefabMap.Count == 0)
            {
                var config = SystemAPI.ManagedAPI.GetSingleton<ArmyGroupVolumeObstacleConfig>();
                _obstacleTypePrefabMap.Add(FactionTag.Light, config.lightCityObstacle);
                _obstacleTypePrefabMap.Add(FactionTag.Dark, config.darkCityObstacle);
                _obstacleTypePrefabMap.Add(FactionTag.Neutral, config.neutralCityObstacle);
            }
        }

        protected override void OnUpdate()
        {
            // This system only update when there is update city navmesh request
            SpawnVolumeObstacleInMainScene();
            EntityManager.DestroyEntity(SystemAPI.GetSingletonEntity<UpdateCityNavMeshRequest>());
        }

        private void SpawnVolumeObstacleInMainScene()
        {
            foreach (var go in _entityMap.Values)
            {
                Object.Destroy(go);
            }
            _entityMap.Clear();
            var shouldUpdateLightMesh = false;
            var shouldUpdateDarkMesh = false;
            // Spawn Main scene obstacle/volume so that navmesh can recognize it
            foreach (var (localTransform, request, entity) in SystemAPI
                         .Query<RefRO<LocalTransform>, RefRO<VolumeObstacleSpawnRequest>>()
                         .WithAll<CityAttr>()
                         .WithEntityAccess())
            {
                switch (request.ValueRO.RequestFromFaction)
                {
                    case FactionTag.Light:
                        shouldUpdateLightMesh = true;
                        break;
                    case FactionTag.Dark:
                        shouldUpdateDarkMesh = true;
                        break;
                    case FactionTag.Neutral:
                        shouldUpdateLightMesh = true;
                        shouldUpdateDarkMesh = true;
                        break;
                }
                var req = request.ValueRO;
                var transform = localTransform.ValueRO;
                var obstacle = Object.Instantiate(_obstacleTypePrefabMap[req.RequestFromFaction],
                    transform.Position,
                    transform.Rotation
                );
                var navMeshObstacle = obstacle.GetComponent<NavMeshModifierVolume>();
                navMeshObstacle.size = req.Size;
                navMeshObstacle.center = req.Center;
                _entityMap.Add(entity, obstacle);
            }
            var ecb = new EntityCommandBuffer(Allocator.Temp);
            UpdateNavMeshSurface(shouldUpdateLightMesh,  shouldUpdateDarkMesh, ecb);        
            ecb.Playback(EntityManager);
            ecb.Dispose();
        }
        private static void UpdateNavMeshSurface(bool shouldUpdateAllyMesh,  bool shouldUpdateEnemyMesh, EntityCommandBuffer ecb)
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
    }
}