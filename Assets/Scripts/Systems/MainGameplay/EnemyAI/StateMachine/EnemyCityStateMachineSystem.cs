using SparFlame.Components.General;
using SparFlame.Components.MainGameplay;
using SparFlame.Components.SubGameplay;
using SparFlame.Core.Structs;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;

// ReSharper disable Unity.Entities.SingletonMustBeRequested

namespace SparFlame.Systems.MainGameplay.EnemyAI
{
    [BurstCompile]
    [UpdateInGroup(typeof(InitializationSystemGroup))]
    public partial struct EnemyCityStateMachineSystem : ISystem
    {
        private NativeParallelHashMap<IntPair, float> _cityDistanceMap;
        private NativeHashMap<int, ExpStaticConfig> _expDatabase;
        private ComponentLookup<ArmyGroupAttr> _armyGroupAttrLookup;
        private ComponentLookup<ArmyGroupThreatenData> _threatenDataLookup;
        private ComponentLookup<ArmyGroupInGarrison> _armyGroupInGarrisonLookup;
        private ComponentLookup<UnitAttr> _unitAttrLookup;
        private ComponentLookup<SubGameplayGeneralAttr> _subGameplayGeneralLookup;
        private ComponentLookup<ArmyGroupStatData> _armyGroupStatDataLookup;
        private BufferLookup<EnemyArmyGroupCompositionData> _compositionLookup;
        private ComponentLookup<PrefabId> _prefabIdLookup;
        private ComponentLookup<AttackAbility> _attackLookup;
        private ComponentLookup<HarvestAbility> _harvestLookup;
        private ComponentLookup<HealAbility> _healLookup;
        private UnitUpgradeAspect.Lookup _unitUpGradeAspectLookup;
        private ComponentLookup<GlobalSingleId> _singleIdLookup;

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<WorldTimeData>();
            state.RequireForUpdate<FormationConfig>();
            state.RequireForUpdate<ArmyGroupThreatenCalculationConfig>();
            state.RequireForUpdate<ExpStaticConfig>();
            state.RequireForUpdate<GameStatusData>();
            state.RequireForUpdate<EndInitializationEntityCommandBufferSystem.Singleton>();

            _armyGroupAttrLookup = state.GetComponentLookup<ArmyGroupAttr>(true);
            _threatenDataLookup = state.GetComponentLookup<ArmyGroupThreatenData>(true);
            _armyGroupInGarrisonLookup = state.GetComponentLookup<ArmyGroupInGarrison>(true);
            _unitAttrLookup = state.GetComponentLookup<UnitAttr>(true);
            _subGameplayGeneralLookup = state.GetComponentLookup<SubGameplayGeneralAttr>(true);
            _armyGroupStatDataLookup = state.GetComponentLookup<ArmyGroupStatData>(true);
            _compositionLookup = state.GetBufferLookup<EnemyArmyGroupCompositionData>(true);

            _prefabIdLookup = state.GetComponentLookup<PrefabId>(true);
            _attackLookup = state.GetComponentLookup<AttackAbility>(true);
            _harvestLookup = state.GetComponentLookup<HarvestAbility>(true);
            _healLookup = state.GetComponentLookup<HealAbility>(true);
            _unitUpGradeAspectLookup = new UnitUpgradeAspect.Lookup(ref state);
            _singleIdLookup = state.GetComponentLookup<GlobalSingleId>(true);
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            if (!_cityDistanceMap.IsCreated)
            {
                Initialize();
            }

            var gameStatus = SystemAPI.GetSingleton<GameStatusData>().Value;
            if(gameStatus != GameStatus.MainGaming && gameStatus != GameStatus.SubGaming)return;
            var subGameStatusData = SystemAPI.GetSingleton<SubGameStatusData>();
            if(GameStatusUtils.IsInBattle(subGameStatusData))return;
            
            _armyGroupStatDataLookup.Update(ref state);
            _subGameplayGeneralLookup.Update(ref state);
            _unitAttrLookup.Update(ref state);
            _armyGroupInGarrisonLookup.Update(ref state);
            _threatenDataLookup.Update(ref state);
            _armyGroupAttrLookup.Update(ref state);
            _compositionLookup.Update(ref state);
            _healLookup.Update(ref state);
            _harvestLookup.Update(ref state);
            _attackLookup.Update(ref state);
            _prefabIdLookup.Update(ref state);
            _unitUpGradeAspectLookup.Update(ref state);
            _singleIdLookup.Update(ref state);
            var debug = new EnemyAIMainGameplayDebug();
            if (SystemAPI.HasSingleton<DebugTag>() && SystemAPI.HasSingleton<EnemyAIMainGameplayDebug>())
                debug = SystemAPI.GetSingleton<EnemyAIMainGameplayDebug>();
            var ecbP = SystemAPI.GetSingleton<EndInitializationEntityCommandBufferSystem.Singleton>()
                .CreateCommandBuffer(state.WorldUnmanaged).AsParallelWriter();
            new EnemyCityStateMachineJob
            {
                ECB = ecbP,
                Debug = debug,
                CalConfig = SystemAPI.GetSingleton<ArmyGroupThreatenCalculationConfig>(),
                FormationConfig = SystemAPI.GetSingleton<FormationConfig>(),
                CityDistanceMap = _cityDistanceMap,
                CurTotalHours = SystemAPI.GetSingleton<WorldTimeData>().totalHours,
                VeryRadicalPossibilityBuffer = SystemAPI.GetSingletonBuffer<VeryRadicalPossibilityConfig>(),

                ArmyGroupAttrLookup = _armyGroupAttrLookup,
                ThreatenDataLookup = _threatenDataLookup,
                ArmyGroupInGarrisonLookup = _armyGroupInGarrisonLookup,
                UnitAttrLookup = _unitAttrLookup,
                SubGameplayGeneralLookup = _subGameplayGeneralLookup,
                ArmyGroupStatDataLookup = _armyGroupStatDataLookup,
                CompositionLookup = _compositionLookup,
                PrefabIdLookup = _prefabIdLookup,
                AttackLookup = _attackLookup,
                HarvestLookup = _harvestLookup,
                HealLookup = _healLookup,
                ExpDatabase = _expDatabase,
                UnitUpgradeAspectLookup = _unitUpGradeAspectLookup,
                SingleIdLookup = _singleIdLookup
            }.ScheduleParallel();
        }


        private void Initialize()
        {
            _cityDistanceMap = new NativeParallelHashMap<IntPair, float>(12, Allocator.Persistent);
            var roadPointDatas = SystemAPI.GetSingletonBuffer<RoadPointData>();
            foreach (var data in roadPointDatas)
            {
                _cityDistanceMap.TryAdd(new IntPair(data.CityAId, data.CityBId), data.TotalDistance);
            }

            var expDatabase = SystemAPI.GetSingletonBuffer<ExpStaticConfig>();
            _expDatabase = new NativeHashMap<int, ExpStaticConfig>(expDatabase.Length, Allocator.Persistent);
            foreach (var expData in expDatabase)
            {
                _expDatabase.Add(expData.PrefabId, expData);
            }
        }

        public void OnDestroy(ref SystemState state)
        {
            _cityDistanceMap.Dispose();
            _expDatabase.Dispose();
        }
    }
}