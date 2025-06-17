using Unity.Entities;
using UnityEngine;

namespace SparFlame.GamePlaySystem.Building
{
    public class ConstructableTagAuthoring : MonoBehaviour
    {
        public bool initConstructable = true;
        private class ConstructableTagAuthoringBaker : Baker<ConstructableTagAuthoring>
        {
            public override void Bake(ConstructableTagAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.Dynamic);
                AddComponent<ConstructableTag>(entity);
                SetComponentEnabled<ConstructableTag>(entity, authoring.initConstructable);
            }
        }
    }
}