using Unity.Entities;
using Unity.Rendering;
using UnityEngine;

namespace SparFlame.Systems.Map
{
    public class MiniMapMaterialAuthoring : MonoBehaviour
    {
        private class MiniMapMaterialAuthoringBaker : Baker<MiniMapMaterialAuthoring>
        {
            public override void Bake(MiniMapMaterialAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.Renderable);
                AddComponent<MiniMapMaterialTag>(entity);
                AddComponent<MiniMapColorVector4Override>(entity);
            }
        }
    }

    public struct MiniMapMaterialTag : IComponentData
    {
        
    }
}