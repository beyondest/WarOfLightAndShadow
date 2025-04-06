using Unity.Entities;
using UnityEngine;

namespace SparFlame.GamePlaySystem.Interact
{
    public class StatAttributesAuthoring : MonoBehaviour
    {
        public int statMaxValue = 100;
        private class Baker : Baker<StatAttributesAuthoring>
        {
            public override void Bake(StatAttributesAuthoring attributesAuthoring)
            {
                var entity = GetEntity(TransformUsageFlags.Dynamic);
                AddComponent(entity, new StatData
                {
                    MaxValue = attributesAuthoring.statMaxValue,
                    CurValue = attributesAuthoring.statMaxValue
                });
            }
        }
    }

    public struct StatData : IComponentData
    {
        public int MaxValue;
        public int CurValue;
        
    }
        


}