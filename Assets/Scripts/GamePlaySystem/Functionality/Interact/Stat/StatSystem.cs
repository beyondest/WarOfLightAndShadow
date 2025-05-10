using System;
using System.Runtime.CompilerServices;
using SparFlame.GamePlaySystem.Building;
using SparFlame.GamePlaySystem.Garrison;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using SparFlame.GamePlaySystem.General;
using SparFlame.GamePlaySystem.Movement;
using SparFlame.GamePlaySystem.Ooc;
using SparFlame.GamePlaySystem.Resource;
using SparFlame.GamePlaySystem.Units;
using SparFlame.GamePlaySystem.UnitSelection;
using Unity.Transforms;
// ReSharper disable SwitchStatementHandlesSomeKnownEnumValuesWithDefault

namespace SparFlame.GamePlaySystem.Interact
{
    [UpdateBefore(typeof(GarrisonSystem))]
    public partial struct StatSystem : ISystem
    {
        private ComponentLookup<GeneralAttr> _interactableAttrLookup;
        private ComponentLookup<VolumeObstacleTag> _volumeObstacleTagLookup;
        private ComponentLookup<StatData> _statDataLookup;
        private ComponentLookup<LocalTransform> _localTransformLookup;
        private ComponentLookup<ResourceAttr> _resourceAttrLookup;
        private ComponentLookup<RenewableData> _renewableResourceDataLookup;
        private ComponentLookup<InGarrison> _inGarrisonLookup;
        private ComponentLookup<OocTag> _oocTagLookup;
        private ComponentLookup<BuildingAttr> _buildingAttrLookup;
        private ComponentLookup<InTeamTag> _inTeamTagLookup;
        private ComponentLookup<UnitAttr> _unitAttrLookup;
        private ComponentLookup<DwellingAttr> _dwellingAttrLookup;

        private BufferLookup<InsightTarget> _insightTargetLookup;
        private BufferLookup<CostList> _costListLookup;


        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<PlayerFactionData>();
            state.RequireForUpdate<EndSimulationEntityCommandBufferSystem.Singleton>();
            state.RequireForUpdate<GamingTag>();
            state.RequireForUpdate<EnemyResourceDataTag>();
            state.RequireForUpdate<AllyResourceDataTag>();
            state.RequireForUpdate<StatSystemConfig>();
            state.RequireForUpdate<SightSystemConfig>();
            state.RequireForUpdate<OocSystemConfig>();

            _interactableAttrLookup = state.GetComponentLookup<GeneralAttr>(true);
            _volumeObstacleTagLookup = state.GetComponentLookup<VolumeObstacleTag>(true);
            _statDataLookup = state.GetComponentLookup<StatData>();
            _localTransformLookup = state.GetComponentLookup<LocalTransform>(true);
            _resourceAttrLookup = state.GetComponentLookup<ResourceAttr>();
            _renewableResourceDataLookup = state.GetComponentLookup<RenewableData>(true);
            _oocTagLookup = state.GetComponentLookup<OocTag>(true);
            _buildingAttrLookup = state.GetComponentLookup<BuildingAttr>(true);
            _inGarrisonLookup = state.GetComponentLookup<InGarrison>(true);
            _insightTargetLookup = state.GetBufferLookup<InsightTarget>();
            _costListLookup = state.GetBufferLookup<CostList>(true);
            _inTeamTagLookup = state.GetComponentLookup<InTeamTag>(true);
            _unitAttrLookup = state.GetComponentLookup<UnitAttr>(true);
            _dwellingAttrLookup = state.GetComponentLookup<DwellingAttr>(true);
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            _statDataLookup.Update(ref state);
            _interactableAttrLookup.Update(ref state);
            _volumeObstacleTagLookup.Update(ref state);
            _localTransformLookup.Update(ref state);
            _resourceAttrLookup.Update(ref state);
            _renewableResourceDataLookup.Update(ref state);
            _insightTargetLookup.Update(ref state);
            _inGarrisonLookup.Update(ref state);
            _oocTagLookup.Update(ref state);
            _buildingAttrLookup.Update(ref state);
            _costListLookup.Update(ref state);
            _inTeamTagLookup.Update(ref state);
            _unitAttrLookup.Update(ref state);
            _dwellingAttrLookup.Update(ref state);
            var autoChooseTargetSystemConfig = SystemAPI.GetSingleton<SightSystemConfig>();
            var oocSystemConfig = SystemAPI.GetSingleton<OocSystemConfig>();
            // var config = SystemAPI.GetSingleton<StatSystemConfig>();
            var ecbSingleton = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>();

