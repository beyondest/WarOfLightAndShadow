using System;
using System.Collections.Generic;
using SparFlame.GamePlaySystem.General;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;
using UnityEngine.Serialization;

namespace SparFlame.GamePlaySystem.Building
{
    public class PlacementSystemAuthoring : MonoBehaviour
    {
        public int modelChildIndex = 1;
        public GameObject ghostTriggerPrefab;
        public GameObject allyResource;
        public GameObject enemyResource;
        public GameObject validRef;
        public GameObject overlappingRef;
        public GameObject notEnoughResourceRef;
        public GameObject notConstructableRef;

        private class PlaceSystemAuthoringBaker : Baker<PlacementSystemAuthoring>
        {
            public override void Bake(PlacementSystemAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.None);


                AddComponent(entity, new PlacementSystemConfig
                {
                    ModelChildIndex = authoring.modelChildIndex,
                    AllyResourceEntity = GetEntity(authoring.allyResource, TransformUsageFlags.None),
                    EnemyResourceEntity = GetEntity(authoring.enemyResource, TransformUsageFlags.None),

                    GhostTriggerPrefab = GetEntity(authoring.ghostTriggerPrefab, TransformUsageFlags.Dynamic),

                    ValidPreset = GetEntity(authoring.validRef, TransformUsageFlags.None),
                    OverlappingPreset = GetEntity(authoring.overlappingRef, TransformUsageFlags.None),
                    NotEnoughResourcesPreset = GetEntity(authoring.notEnoughResourceRef, TransformUsageFlags.None),
                    NotConstructablePreset = GetEntity(authoring.notConstructableRef, TransformUsageFlags.None),
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

    [Serializable]
    public struct StateTypeMaterialRefPair
    {
        public PlacementStateType state;
        public GameObject materialRef;
    }

    public struct PlacementSystemConfig : IComponentData
    {
        public int ModelChildIndex;
        public Entity AllyResourceEntity;
        public Entity EnemyResourceEntity;

        public Entity GhostTriggerPrefab;

        // Material preset
        public Entity ValidPreset;
        public Entity OverlappingPreset;
        public Entity NotEnoughResourcesPreset;
        public Entity NotConstructablePreset;
    }

    public enum PlacementCommandType
    {
        Drag,
        Start,
        End,
        Build
    }

    public struct PlacementCommandData : IComponentData
    {
        // Command side
        public PlacementCommandType CommandType;
        public FactionTag Faction;
        public quaternion Rotation;
        public Entity TargetBuilding;
        public bool IsMovementShow;

        // Feedback
        public Entity GhostModelEntity; // Only the model of target building
        public Entity GhostTriggerEntity;
        public PlacementStateType State;
        
        // Internal data
        public LocalTransform OriTransform;
        public LocalTransform RelativeTransformToParent;
    }
}