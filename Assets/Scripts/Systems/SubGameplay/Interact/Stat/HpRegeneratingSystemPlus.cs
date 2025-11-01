// using SparFlame.Components.General;
// using SparFlame.Components.MainGameplay;
// using SparFlame.Components.SubGameplay;
// using Unity.Burst;
// using Unity.Collections;
// using Unity.Entities;
//
// namespace SparFlame.Systems.SubGameplay.Interact
// {
//     [BurstCompile]
//     public partial struct HpRegeneratingSystemPlus : ISystem
//     {
//         [BurstCompile]
//         public void OnCreate(ref SystemState state)
//         {
//             state.RequireForUpdate<SubGameStatusData>();
//             state.RequireForUpdate<HpRegenerationConfig>();
//             state.RequireForUpdate<WorldTimeData>();
//         }
//
//
//         [BurstCompile]
//         public void OnUpdate(ref SystemState state)
//         {
//             var subGameStatusData = SystemAPI.GetSingleton<SubGameStatusData>();
//             // Only regenerate hp in player city
//             if (subGameStatusData.SubGameStatus != SubGameStatus.PlayerCity) return;
//
//             var curTotalHours = SystemAPI.GetSingleton<WorldTimeData>().totalHours;
//
//             var config = SystemAPI.GetSingleton<HpRegenerationConfig>();
//             float deltaHours;
//
//             // Regenerate hp for army group units
//             foreach (var (timer, entity) in SystemAPI.Query<RefRW<HpRegenerateTimer>>().WithAll<InSubGameTag>()
//                          .WithEntityAccess())
//             {
//                 deltaHours = curTotalHours - timer.ValueRO.lastCheckTotalHours;
//                 var percent = deltaHours * config.unitPercentPerHour;
//                 if (percent < 0.01) continue;
//                 timer.ValueRW.lastCheckTotalHours = curTotalHours;
//                 foreach (var unit in SystemAPI.GetBuffer<ArmyGroupUnit>(entity))
//                 {
//                     if(!SystemAPI.HasComponent<StatData>(unit.Unit))continue;
//                     var stat = SystemAPI.GetComponent<StatData>(unit.Unit);
//                     var amount = (int)(stat.maxValue * percent);
//                     stat.curValue += amount;
//                     if (stat.curValue > stat.maxValue)
//                     {
//                         stat.curValue = stat.maxValue;
//                     }
//
//                     SystemAPI.SetComponent(unit.Unit, stat);
//                 }
//             }
//
//             var cityTimer = SystemAPI.GetComponent<HpRegenerateTimer>(subGameStatusData.City);
//             deltaHours = curTotalHours - cityTimer.lastCheckTotalHours ;
//             if (deltaHours > 1)
//             {
//                 cityTimer.lastCheckTotalHours = curTotalHours;
//                 SystemAPI.SetComponent(subGameStatusData.City, cityTimer);
//                 state.Dependency = new CityUnitHpRegenerateJob
//                 {
//                     DeltaHours = deltaHours,
//                     Config = config
//                 }.ScheduleParallel(state.Dependency);
//             }
//         }
//
//
//         [BurstCompile]
//         [WithNone(typeof(InArmyGroup))]
//         public partial struct CityUnitHpRegenerateJob : IJobEntity
//         {
//             [ReadOnly] public float DeltaHours;
//             [ReadOnly] public HpRegenerationConfig Config;
//
//             private void Execute(ref StatData stat, in SubGameplayGeneralAttr generalAttr)
//             {
//                 var speed = generalAttr.BaseTag == BaseTag.Units
//                     ? Config.unitPercentPerHour
//                     : Config.buildingPercentPerHour;
//                 var amount = (int)(stat.maxValue * DeltaHours * speed);
//                 stat.curValue += amount;
//                 if (stat.curValue > stat.maxValue)
//                 {
//                     stat.curValue = stat.maxValue;
//                 }
//             }
//         }
//     }
// }