            var statRnd = SystemAPI.GetSingletonRW<StatRnd>();
            var rndValue = statRnd.ValueRW.Rnd.NextFloat();


            if (!(SystemAPI.HasSingleton<DebugTag>() && SystemAPI.TryGetSingleton(out StatDebug statDebug)))
            {
                statDebug = new StatDebug
                {
                    enabled = false
                };
            }
            new CheckStatChangeRequest
            {
                ECB = ecbSingleton.CreateCommandBuffer(state.WorldUnmanaged).AsParallelWriter(),
                RandomValue = rndValue,
                
                SightConfig = autoChooseTargetSystemConfig,
                OocConfig = oocSystemConfig,
                PlayerFaction = SystemAPI.GetSingleton<PlayerFactionData>().Value,
                
                // Debug
                StatDebug = statDebug,
                
                // Look up
                GeneralAttrLookup = _interactableAttrLookup,
                ObstacleTagLookup = _volumeObstacleTagLookup,
                StatLookup = _statDataLookup,
                TransformLookup = _localTransformLookup,
                TargetListLookup = _insightTargetLookup,
                ResourceAttrLookup = _resourceAttrLookup,
                RenewableResourceDataLookup = _renewableResourceDataLookup,
                InGarrisonLookup = _inGarrisonLookup,
                OocTagLookup = _oocTagLookup,
                CostListLookup = _costListLookup,
                BuildingAttrLookup = _buildingAttrLookup,
                InTeamTagLookup = _inTeamTagLookup,
                UnitAttrLookup = _unitAttrLookup,
                DwellingAttrLookup = _dwellingAttrLookup,
            }.ScheduleParallel();
        }


        [BurstCompile]
        public partial struct CheckStatChangeRequest : IJobEntity
        {
            public EntityCommandBuffer.ParallelWriter ECB;
            public float RandomValue;
            [ReadOnly] public FactionTag PlayerFaction;
            [ReadOnly] public SightSystemConfig SightConfig;
            [ReadOnly] public OocSystemConfig OocConfig;
            [ReadOnly] public StatDebug StatDebug;
            
            [NativeDisableParallelForRestriction] public BufferLookup<InsightTarget> TargetListLookup;
            [NativeDisableParallelForRestriction] public ComponentLookup<StatData> StatLookup;

            [ReadOnly] public ComponentLookup<GeneralAttr> GeneralAttrLookup;
            [ReadOnly] public ComponentLookup<VolumeObstacleTag> ObstacleTagLookup;
            [ReadOnly] public ComponentLookup<LocalTransform> TransformLookup;
            [ReadOnly] public ComponentLookup<ResourceAttr> ResourceAttrLookup;
            [ReadOnly] public ComponentLookup<RenewableData> RenewableResourceDataLookup;
            [ReadOnly] public ComponentLookup<InGarrison> InGarrisonLookup;
            [ReadOnly] public ComponentLookup<OocTag> OocTagLookup;
            [ReadOnly] public ComponentLookup<DwellingAttr> DwellingAttrLookup;
            [ReadOnly] public BufferLookup<CostList> CostListLookup; // For population release

            [ReadOnly] public ComponentLookup<BuildingAttr> BuildingAttrLookup;
            [ReadOnly] public ComponentLookup<InTeamTag> InTeamTagLookup;
            [ReadOnly] public ComponentLookup<UnitAttr> UnitAttrLookup;
            

