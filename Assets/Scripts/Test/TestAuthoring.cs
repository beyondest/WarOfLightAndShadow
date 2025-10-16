using System;
using Unity.Entities;
using UnityEngine;

namespace SparFlame.Test
{
    public class TestAuthoring : MonoBehaviour
    {
        public  readonly int value;
        private class TestAuthoringBaker : Baker<TestAuthoring>
        {
            public override void Bake(TestAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.Dynamic);
                AddComponent(entity, new TestTag{ID = authoring.value});
                var a =   typeof(tt);
                var tag = new TestTag();
                ref readonly var b = ref tag ;
                var tt = new tt();
                int i;
            }
        }
    }
    public struct TestTag : IComponentData
    {
        public int ID;
    }

    public class tt
    {
        public void EE(ref int v)
        {
            
        }
    }

    public struct InefficientStruct 
    {
        public bool a;
    }

}