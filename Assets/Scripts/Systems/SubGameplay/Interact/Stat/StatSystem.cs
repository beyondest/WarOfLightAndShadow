using SparFlame.Components.General;
using SparFlame.Components.SubGameplay;
using SparFlame.Core.Utils;
using SparFlame.Systems.SubGameplay.Garrison;
using SparFlame.Systems.SubGameplay.Ooc;
using Unity.Burst;
using Unity.Entities;
using Unity.Transforms;

// ReSharper disable SwitchStatementHandlesSomeKnownEnumValuesWithDefault

namespace SparFlame.Systems.SubGameplay.Interact
{
    [UpdateBefore(typeof(GarrisonSystem))]
    public partial struct StatSystem : ISystem
    {
        private ComponentLookup<SubGameplayGeneralAttr> _generalAttrLookup;
        private ComponentLookup<VolumeObstacleTag> _volumeObstacleTagLookup;
        private ComponentLookup<StatData> _statDataLookup;
        private ComponentLookup<LocalTransform> _localTransformLookup;
        private ComponentLookup<ResourceAttr> _resourceAttrLookup;
        private ComponentLookup<RenewableData> _renewableResourceDataLookup;
        private ComponentLookup<OocTag> _oocTagLookup;
        private ComponentLookup<BuildingAttr> _buildingAttrLookup;
        private ComponentLookup<InTeamTag> _inTeamTagLookup;
        private ComponentLookup<UnitAttr> _unitAttrLookup;
        private ComponentLookup<CapacityBuildingAttr> _dwellingAttrLookup;
        private ComponentLookup<ExpData> _expDataLookup;

        private BufferLookup<InsightTarget> _insightTargetLookup;
        private BufferLookup<CostList> _costListLookup;

        
        private ComponentLookup<LightShieldUnderDefend> _lightShieldUnderDefendLookup;
        private ComponentLookup<LightShieldBuff> _lightShieldBuffLookup;
        private ComponentLookup<DarkShieldTauntBuff> _darkShieldTauntBuffLookup;
        private ComponentLookup<DarkShieldTauntedBuff> _darkShieldTauntedBuffLookup;
        private ComponentLookup<LightArcherBuff> _lightArcherBuffLookup;
        private ComponentLookup<DarkClericBuff> _darkClericBuffLookup;
        private ComponentLookup<LightClericBuff> _lightClericBuffLookup;
        private ComponentLookup<LightMagicDamageBuff> _lightMagicDamageBuffLookup;
        private ComponentLookup<DarkMagicDamageBuff> _darkMagicDamageBuffLookup;
        private ComponentLookup<CavalryMoveBuff> _cavalryMoveBuffLookup;
        private ComponentLookup<BuildingGarrisonBuff> _buildingGarrisonBuffLookup;
        private ComponentLookup<UnitGarrisonBuff> _unitGarrisonBuffLookup;
        private ComponentLookup<InGarrison> _inGarrisonLookup;
        private ComponentLookup<ConstructingTimer> _constructingTimerLookup;
        private ComponentLookup<CityTaskUniqueId> _cityTaskUniqueIdLookup;
        private ComponentLookup<ConjuringTag> _conjuringTagLookup;
        private ComponentLookup<GeneratingTag> _generatingTagLookup;
        private ComponentLookup<GenerateAttr> _generateAttrLookup;

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<SubGameStatusData>();
            state.RequireForUpdate<GarrisonBuffConfig>();
            state.RequireForUpdate<CavalryMoveBuffConfig>();
            state.RequireForUpdate<PlayerFactionData>();
            state.RequireForUpdate<EndSimulationEntityCommandBufferSystem.Singleton>();
            state.RequireForUpdate<SubGamingTag>();
            state.RequireForUpdate<StatSystemConfig>();
            state.RequireForUpdate<SightSystemConfig>();
            state.RequireForUpdate<OocSystemConfig>();

