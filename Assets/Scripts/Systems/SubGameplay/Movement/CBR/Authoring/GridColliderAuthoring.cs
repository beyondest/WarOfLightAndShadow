using System;
using Unity.Entities;
using UnityEngine;

namespace SparFlame.Systems.SubGameplay.Movement.CBR
{
    public class GridColliderAuthoring : MonoBehaviour
    {
        public GridColliderConfig config;

        private class GridColliderAuthoringBaker : Baker<GridColliderAuthoring>
        {
            public override void Bake(GridColliderAuthoring authoring)
            {
                var e = GetEntity(TransformUsageFlags.None);
                AddComponent(e, authoring.config);
            }
        }
    }
    [Serializable]
    public struct GridColliderConfig : IComponentData
    {
        public float gridSize;
        public float overInitRatio;
    }
}


