using SparFlame.Components.General;
using SparFlame.Components.MainGameplay;
using SparFlame.Components.VFX;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Transforms;

namespace SparFlame.Systems.MainGameplay.EnemyAI
{
    public partial struct SimulateInvadingSupportCitySystem : ISystem
    {
     

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<WorldTimeData>();
            state.RequireForUpdate<SubGameStatusData>();
            state.RequireForUpdate<GameStatusData>();
            state.RequireForUpdate<InvadeSupportCityConfig>();
            state.RequireForUpdate<InvadingSupportCityTag>();
            state.RequireForUpdate<SupportFightTag>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var gameStatusData = SystemAPI.GetSingleton<GameStatusData>().Value;
            if (gameStatusData != GameStatus.MainGaming && gameStatusData != GameStatus.SubGaming) return;
            var subGameStatusData = SystemAPI.GetSingleton<SubGameStatusData>();
            if (GameStatusUtils.IsInBattle(subGameStatusData)) return;
   
            var curHours = SystemAPI.GetSingleton<WorldTimeData>().totalHours;
           
          
                var ecb = new EntityCommandBuffer(Allocator.Temp);
                var config = SystemAPI.GetSingleton<InvadeSupportCityConfig>();
                var supportCity = SystemAPI.GetSingletonEntity<SupportFightTag>();
                var cityPos = SystemAPI.GetComponent<LocalTransform>(supportCity).Position;
                foreach (var (statData,tag, entity) in SystemAPI.Query<
                             RefRW<ArmyGroupStatData>, RefRW<InvadingSupportCityTag>>().WithEntityAccess())
                {
                    if(tag.ValueRO.LastCheckTime + 1 > curHours)continue;
                    
                    statData.ValueRW.totalCurrentHp -= config.ReduceHpRatioPerHour * statData.ValueRO.totalMaxHp *
                                                       (int)(curHours - tag.ValueRO.LastCheckTime);
                    tag.ValueRW.LastCheckTime = curHours;
                    var hitVfx = ecb.CreateEntity();
                    ecb.AddComponent<MainGameplayEntityTag>(hitVfx);
                    ecb.AddComponent(hitVfx, new VFXRequest
                    {
                        VFXName = VFXName.ArmyGroupHitSupportCity,
                        KeepDuration = float.MaxValue,
                        RequestType = VFXRequestType.Spawn,
                        SpawnPosition = cityPos,
                    });
                    if (statData.ValueRW.totalCurrentHp <= 0)
                    {
                        ArmyGroupUtils.DestroyArmyGroup(entity, ecb, state.EntityManager);
                        var hintRequest = ecb.CreateEntity();
                        ecb.AddComponent(hintRequest, new HintRequest
                        {
                            Name = HintName.EnemyArmyGroupIsBeingDestroyedByYourAllies
                        });
                        ecb.AddComponent<MainGameplayEntityTag>(hintRequest);
                        var removeRequest = ecb.CreateEntity();
                        ecb.AddComponent<MainGameplayEntityTag>(removeRequest);
                        ecb.AddComponent(removeRequest, new ClearCityFutureInvadersRequest
                        {
                            City = supportCity
                        });
                    }
                }
                ecb.Playback(state.EntityManager);
            
            
        }
    }
}