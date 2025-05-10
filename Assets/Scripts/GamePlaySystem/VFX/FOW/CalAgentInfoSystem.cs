using SparFlame.GamePlaySystem.CameraControl;
using SparFlame.GamePlaySystem.General;
using SparFlame.GamePlaySystem.UnitSelection;
using Unity.Entities;
using Unity.Burst;
using Unity.Collections;
using Unity.Mathematics;
using Unity.Transforms;

namespace SparFlame.GamePlaySystem.Fow
{
    [BurstCompile]
    public partial struct CalAgentInfoSystem : ISystem
    {
        private ComponentLookup<ContributeSightTag> _contributeSightTagLookup;
        private ComponentLookup<DisappearInFowTag> _disappearInFowTagLookup;
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<FowTag>();
            state.RequireForUpdate<FowConfig>();
            state.RequireForUpdate<GamingTag>();
            _contributeSightTagLookup = state.GetComponentLookup<ContributeSightTag>(true);
            _disappearInFowTagLookup = state.GetComponentLookup<DisappearInFowTag>(true);
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var config = SystemAPI.GetSingleton<FowConfig>();
            var fowTag = SystemAPI.GetSingletonEntity<FowTag>();
            var transform = SystemAPI.GetComponent<LocalTransform>(fowTag);
            var localToWorld = SystemAPI.GetComponent<LocalToWorld>(fowTag);
            _contributeSightTagLookup.Update(ref state);
            _disappearInFowTagLookup.Update(ref state);
            new CalAgentInfoJob2
            {
                FogCenter = transform,
                FogToWorld = localToWorld,
                FowTextureSize = config.FowTextureSize,
                ContributeSightLookup = _contributeSightTagLookup,
                DisappearInFowLookup = _disappearInFowTagLookup
            }.ScheduleParallel();

        }
        
  
        
        [BurstCompile]
        [WithAll(typeof(InCameraExtendView))]
        private partial struct CalAgentInfoJob2 : IJobEntity
        {
            [ReadOnly] public LocalTransform FogCenter;
            [ReadOnly] public LocalToWorld FogToWorld;
            [ReadOnly] public float FowTextureSize;
            [ReadOnly] public ComponentLookup<ContributeSightTag> ContributeSightLookup;
            [ReadOnly] public ComponentLookup<DisappearInFowTag> DisappearInFowLookup;
        
            private void Execute(ref FowAgentData agent, ref LocalTransform agentTransform, Entity selfEntity)
            {
                var worldPosition = agentTransform.Position;
        
                var worldToLocal = math.inverse(FogToWorld.Value);
                var localPos = math.transform(worldToLocal, worldPosition); // same as InverseTransformPoint
        
                if (DisappearInFowLookup.HasComponent(selfEntity))
                {
                    var uv = new float2(localPos.x, localPos.z) * FowTextureSize;
                    agent.UV = new float4(uv.x, uv.y, 0, 0);
                }
        
                if (ContributeSightLookup.HasComponent(selfEntity))
                {
                    var relativePos = localPos * FogCenter.Scale; 
                    agent.RelativePosition = relativePos;
                    var forward = agentTransform.Forward();
                    var relativeForward = math.mul(worldToLocal, new float4(forward, 0)).xyz;
                    agent.RelativeForward = relativeForward;
                }
            }
        }

    }
}