            private void Execute([ChunkIndexInQuery] int index, in StatChangeRequest request, Entity entity)
            {
                if (!StatLookup.HasComponent(request.Interactee)) return;
                var interactorAttr = new GeneralAttr();
                if (!request.KillByUnNormal &&
                    !GeneralAttrLookup.TryGetComponent(request.Interactor, out interactorAttr)) return;
                if (!GeneralAttrLookup.TryGetComponent(request.Interactee, out var interacteeAttr)) return;

                // Handle Stat Change Request. This request is destroyed other place, like pop number system
                ref var statInteractee = ref StatLookup.GetRefRW(request.Interactee).ValueRW;
                var statInteractor = StatLookup[request.Interactor].CurValue;
                // This entity is already dead and handled by other request handling process
                if (statInteractee.CurValue <= 0 || statInteractor <= 0) return;

                
                statInteractee.CurValue = request.InteractType == InteractType.Heal
                    ? math.min(statInteractee.MaxValue, statInteractee.CurValue + request.AbsAmount)
                    : math.max(0, statInteractee.CurValue - request.AbsAmount);
                if (StatDebug.enabled)
                {
                    DebugCheck(request, interacteeAttr, ref statInteractee);
                }
                // Check interact type and do different jobs according to interact type 
                CheckInteractType(in request, in statInteractee, in interactorAttr, in interacteeAttr, index);

                // Remove Dead Entities, like units, resources, buildings
                if (statInteractee.CurValue <= 0)
                {
                    RemoveAndSendRequest(ref statInteractee, request.Interactee, index, in interacteeAttr);
                }

                // Destroy this stat change request, cause each request is dealt only one time
                ECB.DestroyEntity(index, entity);
            }

            private void DebugCheck(in StatChangeRequest request,in GeneralAttr interacteeAttr, ref StatData statInteractee)
            {
                if (StatDebug.playerStatGeneralInfinite && interacteeAttr.FactionTag == PlayerFaction)
                    statInteractee.CurValue = statInteractee.MaxValue;
                if (StatDebug.playerStatGeneralZero && interacteeAttr.FactionTag == PlayerFaction)
                    statInteractee.CurValue = 0f;
                if(StatDebug.aiStatGeneralInfinite && interacteeAttr.FactionTag == ~PlayerFaction)
                    statInteractee.CurValue = statInteractee.MaxValue;
                if(StatDebug.aiStatGeneralZero && interacteeAttr.FactionTag == ~PlayerFaction)
                    statInteractee.CurValue = 0f;
                if (StatDebug.playerCrystalStatInfinite && interacteeAttr.FactionTag == PlayerFaction && interacteeAttr.BaseTag == BaseTag.Buildings)
                {
                    var buildingAttr = BuildingAttrLookup[request.Interactee];
                    if (buildingAttr is { SubTypeIndex: (int)OrnamentType.Crystal, Type: BuildingType.Ornaments })
                    {
                        statInteractee.CurValue = statInteractee.MaxValue;
                    }
                }
                if (StatDebug.playerCrystalStatZero && interacteeAttr.FactionTag == PlayerFaction && interacteeAttr.BaseTag == BaseTag.Buildings)
                {
                    var buildingAttr = BuildingAttrLookup[request.Interactee];
                    if (buildingAttr is { SubTypeIndex: (int)OrnamentType.Crystal, Type: BuildingType.Ornaments })
                    {
                        statInteractee.CurValue = 0f;
                    }
                }
                if (StatDebug.aiCrystalStatInfinite && interacteeAttr.FactionTag == ~PlayerFaction && interacteeAttr.BaseTag == BaseTag.Buildings)
                {
                    var buildingAttr = BuildingAttrLookup[request.Interactee];
                    if (buildingAttr is { SubTypeIndex: (int)OrnamentType.Crystal, Type: BuildingType.Ornaments })
                    {
                        statInteractee.CurValue = statInteractee.MaxValue;
                    }
                }
                if (StatDebug.aiCrystalStatZero && interacteeAttr.FactionTag == ~PlayerFaction && interacteeAttr.BaseTag == BaseTag.Buildings)
                {
                    var buildingAttr = BuildingAttrLookup[request.Interactee];
                    if (buildingAttr is { SubTypeIndex: (int)OrnamentType.Crystal, Type: BuildingType.Ornaments })
                    {
                        statInteractee.CurValue = 0f;
                    }
                }
                if (StatDebug.playerUnitStatInfinite && interacteeAttr.FactionTag == PlayerFaction  && interacteeAttr.BaseTag == BaseTag.Units)
                {
                    statInteractee.CurValue = statInteractee.MaxValue;
                }
                if (StatDebug.playerUnitStatZero && interacteeAttr.FactionTag == PlayerFaction  && interacteeAttr.BaseTag == BaseTag.Units)
                {
                    statInteractee.CurValue = 0f;
                }
                if (StatDebug.aiUnitStatInfinite && interacteeAttr.FactionTag == ~PlayerFaction  && interacteeAttr.BaseTag == BaseTag.Units)
                {
                    statInteractee.CurValue = statInteractee.MaxValue;
                }
                if (StatDebug.aiUnitStatZero && interacteeAttr.FactionTag == ~PlayerFaction  && interacteeAttr.BaseTag == BaseTag.Units)
                {
                    statInteractee.CurValue = 0f;
                }
                if(StatDebug.resourceStatInfinite && interacteeAttr.BaseTag == BaseTag.Resources)
                    statInteractee.CurValue = statInteractee.MaxValue;
                if(StatDebug.resourceStatZero && interacteeAttr.BaseTag == BaseTag.Resources)
                    statInteractee.CurValue = 0f;
            }

