using SparFlame.Components.General;
using SparFlame.Components.MainGameplay;
using SparFlame.Components.VFX;
using SparFlame.Systems.General.Battle;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
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
            state.RequireForUpdate<ArmyGroupGarrisonRequest>();
            state.RequireForUpdate<ArmyGroupGarrisonSystemConfig>();
            _requestQuery = SystemAPI.QueryBuilder().WithAll<ArmyGroupGarrisonRequest>().Build();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
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


                // ArmyGroup garrison in 
                if (request.IfGarrisonIn)
                {
                    // Add to buffer
                    garrisonEntities.Add(new CityGarrisonEntity
                    {
                        ArmyGroup = request.ArmyGroup
                    });

                    ecb.AddComponent(request.ArmyGroup, new ArmyGroupInGarrison
                    {
                        City = request.City,
                    });
                    var transform = SystemAPI.GetComponent<LocalTransform>(request.ArmyGroup);

                    // Reassign loading center and loading scale
                    var pos = transform.Position;
                    var gridIndex =
                        BattleUtils.GetClosestGrids(pos, SystemAPI.GetComponent<LocalTransform>(request.City));
                    var loadingInfos = SystemAPI.GetBuffer<LoadingGridInfo>(request.City);
                    var loadingInfo = loadingInfos[gridIndex];
                    var armyGroupAttr = SystemAPI.GetComponent<ArmyGroupAttr>(request.ArmyGroup);
                    armyGroupAttr.loadingCenter = loadingInfo.innerCenter;
                    var maxDelta = math.max(armyGroupAttr.boundingBoxDelta.x, armyGroupAttr.boundingBoxDelta.y);
                    armyGroupAttr.loadingScale = loadingInfo.innerSize == 0 ? 1 : loadingInfo.innerSize / maxDelta;
                    armyGroupAttr.loadingScale = math.min(1, armyGroupAttr.loadingScale);
                    ecb.SetComponent(request.ArmyGroup, armyGroupAttr);

                    // Hide the garrison army group entity
                    transform.Position += config.hidePositionBias;
                    ecb.SetComponent(request.ArmyGroup, transform);
                    
                    // Remove the selected state
                    ecb.SetComponentEnabled<ArmyGroupSelected>(request.ArmyGroup, false);
                    var vfxRequest = ecb.CreateEntity();
                    ecb.AddComponent<MainGameplayEntityTag>(vfxRequest);
                    ecb.AddComponent(vfxRequest,  new VFXRequest
                    {
                        RequestType = VFXRequestType.Kill,
                        VFXTrackTarget = request.ArmyGroup,
                        VFXName = VFXName.ArmyGroupSelectionIndicator,

                    });
                }
                // ArmyGroup garrison out
                else
                {
                    for (var j = garrisonEntities.Length - 1; j >= 0; j--)
                    {
                        var garrisonEntity = garrisonEntities[j];

                        if (garrisonEntity.ArmyGroup != request.ArmyGroup) continue;

                        garrisonEntities.RemoveAt(j);
                        if (SystemAPI.HasComponent<ArmyGroupInGarrison>(garrisonEntity.ArmyGroup))
                        {
                            ecb.RemoveComponent<ArmyGroupInGarrison>(garrisonEntity.ArmyGroup);
                            // Show the get out army group entity
                            var transform = SystemAPI.GetComponent<LocalTransform>(garrisonEntity.ArmyGroup);
                            transform.Position -= config.hidePositionBias;
                            ecb.SetComponent(garrisonEntity.ArmyGroup, transform);
                        }
                        break;
                    }
                }
            }
        }
    }
}