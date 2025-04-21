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
using SparFlame.GamePlaySystem.UnitSelection;
using Unity.Transforms;

namespace SparFlame.GamePlaySystem.Interact
{
    [UpdateBefore(typeof(TransformSystemGroup))]
    [UpdateBefore(typeof(ResourceSystem))]
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
        private BufferLookup<InsightTarget> _insightTargetLookup;
        private BufferLookup<CostList> _costListLookup;

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<EndSimulationEntityCommandBufferSystem.Singleton>();
            state.RequireForUpdate<NotPauseTag>();
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
            _oocTagLookup = state.GetComponentLookup<OocTag>();
            _inGarrisonLookup = state.GetComponentLookup<InGarrison>(true);
            _insightTargetLookup = state.GetBufferLookup<InsightTarget>();
            _costListLookup = state.GetBufferLookup<CostList>(true);
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
            _costListLookup.Update(ref state);
            var autoChooseTargetSystemConfig = SystemAPI.GetSingleton<SightSystemConfig>();
            var oocSystemConfig = SystemAPI.GetSingleton<OocSystemConfig>();
            // var config = SystemAPI.GetSingleton<StatSystemConfig>();
            var ecbSingleton = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>();

            var statRnd = SystemAPI.GetSingletonRW<StatRnd>();
            var rndValue = statRnd.ValueRW.Rnd.NextFloat();
            new CheckStatChangeRequest
            {
                ECB = ecbSingleton.CreateCommandBuffer(state.WorldUnmanaged).AsParallelWriter(),
                RandomValue = rndValue,
                InteractableAttrLookup = _interactableAttrLookup,
                ObstacleTagLookup = _volumeObstacleTagLookup,
                StatLookup = _statDataLookup,
                TransformLookup = _localTransformLookup,
                TargetListLookup = _insightTargetLookup,
                ResourceAttrLookup = _resourceAttrLookup,
                RenewableResourceDataLookup = _renewableResourceDataLookup,
                InGarrisonLookup = _inGarrisonLookup,
                OocTagLookup = _oocTagLookup,
                CostListLookup = _costListLookup,
                SightConfig = autoChooseTargetSystemConfig,
                OocConfig = oocSystemConfig
            }.ScheduleParallel();
        }




        [BurstCompile]
        public partial struct CheckStatChangeRequest : IJobEntity
        {
            public EntityCommandBuffer.ParallelWriter ECB;
            public float RandomValue;
            [NativeDisableParallelForRestriction] public BufferLookup<InsightTarget> TargetListLookup;
            [NativeDisableParallelForRestriction] public ComponentLookup<StatData> StatLookup;

            [ReadOnly] public ComponentLookup<GeneralAttr> InteractableAttrLookup;
            [ReadOnly] public ComponentLookup<VolumeObstacleTag> ObstacleTagLookup;
            [ReadOnly] public ComponentLookup<LocalTransform> TransformLookup;
            [ReadOnly] public ComponentLookup<ResourceAttr> ResourceAttrLookup;
            [ReadOnly] public ComponentLookup<RenewableData> RenewableResourceDataLookup;
            [ReadOnly] public ComponentLookup<InGarrison> InGarrisonLookup;
            [ReadOnly] public ComponentLookup<OocTag> OocTagLookup;
            [ReadOnly] public BufferLookup<CostList> CostListLookup; // For population release
            [ReadOnly] public SightSystemConfig SightConfig;
            [ReadOnly] public OocSystemConfig OocConfig;


            private void Execute([ChunkIndexInQuery] int index, in StatChangeRequest request, Entity entity)
            {
                // TODO : Check where wrong
                // These 2 should not happen when the stat system updates after interact state machine, but it happens sometimes.
                if (!StatLookup.HasComponent(request.Interactee)) return;
                if (!InteractableAttrLookup.TryGetComponent(request.Interactor, out var interactorAttr)) return;
                if (!InteractableAttrLookup.TryGetComponent(request.Interactee, out var interacteeAttr)) return;

                // Handle Stat Change Request. This request is destroyed other place, like pop number system
                ref var statInteractee = ref StatLookup.GetRefRW(request.Interactee).ValueRW;

                // This entity is already dead and handled by other request handling process
                if (statInteractee.CurValue <= 0) return;

                statInteractee.CurValue = request.InteractType == InteractType.Heal
                    ? math.min(statInteractee.MaxValue, statInteractee.CurValue + request.Amount)
                    : math.max(0, statInteractee.CurValue - request.Amount);

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

            private void CheckInteractType(in StatChangeRequest request, in StatData statInteractee,
                in GeneralAttr interactorAttr, in GeneralAttr interacteeAttr,
                int index)
            {
                switch (request.InteractType)
                {
                    // Raise attacker value in target list only when this hit will not kill the target
                    case InteractType.Attack when statInteractee.CurValue > 0:
                    {
                        // TODO : This should cause a race condition, but it works fine for now.
                        // Update interactee targetList if it has
                        if (TargetListLookup.TryGetBuffer(request.Interactee, out var targetsBuffer))
                        {
                            int i;
                            for (i = 0; i < targetsBuffer.Length; i++) // Look for interactor
                            {
                                var target = targetsBuffer[i];
                                if (target.Entity != request.Interactor) continue;
                                var statChangeValue = CalStatChangeValue(request.Amount, in SightConfig);
                                target.StatChangValue += statChangeValue;
                                targetsBuffer[i] = target;
                            }

                            // Target Not in sight but get attacked, should add it to target list
                            if (i == targetsBuffer.Length)
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
                        // Update Ooc info(attack state or under attack state tag)
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
                        StatUtils.GenerateGarrisonUnitDieRequest(interacteeEntity, index, interacteeAttr, ref InGarrisonLookup, ECB);
                        StatUtils.GenerateReleasePopulationRequest(interacteeEntity, index, interacteeAttr, ref CostListLookup, ECB);
                        var entity = ECB.CreateEntity(index);
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

   

            private void UpdateOocInfo(StatChangeRequest request, GeneralAttr interactorAttr, GeneralAttr interacteeAttr, int index)
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