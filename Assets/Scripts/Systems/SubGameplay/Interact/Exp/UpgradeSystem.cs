using SparFlame.Components.General;
using SparFlame.Components.MainGameplay;
using SparFlame.Components.SubGameplay;
using SparFlame.Components.VFX;
using SparFlame.Core.Utils;
using SparFlame.Systems.General.Audio;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Physics;
using Unity.Transforms;

namespace SparFlame.Systems.SubGameplay.Interact
{
    public struct UpgradeRequest : IComponentData
    {
        public Entity FromEntity;
    }

    [BurstCompile]
    // [UpdateAfter(typeof(StatSystem))]
    // [UpdateBefore(typeof(GarrisonSystem))]
    [UpdateInGroup(typeof(InitializationSystemGroup))]
    public partial struct UpgradeSystem : ISystem
    {
        private NativeHashMap<int, ExpStaticConfig> _expDatabase;

        private ComponentLookup<SubGameplayGeneralAttr> _generalAttrLookup;
        private ComponentLookup<LocalTransform> _localTransformLookup;
        private ComponentLookup<ExpData> _expDataLookup;
        private ComponentLookup<InGarrison> _inGarrisonLookup;
        private ComponentLookup<BasicStateData> _basicStateLookup;
        private ComponentLookup<PhysicsMass> _physicsMassLookup;
        private ComponentLookup<UnitAttr> _unitAttrLookup;
        private ComponentLookup<BuildingAttr> _buildingAttrLookup;
        private ComponentLookup<CapacityBuildingAttr> _dwellingAttrLookup;
        private ComponentLookup<CityTaskUniqueId> _cityTaskUniqueIdLookup;

        private ComponentLookup<StatData> _statDataLookup;
        private ComponentLookup<MovableData> _movableDataLookup;
        private ComponentLookup<AttackAbility> _attackAbilityLookup;
        private ComponentLookup<HealAbility> _healAbilityLookup;
        private ComponentLookup<HarvestAbility> _harvestAbilityLookup;

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<SubGameStatusData>();
            state.RequireForUpdate<WorldTimeData>();
            // state.RequireForUpdate<EndSimulationEntityCommandBufferSystem.Singleton>();
            state.RequireForUpdate<SubGamingTag>();
            state.RequireForUpdate<UpgradeRequest>();
            state.RequireForUpdate<ExpSystemConfig>();
            state.RequireForUpdate<ExpStaticConfig>();

            _generalAttrLookup = state.GetComponentLookup<SubGameplayGeneralAttr>(true);
            _localTransformLookup = state.GetComponentLookup<LocalTransform>(true);
            _expDataLookup = state.GetComponentLookup<ExpData>(true);
            _inGarrisonLookup = state.GetComponentLookup<InGarrison>(true);
            _basicStateLookup = state.GetComponentLookup<BasicStateData>(true);
            _physicsMassLookup = state.GetComponentLookup<PhysicsMass>(true);
            _unitAttrLookup = state.GetComponentLookup<UnitAttr>(true);
            _buildingAttrLookup = state.GetComponentLookup<BuildingAttr>(true);
            _dwellingAttrLookup = state.GetComponentLookup<CapacityBuildingAttr>(true);
            _cityTaskUniqueIdLookup = state.GetComponentLookup<CityTaskUniqueId>(true);

            _statDataLookup = state.GetComponentLookup<StatData>();
            _movableDataLookup = state.GetComponentLookup<MovableData>();
            _attackAbilityLookup = state.GetComponentLookup<AttackAbility>();
            _healAbilityLookup = state.GetComponentLookup<HealAbility>();
            _harvestAbilityLookup = state.GetComponentLookup<HarvestAbility>();
        }

