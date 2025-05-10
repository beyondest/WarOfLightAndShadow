using SparFlame.BootStrapper;
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
                gameWaveData.ValueRW.NeedUpdateWaveTime = 0;
                gameWaveData.ValueRW.IfWaveUpdateThisFrame = false;
                return;
            }
            if(gameStatusData.Value != GameStatus.Gaming)return;
            
            if (gameWaveData.ValueRW.IfWaveUpdateThisFrame)
                gameWaveData.ValueRW.IfWaveUpdateThisFrame = false;
            var curTime = SystemAPI.GetSingleton<GameTimeData>().ElapsedTime;
            var curInterval = GeneralUtils.GetPointData<WavePointToIntervalData, int>((float)curTime,
                SystemAPI.GetSingletonBuffer<WavePointToIntervalData>());
            
            if (!_requestQuery.IsEmpty)
            {
                var requests = _requestQuery.ToEntityArray(Allocator.Temp);
                state.EntityManager.DestroyEntity(requests);
                gameWaveData.ValueRW.IfWaveUpdateThisFrame = true;
                gameWaveData.ValueRW.CurWaveIndex += 1;
                gameWaveData.ValueRW.NeedUpdateWaveTime = (float)curTime + curInterval;
                return;
            }
            
            if (curTime < gameWaveData.ValueRO.NeedUpdateWaveTime) return;
            
            
            gameWaveData.ValueRW.NeedUpdateWaveTime = (float)curTime + curInterval;
            gameWaveData.ValueRW.CurWaveIndex += 1;
            gameWaveData.ValueRW.IfWaveUpdateThisFrame = true;
            
        }

 
    }
}