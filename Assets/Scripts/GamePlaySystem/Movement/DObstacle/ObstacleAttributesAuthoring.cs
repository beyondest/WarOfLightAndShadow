using Unity.Entities;
using UnityEngine;

namespace SparFlame.GamePlaySystem.Movement
{
    public class ObstacleAttributesAuthoring : MonoBehaviour
    {
        [Tooltip("If isDynamic, then the system will synchronize the location of obstacle in main scene with the entity")]
        public bool isDynamic;
        [Tooltip("This type must be corresponding to the isDynamic flag, e.g.: Unit type must be dynamic")]
        public ObstaclePrefabType obstaclePrefabType;
        
        [Tooltip("Only work for isDynamic flag")]
        public float syncPositionInterval = 0.5f;
        class Baker : Baker<ObstacleAttributesAuthoring>
        {
            public override void Bake(ObstacleAttributesAuthoring authoring)
            {
                authoring.CheckCorresponding();
                var entity = GetEntity(TransformUsageFlags.Dynamic);
                AddComponent<ObstacleTag>(entity);
                AddComponent<ObstacleSpawnRequest>(entity);
                SetComponentEnabled<ObstacleSpawnRequest>(entity, true);
                if(authoring.isDynamic)
                    AddComponent(entity, new DynamicObstacleData
                    {
                        SyncPositionInterval = authoring.syncPositionInterval,
                    });
            }
        }


        private void CheckCorresponding()
        {
            if(obstaclePrefabType == ObstaclePrefabType.Unit && !isDynamic) 
                Debug.LogError("Obstacle Unit Prefab must be dynamic");
            if(obstaclePrefabType != ObstaclePrefabType.Unit && isDynamic)
                Debug.LogError("Only support obstacle unit prefab for dynamic");
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

    public struct ObstacleSpawnRequest : IComponentData, IEnableableComponent
    {
        
    }

    public struct ObstacleDestroyRequest : IComponentData
    {
        public Entity Entity;
    }
}