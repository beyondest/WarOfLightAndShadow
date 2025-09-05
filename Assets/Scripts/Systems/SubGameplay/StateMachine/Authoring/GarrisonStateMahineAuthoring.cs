using Unity.Entities;
using UnityEngine;

namespace SparFlame.Systems.SubGameplay.StateMachine
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