        public void OnDestroy(ref SystemState state)
        {
            if (_expDatabase.IsCreated)
                _expDatabase.Dispose();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            if (!_expDatabase.IsCreated)
            {
                Initialize();
            }

            _harvestAbilityLookup.Update(ref state);
            _healAbilityLookup.Update(ref state);
            _attackAbilityLookup.Update(ref state);
            _movableDataLookup.Update(ref state);
            _statDataLookup.Update(ref state);
            _buildingAttrLookup.Update(ref state);
            _unitAttrLookup.Update(ref state);
            _physicsMassLookup.Update(ref state);
            _basicStateLookup.Update(ref state);
            _inGarrisonLookup.Update(ref state);
            _expDataLookup.Update(ref state);
            _localTransformLookup.Update(ref state);
            _generalAttrLookup.Update(ref state);
            _dwellingAttrLookup.Update(ref state);
            _cityTaskUniqueIdLookup.Update(ref state);
            var ecb = new EntityCommandBuffer(Allocator.TempJob);
            var ecbP = ecb.AsParallelWriter();
            var job =new UpgradeJob
            {
                ExpDatabase = _expDatabase,
                GeneralAttrLookup = _generalAttrLookup,
                LocalTransformLookup = _localTransformLookup,
                ExpDataLookup = _expDataLookup,
                InGarrisonLookup = _inGarrisonLookup,
                BasicStateLookup = _basicStateLookup,
                PhysicsMassLookup = _physicsMassLookup,
                UnitAttrLookup = _unitAttrLookup,
                BuildingAttrLookup = _buildingAttrLookup,
                StatDataLookup = _statDataLookup,
                MovableDataLookup = _movableDataLookup,
                AttackAbilityLookup = _attackAbilityLookup,
                HealAbilityLookup = _healAbilityLookup,
                HarvestAbilityLookup = _harvestAbilityLookup,
                ECB = ecbP,
                CurrentTotalHours = SystemAPI.GetSingleton<WorldTimeData>().totalHours,
                DwellingAttrLookup = _dwellingAttrLookup,
                CityTaskUniqueIdLookup = _cityTaskUniqueIdLookup,
                City = SystemAPI.GetSingleton<SubGameStatusData>().City
            }.ScheduleParallel(state.Dependency);
            job.Complete();
            ecb.Playback(state.EntityManager);
            ecb.Dispose();
        }

        private void Initialize()
        {
            _expDatabase = new NativeHashMap<int, ExpStaticConfig>(10, Allocator.Persistent);
            var buffer = SystemAPI.GetSingletonBuffer<ExpStaticConfig>();
            foreach (var config in buffer)
            {
                _expDatabase.Add(config.GlobalIdx, config);
            }
        }

        [BurstCompile]
        public partial struct UpgradeJob : IJobEntity
        {
            public EntityCommandBuffer.ParallelWriter ECB;
            [ReadOnly] public float CurrentTotalHours;
            [ReadOnly] public Entity City;
            
            [ReadOnly] public NativeHashMap<int, ExpStaticConfig> ExpDatabase;
            [ReadOnly] public ComponentLookup<SubGameplayGeneralAttr> GeneralAttrLookup;
            [ReadOnly] public ComponentLookup<LocalTransform> LocalTransformLookup;
            [ReadOnly] public ComponentLookup<ExpData> ExpDataLookup;
            [ReadOnly] public ComponentLookup<InGarrison> InGarrisonLookup;
            [ReadOnly] public ComponentLookup<BasicStateData> BasicStateLookup;
            [ReadOnly] public ComponentLookup<PhysicsMass> PhysicsMassLookup;
            [ReadOnly] public ComponentLookup<UnitAttr> UnitAttrLookup;
            [ReadOnly] public ComponentLookup<BuildingAttr> BuildingAttrLookup;
            [ReadOnly] public ComponentLookup<CapacityBuildingAttr> DwellingAttrLookup;
            [ReadOnly] public ComponentLookup<CityTaskUniqueId> CityTaskUniqueIdLookup;
            [ReadOnly] public ComponentLookup<InArmyGroup> InArmyGroupLookup;

            [NativeDisableParallelForRestriction] public ComponentLookup<StatData> StatDataLookup;
            [NativeDisableParallelForRestriction] public ComponentLookup<MovableData> MovableDataLookup;
            [NativeDisableParallelForRestriction] public ComponentLookup<AttackAbility> AttackAbilityLookup;
            [NativeDisableParallelForRestriction] public ComponentLookup<HealAbility> HealAbilityLookup;
            [NativeDisableParallelForRestriction] public ComponentLookup<HarvestAbility> HarvestAbilityLookup;


