using SparFlame.Components.General;
using SparFlame.Components.SubGameplay;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;

namespace SparFlame.Systems.SubGameplay.Interact
{
    public partial struct HpRegeneratingSystem : ISystem
    {
        
        private ComponentLookup<AITag> _aiTagLookup;
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<GameTimeData>();
            state.RequireForUpdate<SubGamingTag>();
            state.RequireForUpdate<StatSystemConfig>();
            _aiTagLookup = state.GetComponentLookup<AITag>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            _aiTagLookup.Update(ref state);
            var config = SystemAPI.GetSingleton<StatSystemConfig>();
            new GarrisonHpRegenerationJob
            {
                AITagLookup = _aiTagLookup,
                Config = config,
                DeltaTime = SystemAPI.GetSingleton<GameTimeData>().DeltaTime
            }.ScheduleParallel();
            new NormalHpRegenerationJob
            {
                AITagLookup = _aiTagLookup,
                Config = config,
                DeltaTime = SystemAPI.GetSingleton<GameTimeData>().DeltaTime
            }.ScheduleParallel();
        }

       
        
        [BurstCompile]
        [WithAll(typeof(InGarrison))]
        [WithAll(typeof(GarrisonStateTag))]
        [WithNone(typeof(ResourceAttr))]
        private partial struct GarrisonHpRegenerationJob : IJobEntity
        {
            [ReadOnly] public float DeltaTime;
            [ReadOnly] public StatSystemConfig Config;
            [ReadOnly] public ComponentLookup<AITag> AITagLookup;
            private void Execute(ref StatData statData, in SubGameplayGeneralAttr subGameplayGeneralAttr,Entity selfEntity)
            {
                if(statData.curValue >= statData.maxValue + statData.bonus)return;
                var isAi = AITagLookup.HasComponent(selfEntity);
                var speedBoost = isAi ? Config.AiBoostRate : 1f;
                var speed = subGameplayGeneralAttr.BaseTag == BaseTag.Units
                    ? Config.NormalHpRegenerationRate
                    : Config.BuildingHpRegenerationRate;
                speed *= speedBoost;
                
                statData.curValue += DeltaTime * speed;
                if(statData.curValue >= statData.maxValue + statData.bonus)statData.curValue = statData.maxValue;
            }
        }
        
        [BurstCompile]
        [WithNone(typeof(OocTag))]
        [WithNone(typeof(GarrisonStateTag))]
        [WithNone(typeof(ResourceAttr))]
        private partial struct NormalHpRegenerationJob : IJobEntity
        {
            [ReadOnly] public float DeltaTime;
            [ReadOnly] public StatSystemConfig Config;
            [ReadOnly] public ComponentLookup<AITag> AITagLookup;
            private void Execute(ref StatData statData, in SubGameplayGeneralAttr subGameplayGeneralAttr,Entity selfEntity)
            {
                if(statData.curValue >= statData.maxValue + statData.bonus)return;
                var isAi = AITagLookup.HasComponent(selfEntity);
                var speedBoost = isAi ? Config.NormalHpRegenerationRate : 1f;
                var speed = subGameplayGeneralAttr.BaseTag == BaseTag.Units
                    ? Config.NormalHpRegenerationRate
                    : Config.BuildingHpRegenerationRate;
                speed *= speedBoost;
                statData.curValue += DeltaTime * speed;
                if(statData.curValue >= statData.maxValue + statData.bonus)statData.curValue = statData.maxValue;
            }
        }
    }
}