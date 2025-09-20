using SparFlame.Components.General;
using SparFlame.Components.MainGameplay;
using SparFlame.Components.SubGameplay;
using SparFlame.Core.Structs;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;

namespace SparFlame.Systems.MainGameplay.EnemyAI
{
    [BurstCompile]
    [UpdateInGroup(typeof(InitializationSystemGroup))]
    public partial struct EnemyCityStateMachine : ISystem
    {
        private NativeParallelHashMap<IntPair, float> _cityDistanceMap;
        
        private ComponentLookup<ExpData> _expDataLookup;
        private ComponentLookup<ArmyGroupAttr> _armyGroupAttrLookup;
        private ComponentLookup<ArmyGroupThreatenData> _threatenDataLookup;
        private ComponentLookup<ArmyGroupInGarrison> _armyGroupInGarrisonLookup;
        private ComponentLookup<UnitAttr> _unitAttrLookup;
        private ComponentLookup<SubGameplayGeneralAttr> _subGameplayGeneralLookup;
        private ComponentLookup<FocusOnPlayerTag> _focusOnPlayerLookup;
        private ComponentLookup<ArmyGroupStatData> _armyGroupStatDataLookup;
        private BufferLookup<EnemyArmyGroupCompositionData> _compositionLookup;

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<WorldTimeData>();
            state.RequireForUpdate<FormationConfig>();
            state.RequireForUpdate<ArmyGroupThreatenCalculationConfig>();
            state.RequireForUpdate<EndInitializationEntityCommandBufferSystem.Singleton>();

            _expDataLookup = state.GetComponentLookup<ExpData>(true);
            _armyGroupAttrLookup = state.GetComponentLookup<ArmyGroupAttr>(true);
            _threatenDataLookup = state.GetComponentLookup<ArmyGroupThreatenData>(true);
            _armyGroupInGarrisonLookup = state.GetComponentLookup<ArmyGroupInGarrison>(true);
            _unitAttrLookup = state.GetComponentLookup<UnitAttr>(true);
            _subGameplayGeneralLookup = state.GetComponentLookup<SubGameplayGeneralAttr>(true);
            _focusOnPlayerLookup = state.GetComponentLookup<FocusOnPlayerTag>(true);
            _armyGroupStatDataLookup = state.GetComponentLookup<ArmyGroupStatData>(true);
            _compositionLookup = state.GetBufferLookup<EnemyArmyGroupCompositionData>(true);
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            if (!_cityDistanceMap.IsCreated)
            {
                _cityDistanceMap = new NativeParallelHashMap<IntPair, float>(12, Allocator.Persistent);
                var roadPointDatas = SystemAPI.GetSingletonBuffer<RoadPointData>();
                foreach (var data in roadPointDatas)
                {
                    _cityDistanceMap.TryAdd(new IntPair(data.CityAId, data.CityBId), data.TotalDistance);
                }
            }
            _armyGroupStatDataLookup.Update(ref state);
            _focusOnPlayerLookup.Update(ref state);
            _subGameplayGeneralLookup.Update(ref state);
            _unitAttrLookup.Update(ref state);
            _armyGroupInGarrisonLookup.Update(ref state);
            _threatenDataLookup.Update(ref state);
            _armyGroupAttrLookup.Update(ref state);
            _expDataLookup.Update(ref state);
            _compositionLookup.Update(ref state);
            
            var ecbP = SystemAPI.GetSingleton<EndInitializationEntityCommandBufferSystem.Singleton>()
                .CreateCommandBuffer(state.WorldUnmanaged).AsParallelWriter();
            new EnemyCityStateMachineJob
            {
                ECB = ecbP,
                CalConfig = SystemAPI.GetSingleton<ArmyGroupThreatenCalculationConfig>(),
                FormationConfig = SystemAPI.GetSingleton<FormationConfig>(),
                CityDistanceMap = _cityDistanceMap,
                CurTotalHours = SystemAPI.GetSingleton<WorldTimeData>().totalHours,
                VeryRadicalPossibilityBuffer = SystemAPI.GetSingletonBuffer<VeryRadicalPossibility>(),

                ExpLookup = _expDataLookup,
                ArmyGroupAttrLookup = _armyGroupAttrLookup,
                ThreatenDataLookup = _threatenDataLookup,
                ArmyGroupInGarrisonLookup = _armyGroupInGarrisonLookup,
                UnitAttrLookup = _unitAttrLookup,
                SubGameplayGeneralLookup = _subGameplayGeneralLookup,
                FocusOnPlayerLookup = _focusOnPlayerLookup,
                ArmyGroupStatDataLookup = _armyGroupStatDataLookup,
                CompositionLookup = _compositionLookup,
                
            }.ScheduleParallel();
        }

        public void OnDestroy(ref SystemState state)
        {
            _cityDistanceMap.Dispose();
        }
    }
}