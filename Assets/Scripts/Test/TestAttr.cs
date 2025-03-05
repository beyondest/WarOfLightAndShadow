using Unity.Entities;
using UnityEngine;

namespace SparFlame.Test
{
    class TestAttr : MonoBehaviour
    {
        class TestAttrBaker : Baker<TestAttr>
        {
            public override void Bake(TestAttr authoring)
            {
                var entity = GetEntity(TransformUsageFlags.Dynamic);
                AddComponent(entity, new TestAttrData
                {
                    Value = 1
                });
                AddComponent<PauseData>(entity);
                SetComponentEnabled<PauseData>(entity, true);
            }
        }
    }

    public struct PauseData : IComponentData, IEnableableComponent
    {
        
    }
    public struct TestAttrData : IComponentData
    {
        public int Value;
    }
}
