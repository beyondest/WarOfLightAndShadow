using System;
using Unity.Entities;
using UnityEngine;

namespace SparFlame.Test
{
    public class TestSavingAuthoring : MonoBehaviour
    {
        public TestSavingConfig config;
        private class TestSavingAuthoringBaker : Baker<TestSavingAuthoring>
        {
            public override void Bake(TestSavingAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.None);
                AddComponent(entity,authoring.config);
            }
        }
    }

    [Serializable]
    public struct TestSavingConfig : IComponentData
    {
        public int count;
        
    }
}