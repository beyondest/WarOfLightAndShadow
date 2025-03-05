using Unity.Burst;
using Unity.Entities;
using UnityEngine;

namespace SparFlame.Test
{
    partial struct TestSystem : ISystem
    {
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<TestAttrData>();
        }

        public void OnUpdate(ref SystemState state)
        {
            var testData = SystemAPI.GetSingleton<TestAttrData>();
            Debug.Log($"{testData.Value}");
                                
        }

        public void OnDestroy(ref SystemState state)
        {
        
        }
    }
}
