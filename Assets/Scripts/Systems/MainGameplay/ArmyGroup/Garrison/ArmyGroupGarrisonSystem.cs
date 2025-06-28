using SparFlame.Components.General;
using SparFlame.Components.MainGameplay;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Transforms;

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
            if (gameStatus != GameStatus.MainGaming && gameStatus != GameStatus.SubGaming) return;
            var ecb = new EntityCommandBuffer(Allocator.Temp);


            DealGarrisonRequest(ref state, ecb);


            ecb.Playback(state.EntityManager);
            ecb.Dispose();
        }

        private void DealGarrisonRequest(ref SystemState state, EntityCommandBuffer ecb)
        {
            var entities = _requestQuery.ToEntityArray(Allocator.Temp);
            var requests = _requestQuery.ToComponentDataArray<ArmyGroupGarrisonRequest>(Allocator.Temp);
            var config = SystemAPI.GetSingleton<ArmyGroupGarrisonSystemConfig>();

            for (var k = 0; k < entities.Length; k++)
            {
                var entity = entities[k];
                var request = requests[k];
                ecb.DestroyEntity(entity);

                var garrisonEntities = SystemAPI.GetBuffer<CityGarrisonEntity>(request.City);
                var garrisonDatas = SystemAPI.GetBuffer<CityGarrisonTypeData>(request.City);


                int i;
                // Add unit type count if this unit type already exists
                for (i = 0; i < garrisonDatas.Length; i++)
                {
                    var data = garrisonDatas[i];
                    if (data.iconType == request.IconType)
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
                            count = 1,
                            iconType = request.IconType
                        });
                    }
                    else
                    {
                        var data = garrisonDatas[i];
                        data.count++;
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
                    // Hide the garrison army group entity
                    var transform = SystemAPI.GetComponent<LocalTransform>(request.ArmyGroup);
                    transform.Position += config.hidePositionBias;
                    ecb.SetComponent(request.ArmyGroup, transform);
                }
                // ArmyGroup garrison out
                else
                {
                    if (i == garrisonDatas.Length)
                        continue;
                    var data = garrisonDatas[i];
                    if (request.IfGarrisonOutAllSameIcon)
                        data.count = 0;
                    else
                        data.count--;
                    if (data.count == 0)
                        garrisonDatas.RemoveAt(i);
                    else
                    {
                        garrisonDatas[i] = data;
                    }

                    var removeSpecifiedEntity = request.ArmyGroup != Entity.Null;

                    for (var j = garrisonEntities.Length - 1; j >= 0; j--)
                    {
                        var garrisonEntity = garrisonEntities[j];
                        if (removeSpecifiedEntity)
                        {
                            if ( garrisonEntity.ArmyGroup != request.ArmyGroup) continue;
                        }
                        else
                        {
                            if(SystemAPI.GetComponent<ArmyGroupAttr>(garrisonEntity.ArmyGroup).iconType != request.IconType) continue;
                        }
                        garrisonEntities.RemoveAt(j);
                        if (SystemAPI.HasComponent<ArmyGroupInGarrison>(garrisonEntity.ArmyGroup))
                        {
                            ecb.RemoveComponent<ArmyGroupInGarrison>(garrisonEntity.ArmyGroup);
                            // Show the get out army group entity
                            var transform = SystemAPI.GetComponent<LocalTransform>(garrisonEntity.ArmyGroup);
                            transform.Position -= config.hidePositionBias;
                            ecb.SetComponent(garrisonEntity.ArmyGroup, transform);
                        }
                        if (!request.IfGarrisonOutAllSameIcon) break;
                    }
                }
            }
        }
    }
}