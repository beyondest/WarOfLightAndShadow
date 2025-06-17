using Sirenix.OdinInspector;
using SparFlame.GamePlaySystem.General;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

namespace SparFlame.GamePlaySystem.Building
{
    public class ConstructSystemAuthoring : MonoBehaviour
    {
        
        [Header("Prefabs")]
        [AssetsOnly]
        public GameObject ghostTriggerPrefab;
        [AssetsOnly]
        public GameObject validRef;
        [AssetsOnly]

        public GameObject overlappingRef;
        [AssetsOnly]

        public GameObject notEnoughResourceRef;
        [AssetsOnly]

        public GameObject notConstructableRef;

        [AssetsOnly] public GameObject previewGridPrefab;
        [AssetsOnly] public GameObject previewCubePrefab;
        [AssetsOnly] public GameObject previewAttackRangePrefab;
        
        
        [Header("General config")]
        [Tooltip("This location is used for hiding building when enter movement ghost show")]
        public float3 hideBuildingLocation  = new float3(0, -100, 0);
        public float rotateSpeed = 2f;
        public float recycleScale = 0.5f;
        public float constructionGridSize = 2f;
        
        private class PlaceSystemAuthoringBaker : Baker<ConstructSystemAuthoring>
        {
            public override void Bake(ConstructSystemAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.None);
                AddComponent(entity, new ConstructSystemConfig
                {
                    HideBuildingLocation = authoring.hideBuildingLocation,
                    RotateSpeed = authoring.rotateSpeed,
                    RecycleScale = authoring.recycleScale,
                    ConstructionGridSize = authoring.constructionGridSize,
                });

                
                AddComponent(entity, new ConstructSystemPrefabs
                {
                    GhostTriggerPrefab = GetEntity(authoring.ghostTriggerPrefab, TransformUsageFlags.Dynamic),
                    ValidPreset = GetEntity(authoring.validRef, TransformUsageFlags.None),
                    OverlappingPreset = GetEntity(authoring.overlappingRef, TransformUsageFlags.None),
                    NotEnoughResourcesPreset = GetEntity(authoring.notEnoughResourceRef, TransformUsageFlags.None),
                    NotConstructablePreset = GetEntity(authoring.notConstructableRef, TransformUsageFlags.None),
                    PreviewAttackRangePrefab = GetEntity(authoring.previewAttackRangePrefab, TransformUsageFlags.Dynamic),
                    GridPrefab = GetEntity(authoring.previewGridPrefab, TransformUsageFlags.Dynamic),
                    PreviewCubePrefab = GetEntity(authoring.previewCubePrefab, TransformUsageFlags.Dynamic),
                });
                AddComponent(entity, new ConstructCommandData
                {
                    CommandType = ConstructCommandType.None,
                    Faction =  FactionTag.Neutral,
                    GhostModelEntity = Entity.Null,
                    GhostTriggerEntity = Entity.Null,
                    IsMovementShow = false,
                    OriTransform = default,
                    RotationAngle = 0,
                    State = PlacementStateType.NotConstructable,
                    TargetBuilding = Entity.Null
                });
            }
        }
    }

    public enum PlacementStateType
    {
        Valid,
        Overlapping,
        NotEnoughResources,
        NotConstructable,
    }

    public struct ConstructSystemPrefabs : IComponentData
    {
        public Entity GhostTriggerPrefab;
        public Entity GridPrefab;
        public Entity PreviewCubePrefab;
        public Entity PreviewAttackRangePrefab;
        

        // Material preset
        public Entity ValidPreset;
        public Entity OverlappingPreset;
        public Entity NotEnoughResourcesPreset;
        public Entity NotConstructablePreset;
        
        
    }
    public struct ConstructSystemConfig : IComponentData
    {
        public float3 HideBuildingLocation;
        public float RotateSpeed;
        public float RecycleScale;
        public float ConstructionGridSize;
    }

    public enum ConstructCommandType
    {
        None = 0,
        Drag = 1,
        Start = 2,
        End = 3,
        Build = 4
    }

    public struct ConstructCommandData : IComponentData
    {
        // Command side
        public ConstructCommandType CommandType;
        public float RotationAngle;
        public Entity TargetBuilding;
        public bool IsMovementShow;
        public bool EnterConstruct;

        // Feedback
        public PlacementStateType State;
        
        // Internal data
        public FactionTag Faction;
        public LocalTransform OriTransform;
        public Entity GhostModelEntity; // Only the model of target building
        public Entity GhostTriggerEntity;
        public Entity PreviewAttackRangeEntity;
        public Entity PreviewCube;

    }

    public struct ConstructableTag : IComponentData,IEnableableComponent
    {
        
    }


}