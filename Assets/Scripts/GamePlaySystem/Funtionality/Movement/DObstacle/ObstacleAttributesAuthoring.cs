using Unity.Entities;
using UnityEngine;
using UnityEngine.AI;
using Unity.Mathematics;
using UnityEngine.Serialization;

namespace SparFlame.GamePlaySystem.Movement
{
    public class ObstacleAttributesAuthoring : MonoBehaviour
    {
        [Tooltip("If isDynamic, then the system will synchronize the location of obstacle in main scene with the entity")]
        public bool isDynamic;
        
        public ObstacleShapeType obstacleShapeType;
        
        [Tooltip("Only work for isDynamic flag")]
        public float syncPositionInterval = 0.5f;
        class Baker : Baker<ObstacleAttributesAuthoring>
        {
            public override void Bake(ObstacleAttributesAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.Dynamic);
                if (!authoring.TryGetComponent<NavMeshObstacle>(out var obstacle))
                {
                    Debug.LogError("No NavMeshObstacle found, use this authoring must include a NavMeshObstacle");
                    return;
                }
                
                AddComponent<ObstacleTag>(entity);
                AddComponent(entity, new ObstacleSpawnRequest
                {
                    Center = obstacle.center,
                    Size = obstacle.size,
                    ObstacleShapeType = authoring.obstacleShapeType
                });
                if(authoring.isDynamic)
                    AddComponent(entity, new DynamicObstacleData
                    {
                        SyncPositionInterval = authoring.syncPositionInterval,
                    });
            }
        }
    }

    public struct DynamicObstacleData : IComponentData
    {
        public float SyncPositionInterval;
        public float SyncTime;
    }
    
    
    /// <summary>
    /// TODO : Each time when you try to destroy an entity with ObstacleTag, create entity with ObstacleDestroyRequest.
    /// </summary>
    public struct ObstacleTag : IComponentData
    {
    }

    public struct ObstacleSpawnRequest : IComponentData
    {
        public float3 Center;
        public float3 Size;
        public ObstacleShapeType ObstacleShapeType;
    }

    public struct ObstacleDestroyRequest : IComponentData
    {
        public Entity FromEntity;
    }
}