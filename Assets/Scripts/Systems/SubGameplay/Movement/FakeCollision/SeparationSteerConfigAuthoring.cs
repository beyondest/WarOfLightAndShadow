using Unity.Entities;
using UnityEngine;

namespace SparFlame.Systems.SubGameplay.Movement.FakeCollision
{
    public class SeparationSteerConfigAuthoring : MonoBehaviour
    {
        public float coefficientOfOverlap;

        private class SeparationSteerConfigAuthoringBaker : Baker<SeparationSteerConfigAuthoring>
        {
            public override void Bake(SeparationSteerConfigAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.None);
                AddComponent(entity, new SeparationSteerConfig
                {
                    CoefficientOfOverlap = authoring.coefficientOfOverlap
                });
            }
        }
    }

    public struct SeparationSteerConfig : IComponentData
    {
        public float CoefficientOfOverlap;
    }
}