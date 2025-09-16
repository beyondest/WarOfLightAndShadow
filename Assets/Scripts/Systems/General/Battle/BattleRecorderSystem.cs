using SparFlame.Components.General;
using SparFlame.Components.MainGameplay;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

// ReSharper disable Unity.Entities.SingletonMustBeRequested

namespace SparFlame.Systems.General.Battle
{
    public partial struct BattleRecorderSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<BattleRecorderConfig>();
            state.RequireForUpdate<GameTimeData>();
            state.RequireForUpdate<SubGameStatusData>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var subGameStatusData = SystemAPI.GetSingleton<SubGameStatusData>();
            if (!GameStatusUtils.IsInBattle(subGameStatusData))
            {
                if (SystemAPI.HasSingleton<BattleRecorder>())
                {
                    state.EntityManager.DestroyEntity(SystemAPI.GetSingletonEntity<BattleRecorder>());
                }

                return;
            }

            if (!SystemAPI.HasSingleton<BattleRecorder>())
            {
                state.EntityManager.CreateSingleton(new BattleRecorder
                {
                    StartTime = SystemAPI.GetSingleton<GameTimeData>().ElapsedTime,
                    EnemySideDiedCount = 0,
                    PlayerSideDiedCount = 0,
                    EnemySideDestroyedBuildingsCount = 0,
                    PlayerSideDestroyedBuildingsCount = 0,

                    PlayerUnitsUpgradeCount = 0,
                    DestroyedRewardValue = 0,
                    KilledRewardValue = 0,
                });
            }

            var ecb = new EntityCommandBuffer(Allocator.Temp);
            DealPlayerUnitUpgradeRequest(ref state, ecb);
            DealEnemySideDiedDestroyed(ref state, ecb);
            DealArmyGroupStatChangeRequest(ref state, ecb);
            DealPlayerSideDiedDestroyed(ref state, ecb);
            ecb.Playback(state.EntityManager);
            ecb.Dispose();
        }

        private void DealPlayerUnitUpgradeRequest(ref SystemState state, EntityCommandBuffer ecb)
        {
            ref var recorder = ref SystemAPI.GetSingletonRW<BattleRecorder>().ValueRW;
            foreach (var (_, entity) in SystemAPI.Query<RefRO<BattleRecorderPlayerUnitsUpgrade>>()
                         .WithEntityAccess())
            {
                ecb.DestroyEntity(entity);
                recorder.PlayerUnitsUpgradeCount++;
            }
        }

        private void DealArmyGroupStatChangeRequest(ref SystemState state, EntityCommandBuffer ecb)
        {
            foreach (var (request, entity) in SystemAPI.Query<RefRO<ArmyGroupStatChangeRequest>>().WithEntityAccess())
            {
                ecb.DestroyEntity(entity);
                var armyGroupStatData = SystemAPI.GetComponentRW<ArmyGroupStatData>(request.ValueRO.ArmyGroup);
                armyGroupStatData.ValueRW.totalCurrentHp += request.ValueRO.StatChangeValue;
                armyGroupStatData.ValueRW.totalCurrentHp = math.clamp(armyGroupStatData.ValueRW.totalCurrentHp, 0,
                    armyGroupStatData.ValueRW.totalMaxHp);
            }
        }


        private void DealPlayerSideDiedDestroyed(ref SystemState state, EntityCommandBuffer ecb)
        {
            ref var recorder = ref SystemAPI.GetSingletonRW<BattleRecorder>().ValueRW;
            foreach (var (_, entity) in SystemAPI.Query<RefRO<BattleRecorderPlayerSideDied>>().WithEntityAccess())
            {
                ecb.DestroyEntity(entity);
                recorder.PlayerSideDiedCount++;
            }

            foreach (var (_, entity) in SystemAPI.Query<RefRO<BattleRecorderPlayerSideDestroyedBuilding>>()
                         .WithEntityAccess())
            {
                ecb.DestroyEntity(entity);
                recorder.PlayerSideDestroyedBuildingsCount++;
            }
        }

        private void DealEnemySideDiedDestroyed(ref SystemState state, EntityCommandBuffer ecb)
        {
            var config = SystemAPI.GetSingleton<BattleRecorderConfig>();
            ref var recorder = ref SystemAPI.GetSingletonRW<BattleRecorder>().ValueRW;
            foreach (var (request, entity) in SystemAPI.Query<RefRO<BattleRecorderEnemySideDied>>().WithEntityAccess())
            {
                ecb.DestroyEntity(entity);
                recorder.EnemySideDiedCount++;
                recorder.KilledRewardValue += request.ValueRO.DiedUnitLevel * config.killedGainToLevel;
            }

            foreach (var (request, entity) in SystemAPI.Query<RefRO<BattleRecorderEnemySideDestroyedBuilding>>()
                         .WithEntityAccess())
            {
                ecb.DestroyEntity(entity);
                recorder.EnemySideDestroyedBuildingsCount++;
                recorder.DestroyedRewardValue +=
                    ((int)request.ValueRO.DestroyedBuildingTier - 2) * config.destroyedGainToTier;
            }
        }
    }
}