            _generalAttrLookup = state.GetComponentLookup<SubGameplayGeneralAttr>(true);
            _volumeObstacleTagLookup = state.GetComponentLookup<VolumeObstacleTag>(true);
            _statDataLookup = state.GetComponentLookup<StatData>();
            _localTransformLookup = state.GetComponentLookup<LocalTransform>(true);
            _resourceAttrLookup = state.GetComponentLookup<ResourceAttr>();
            _renewableResourceDataLookup = state.GetComponentLookup<RenewableData>(true);
            _oocTagLookup = state.GetComponentLookup<OocTag>();
            _buildingAttrLookup = state.GetComponentLookup<BuildingAttr>(true);
            _insightTargetLookup = state.GetBufferLookup<InsightTarget>();
            _costListLookup = state.GetBufferLookup<CostList>(true);
            _inTeamTagLookup = state.GetComponentLookup<InTeamTag>(true);
            _unitAttrLookup = state.GetComponentLookup<UnitAttr>();
            _dwellingAttrLookup = state.GetComponentLookup<CapacityBuildingAttr>(true);
            _expDataLookup = state.GetComponentLookup<ExpData>(true);
            _inGarrisonLookup = state.GetComponentLookup<InGarrison>(true);
            _generateAttrLookup = state.GetComponentLookup<GenerateAttr>(true);
            _generatingTagLookup = state.GetComponentLookup<GeneratingTag>(true);
            
            _lightShieldUnderDefendLookup = state.GetComponentLookup<LightShieldUnderDefend>(true);
            _lightShieldBuffLookup = state.GetComponentLookup<LightShieldBuff>(true);
            _darkShieldTauntBuffLookup = state.GetComponentLookup<DarkShieldTauntBuff>(true);
            _darkShieldTauntedBuffLookup = state.GetComponentLookup<DarkShieldTauntedBuff>(true);
            _lightArcherBuffLookup = state.GetComponentLookup<LightArcherBuff>(true);
            _darkClericBuffLookup = state.GetComponentLookup<DarkClericBuff>(true);
            _lightClericBuffLookup = state.GetComponentLookup<LightClericBuff>(true);
            _lightMagicDamageBuffLookup = state.GetComponentLookup<LightMagicDamageBuff>(true);
            _darkMagicDamageBuffLookup = state.GetComponentLookup<DarkMagicDamageBuff>(true);
            _cavalryMoveBuffLookup = state.GetComponentLookup<CavalryMoveBuff>(true);
            _buildingGarrisonBuffLookup = state.GetComponentLookup<BuildingGarrisonBuff>(true);
            _unitGarrisonBuffLookup = state.GetComponentLookup<UnitGarrisonBuff>(true);
            _constructingTimerLookup = state.GetComponentLookup<ConstructingTimer>(true);
            _cityTaskUniqueIdLookup = state.GetComponentLookup<CityTaskUniqueId>(true);
            _conjuringTagLookup = state.GetComponentLookup<ConjuringTag>(true);
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            _statDataLookup.Update(ref state);
            _generalAttrLookup.Update(ref state);
            _volumeObstacleTagLookup.Update(ref state);
            _localTransformLookup.Update(ref state);
            _resourceAttrLookup.Update(ref state);
            _renewableResourceDataLookup.Update(ref state);
            _insightTargetLookup.Update(ref state);
            _oocTagLookup.Update(ref state);
            _buildingAttrLookup.Update(ref state);
            _costListLookup.Update(ref state);
            _inTeamTagLookup.Update(ref state);
            _unitAttrLookup.Update(ref state);
            _dwellingAttrLookup.Update(ref state);
            _expDataLookup.Update(ref state);
            _inGarrisonLookup.Update(ref state);
            _conjuringTagLookup.Update(ref state);
            _generateAttrLookup.Update(ref state);
            _generatingTagLookup.Update(ref state);
            
            
            _lightShieldUnderDefendLookup.Update(ref state);
            _lightShieldBuffLookup.Update(ref state);
            _darkShieldTauntBuffLookup.Update(ref state);
            _darkShieldTauntedBuffLookup.Update(ref state);
            _lightArcherBuffLookup.Update(ref state);
            _darkClericBuffLookup.Update(ref state);
            _lightClericBuffLookup.Update(ref state);
            _lightMagicDamageBuffLookup.Update(ref state);
            _darkMagicDamageBuffLookup.Update(ref state);
            _cavalryMoveBuffLookup.Update(ref state);
            _buildingGarrisonBuffLookup.Update(ref state);
            _unitGarrisonBuffLookup.Update(ref state);
            _constructingTimerLookup.Update(ref state);
            _cityTaskUniqueIdLookup.Update(ref state);
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

