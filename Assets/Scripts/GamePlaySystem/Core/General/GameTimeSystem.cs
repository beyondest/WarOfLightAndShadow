using Unity.Burst;
using Unity.Entities;

namespace SparFlame.GamePlaySystem.General
{
    [UpdateInGroup(typeof(InitializationSystemGroup))]
    public partial struct GameTimeSystem : ISystem
    {
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<GameBasicConfig>();
            state.RequireForUpdate<GameTimeScale>();
            state.RequireForUpdate<GameStatusData>();
            state.RequireForUpdate<GameTimeData>();
        }

        public void OnUpdate(ref SystemState state)
        {
            var gameBasicConfig = SystemAPI.GetSingleton<GameBasicConfig>();
            var gameStatus = SystemAPI.GetSingleton<GameStatusData>().Value;
            var realDeltaTime = SystemAPI.Time.DeltaTime;
            
            var gameTimeScale = SystemAPI.GetSingletonRW<GameTimeScale>();
            var gameTimeData = SystemAPI.GetSingletonRW<GameTimeData>();
            var fixedStepGroup = state.World.GetExistingSystemManaged<FixedStepSimulationSystemGroup>();
            if (gameStatus == GameStatus.Init)
            {
                gameTimeData.ValueRW.DeltaTime = realDeltaTime;
                gameTimeData.ValueRW.ElapsedTime = 0f;
                gameTimeScale.ValueRW.Value = 1f;
                fixedStepGroup.Timestep = gameBasicConfig.basicFixStep;
                return;
            }

            if (gameStatus != GameStatus.MainGaming && gameStatus != GameStatus.SubGaming ) return;
            
            gameTimeData.ValueRW.DeltaTime = gameTimeScale.ValueRW.Value * realDeltaTime;
            gameTimeData.ValueRW.ElapsedTime += gameTimeData.ValueRO.DeltaTime;
            fixedStepGroup.Timestep = gameBasicConfig.basicFixStep/ gameTimeScale.ValueRO.Value;
        }
    }
}