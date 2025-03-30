using System;
using System.Collections.Generic;
using SparFlame.GamePlaySystem.Building;
using Unity.Entities;
using UnityEngine;
using UnityEngine.Serialization;

namespace SparFlame.UI.GamePlay.BuildingWindows
{
    public class BuildingIndexDataBaseAuthoring : MonoBehaviour
    {
        public List<BuildingIndexPrefabPair> buildingIndexPrefabPairs;

        private class Baker : Baker<BuildingIndexDataBaseAuthoring>
        {
            public override void Bake(BuildingIndexDataBaseAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.None);
                var buffer = AddBuffer<BuildingIndexPrefab>(entity);
                foreach (var pair in authoring.buildingIndexPrefabPairs)
                {
                    buffer.Add(new BuildingIndexPrefab
                    {
                        Type = pair.type,
                        Prefab = GetEntity(pair.prefab, TransformUsageFlags.Dynamic)
                    });
                }
            }
        }
    }

    public struct BuildingIndexPrefab : IBufferElementData
    {
        public BuildingType Type;
        public Entity Prefab;
    }

    [Serializable]
    public struct BuildingIndexPrefabPair
    {
        public BuildingType type;
        public GameObject prefab;
    }
}