using SparFlame.GamePlaySystem.Animation;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;

namespace SparFlame.GamePlaySystem.State
{
    public struct ChangeStateRequest : IComponentData
    {
        public UnitAnimationState TargetState;
    }
    public partial struct AnimationTestStateChangeSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<AnimationPlayConfig>();
            state.RequireForUpdate<AnimationStateData>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var ecb = new EntityCommandBuffer(Allocator.TempJob);
            var job = new TestChangeStateJob
            {
                CurTime = (float)SystemAPI.Time.ElapsedTime,
                Config = SystemAPI.GetSingleton<AnimationPlayConfig>(),
                ECB =ecb
            }.Schedule(state.Dependency);
            job.Complete();
            ecb.Playback(state.EntityManager);
            ecb.Dispose();
        }

        
        
        [BurstCompile]
        public partial struct TestChangeStateJob : IJobEntity
        {
            public EntityCommandBuffer ECB;
            [ReadOnly] public float CurTime;
            [ReadOnly] public AnimationPlayConfig Config;
            private void Execute(in ChangeStateRequest request,ref AnimationStateData data )
            {
                var oriValue = data.State;
                data.State = request.TargetState;

                if (data.State != oriValue )
                {
                    data.Blending = true;
                    data.ClipAIndex = (int)oriValue;
                    data.ClipBIndex = (int)data.State;
                    data.ClipAWeight = 1f;
                    data.ClipBWeight = 0f;
                    data.ClipBStartTime = CurTime;
                    return;
                }

                if (data.Blending)
                {
                    var t = CurTime - data.ClipBStartTime;
                    if (t > Config.blendDuration)
                    {
                        data.ClipAIndex = data.ClipBIndex;
                        data.ClipAStartTime = data.ClipBStartTime;
                        data.ClipAWeight = 1f;
                        data.Blending = false;
                        return;
                        // data.ClipBIndex = -1;
                        // data.ClipBStartTime = 0;
                        // data.ClipBWeight = 0;
                    }
                    var weightB = t / Config.blendDuration;
                    data.ClipAWeight = 1 - weightB;
                    data.ClipBWeight = weightB;
                }
            }
        }
    }
}