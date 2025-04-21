using Unity.Entities;
using UnityEngine;

namespace SparFlame.Test
{
    public class TestDatabaseAuthoring : MonoBehaviour
    {
        public TestDatabaseSo so;
        private class TestDatabaseBaker : Baker<TestDatabaseAuthoring>
        {
            public override void Bake(TestDatabaseAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.None);
                AddComponent(entity, new TestPrefab
                {
                    Prefab = GetEntity(authoring.so.prefab, TransformUsageFlags.Dynamic)
                });
            }
        }
    }

    public struct TestPrefab : IComponentData
    {
        public Entity Prefab;
    }
}