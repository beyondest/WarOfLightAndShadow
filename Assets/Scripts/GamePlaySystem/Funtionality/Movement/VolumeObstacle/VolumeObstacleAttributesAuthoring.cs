using Unity.Entities;
using UnityEngine;
using Unity.Mathematics;
using SparFlame.GamePlaySystem.General;
using SparFlame.GamePlaySystem.Building;
using SparFlame.GamePlaySystem.Interact;
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

                    // this is interactable building, like archer tower
                    if (authoring.TryGetComponent(out InteractAbilityAttributesAuthoring interactAbility))
                    {
                        // Default is 0f , if not 0, this is an attackable building so the high-cost area should cover more
                        volumeRadius = interactAbility.interactType == InteractType.Attack? interactAbility.interactRange : 0f;
                        // If this is an attackable building, the area is more dangerous and volume should be high cost
                        // And the areaType is determined by tier, the higher the tier, the higher cost it should be
                        areaType = interactAbility.interactType == InteractType.Attack? (AreaType)buildingAttr.tier : (AreaType)(buildingAttr.tier + 10);
                    }
                    // this is not interactable building, volume radius set to default, and areaType set via tier
                    else
                    {
                        volumeRadius = 0f;
                        areaType = (AreaType)buildingAttr.tier;
                    }
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