            private void CheckInteractType(in StatChangeRequest request, in StatData statInteractee,
                in GeneralAttr interactorAttr, in GeneralAttr interacteeAttr,
                int index)
            {
                switch (request.InteractType)
                {
                    // Raise attacker value in target list only when this hit will not kill the target
                    // Interactor stat must > 0 because it is checked before
                    case InteractType.Attack:
                    {
                        // TODO : This should cause a race condition, but it works fine for now.
                        // Update interactee targetList if it has
                        if (statInteractee.CurValue > 0
                            && TargetListLookup.TryGetBuffer(request.Interactee, out var targetsBuffer))
                        {
                            int i;
                            for (i = 0; i < targetsBuffer.Length; i++) // Look for interactor
                            {
                                var target = targetsBuffer[i];
                                if (target.Entity == request.Interactor)
                                    break;
                            }

                            // Target Not in sight but get attacked, should add it to target list
                            if (i == targetsBuffer.Length)
                            {
                                ECB.AppendToBuffer(index, request.Interactee, new InsightTarget
                                {
                                    Entity = request.Interactor,
                                    PriorityValue = 0f,
                                    DisValue = 0f,
                                    StatChangValue = 0f,
                                    InteractOverride = 0f,
                                    MemoryValue = 0f,
                                    TotalValue = 0f
                                });
                            }
                            else
                            {
                                var target = targetsBuffer[i];
                                var statChangeValue = CalStatChangeValue(request.AbsAmount, in SightConfig);
                                target.StatChangValue += statChangeValue;
                                targetsBuffer[i] = target;
                            }
                        }
                        // Update Ooc info(attack state or under attack state tag)

                        if (statInteractee.CurValue > 0)
                            UpdateOocInfo(request, interactorAttr, interacteeAttr, index);
                        StatUtils.GeneratePopNumberRequest(ref TransformLookup, request, interactorAttr, index, ECB);
                        break;
                    }
                    case InteractType.Harvest:
                        var resourceAttr = ResourceAttrLookup[request.Interactee];
                        StatUtils.GenerateHarvestResourceRequest(request, interactorAttr, resourceAttr, index, ECB);
                        StatUtils.GeneratePopNumberRequest(ref TransformLookup, request, interactorAttr, index, ECB);
                        break;
                    case InteractType.Heal:
                        StatUtils.GeneratePopNumberRequest(ref TransformLookup, request, interactorAttr, index, ECB);
                        break;
                }
            }


