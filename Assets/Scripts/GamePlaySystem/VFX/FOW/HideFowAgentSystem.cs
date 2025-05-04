using SparFlame.GamePlaySystem.General;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Entities.Graphics;
using Unity.Rendering;

namespace SparFlame.GamePlaySystem.Fow
{
    public partial struct HideFowAgentSystem : ISystem
    {
        private BufferLookup<LinkedEntityGroup> _childrenLookup;
        private ComponentLookup<MaterialMeshInfo> _meshLookup;

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<GamingTag>();
            state.RequireForUpdate<EndSimulationEntityCommandBufferSystem.Singleton>();
            state.RequireForUpdate<HideFowAgentRequest>();
            _childrenLookup = state.GetBufferLookup<LinkedEntityGroup>(true);
            _meshLookup = state.GetComponentLookup<MaterialMeshInfo>(true);
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            _childrenLookup.Update(ref state);
            _meshLookup.Update(ref state);
            new HideOrShowFowAgentJob
            {
                ChildrenLookup = _childrenLookup,
                MeshLookup = _meshLookup,
                ECB = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>()
                    .CreateCommandBuffer(state.WorldUnmanaged).AsParallelWriter(),
            }.ScheduleParallel();
        }

 

        [BurstCompile]
        private partial struct HideOrShowFowAgentJob : IJobEntity
        {
            [ReadOnly] public BufferLookup<LinkedEntityGroup> ChildrenLookup;
            [ReadOnly] public ComponentLookup<MaterialMeshInfo> MeshLookup;
            public EntityCommandBuffer.ParallelWriter ECB;

            private void Execute([ChunkIndexInQuery] int index, Entity selfEntity, in HideFowAgentRequest request)
            {
                ECB.RemoveComponent<HideFowAgentRequest>(index, selfEntity);
                HideOrShowRecursively(selfEntity, request.Hide, index);
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