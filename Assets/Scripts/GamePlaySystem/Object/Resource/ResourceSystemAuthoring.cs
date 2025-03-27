using UnityEngine;

namespace SparFlame.GamePlaySystem.Resource
{
    public class ResourceSystemAuthoring : MonoBehaviour
    {
        private class ResourceSystemAuthoringBaker : Unity.Entities.Baker<ResourceSystemAuthoring>
        {
            public override void Bake(ResourceSystemAuthoring authoring)
            {
            }
        }
    }
}