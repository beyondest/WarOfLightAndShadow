using SparFlame.GamePlaySystem.Garrison;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace SparFlame.GamePlaySystem.State
{
    public class GarrisonStateMahineAuthoring : MonoBehaviour
    {
        
        private class GarrisonStateMahineAuthoringBaker : Baker<GarrisonStateMahineAuthoring>
        {
            public override void Bake(GarrisonStateMahineAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.None);
                AddComponent(entity, new GarrisonStateMachineConfig
                {
                    
                });
            }
        }
    }

    public struct GarrisonStateMachineConfig : IComponentData
    {
        
    }


}