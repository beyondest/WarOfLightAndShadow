using SparFlame.Components.SubGameplay;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace SparFlame.Systems.SubGameplay.Garrison
{
    public class GarrisonSystemAuthoring : MonoBehaviour
    {
        public float3 hidePositionBias;
        public float garrisonRadius;
        private class GarrisonSystemAuthoringBaker : Baker<GarrisonSystemAuthoring>
        {
            public override void Bake(GarrisonSystemAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.None);
                AddComponent(entity, new GarrisonSystemConfig
                {
                    HidePositionBias = authoring.hidePositionBias,
                    GarrisonRadiusSq = authoring.garrisonRadius * authoring.garrisonRadius,
                });
            }
        }
    }
   
    





    public struct GarrisonGetOut : IComponentData
    {
        
    }

}