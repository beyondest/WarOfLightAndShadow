using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace SparFlame.Systems.General.BasicControl.Init
{
    public class GenerateCrystalAuthoring : MonoBehaviour
    {
        private class GenerateCrystalAuthoringBaker : Baker<GenerateCrystalAuthoring>
        {
            public override void Bake(GenerateCrystalAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.None);
                AddComponent(entity, new GenerateCrystalRequest
                {
                    Position = authoring.transform.position,
                });
            }
        }
    }

    public struct GenerateCrystalRequest : IComponentData
    {
        public float3 Position;
    }
}