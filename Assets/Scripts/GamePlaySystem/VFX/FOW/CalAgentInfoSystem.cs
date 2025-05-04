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
    [UpdateAfter(typeof(CalWorldToScreenSystem))]
    public partial struct CalAgentInfoSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<FowTag>();
            state.RequireForUpdate<FowConfig>();
            state.RequireForUpdate<GamingTag>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var config = SystemAPI.GetSingleton<FowConfig>();
            var fowTag = SystemAPI.GetSingletonEntity<FowTag>();
            var transform = SystemAPI.GetComponent<LocalTransform>(fowTag);
            var localToWorld = SystemAPI.GetComponent<LocalToWorld>(fowTag);
            new CalAgentInfoJob2
            {
                FogCenter = transform,
                FogToWorld = localToWorld,
                FowTextureSize = config.FowTextureSize
            }.ScheduleParallel();

        }
        
  
        
        [BurstCompile]
        [WithAll(typeof(InCameraExtendView))]
        private partial struct CalAgentInfoJob2 : IJobEntity
        {
            [ReadOnly] public LocalTransform FogCenter;
            [ReadOnly] public LocalToWorld FogToWorld;
            [ReadOnly] public float FowTextureSize;
        
            private void Execute(ref FowAgentData agent, ref LocalTransform agentTransform)
            {
                var worldPosition = agentTransform.Position;
        
                var worldToLocal = math.inverse(FogToWorld.Value);
                var localPos = math.transform(worldToLocal, worldPosition); // same as InverseTransformPoint
        
                if (agent.DisappearInFow)
                {
                    var uv = new float2(localPos.x, localPos.z) * FowTextureSize;
                    agent.UV = new float4(uv.x, uv.y, 0, 0);
                }
        
                if (agent.ContributeToFOV)
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