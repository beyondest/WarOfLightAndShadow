using SparFlame.Components.SubGameplay;
using Unity.Entities;
using UnityEngine;

namespace SparFlame.Database.AttributesAuthoring
{
    public class TerrainTagAuthoring : MonoBehaviour
    {
        private class TerrainTagAuthoringBaker : Baker<TerrainTagAuthoring>
        {
            public override void Bake(TerrainTagAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.None);
                AddComponent<TerrainTag>(entity);
            }
        }
    }
}