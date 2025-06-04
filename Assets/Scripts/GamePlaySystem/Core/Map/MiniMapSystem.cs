using System.Collections.Generic;
using System.Runtime.CompilerServices;
using SparFlame.GamePlaySystem.General;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Entities.Graphics;
using Unity.Rendering;

namespace SparFlame.GamePlaySystem.Map
{
    public partial struct MiniMapSystem : ISystem
    {
        private ComponentLookup<MiniMapMaterialTag> _miniMapMaterialLookup;
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<MiniMapConfig>();
            state.RequireForUpdate<PlayerFactionData>();
            state.RequireForUpdate<EndSimulationEntityCommandBufferSystem.Singleton>();
            state.RequireForUpdate<GamingTag>();
            _miniMapMaterialLookup = state.GetComponentLookup<MiniMapMaterialTag>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var playerFaction = SystemAPI.GetSingleton<PlayerFactionData>().Value;
            var colorConfig = SystemAPI.GetSingleton<MiniMapConfig>();
            _miniMapMaterialLookup.Update(ref state);
            // new MiniMapJob
            // {
            //     PlayerFaction = playerFaction,
            //     ECB = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>()
            //         .CreateCommandBuffer(state.WorldUnmanaged).AsParallelWriter(),
            //     MiniMapTagLookup = _miniMapMaterialLookup,
            //     Config = colorConfig,
            // }.ScheduleParallel();
            var ecb = new EntityCommandBuffer(Allocator.Temp);
            
            foreach (var (attr, entity) in SystemAPI.Query<RefRO<GeneralAttr>>().WithEntityAccess().WithNone<MiniMapInitCompleteTag>())
            {
                var buffer = SystemAPI.GetBuffer<LinkedEntityGroup>(entity);
                ecb.AddComponent<MiniMapInitCompleteTag>( entity);
                if(attr.ValueRO.FactionTag == FactionTag.Neutral)continue;
                var color = attr.ValueRO.FactionTag == playerFaction ? colorConfig.PlayerColor :
                    colorConfig.EnemyColor;
                foreach (var group in buffer)
                {
                    if (SystemAPI.HasComponent<MiniMapMaterialTag>(group.Value))
                    {
                        ecb.SetComponent( group.Value, new MiniMapColorVector4Override
                        {
                            Value = color
                        });
                        var renderFilterSettings =
                            state.EntityManager.GetSharedComponent<RenderFilterSettings>(group.Value);
                        renderFilterSettings.Layer = colorConfig.Layer;
                        ecb.SetSharedComponent( group.Value, renderFilterSettings );
                    }
                }
            }
            ecb.Playback(state.EntityManager);
            ecb.Dispose();
        }
        
        // [BurstCompile]
        // [WithNone(typeof(MiniMapInitCompleteTag))]
        // public partial struct MiniMapJob : IJobEntity
        // {
        //     public MiniMapConfig Config;
        //     public FactionTag PlayerFaction;
        //     public EntityCommandBuffer.ParallelWriter ECB;
        //     [ReadOnly] public ComponentLookup<MiniMapMaterialTag> MiniMapTagLookup;
        //     private void Execute([ChunkIndexInQuery]int in GeneralAttr generalAttr,in DynamicBuffer<LinkedEntityGroup> groups,
        //         Entity selfEntity)
        //     {
        //         ECB.AddComponent<MiniMapInitCompleteTag>( selfEntity);
        //         if(generalAttr.FactionTag == FactionTag.Neutral)return;
        //         var color = generalAttr.FactionTag == PlayerFaction ? Config.PlayerColor :
        //         Config.EnemyColor;
        //         foreach (var group in groups)
        //         {
        //             if (MiniMapTagLookup.HasComponent(group.Value))
        //             {
        //                ECB.SetComponent( group.Value, new MiniMapColorVector4Override
        //                {
        //                    Value = color
        //                });
        //                ECB.SetSharedComponent( group.Value, new RenderFilterSettings
        //                {
        //                    Layer = Config.Layer
        //                });
        //             }
        //         }
        //     }
        //     
        //    
        // }

    }
}