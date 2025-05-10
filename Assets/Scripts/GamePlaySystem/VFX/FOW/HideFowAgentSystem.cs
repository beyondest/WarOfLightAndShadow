using SparFlame.GamePlaySystem.General;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Rendering;

namespace SparFlame.GamePlaySystem.Fow
{
    public partial struct HideFowAgentSystem : ISystem
    {
        private BufferLookup<LinkedEntityGroup> _childrenLookup;
        private ComponentLookup<MaterialMeshInfo> _meshLookup;
        private ComponentLookup<InverseDisappearTag>  _inverseDisappearTagLookup;

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<GamingTag>();
            state.RequireForUpdate<EndSimulationEntityCommandBufferSystem.Singleton>();
            state.RequireForUpdate<HideFowAgentRequest>();
            state.RequireForUpdate<FowConfig>();
            _childrenLookup = state.GetBufferLookup<LinkedEntityGroup>(true);
            _meshLookup = state.GetComponentLookup<MaterialMeshInfo>(true);
            _inverseDisappearTagLookup = state.GetComponentLookup<InverseDisappearTag>(true);
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            _childrenLookup.Update(ref state);
            _meshLookup.Update(ref state);
            _inverseDisappearTagLookup.Update(ref state);
            new HideOrShowFowAgentJob
            {
                ChildrenLookup = _childrenLookup,
                MeshLookup = _meshLookup,
                ECB = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>()
                    .CreateCommandBuffer(state.WorldUnmanaged).AsParallelWriter(),
                InverseDisappearTagLookup = _inverseDisappearTagLookup
            }.ScheduleParallel();
        }

 

        [BurstCompile]
        private partial struct HideOrShowFowAgentJob : IJobEntity
        {
            [ReadOnly] public BufferLookup<LinkedEntityGroup> ChildrenLookup;
            [ReadOnly] public ComponentLookup<MaterialMeshInfo> MeshLookup;
            [ReadOnly] public ComponentLookup<InverseDisappearTag> InverseDisappearTagLookup;
            public EntityCommandBuffer.ParallelWriter ECB;

            private void Execute([ChunkIndexInQuery] int index, Entity selfEntity, in HideFowAgentRequest request)
            {
                ECB.RemoveComponent<HideFowAgentRequest>(index, selfEntity);
                var hide = request.Hide;
                if (InverseDisappearTagLookup.HasComponent(selfEntity))
                {
                    hide = !hide;
                }
                HideOrShowRecursively(selfEntity, hide, index);
            }

            private void HideOrShowRecursively(Entity entity, bool hide, int index)
            {
                if (!ChildrenLookup.HasBuffer(entity))
                    return;
                var buffer = ChildrenLookup[entity];
                for (var i = 1; i < buffer.Length; i++)
                {
                    if (!MeshLookup.HasComponent(buffer[i].Value))
                    {
                        HideOrShowRecursively(buffer[i].Value, hide, index);
                        continue;
                    }

                    if (hide) ECB.AddComponent<DisableRendering>(index, buffer[i].Value);
                    else ECB.RemoveComponent<DisableRendering>(index, buffer[i].Value);
                    HideOrShowRecursively(buffer[i].Value, hide, index);
                }
            }
        }
    }


    public struct HideFowAgentRequest : IComponentData
    {
        public bool Hide;
    }
}