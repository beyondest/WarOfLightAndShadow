using SparFlame.Components.General;
using Unity.Entities;
using UnityEngine;

namespace SparFlame.Systems.MainGameplay.EnemyAI
{
    public class SimulateInvadingSupportCityAuthoring : MonoBehaviour
    {
        public float reduceHpPerHour = 5;
        private class
            SimulateInvadingSupportCityAuthoringBaker : Baker<SimulateInvadingSupportCityAuthoring>
        {
            public override void Bake(SimulateInvadingSupportCityAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.None);
                AddComponent(entity, new InvadeSupportCityConfig
                {
                    ReduceHpPerHour = authoring.reduceHpPerHour
                });
            }
        }
    }
}