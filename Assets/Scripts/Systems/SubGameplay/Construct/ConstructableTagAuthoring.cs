using SparFlame.Components.SubGameplay;
using Unity.Entities;
using UnityEngine;

namespace SparFlame.Systems.SubGameplay.Construct
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