using System;
using Unity.Entities;
using UnityEngine;

namespace SparFlame.Systems.SubGameplay.Interact
{
    public class GridVisionSystemAuthoring : MonoBehaviour
    {
        public GridVisionConfig config;
        private class GridVisionSystemAuthoringBaker : Baker<GridVisionSystemAuthoring>
        {
            public override void Bake(GridVisionSystemAuthoring authoring)
            {
                var e = GetEntity(TransformUsageFlags.None);
                AddComponent(e, authoring.config);
            }
        }
    }

    [Serializable]
    public struct GridVisionConfig : IComponentData
    {
        public float gridSize;
        public float overInitRatio;
    }
}