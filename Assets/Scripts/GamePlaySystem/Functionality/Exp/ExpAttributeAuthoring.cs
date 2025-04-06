using Unity.Entities;
using UnityEngine;

namespace SparFlame.GamePlaySystem.Exp
{
    public class ExpAttributeAuthoring : MonoBehaviour
    {
        public int maxValue;
        
        private class ExpAttributeAuthoringBaker : Baker<ExpAttributeAuthoring>
        {
            public override void Bake(ExpAttributeAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.Dynamic);
                AddComponent(entity,new ExpData
                {
                    MaxValue = authoring.maxValue,
                    CurValue = 0
                });
            }
        }
    }

    public struct ExpData : IComponentData
    {
        public int MaxValue;
        public int CurValue;
    }
}