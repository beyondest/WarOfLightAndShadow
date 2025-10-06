using SparFlame.Components.General;
using SparFlame.Components.SubGameplay;
using SparFlame.Systems.SubGameplay.Interact;
using Unity.Burst;
using Unity.Entities;

namespace SparFlame.Systems.SubGameplay.StateMachine
{
    public partial struct CastSkillStateMachine : ISystem
    {
        private BufferLookup<AnimationEventData> _eventsLookup;
        private ComponentLookup<HealAbility> _healAbilityLookup;
        private ComponentLookup<AttackAbility> _attackAbilityLookup;

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<ArcherSkillConfig>();
            state.RequireForUpdate<EnemyArcherSkill>();
            state.RequireForUpdate<PlayerArcherSkill>();
            state.RequireForUpdate<PlayerFactionData>();
            state.RequireForUpdate<DarkShieldBuffGeneralConfig>();
            state.RequireForUpdate<DarkCavalryBuffGeneralConfig>();
            state.RequireForUpdate<LightShieldBuffGeneralConfig>();
            state.RequireForUpdate<BlessingBuffGeneralConfig>();
            state.RequireForUpdate<GameTimeData>();
            state.RequireForUpdate<DamageReduceShieldBuffConfig>();
            state.RequireForUpdate<CastSkillConfig>();
            state.RequireForUpdate<AnimationEventTriggerModelIndex>();
            state.RequireForUpdate<EndSimulationEntityCommandBufferSystem.Singleton>();
            state.RequireForUpdate<SubGamingTag>();
            state.RequireForUpdate<CastSkillStateTag>();
            _eventsLookup = state.GetBufferLookup<AnimationEventData>();
            _healAbilityLookup = state.GetComponentLookup<HealAbility>(true);
            _attackAbilityLookup = state.GetComponentLookup<AttackAbility>(true);
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            _eventsLookup.Update(ref state);
            _healAbilityLookup.Update(ref state);
            _attackAbilityLookup.Update(ref state);
            var ecbP = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>()
                .CreateCommandBuffer(state.WorldUnmanaged).AsParallelWriter();
            var gameTimeData = SystemAPI.GetSingleton<GameTimeData>();
            state.Dependency = new CastSkillJob
            {
                EventsLookup = _eventsLookup,
                ElapsedTime = gameTimeData.ElapsedTime,
                AnimationEventModelIndexConfig = SystemAPI.GetSingleton<AnimationEventTriggerModelIndex>(),
                ECB = ecbP,
                CastSkillConfig = SystemAPI.GetSingleton<CastSkillConfig>(),
                DamageReduceShieldBuffConfig = SystemAPI.GetSingleton<DamageReduceShieldBuffConfig>(),
                BlessingBuffGeneralConfig = SystemAPI.GetSingleton<BlessingBuffGeneralConfig>(),
                BlessingBuffAoeTriggerPrefabs = SystemAPI.GetSingletonBuffer<BlessingBuffAoeTriggerPrefabs>(),
                LightShieldBuffGeneralConfig = SystemAPI.GetSingleton<LightShieldBuffGeneralConfig>(),
                DarkCavalryBuffGeneralConfig = SystemAPI.GetSingleton<DarkCavalryBuffGeneralConfig>(),
                DarkShieldBuffGeneralConfig = SystemAPI.GetSingleton<DarkShieldBuffGeneralConfig>(),
                AttackAbilityLookup = _attackAbilityLookup,
                HealAbilityLookup = _healAbilityLookup,
            }.ScheduleParallel(state.Dependency);
            
            state.Dependency = new CastArcherSkillJob
            {
                ECB = ecbP,
                DeltaTime = gameTimeData.DeltaTime,
                Config = SystemAPI.GetSingleton<ArcherSkillConfig>(),
                EnemyArcherSkill = SystemAPI.GetSingleton<EnemyArcherSkill>(),
                PlayerArcherSkill = SystemAPI.GetSingleton<PlayerArcherSkill>(),
                PlayerFactionData = SystemAPI.GetSingleton<PlayerFactionData>(),
                
            }.ScheduleParallel(state.Dependency);
        }
    }

 

   
}