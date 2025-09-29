using SparFlame.Components.General;
using SparFlame.Components.MainGameplay;
using SparFlame.Components.SubGameplay;
using SparFlame.Core.Utils;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Transforms;

// ReSharper disable Unity.Entities.SingletonMustBeRequested

namespace SparFlame.Systems.SubGameplay.EnemyAI
{
    [UpdateInGroup(typeof(InitializationSystemGroup))]
    public partial struct SubGameplayInitDistinguishSystem : ISystem
    {
        private ComponentLookup<BuildingAttr> _buildingAttrLookup;
        private BufferLookup<LinkedEntityGroup> _linkedEntityGroupLookup;
        private ComponentLookup<UnitAttr> _unitAttrLookup;

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<EndInitializationEntityCommandBufferSystem.Singleton>();
            state.RequireForUpdate<GeneralRandom>();
            state.RequireForUpdate<PlayerFactionData>();
            state.RequireForUpdate<GameStatusData>();
            _buildingAttrLookup = state.GetComponentLookup<BuildingAttr>(true);
            _linkedEntityGroupLookup = state.GetBufferLookup<LinkedEntityGroup>(true);
            _unitAttrLookup = state.GetComponentLookup<UnitAttr>(true);
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var gameStatus = SystemAPI.GetSingleton<GameStatusData>();
            if(gameStatus.Value == GameStatus.NotStarted)return;
            _buildingAttrLookup.Update(ref state);
            _linkedEntityGroupLookup.Update(ref state);
            _unitAttrLookup.Update(ref state);
            var playerFactionData = SystemAPI.GetSingleton<PlayerFactionData>();
            var ecbP = SystemAPI.GetSingleton<EndInitializationEntityCommandBufferSystem.Singleton>()
                .CreateCommandBuffer(state.WorldUnmanaged).AsParallelWriter();
            var job =  new DistinguishEnemyJob
            {
                ECB = ecbP,
                BuildingAttrLookup = _buildingAttrLookup,
                PlayerFactionData = playerFactionData
            }.ScheduleParallel(state.Dependency);
            job.Complete();
 
        }

        [BurstCompile]
        [WithNone(typeof(AITag))]
        [WithNone(typeof(PlayerTag))]
        [WithNone(typeof(ResourceAttr))]
        [WithNone(typeof(FakeUnitNeedAddToArmyGroupAfterAssignSingleId))]
        public partial struct DistinguishEnemyJob : IJobEntity
        {
            public EntityCommandBuffer.ParallelWriter ECB;
            [ReadOnly] public ComponentLookup<BuildingAttr> BuildingAttrLookup;
            [ReadOnly] public PlayerFactionData PlayerFactionData;

            private void Execute([ChunkIndexInQuery] int index, in SubGameplayGeneralAttr attr,
                in LocalTransform transform, Entity selfEntity)
            {
                // General distinguish
                var relationship = FactionUtils.GetRelationship(PlayerFactionData.faction,
                    PlayerFactionData.subFaction, attr.Faction, attr.SubFaction);
                if (relationship is Relationship.Self or Relationship.Ally)
                {
                    ECB.AddComponent<PlayerTag>(index, selfEntity);
                    if (attr.BaseTag == BaseTag.Buildings)
                    {
                        var buildingAttr = BuildingAttrLookup[selfEntity];
                        // Add crystal monitor data for enemy AI
                        if (buildingAttr is { Type: BuildingType.Ornaments, SubTypeIndex: (int)OrnamentType.Crystal } )
                        {
                            ECB.AddBuffer<SurroundingData>(index, selfEntity);
                            ECB.AddComponent(index, selfEntity, new SurroundingValue
                            {
                                Value = 0f
                            });
                        }
                    }
                    return;
                }

                ECB.AddComponent<AITag>(index, selfEntity);
                
                // Detail distinguishes
                switch (attr.BaseTag)
                {
                    case BaseTag.Buildings:
                    {
                        var buildingAttr = BuildingAttrLookup[selfEntity];
                        if (buildingAttr is { Type: BuildingType.ConjuringShrines })
                        {
                        }

                        if (buildingAttr is { Type: BuildingType.Ornaments, SubTypeIndex: (int)OrnamentType.Crystal }
                            )
                        {
                            ECB.AddComponent<AIBaseBelongsTo>(index, selfEntity);
                            ECB.AddBuffer<AIBaseTeamGeneralData>(index,
                                selfEntity); // This buffer must be initialized here, and never change length whole game
                            ECB.AddBuffer<AIBaseTeamAvailableData>(index,
                                selfEntity); // This buffer is initialized by other systems, and may change length
                            for (var i = 0; i < 4; i++)
                            {
                                ECB.AppendToBuffer(index, selfEntity, new AIBaseTeamGeneralData
                                {
                                    TeamType = (AITeamType)i,
                                    CurCount = 0
                                });
                            }
                        }

                        break;
                    }
                    case BaseTag.Units:
                        ECB.AddComponent<AIUnitCommandData>(index, selfEntity);
                        ECB.AddComponent<AIUnitCommandUpdate>(index, selfEntity);
                        ECB.SetComponentEnabled<AIUnitCommandUpdate>(index, selfEntity, false);

                        break;
                    case BaseTag.Resources:
                        break;
                    default:
                        BurstSafe.UnexpectedEnum(attr.BaseTag);
                        break;
                }
            }
        }
        
        

    }
}