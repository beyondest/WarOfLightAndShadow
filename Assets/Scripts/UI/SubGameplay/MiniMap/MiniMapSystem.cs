using SparFlame.Components.General;
using SparFlame.Components.SubGameplay;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Entities.Graphics;
using Unity.Rendering;

namespace SparFlame.Systems.Map
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
            state.RequireForUpdate<SubGamingTag>();
            _miniMapMaterialLookup = state.GetComponentLookup<MiniMapMaterialTag>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var playerFactionData = SystemAPI.GetSingleton<PlayerFactionData>();
            var colorConfig = SystemAPI.GetSingleton<MiniMapConfig>();
            _miniMapMaterialLookup.Update(ref state);
    
            var ecb = new EntityCommandBuffer(Allocator.Temp);
            
            foreach (var (attr, entity) in SystemAPI.Query<RefRO<SubGameplayGeneralAttr>>().WithEntityAccess().WithNone<MiniMapInitCompleteTag>())
            {
                var buffer = SystemAPI.GetBuffer<LinkedEntityGroup>(entity);
                ecb.AddComponent<MiniMapInitCompleteTag>( entity);
                var relationship =
                    FactionUtils.GetRelationship(playerFactionData, attr.ValueRO.Faction, attr.ValueRO.SubFaction);
                if(attr.ValueRO.Faction == FactionTag.Neutral)continue;
                var color = relationship switch
                {
                    Relationship.Ally => colorConfig.AllyColor,
                    Relationship.Hostile => colorConfig.HostileColor,
                    Relationship.Player => colorConfig.PlayerColor,
                    _ => colorConfig.NeutralColor
                };
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