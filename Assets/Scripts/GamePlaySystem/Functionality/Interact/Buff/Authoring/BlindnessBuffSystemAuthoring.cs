using System;
using System.Collections.Generic;
using Unity.Entities;
using UnityEngine;

namespace SparFlame.GamePlaySystem.Interact.Blindness
{
    public class BlindnessBuffSystemAuthoring : MonoBehaviour
    {
        [Serializable]
        public struct Pair
        {
            public float lastDuration;
            public float reduceCurrentHpRatio;
        }
        public List<Pair> lastDurations;
        private class BlindnessBuffAuthoringBaker : Baker<BlindnessBuffSystemAuthoring>
        {
            public override void Bake(BlindnessBuffSystemAuthoring systemAuthoring)
            {
                var entity = GetEntity(TransformUsageFlags.None);
                var buffer = AddBuffer<BlindnessConfigs>(entity);
                foreach (var duration in systemAuthoring.lastDurations)
                {
                    buffer.Add(new BlindnessConfigs
                    {
                        LastDuration = duration.lastDuration,
                        ReduceCurrentHpRatio = duration.reduceCurrentHpRatio
                    });
                }

            }
        }
    }

    public struct BlindnessConfigs : IBufferElementData
    {
        public float LastDuration;
        public float ReduceCurrentHpRatio;

    }

    public struct BlindnessAttackBuff : IComponentData
    {
        public float LastDuration;
        public float ReduceCurrentHpRatio;

    }

    public struct BlindnessLastData : IComponentData
    {
        public float StopTime;
    }
    
    


}