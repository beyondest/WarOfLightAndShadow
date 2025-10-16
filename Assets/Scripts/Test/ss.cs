using System;
using Unity.Burst;
using Unity.Entities;

namespace SparFlame.Test
{
    public partial struct ss : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
        }

        [BurstCompile]
        public void OnDestroy(ref SystemState state)
        {

        }
    }
}