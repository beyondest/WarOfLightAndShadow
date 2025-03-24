using Unity.Entities;
using UnityEngine;
using Unity.Mathematics;
using SparFlame.GamePlaySystem.General;
using SparFlame.GamePlaySystem.Building;
using SparFlame.GamePlaySystem.Resource;
using Unity.Physics.Authoring;
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



                var physicsShapeAuthoring = authoring.GetComponent<PhysicsShapeAuthoring>();
                var entity = GetEntity(TransformUsageFlags.Dynamic);
                AddComponent<VolumeObstacleTag>(entity);
                AddComponent(entity, new VolumeObstacleSpawnRequest
                {
                    Center = physicsShapeAuthoring.m_PrimitiveCenter,
                    Size = physicsShapeAuthoring.m_PrimitiveSize,
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

    internal struct SyncObstacleData : IComponentData
    {
        public float SyncPositionInterval;
        public float SyncTime;
    }
    
    
    public struct VolumeObstacleTag : IComponentData
    {
    }

    // TODO : Change request to entity request not component request
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




}