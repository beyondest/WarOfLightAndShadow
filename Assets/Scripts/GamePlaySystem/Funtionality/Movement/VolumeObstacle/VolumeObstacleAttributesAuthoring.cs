using Unity.Entities;
using UnityEngine;
using Unity.Mathematics;
using SparFlame.GamePlaySystem.General;
using SparFlame.GamePlaySystem.Building;
using SparFlame.GamePlaySystem.Resource;
using UnityEngine.AI;
using UnityEngine.Serialization;

namespace SparFlame.GamePlaySystem.Movement
{
    public class ObstacleAttributesAuthoring : MonoBehaviour
    {
        [Tooltip("If isDynamic, then the system will synchronize the location of obstacle in main scene with the entity")]
        public bool isObstacleDynamic;
        public bool notGenerateObstacle;
        public bool notGenerateVolume;
        
 
        
        [Tooltip("Only work for isDynamic flag")]
        public float syncPositionInterval = 0.5f;
        class Baker : Baker<ObstacleAttributesAuthoring>
        {
           
            public override void Bake(ObstacleAttributesAuthoring authoring)
            {
                float volumeRadius = 0f;
                AreaType areaType = AreaType.Walkable;
                
                
                if ((authoring.isObstacleDynamic && authoring.notGenerateObstacle) 
                    ||(authoring.notGenerateObstacle && authoring.notGenerateVolume))
                {
                    Debug.LogError("VolumeObstacle attributes initialization error, parameters contradict");
                }
                
                
                if(!authoring.TryGetComponent<InteractableAttributesAuthoring>(out var interactableAttr))
                {
                    Debug.LogError("VolumeObstacle Attributes require an InteractableAttr component");
                    return;
                }
                if (interactableAttr.factionTag == FactionTag.Neutral && authoring.isObstacleDynamic )
                {
                    Debug.LogError("VolumeObstacle attributes initialization error, parameters contradict");
                    return;
                }
                // Only building needs check , because resource is neutral so being obstacle for all factions
                if (interactableAttr.baseTag == BaseTag.Buildings)
                {
                    if (!authoring.TryGetComponent(out BuildingAttributesAuthoring buildingAttr))
                    {
                        Debug.LogError("Building attributes sets in interactableAttr require an BuildingAttr component");
                        return;
                    }
                    volumeRadius = buildingAttr.interactRange;
                    // this is attackable building
                    areaType = volumeRadius == 0? (AreaType)buildingAttr.tier : (AreaType)(buildingAttr.tier + 10);
                }
                
                
                
                if (!authoring.TryGetComponent<BoxCollider>(out var boxCollider))
                {
                    Debug.LogError("VolumeObstacle Attributes require a BoxCollider component");
                    return;
                }

                
                var entity = GetEntity(TransformUsageFlags.Dynamic);

                AddComponent<VolumeObstacleTag>(entity);
                AddComponent(entity, new VolumeObstacleSpawnRequest
                {
                    Center = boxCollider.center,
                    Size = boxCollider.size,
                    VolumeRadius = volumeRadius,
                    VolumeAreaType = areaType,
                    RequestFromFaction = interactableAttr.factionTag,
                    NotGenerateObstacle = authoring.notGenerateObstacle,
                    NotGenerateVolume = authoring.notGenerateVolume,
                });
                if(authoring.isObstacleDynamic)
                    AddComponent(entity, new SyncObstacleData
                    {
                        SyncPositionInterval = authoring.syncPositionInterval,
                    });
            }
        }
    }

    public struct SyncObstacleData : IComponentData
    {
        public float SyncPositionInterval;
        public float SyncTime;
    }
    
    
    /// <summary>
    /// TODO : Each time when you try to destroy an entity with ObstacleTag, create entity with ObstacleDestroyRequest.
    /// </summary>
    public struct VolumeObstacleTag : IComponentData
    {
    }

    public struct VolumeObstacleSpawnRequest : IComponentData
    {
        public float3 Center;
        public float3 Size;
        /// <summary>
        /// If this is archer tower
        /// </summary>
        public float VolumeRadius;
        public AreaType VolumeAreaType;
        /// <summary>
        /// Will instantiate allyObstacle and enemy volume if from ally, vice versa;
        /// If this is neutral faction, then will generate obstacle for all faction nav mesh map,
        /// and neutral obstacle is not dynamic
        /// </summary>
        public FactionTag RequestFromFaction;
        public bool NotGenerateObstacle;
        public bool NotGenerateVolume;
    }

    public struct VolumeObstacleDestroyRequest : IComponentData
    {
        /// <summary>
        /// If destroy resource, then request from faction is neutral, otherwise is ally or enemy
        /// </summary>
        public FactionTag RequestFromFaction;
        public Entity FromEntity;
    }

    // TODO : Each time you close door, use this request, and don't forget to set door entity box collider deactivate
    public struct DoorControlRequest : IComponentData
    {
        /// <summary>
        /// Open = true, close = false
        /// </summary>
        public bool OpenOrClose;
        public FactionTag RequestFromFaction;
        public Entity FromEntity;
    }
}