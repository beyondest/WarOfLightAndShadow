using SparFlame.Components.General;
using SparFlame.Components.MainGameplay;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;

// ReSharper disable Unity.Entities.SingletonMustBeRequested

namespace SparFlame.Systems.MainGameplay.ArmyGroup
{
    public partial struct ArmyGroupGarrisonSystem : ISystem
    {
        private EntityQuery _requestQuery;

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<GameStatusData>();
            state.RequireForUpdate<ArmyGroupGarrisonRequest>();
            state.RequireForUpdate<ArmyGroupGarrisonSystemConfig>();
            _requestQuery = SystemAPI.QueryBuilder().WithAll<ArmyGroupGarrisonRequest>().Build();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var gameStatus = SystemAPI.GetSingleton<GameStatusData>().Value;
            if(gameStatus != GameStatus.MainGaming && gameStatus != GameStatus.SubGaming)return;
            var ecb = new EntityCommandBuffer(Allocator.Temp);

            
            DealGarrisonRequest(ref state, ecb);


            ecb.Playback(state.EntityManager);
            ecb.Dispose();
        }

        private void DealGarrisonRequest(ref SystemState state, EntityCommandBuffer ecb)
        {
            var entities = _requestQuery.ToEntityArray(Allocator.Temp);
            var requests = _requestQuery.ToComponentDataArray<ArmyGroupGarrisonRequest>(Allocator.Temp);

            for (var k = 0; k < entities.Length; k++)
            {
                var entity = entities[k];
                var request = requests[k];
                ecb.DestroyEntity(entity);


                var garrisonEntities = SystemAPI.GetBuffer<CityGarrisonEntity>(request.City);
                var garrisonDatas = SystemAPI.GetBuffer<CityGarrisonTypeData>(request.City);
                if (garrisonEntities.Length == 0)
                {
                    continue;
                }


                int i;
                // Add unit type count if this unit type already exists
                for (i = 0; i < garrisonDatas.Length; i++)
                {
                    var data = garrisonDatas[i];
                    if (data.IconType == request.IconType)
                    {
                        break;
                    }
                }

                // ArmyGroup garrison in 
                if (request.IfGarrisonIn)
                {
                    // If not exists, add this unit type
                    if (i == garrisonDatas.Length)
                    {
                        garrisonDatas.Add(new CityGarrisonTypeData
                        {
                            Count = 1,
                            IconType = request.IconType
                        });
                    }
                    else
                    {
                        var data = garrisonDatas[i];
                        data.Count++;
                        garrisonDatas[i] = data;
                    }

                    // Add to buffer
                    garrisonEntities.Add(new CityGarrisonEntity
                    {
                        ArmyGroup = request.ArmyGroup
                    });
                    ecb.AddComponent(request.ArmyGroup, new ArmyGroupInGarrison
                    {
                        City = request.City,
                    });
                }
                // ArmyGroup garrison out
                else
                {
                    if (i == garrisonDatas.Length)
                        continue;
                    var data = garrisonDatas[i];
                    data.Count--;
                    if (data.Count == 0)
                        garrisonDatas.RemoveAt(i);
                    else
                    {
                        garrisonDatas[i] = data;
                    }

                    for (var j = garrisonEntities.Length - 1; j >= 0; j--)
                    {
                        var garrisonEntity = garrisonEntities[j];
                        if (garrisonEntity.ArmyGroup != request.ArmyGroup) continue;
                        garrisonEntities.RemoveAt(j);
                        break;
                    }

                    if (SystemAPI.HasComponent<ArmyGroupInGarrison>(request.ArmyGroup))
                    {
                        ecb.RemoveComponent<ArmyGroupInGarrison>(request.ArmyGroup);
                    }
                }
            }
        }
    }
}