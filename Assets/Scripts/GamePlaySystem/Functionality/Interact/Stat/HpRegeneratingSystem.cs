using SparFlame.GamePlaySystem.Garrison;
using SparFlame.GamePlaySystem.General;
using SparFlame.GamePlaySystem.Ooc;
using SparFlame.GamePlaySystem.Resource;
using SparFlame.GamePlaySystem.UnitSelection;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;

namespace SparFlame.GamePlaySystem.Interact
{
    public partial struct HpRegeneratingSystem : ISystem
    {
        
        private ComponentLookup<AITag> _aiTagLookup;
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<GamingTag>();
            state.RequireForUpdate<StatSystemConfig>();
            state.RequireForUpdate<OocSystemConfig>();
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
            private void Execute(ref StatData statData, in GeneralAttr generalAttr,Entity selfEntity)
            {
                if(statData.CurValue >= statData.MaxValue)return;
                var isAi = AITagLookup.HasComponent(selfEntity);
                var speedBoost = isAi ? Config.AiBoostRate : 1f;
                var speed = generalAttr.BaseTag == BaseTag.Units
                    ? Config.NormalHpRegenerationRate
                    : Config.BuildingHpRegenerationRate;
                speed *= speedBoost;
                
                statData.CurValue += DeltaTime * speed;
                if(statData.CurValue >= statData.MaxValue)statData.CurValue = statData.MaxValue;
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
            private void Execute(ref StatData statData, in GeneralAttr generalAttr,Entity selfEntity)
            {
                if(statData.CurValue >= statData.MaxValue)return;
                var isAi = AITagLookup.HasComponent(selfEntity);
                var speedBoost = isAi ? Config.NormalHpRegenerationRate : 1f;
                var speed = generalAttr.BaseTag == BaseTag.Units
                    ? Config.NormalHpRegenerationRate
                    : Config.BuildingHpRegenerationRate;
                speed *= speedBoost;
                statData.CurValue += DeltaTime * speed;
                if(statData.CurValue >= statData.MaxValue)statData.CurValue = statData.MaxValue;
            }
        }
    }
}