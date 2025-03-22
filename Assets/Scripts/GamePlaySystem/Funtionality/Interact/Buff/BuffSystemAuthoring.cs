using Unity.Entities;
using UnityEngine;

namespace SparFlame.GamePlaySystem.Interact
{
    public class BuffSystemAuthoring : MonoBehaviour
    {
        private class BuffSystemAuthoringBaker : Baker<BuffSystemAuthoring>
        {
            public override void Bake(BuffSystemAuthoring authoring)
            {
            }
        }
    }
    
    public struct BuffData : IComponentData
    {
        public float MoveSpeedMultiplier;
        public float InteractSpeedMultiplier;
        public float InteractRangeMultiplier;
        public float InteractAmountMultiplier;
        public float HealthMultiplier;
    }
}