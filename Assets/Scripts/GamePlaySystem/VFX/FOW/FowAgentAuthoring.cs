using SparFlame.GamePlaySystem.CameraControl;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Rendering;
using UnityEngine;

namespace SparFlame.GamePlaySystem.Fow
{
    public class FowAgentAuthoring : MonoBehaviour
    {
        public bool contributeToFOV;
        public float sightRange;
        public float sightAngle;
        public bool disappearInFow;
        public float disappearAlphaThreshold;
        
        private class FowAgentAuthoringBaker : Baker<FowAgentAuthoring>
        {
            public override void Bake(FowAgentAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.Dynamic);
                AddComponent(entity, new FowAgentData
                {
                    ContributeToFOV   = authoring.contributeToFOV,
                    DisappearInFow   = authoring.disappearInFow,
                    DisappearAlphaThreshold = authoring.disappearAlphaThreshold,
                    SightRange = authoring.sightRange,
                    SightCos = Mathf.Cos(authoring.sightAngle * 0.5f * Mathf.Deg2Rad)
                });
                AddComponent<ScreenPos>(entity);
                AddComponent<InCameraView>(entity);
                SetComponentEnabled<InCameraView>(entity, false);
            }
        }
    }
    public struct FowAgentData : IComponentData
    {
        // Static data
        public bool ContributeToFOV;
        public float SightRange;
        public float SightCos;
        public bool DisappearInFow;
        public float DisappearAlphaThreshold;
        
        // Dynamic data
        public bool IsInsight;
        public float3 RelativePosition;
        public float3 RelativeForward;
        public float4 UV;
    }

    public struct ContributeSightTag : IComponentData
    {
        
    }

    public struct DisappearInFowTag : IComponentData
    {
        
    }

    public readonly partial struct FowAgent : IAspect
    {
        private readonly RefRW<FowAgentData> _fovAgentData;
        private readonly Entity _self;
        public bool ContributeToFOV => _fovAgentData.ValueRW.ContributeToFOV;
        public float SightRange => _fovAgentData.ValueRW.SightRange;
        public bool DisappearInFow => _fovAgentData.ValueRW.DisappearInFow;
        public float DisappearAlphaThreshold => _fovAgentData.ValueRW.DisappearAlphaThreshold;
        public bool IsInsight => _fovAgentData.ValueRW.IsInsight;

        public float SightCos => _fovAgentData.ValueRW.SightCos;
        public float3 RelativePosition => _fovAgentData.ValueRW.RelativePosition;
        public float3 RelativeForward => _fovAgentData.ValueRW.RelativeForward;
        public float4 UV => _fovAgentData.ValueRW.UV;
        public Entity Self => _self;
        
        public void SetUnderFow(bool isInSight, EntityCommandBuffer ecb,EntityManager entityManager)
        {
            if (!DisappearInFow) return;
            // This request should be removed every frame before next time calculation
            // TODO find a safer way 
            if (IsInsight != isInSight)
            {
                _fovAgentData.ValueRW.IsInsight = isInSight;
                ecb.AddComponent(_self, new HideFowAgentRequest
                {
                    Hide = !isInSight
                });
            }
        }
    }
}