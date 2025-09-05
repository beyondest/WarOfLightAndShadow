using SparFlame.Components.General;
using SparFlame.Components.Input;
using SparFlame.Components.SubGameplay;
using Unity.Burst;
using Unity.Entities;
using Unity.Rendering;

namespace SparFlame.Systems.General.VFX
{
    
    public partial struct HighLightSystem : ISystem
    {

        private struct HighLightData : IComponentData
        {
            public Entity PreHighLightEntity;

        }
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<WaitInfo>();
            state.RequireForUpdate<HighLightSystemConfig>();
            state.RequireForUpdate<HighLightData>();
            state.RequireForUpdate<PlayerFactionData>();
            state.RequireForUpdate<InputMouseData>();
            state.RequireForUpdate<GameStatusData>();
            state.EntityManager.CreateSingleton(new HighLightData
            {
                PreHighLightEntity = Entity.Null
            });
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var gameStatus = SystemAPI.GetSingleton<GameStatusData>().Value;
            if(gameStatus != GameStatus.MainGaming && gameStatus != GameStatus.SubGaming)return;
            var waitInfo = SystemAPI.GetSingleton<WaitInfo>();
            if(waitInfo.WaitType != WaitType.None)return;
            
            var inputMouseData = SystemAPI.GetSingleton<InputMouseData>();
            var playerFactionData = SystemAPI.GetSingleton<PlayerFactionData>();
            var config = SystemAPI.GetSingleton<HighLightSystemConfig>();
            ref var data = ref SystemAPI.GetSingletonRW<HighLightData>().ValueRW;
            if(data.PreHighLightEntity == inputMouseData.HitEntity && !inputMouseData.IsOverUI)return;
            ResetPreviousHighLightEntity(ref state, ref data);
            if(inputMouseData.IsOverUI) return;
            if (inputMouseData.HitEntity != Entity.Null)
            {
                data.PreHighLightEntity = inputMouseData.HitEntity;
                if(!SystemAPI.HasBuffer<LinkedEntityGroup>(inputMouseData.HitEntity))return;
                if(!SystemAPI.HasComponent<SubGameplayGeneralAttr>(inputMouseData.HitEntity))return;
                var generalAttr = SystemAPI.GetComponent<SubGameplayGeneralAttr>(inputMouseData.HitEntity);
                var groups = SystemAPI.GetBuffer<LinkedEntityGroup>(inputMouseData.HitEntity);
                for (int i = 1; i < groups.Length; i++)
                {
                    var entity = groups[i].Value;
                    if (SystemAPI.HasComponent<HighLightableTag>(entity))
                    {
                        var scale = generalAttr.BaseTag switch
                        {
                            BaseTag.Buildings => config.BuildingHighLightScale,
                            BaseTag.Resources => config.ResourceHighLightScale,
                            BaseTag.Units => config.UnitHighLightScale,
                            _ => 0f // this should never hapen
                        };
                        SystemAPI.SetComponent(entity, new HightLightScaleFloatOverride
                        {
                            Value = scale
                        });
                        var relationship = FactionUtils.GetRelationship(playerFactionData, generalAttr.Faction,
                            generalAttr.SubFaction);
                        var color = relationship switch
                        {
                            Relationship.Ally => config.AllyHighLightColor,
                            Relationship.Hostile => config.HostileHighLightColor,
                            Relationship.Player => config.AllyHighLightColor,
                            Relationship.Neutral => config.NeutralHighLightColor,
                            _ => config.NeutralHighLightColor // this should never happen
                        };

                        SystemAPI.SetComponent(entity, new HighLightColorVector4Override
                        {
                            Value = color
                        });
                    }
                }
            }
            
        }

        private void ResetPreviousHighLightEntity(ref SystemState state, ref HighLightData data)
        {
            
            if(data.PreHighLightEntity == Entity.Null)return;
            // The entity is dead
            if (!SystemAPI.HasComponent<SubGameplayGeneralAttr>(data.PreHighLightEntity))
            {
                data.PreHighLightEntity = Entity.Null;
                return;
            }
            var groups = SystemAPI.GetBuffer<LinkedEntityGroup>(data.PreHighLightEntity);
            for (int i = 1; i < groups.Length; i++)
            {
                var entity = groups[i].Value;
                if (SystemAPI.HasComponent<HighLightableTag>(entity))
                {
                    SystemAPI.SetComponent(entity, new HightLightScaleFloatOverride
                    {
                        Value = 0f
                    });
                }
            }
            data.PreHighLightEntity = Entity.Null;
           
        }
    }
}