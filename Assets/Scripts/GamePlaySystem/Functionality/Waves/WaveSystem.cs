using SparFlame.GamePlaySystem.General;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;

namespace SparFlame.GamePlaySystem.Waves
{
    [UpdateInGroup(typeof(InitializationSystemGroup))]
    [UpdateAfter(typeof(GameTimeSystem))]
    public partial struct WaveSystem : ISystem
    {
        private EntityQuery _requestQuery;
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<GameTimeData>();
            state.RequireForUpdate<GameWaveData>();
            state.RequireForUpdate<GameStatusData>();
            state.RequireForUpdate<GameWaveSystemConfig>();
            _requestQuery = SystemAPI.QueryBuilder().WithAll<NextWaveRequest>().Build();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var gameWaveData = SystemAPI.GetSingletonRW<GameWaveData>();
            var gameStatusData = SystemAPI.GetSingleton<GameStatusData>();
            if (gameStatusData.Value == GameStatus.Init)
            {
                gameWaveData.ValueRW.CurWaveIndex = 0;
                gameWaveData.ValueRW.NextWaveRemainingTime = 0f;
                gameWaveData.ValueRW.IfWaveUpdateThisFrame = false;
                gameWaveData.ValueRW.CurWaveInterval = 0f;
                return;
            }
            if(gameStatusData.Value != GameStatus.Gaming)return;
            
            if (gameWaveData.ValueRW.IfWaveUpdateThisFrame)
                gameWaveData.ValueRW.IfWaveUpdateThisFrame = false;
            var timeData = SystemAPI.GetSingleton<GameTimeData>();
            var curInterval = GeneralUtils.GetPointData<WavePointToIntervalData, int>(timeData.ElapsedTime,
                SystemAPI.GetSingletonBuffer<WavePointToIntervalData>());
            gameWaveData.ValueRW.CurWaveInterval = curInterval;
            if (!_requestQuery.IsEmpty)
            {
                var requests = _requestQuery.ToEntityArray(Allocator.Temp);
                state.EntityManager.DestroyEntity(requests);
                gameWaveData.ValueRW.IfWaveUpdateThisFrame = true;
                gameWaveData.ValueRW.CurWaveIndex += 1;
                gameWaveData.ValueRW.NextWaveRemainingTime =  curInterval;
                return;
            }


            var delta = timeData.DeltaTime;
            if (SystemAPI.HasSingleton<DebugTag>() && SystemAPI.TryGetSingleton(out WaveDebug debug))
            {
                delta *= debug.waveSpeedUpScale;
            }
            gameWaveData.ValueRW.NextWaveRemainingTime -= delta;
            if (gameWaveData.ValueRO.NextWaveRemainingTime > 0f) return;
            
            gameWaveData.ValueRW.NextWaveRemainingTime = curInterval;
            gameWaveData.ValueRW.CurWaveIndex += 1;
            gameWaveData.ValueRW.IfWaveUpdateThisFrame = true;
            
        }

 
    }
}