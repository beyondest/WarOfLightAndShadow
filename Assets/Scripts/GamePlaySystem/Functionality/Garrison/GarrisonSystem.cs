using SparFlame.GamePlaySystem.Building;
using SparFlame.GamePlaySystem.General;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Physics;
using Unity.Transforms;

namespace SparFlame.GamePlaySystem.Garrison
{
    [BurstCompile]
    public partial struct GarrisonSystem : ISystem
    {
        private NativeHashSet<Entity> _alreadyTagged;
        private ComponentLookup<GarrisonAttr> _garrisonAttrLookup;
        private ComponentLookup<GarrisonStateTag> _garrisonStateTagLookup;
        private ComponentLookup<LocalTransform> _localTransformLookup;
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<EndSimulationEntityCommandBufferSystem.Singleton>();
            state.RequireForUpdate<NotPauseTag>();
            state.RequireForUpdate<GarrisonSystemConfig>();
            _garrisonAttrLookup = state.GetComponentLookup<GarrisonAttr>(true);
            _garrisonStateTagLookup = state.GetComponentLookup<GarrisonStateTag>(true);
            _localTransformLookup = state.GetComponentLookup<LocalTransform>();

            _alreadyTagged = new NativeHashSet<Entity>(16,Allocator.Persistent);
        }
        
        
        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var config = SystemAPI.GetSingleton<GarrisonSystemConfig>();
            var ecb = new EntityCommandBuffer(Allocator.Temp);
            var ecbSingleton = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>();
            var ecbP = ecbSingleton.CreateCommandBuffer(state.WorldUnmanaged).AsParallelWriter();
            
