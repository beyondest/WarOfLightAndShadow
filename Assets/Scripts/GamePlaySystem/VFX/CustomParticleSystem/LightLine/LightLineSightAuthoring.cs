using SparFlame.GamePlaySystem.CameraControl;
using SparFlame.GamePlaySystem.Fow;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace SparFlame.GamePlaySystem.CustomParticleSystem.LightLine
{
    public class LightLineSightAuthoring : MonoBehaviour
    {
        public float sightRange;
        public float sightDegree = 360;
        private class LightLineSightAuthoringBaker : Baker<LightLineSightAuthoring>
        {
            public override void Bake(LightLineSightAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.Dynamic);
                AddComponent<LightLineSightTag>(entity);
                AddComponent<ContributeSightTag>(entity);
                SetComponentEnabled<ContributeSightTag>(entity,true);
                AddComponent(entity, new FowAgentData
                {
                    SightRange = authoring.sightRange,
                    SightCos =  Mathf.Cos(authoring.sightDegree * 0.5f * Mathf.Deg2Rad),
                    IsInsight = true
                });
                AddComponent(entity, new ScreenPos
                {
                    ScreenPosition = float2.zero
                });
                AddComponent<InCameraView>(entity);
                AddComponent<InCameraExtendView>(entity);
                SetComponentEnabled<InCameraView>(entity, false);
                SetComponentEnabled<InCameraExtendView>(entity, false);

            }
        }
    }
}