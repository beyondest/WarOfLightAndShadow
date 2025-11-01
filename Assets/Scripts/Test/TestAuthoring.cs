using Unity.Entities;
using UnityEngine;

namespace SparFlame.Test
{
    public class TestAuthoring : MonoBehaviour
    {
        public GameObject go;
        public   int value;
        private class TestAuthoringBaker : Baker<TestAuthoring>
        {
            public override void Bake(TestAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.Dynamic);
                AddComponent(entity, new TestTag{ID = authoring.value});
              
            }
        }
    }
    public struct TestTag : IComponentData
    {
        public int ID;
    }

}