            DealGarrisonInBuildingRequest(ref state, ecb, config);
            DealGarrisonMoveOutCommand(ref state, ecb, config);
            DealGarrisonUnitDieRequest(ref state, ecb, config);
            ecb.Playback(state.EntityManager);
            ecb.Dispose();
            _garrisonAttrLookup.Update(ref state);
            _garrisonStateTagLookup.Update(ref state);
            _localTransformLookup.Update(ref state);
            new GarrisonGetOutJob
            {
                Config = config,
                ECB = ecbP,
                GarrisonAttrLookup = _garrisonAttrLookup,
                GarrisonStateTagLookup = _garrisonStateTagLookup,
                LocalTransformLookup = _localTransformLookup,
            }.ScheduleParallel();
        }

        [BurstCompile]
        public void OnDestroy(ref SystemState state)
        {
            if(_alreadyTagged.IsCreated)
                _alreadyTagged.Dispose();
        }

        [BurstCompile]
        private void DealGarrisonUnitDieRequest(ref SystemState state, EntityCommandBuffer ecb,
            GarrisonSystemConfig config)
        {
            foreach (var (reqRo, entity) in SystemAPI.Query<RefRO<GarrisonUnitDieRequest>>().WithEntityAccess())
            {
                var request = reqRo.ValueRO;
                // Check if building is destroyed
                if (!SystemAPI.HasComponent<BuildingAttr>(request.BuildingEntity))
                {
                    ecb.DestroyEntity(entity);
                    continue;
                }

                var dataBuffer = SystemAPI.GetBuffer<GarrisonTypeData>(request.BuildingEntity);
                var entityBuffer = SystemAPI.GetBuffer<GarrisonEntity>(request.BuildingEntity);
                int i;
                for (i = 0; i < dataBuffer.Length; i++)
                {
                    var data = dataBuffer[i];
                    if (data.ID == request.Id)
                        break;
                }

                // ID not valid
                if (i == dataBuffer.Length)
                {
                    ecb.DestroyEntity(entity);
                    continue;
                }
                // Id valid, remove died unit
                var data2 = dataBuffer[i];
                data2.Count--;
                if (data2.Count == 0)
                {
                    dataBuffer.RemoveAt(i);
                }
                else
                {
                    dataBuffer[i] = data2;
                }
                for (var j = entityBuffer.Length - 1; j >= 0; j--)
                {
                    if (entityBuffer[j].Value == request.UnitEntity)
                        entityBuffer.RemoveAt(j);
                }

                // Remove bonus if count is lower than minimum request
                if (entityBuffer.Length < config.MinCountToTriggerDefenceBuff
                    && SystemAPI.HasComponent<UnderDefence>(request.BuildingEntity))
                    ecb.RemoveComponent<UnderDefence>(request.BuildingEntity);
                ecb.DestroyEntity(entity);
            }
        }

        [BurstCompile]
        private void DealGarrisonMoveOutCommand(ref SystemState state, EntityCommandBuffer ecb,
            GarrisonSystemConfig config)
        {
            _alreadyTagged.Clear();
            foreach (var (commandRo, entity) in SystemAPI.Query<RefRO<GarrisonMoveOutCommand>>().WithEntityAccess())
            {
                var command = commandRo.ValueRO;
                // Check if building is destroyed. The garrison state machine will move unit out if they are still in building
                if (!SystemAPI.HasComponent<BuildingAttr>(command.BuildingEntity))
                {
                    ecb.DestroyEntity(entity);
                    continue;
                }

                var entityBuffer = SystemAPI.GetBuffer<GarrisonEntity>(command.BuildingEntity);
                var dataBuffer = SystemAPI.GetBuffer<GarrisonTypeData>(command.BuildingEntity);
                if (entityBuffer.Length == 0)
                {
                    ecb.DestroyEntity(entity);
                    continue;
                }


                // Check if move out all units 
                if (command.MoveOutAll)
                {
                    foreach (var garrisonEntity in entityBuffer)
                    {
                        // Move out garrison units
                        if (_alreadyTagged.Add(garrisonEntity.Value))
                        {
                            ecb.AddComponent<GarrisonGetOut>(garrisonEntity.Value);
                        }
                    }
                    // Clear count , buff, buffer, continue
                    entityBuffer.Clear();
                    dataBuffer.Clear();
                    if (SystemAPI.HasComponent<UnderDefence>(command.BuildingEntity))
                        ecb.RemoveComponent<UnderDefence>(command.BuildingEntity);
                    ecb.DestroyEntity(entity);
                    continue;
                }

                // Get Garrison data
                int i;
                for (i = 0; i < dataBuffer.Length; i++)
                {
                    var data = dataBuffer[i];
                    if (data.ID == command.MoveOutUnitId)
                        break;
                }

                // command unit id not valid, continue 
                if (i == dataBuffer.Length)
                {
                    ecb.DestroyEntity(entity);
                    continue;
                }
                var data2 = dataBuffer[i];
                // Move out all same id
                if (command.MoveOutAllSameId)
                {
                    dataBuffer.RemoveAt(i);
                    for (var j = entityBuffer.Length - 1; j >= 0; j--)
                    {
                        if (entityBuffer[j].Id != command.MoveOutUnitId) continue;
                        var garrisonEntity = entityBuffer[j];
                        // Move out garrison units
                        if (_alreadyTagged.Add(garrisonEntity.Value))
                        {
                            ecb.AddComponent<GarrisonGetOut>(garrisonEntity.Value);
                        }
                        entityBuffer.RemoveAt(j);
                    }
                }
                // Move out only one unit
                else
                {
                    data2.Count--;
                    if (data2.Count == 0)
                        dataBuffer.RemoveAt(i);
                    else
                        dataBuffer[i] = data2;
                    for (var j = entityBuffer.Length - 1; j >= 0; j--)
                    {
                        var garrisonEntity = entityBuffer[j];
                        if (garrisonEntity.Id != command.MoveOutUnitId) continue;
                        // Move out garrison units
                        if (_alreadyTagged.Add(garrisonEntity.Value))
                        {
                            ecb.AddComponent<GarrisonGetOut>(garrisonEntity.Value);
                        }
                        entityBuffer.RemoveAt(j);
                        break;
                    }
                }
                ecb.DestroyEntity(entity);
                // if counts lower than trigger count, remove bonus
                if (entityBuffer.Length < config.MinCountToTriggerDefenceBuff
                    && SystemAPI.HasComponent<UnderDefence>(command.BuildingEntity))
                    ecb.RemoveComponent<UnderDefence>(command.BuildingEntity);
            }
        }

        [BurstCompile]
        private void DealGarrisonInBuildingRequest(ref SystemState state, EntityCommandBuffer ecb,
            in GarrisonSystemConfig config)
        {
            foreach (var (inReq, entity) in SystemAPI.Query<RefRO<GarrisonInBuildingRequest>>().WithEntityAccess())
            {
                var inRequest = inReq.ValueRO;
                // Safety Check
                if (!SystemAPI.HasComponent<BuildingAttr>(inRequest.BuildingEntity))
                {
                    // Although this should not happen, but add to make sure safe
                    ecb.DestroyEntity(entity);
                    continue;
                }

                var buildingAttr = SystemAPI.GetComponent<BuildingAttr>(inRequest.BuildingEntity);
                var garrisonDatas = SystemAPI.GetBuffer<GarrisonTypeData>(inRequest.BuildingEntity);
                var garrisonEntities = SystemAPI.GetBuffer<GarrisonEntity>(inRequest.BuildingEntity);
                int i;
                // Add unit type count if this unit type already exists
                for (i = 0; i < garrisonDatas.Length; i++)
                {
                    var data = garrisonDatas[i];
                    if (data.ID == inRequest.Id)
                    {
                        break;
                    }
                }

                // If not exists, add this unit type
                if (i == garrisonDatas.Length)
                {
                    garrisonDatas.Add(new GarrisonTypeData
                    {
                        Count = 1,
                        ID = inRequest.Id,
                        UnitType = inRequest.UnitType,
                    });
                }
                else
                {
                    var data = garrisonDatas[i];
                    data.Count++;
                    garrisonDatas[i] = data;
                }

                // Add to buffer
                garrisonEntities.Add(new GarrisonEntity
                {
                    Id = inRequest.Id,
                    Value = inRequest.UnitEntity
                });

                // Fortification building will gain under defence buff if count exceeds
                if (garrisonEntities.Length >= config.MinCountToTriggerDefenceBuff
                    && buildingAttr.Type == BuildingType.Fortifications
                    && !SystemAPI.HasComponent<UnderDefence>(inRequest.BuildingEntity))
                {
                    var attackAbility = SystemAPI.GetComponent<AttackAbility>(inRequest.BuildingEntity);
                    ecb.AddComponent(entity, new UnderDefence
                    {
                        RangeSq = attackAbility.RangeSq
                    });
                }
                ecb.DestroyEntity(entity);
            }
        }

        [BurstCompile]
        [WithAll(typeof(GarrisonGetOut))]
        private partial struct GarrisonGetOutJob : IJobEntity
        {
            
            [NativeDisableParallelForRestriction] public ComponentLookup<LocalTransform> LocalTransformLookup;
            [ReadOnly] public ComponentLookup<GarrisonStateTag> GarrisonStateTagLookup;
            [ReadOnly] public ComponentLookup<GarrisonAttr> GarrisonAttrLookup;
            [ReadOnly] public GarrisonSystemConfig Config;
            public EntityCommandBuffer.ParallelWriter ECB;

            private void Execute([ChunkIndexInQuery] int index, ref PhysicsMass mass,
                ref InGarrison inGarrison, Entity selfEntity)
            {
                ref var transform = ref LocalTransformLookup.GetRefRW(selfEntity).ValueRW;
                var buildingTransform = LocalTransformLookup[inGarrison.BuildingEntity];
                if (GarrisonStateTagLookup.IsComponentEnabled(selfEntity))
                {
                    GarrisonUtils.PosGetOut(ref inGarrison, ref transform, buildingTransform,
                        GarrisonAttrLookup[inGarrison.BuildingEntity], ref mass, Config, false);
                }
                ECB.SetComponentEnabled<GarrisonStateTag>(index, selfEntity, false);
                ECB.SetComponentEnabled<IdleStateTag>(index, selfEntity, true);
                ECB.RemoveComponent<InGarrison>(index, selfEntity);
                ECB.RemoveComponent<GarrisonGetOut>(index, selfEntity);
            }
        }
    }
}