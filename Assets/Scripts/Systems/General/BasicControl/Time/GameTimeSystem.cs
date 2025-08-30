using SparFlame.Components.General;
using Unity.Entities;

namespace SparFlame.Systems.General.BasicControl
{
    [UpdateInGroup(typeof(InitializationSystemGroup))]
    public partial struct GameTimeSystem : ISystem
    {
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<SubGameStatusData>();
            state.RequireForUpdate<GameBasicConfig>();
            state.RequireForUpdate<GameTimeScale>();
            state.RequireForUpdate<GameStatusData>();
            state.RequireForUpdate<GameTimeData>();
            state.RequireForUpdate<WorldTimeData>();
            state.RequireForUpdate<GameTimeConfig>();
        }

        public void OnUpdate(ref SystemState state)
        {
            var gameBasicConfig = SystemAPI.GetSingleton<GameBasicConfig>();
            var gameTimeConfig = SystemAPI.GetSingleton<GameTimeConfig>();
            
            var gameStatus = SystemAPI.GetSingleton<GameStatusData>().Value;
            var realDeltaTime = SystemAPI.Time.DeltaTime;
            
            var gameTimeScale = SystemAPI.GetSingletonRW<GameTimeScale>();
            var gameTimeData = SystemAPI.GetSingletonRW<GameTimeData>();
            var worldTimeData = SystemAPI.GetSingletonRW<WorldTimeData>();
            var fixedStepGroup = state.World.GetExistingSystemManaged<FixedStepSimulationSystemGroup>();
            
            if (gameStatus == GameStatus.Init)
            {
                gameTimeData.ValueRW.DeltaTime = realDeltaTime;
                gameTimeData.ValueRW.ElapsedTime = 0f;
                gameTimeScale.ValueRW.Value = 1f;
                fixedStepGroup.Timestep = gameBasicConfig.basicFixStep;
                worldTimeData.ValueRW = gameTimeConfig.initWorldTimeData;
                return;
            }

            // When in main menu or paused, game time is not updated.
            if (gameStatus != GameStatus.MainGaming && gameStatus != GameStatus.SubGaming ) return;
            
            gameTimeData.ValueRW.DeltaTime = gameTimeScale.ValueRW.Value * realDeltaTime;
            gameTimeData.ValueRW.ElapsedTime += gameTimeData.ValueRO.DeltaTime;
            fixedStepGroup.Timestep = gameBasicConfig.basicFixStep/ gameTimeScale.ValueRO.Value;
            
            // When in battle, world time is not updated.
            var subGameStatusData = SystemAPI.GetSingleton<SubGameStatusData>();
            if (GameStatusUtils.IsInBattle(subGameStatusData))
            {
                worldTimeData.ValueRW.deltaHour = 0f;
                return;
            }
            worldTimeData.ValueRW.deltaHour =  gameTimeData.ValueRO.DeltaTime * gameTimeConfig.gameTimeSecondToWorldTimeHour;
            worldTimeData.ValueRW.hour += worldTimeData.ValueRO.deltaHour;
            if (worldTimeData.ValueRO.hour >= 24)
            {
                worldTimeData.ValueRW.hour = 0;
                worldTimeData.ValueRW.day += 1;
                if (worldTimeData.ValueRO.day > 30)
                {
                    worldTimeData.ValueRW.day = 0;
                    worldTimeData.ValueRW.month += 1;
                    if (worldTimeData.ValueRO.month > 12)
                    {
                        worldTimeData.ValueRW.month = 0;
                        worldTimeData.ValueRW.year += 1;
                    }
                }
            }
        }
    }
}