            var ecbP = ecbSingleton.CreateCommandBuffer(state.WorldUnmanaged).AsParallelWriter();

            var job = new BuffApplyJob
            {
                ECB = ecbP,
                GeneralAttrLookup = _generalAttrLookup,
                ExpDataLookup = _expDataLookup,
                StatDataLookup = _statDataLookup,
                UnitAttrLookup = _unitAttrLookup,

                LightShieldUnderDefendLookup = _lightShieldUnderDefendLookup,
                LightShieldBuffLookup = _lightShieldBuffLookup,
                DarkShieldTauntBuffLookup = _darkShieldTauntBuffLookup,
                DarkShieldTauntedBuffLookup = _darkShieldTauntedBuffLookup,
                LightArcherBuffLookup = _lightArcherBuffLookup,
                DarkClericBuffLookup = _darkClericBuffLookup,
                LightClericBuffLookup = _lightClericBuffLookup,
                LightMagicDamageBuffLookup = _lightMagicDamageBuffLookup,
                DarkMagicDamageBuffLookup = _darkMagicDamageBuffLookup,
                CavalryMoveBuffLookup =  _cavalryMoveBuffLookup,
                BuildingGarrisonBuffLookup = _buildingGarrisonBuffLookup,
                UnitGarrisonBuffLookup = _unitGarrisonBuffLookup,
                
                DarkShieldBuffConfigs = SystemAPI.GetSingletonBuffer<DarkShieldBuffConfig>(),
                LightShieldBuffConfigs = SystemAPI.GetSingletonBuffer<LightShieldBuffConfig>(),
                LightArcherBuffConfigs = SystemAPI.GetSingletonBuffer<LightArcherBuffConfig>(),
                DarkClericBuffConfigs = SystemAPI.GetSingletonBuffer<DarkClericBuffConfig>(),
                LightClericBuffConfigs = SystemAPI.GetSingletonBuffer<LightClericBuffConfig>(),
                LightMagicDamageBuffConfigs = SystemAPI.GetSingletonBuffer<LightMagicDamageBuffConfig>(),
                DarkMagicDamageBuffConfigs = SystemAPI.GetSingletonBuffer<DarkMagicDamageBuffConfig>(),
                CavalryMoveBuffConfig = SystemAPI.GetSingleton<CavalryMoveBuffConfig>(),
                GarrisonBuffConfig = SystemAPI.GetSingleton<GarrisonBuffConfig>(),
                
            }.Schedule(state.Dependency);
            job.Complete();

            new CheckStatChangeRequest
            {
                ECB = ecbP,
                RandomValue = rndValue,
                SightConfig = autoChooseTargetSystemConfig,
                OocConfig = oocSystemConfig,
                PlayerFactionData = SystemAPI.GetSingleton<PlayerFactionData>(),
                CurrentCity = SystemAPI.GetSingleton<SubGameStatusData>().City,
                
                // Debug
                StatDebug = statDebug,

                // Look up
                GeneralAttrLookup = _generalAttrLookup,
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
                CapacityBuildingAttrLookup = _dwellingAttrLookup,
                ExpDataLookup = _expDataLookup,
                ConstructingTimerLookup =_constructingTimerLookup,
                CityTaskUniqueIdLookup =_cityTaskUniqueIdLookup ,
                ConjuringTagLookup = _conjuringTagLookup,
                GeneratingTagLookup = _generatingTagLookup,
                GenerateAttrLookup = _generateAttrLookup,
            }.Schedule();
        }
    }
}