using System;
using System.Collections.Generic;
using SparFlame.GamePlaySystem.General;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Serialization;

namespace SparFlame.GamePlaySystem.Building.GamePlaySystem.Core.Object.Building
{
    public class PlacementSystemAuthoring : MonoBehaviour
    {
        public int modelChildIndex = 1;
        public GameObject ghostTriggerPrefab;
        public GameObject allyResource;
        public GameObject enemyResource;
        public List<StateTypeMaterialRefPair> stateTypeMaterialRefs;

        private class PlaceSystemAuthoringBaker : Baker<PlacementSystemAuthoring>
        {
            public override void Bake(PlacementSystemAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.None);
                GameObject valid = new();
                GameObject overlapping = new();
                GameObject notEnough = new();
                GameObject notConstructable = new();
                foreach (var pair in authoring.stateTypeMaterialRefs)
                {
                    switch (pair.state)
                    {
                        case PlacementStateType.Valid:
                            valid = pair.materialRef;
                            break;
                        case PlacementStateType.Overlapping:
                            overlapping = pair.materialRef;
                            break;
                        case PlacementStateType.NotEnoughResources:
                            notEnough = pair.materialRef;
                            break;
                        case PlacementStateType.NotConstructable:
                            notConstructable = pair.materialRef;
                            break;
                        default:
                            throw new ArgumentOutOfRangeException();
                    }
                }

                AddComponent(entity, new PlacementSystemConfig
                {
                    ModelChildIndex = authoring.modelChildIndex,
                    AllyResourceEntity = GetEntity(authoring.allyResource, TransformUsageFlags.None),
                    EnemyResourceEntity = GetEntity(authoring.enemyResource, TransformUsageFlags.None),

                    GhostTriggerPrefab = GetEntity(authoring.ghostTriggerPrefab, TransformUsageFlags.Dynamic),

                    ValidPreset = GetEntity(valid, TransformUsageFlags.None),
                    OverlappingPreset = GetEntity(overlapping, TransformUsageFlags.None),
                    NotEnoughResourcesPreset = GetEntity(notEnough, TransformUsageFlags.None),
                    NotConstructablePreset = GetEntity(notConstructable, TransformUsageFlags.None),
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

        // Feedback
        public Entity GhostEntity;
        public Entity GhostTriggerEntity;
        public PlacementStateType State;
    }
}