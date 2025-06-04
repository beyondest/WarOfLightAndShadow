using Unity.Entities;
using UnityEngine;

namespace SparFlame.Test
{
    public class TestAuthoring : MonoBehaviour
    {
        public int value;
        private class TestAuthoringBaker : Baker<TestAuthoring>
        {
            public override void Bake(TestAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.Renderable);
                AddComponent(entity, new TestC
                {
                    value = authoring.value
                });
            }
        }
    }

    public struct TestC : IComponentData
    {
        public int value;   
    }
}