            private void Execute([ChunkIndexInQuery] int index, in UpgradeRequest request,
                Entity selfEntity)
            {
                ECB.DestroyEntity(index, selfEntity);
                // Safety check. If this upgrade entity is dead, do not upgrade
                if (!GeneralAttrLookup.TryGetComponent(request.FromEntity, out var fromEntityGeneralAttr)) return;
                if(!StatDataLookup.TryGetComponent(request.FromEntity, out var stat))return;
                if(stat.curValue <= 0)return;
                
                
                var trans = LocalTransformLookup[request.FromEntity];
                var expData = ExpDataLookup[request.FromEntity];
                var expStaticConfig = ExpDatabase[fromEntityGeneralAttr.PrefabID];
                if (fromEntityGeneralAttr.BaseTag == BaseTag.Units && expData.curLevel <= expStaticConfig.MaxLevel)
                {
                    // Next level upgrade
                    // We do not set curValue here because sometimes exp gain may exceed max value, should pass to next level exp
                    expData.curLevel++;
                    expData.maxValue += expStaticConfig.ExpGainPerLevel;
                    ECB.SetComponent(index, request.FromEntity, expData);
                    ref var statData = ref StatDataLookup.GetRefRW(request.FromEntity).ValueRW;
                    statData.maxValue += expStaticConfig.StatPerLevel;
                    statData.curValue = statData.maxValue;
                    ref var movableData = ref MovableDataLookup.GetRefRW(request.FromEntity).ValueRW;
                    movableData.MoveSpeed += expStaticConfig.MoveSpeedPerLevel;
                    if (AttackAbilityLookup.HasComponent(request.FromEntity))
                    {
                        ref var attackAbility = ref AttackAbilityLookup.GetRefRW(request.FromEntity).ValueRW;
                        attackAbility.Amount += expStaticConfig.AttackAmountPerLevel;
                        attackAbility.Range += expStaticConfig.AttackRangePerLevel;
                        attackAbility.Speed += expStaticConfig.AttackSpeedPerLevel;
                        attackAbility.Targets += expStaticConfig.AttackTargetsPerLevel;
                    }

                    if (HealAbilityLookup.HasComponent(request.FromEntity))
                    {
                        ref var healAbility = ref HealAbilityLookup.GetRefRW(request.FromEntity).ValueRW;
                        healAbility.Amount += expStaticConfig.HealAmountPerLevel;
                        healAbility.Range += expStaticConfig.HealRangePerLevel;
                        healAbility.Speed += expStaticConfig.HealSpeedPerLevel;
                        healAbility.Targets += expStaticConfig.HealTargetsPerLevel;
                    }

                    if (HarvestAbilityLookup.HasComponent(request.FromEntity))
                    {
                        ref var harvestAbility = ref HarvestAbilityLookup.GetRefRW(request.FromEntity).ValueRW;
                        harvestAbility.Amount += expStaticConfig.HarvestAmountPerLevel;
                        harvestAbility.Range += expStaticConfig.HarvestRangePerLevel;
                        harvestAbility.Speed += expStaticConfig.HarvestSpeedPerLevel;
                        harvestAbility.Targets += expStaticConfig.HarvestTargetsPerLevel;
                    }

                    var upgradeVFX = ECB.CreateEntity(index);
                    ECB.AddComponent<SubGameplayEntityTag>(index, upgradeVFX);
                    ECB.AddComponent(index, upgradeVFX, new VFXRequest
                    {
                        SpawnPosition = trans.Position,
                        VFXName = VFXName.UnitUpgrade,
                        RequestType = VFXRequestType.Spawn,
                        KeepDuration = 0f,
                        VFXTrackTarget = request.FromEntity,
                        Filter = new VFXSubFilter
                        {
                            FactionFilterEnable = true,
                            Faction = fromEntityGeneralAttr.Faction,
                            TierFilterEnable = false
                        },
                        StatChangeRequest = default,
                        ParabolaTargetPosition = default,
                    });
                    AudioUtils.PlayAudioClip(AudioName.UnitUpgrade, trans.Position, ECB, index);
                }
                else
                {
                    if (expData.curTier == Tier.Tier3 || expStaticConfig.NextTierPrefab == Entity.Null) return;
                    // Next tier upgrade
                    var nextTierEntity = ECB.Instantiate(index, expStaticConfig.NextTierPrefab);
                    ECB.AddComponent<SubGameplayEntityTag>(index, nextTierEntity);
                    ECB.SetComponent(index, nextTierEntity, trans);
                    
                    // If this unit is a garrisoned unit, the upgraded unit still needs to be garrisoned
                    if (InGarrisonLookup.TryGetComponent(request.FromEntity, out var preInGarrison))
                    {
                        if (preInGarrison.InBuilding)
                        {
                            var physicsMass = PhysicsMassLookup[expStaticConfig.NextTierPrefab];
                            physicsMass.InverseMass = 0.0f;
                            preInGarrison.PriorMass = physicsMass.InverseMass;
                            ECB.SetComponent(index, nextTierEntity, physicsMass);
                        }
                        ECB.AddComponent(index, nextTierEntity, preInGarrison);
                        var preBasicStateData = BasicStateLookup[request.FromEntity];
                        ECB.SetComponent(index, nextTierEntity, preBasicStateData);
                        ECB.SetComponentEnabled<IdleStateTag>(index, nextTierEntity, false);
                        switch (preBasicStateData.CurState)
                        {
                            case InteractState.Attacking:
                                ECB.SetComponentEnabled<AttackStateTag>(index, nextTierEntity, true);
                                break;
                            case InteractState.Moving:
                                ECB.SetComponentEnabled<MovingStateTag>(index, nextTierEntity, true);
                                break;
                            case InteractState.Garrison:
                                ECB.SetComponentEnabled<GarrisonStateTag>(index, nextTierEntity, true);
                                break;
                            case InteractState.Harvesting:
                                ECB.SetComponentEnabled<HarvestStateTag>(index, nextTierEntity, true);
                                break;
                            case InteractState.Healing:
                                ECB.SetComponentEnabled<HealStateTag>(index, nextTierEntity, true);
                                break;
                            case InteractState.Idle:
                                ECB.SetComponentEnabled<IdleStateTag>(index, nextTierEntity, true);
                                break;
                        }

                        var garrisonInBuildingRequest = ECB.CreateEntity(index);
                        ECB.AddComponent<SubGameplayEntityTag>(index, garrisonInBuildingRequest);
                        ECB.AddComponent(index, garrisonInBuildingRequest, new GarrisonInBuildingRequest
                        {
                            BuildingEntity = preInGarrison.BuildingEntity,
                            Id = GeneralAttrLookup[expStaticConfig.NextTierPrefab].PrefabID,
                            UnitEntity = nextTierEntity,
                            UnitType = UnitAttrLookup[expStaticConfig.NextTierPrefab].Type,
                        });
                    }

                    if (InArmyGroupLookup.TryGetComponent(request.FromEntity, out var inArmyGroup))
                    {
                        var addToArmyGroupRequest = ECB.CreateEntity(index);
                        ECB.AddComponent<SubGameplayEntityTag>(index, addToArmyGroupRequest);
                        ECB.AddComponent(index, addToArmyGroupRequest, new AddToArmyGroupRequest
                        {
                            ArmyGroup = inArmyGroup.BelongsTo,
                            Unit = nextTierEntity,
                            Type = AddToArmyGroupType.OnlySpecifiedUnit
                        });
                        
                    }

                    if (fromEntityGeneralAttr.BaseTag == BaseTag.Buildings)
                    {
                        var buildingAttr = BuildingAttrLookup[request.FromEntity];
                        ECB.AddComponent(index, nextTierEntity, new ConstructingTimer
                        {
                            builtUpTargetTotalHours = BuildingAttrLookup[expStaticConfig.NextTierPrefab]
                                .ConstructTimeHours + CurrentTotalHours
                        });
                        if (buildingAttr.Type == BuildingType.CapacityBuildings)
                        {
                            
                            var capacityBuildingAttr = DwellingAttrLookup[expStaticConfig.NextTierPrefab];
                            ECB.AddComponent(index, nextTierEntity, CityTaskUniqueIdLookup[request.FromEntity]);
                            
                            var cityTaskAddRequest = ECB.CreateEntity(index);
                            ECB.AddComponent<SubGameplayEntityTag>(index, cityTaskAddRequest);
                            ECB.AddComponent(index,cityTaskAddRequest,new ResourceChangeRequest
                            {
                                City = City,
                                AbsAmount = capacityBuildingAttr.StorageAmount,
                                ResourceType = capacityBuildingAttr.ResourceType,
                                FinishTotalHours = BuildingAttrLookup[expStaticConfig.NextTierPrefab]
                                    .ConstructTimeHours + CurrentTotalHours,
                                FromBuildingUniqueId = CityTaskUniqueIdLookup[request.FromEntity].value,
                                RequestType = ResourceRequestType.StorageAddByTask,
                            });
                        }

                        if (buildingAttr.Type == BuildingType.ConjuringShrines)
                        {
                            ECB.AddComponent(index, nextTierEntity, CityTaskUniqueIdLookup[request.FromEntity]);
                        }
                    }

                    var vfx = ECB.CreateEntity(index);
                    ECB.AddComponent<SubGameplayEntityTag>(index, vfx);
                    ECB.AddComponent(index, vfx, new VFXRequest
                    {
                        SpawnPosition = trans.Position,
                        VFXName = VFXName.NextTier,
                        RequestType = VFXRequestType.Spawn,
                        KeepDuration = 0f,
                        VFXTrackTarget = nextTierEntity,
                        Filter = new VFXSubFilter
                        {
                            FactionFilterEnable = true,
                            Faction = fromEntityGeneralAttr.Faction,
                            TierFilterEnable = false
                        },
                        StatChangeRequest = default,
                        ParabolaTargetPosition = default,
                    });
                    AudioUtils.PlayAudioClip(AudioName.NextTier, trans.Position, ECB, index);
                    var destroyOriginalRequest = ECB.CreateEntity(index);
                    ECB.AddComponent<SubGameplayEntityTag>(index, destroyOriginalRequest);
                    ECB.AddComponent(index, destroyOriginalRequest, new StatChangeRequest
                    {
                        AbsAmount = 9999,
                        Interactee = request.FromEntity,
                        Interactor = Entity.Null,
                        Type = StatChangeType.SimpleCleanUsedAsUpgrade,
                        InteractorSubGameplayGeneralAttr = default
                    });
                }
            }
        }
    }
}