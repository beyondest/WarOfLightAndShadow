using Unity.Entities;
using UnityEngine;

namespace SparFlame.Test
{
    public class TestSavingAuthoring : MonoBehaviour
    {
        private class TestSavingAuthoringBaker : Baker<TestSavingAuthoring>
        {
            public override void Bake(TestSavingAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.None);
                AddComponent<TestSavingTag>(entity);
            }
        }
    }

    public struct TestSavingTag : IComponentData
    {
        
    }
}