            /// <summary>
            /// This method should contain every post process of that dead entity,
            /// cause entity is destroyed and invalid after this system update
            /// </summary>
            /// <param name="statInteractee"></param>
            /// <param name="interacteeEntity"></param>
            /// <param name="index"></param>
            /// <param name="interacteeAttr"></param>
            private void RemoveAndSendRequest(ref StatData statInteractee, Entity interacteeEntity, int index,
                in GeneralAttr interacteeAttr)
            {
                switch (interacteeAttr.BaseTag)
                {
                    case BaseTag.Units:
                        StatUtils.GenerateGarrisonUnitDieRequest(interacteeEntity, index, interacteeAttr,
                            ref InGarrisonLookup, ECB);
                        StatUtils.GenerateReleasePopulationRequest(interacteeEntity, index, interacteeAttr,
                            ref CostListLookup, ECB);

                        if (InTeamTagLookup.TryGetComponent(interacteeEntity, out var inTeamTag))
                        {
                            var request = ECB.CreateEntity(index);
                            ECB.AddComponent(index, request, new RemoveFromTeamRequest
                            {
                                UnitAttr = UnitAttrLookup[interacteeEntity],
                                BelongsToTeam = inTeamTag.BelongsToTeam,
                                UnitToRemove = interacteeEntity
                            });
                            ECB.AddComponent<GameplayEntityTag>(index, request);

                        }

                        var entity = ECB.CreateEntity(index);
                        ECB.AddComponent<GameplayEntityTag>(index, entity);
                        ECB.AddComponent(index, entity, new UnitSelectReduceRequest
                        {
                            IsDead = true,
                            SelectedEntity = Entity.Null
                        });
                        ECB.DestroyEntity(index, interacteeEntity);

                        break;
                    case BaseTag.Buildings:
                        if (ObstacleTagLookup.HasComponent(interacteeEntity))
                        {
                            StatUtils.GenerateDestroyObstacleRequest(interacteeEntity, false, index, ECB);
                        }

                        var buildingAttr = BuildingAttrLookup[interacteeEntity];
                        if (buildingAttr is { Type: BuildingType.Ornaments, SubTypeIndex: (int)OrnamentType.Crystal })
                        {
                            StatUtils.GenerateChangeOccupiedTagRequest(in interacteeAttr,
                                TransformLookup[interacteeEntity].Position, ECB, index);
                            /*var request = ECB.CreateEntity(index);
                            if (interacteeAttr.FactionTag != PlayerFaction)
                            {
                                ECB.AddComponent(index, request, new DestroyEnemyBaseRequest
                                {
                                    Base = interacteeEntity
                                });
                            }*/
                           
                        }

                        if (buildingAttr.Type == BuildingType.Dwellings)
                        {
                            StatUtils.GenerateDwellingDestroyResourceChangeRequest(interacteeEntity, index,
                                interacteeAttr, DwellingAttrLookup[interacteeEntity], ECB);
                        }

                        ECB.DestroyEntity(index, interacteeEntity);
                        break;
                    case BaseTag.Resources:
                        // Check if renewable resource
                        if (RenewableResourceDataLookup.TryGetComponent(interacteeEntity, out var renewableData))
                        {
                            var resourceAttr = ResourceAttrLookup[interacteeEntity];
                            renewableData.RegeneratingLeftTime = renewableData.RegenerationTimeSeconds;
                            var bias = (resourceAttr.AmountRange.upper - resourceAttr.AmountRange.lower) * RandomValue;
                            statInteractee.MaxValue = (int)(resourceAttr.AmountRange.lower + bias);
                            statInteractee.CurValue = statInteractee.MaxValue;
                            ECB.AddComponent<RegeneratingTag>(index, interacteeEntity);
                            ECB.SetComponent(index, interacteeEntity, renewableData);
                        }
                        else
                        {
                            if (ObstacleTagLookup.HasComponent(interacteeEntity))
                            {
                                StatUtils.GenerateDestroyObstacleRequest(interacteeEntity, true, index, ECB);
                            }

                            ECB.DestroyEntity(index, interacteeEntity);
                        }

                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }


            private void UpdateOocInfo(StatChangeRequest request, GeneralAttr interactorAttr,
                GeneralAttr interacteeAttr, int index)
            {
                var interacteeSeconds = interacteeAttr.BaseTag == BaseTag.Buildings
                    ? OocConfig.BuildingOocSeconds
                    : OocConfig.UnitOocSeconds;
                var interactorSeconds = interactorAttr.BaseTag == BaseTag.Buildings
                    ? OocConfig.BuildingOocSeconds
                    : OocConfig.UnitOocSeconds;
                if (OocTagLookup.HasComponent(request.Interactee))
                {
                    ECB.SetComponent(index, request.Interactee, new OocTag
                    {
                        Seconds = interacteeSeconds
                    });
                }
                else
                {
                    ECB.AddComponent(index, request.Interactee, new OocTag
                    {
                        Seconds = interacteeSeconds
                    });
                }

                // Set interactor ooc data
                if (OocTagLookup.HasComponent(request.Interactor))
                {
                    ECB.SetComponent(index, request.Interactor, new OocTag
                    {
                        Seconds = interactorSeconds
                    });
                }
                else
                {
                    ECB.AddComponent(index, request.Interactor, new OocTag
                    {
                        Seconds = interactorSeconds
                    });
                }
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            private static float CalStatChangeValue(float requestAmount, in SightSystemConfig config)
            {
                return requestAmount * config.StatValueChangeMultiplier;
            }
        }
    }
}