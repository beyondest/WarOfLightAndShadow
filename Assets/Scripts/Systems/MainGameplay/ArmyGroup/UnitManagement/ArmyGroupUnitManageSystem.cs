using SparFlame.Components.MainGameplay;
using SparFlame.Components.SubGameplay;
using SparFlame.Core.Utils;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;

// ReSharper disable Unity.Entities.SingletonMustBeRequested

namespace SparFlame.Systems.MainGameplay.ArmyGroup
{
    public partial struct ArmyGroupUnitManageSystem : ISystem
    {
        private EntityQuery _removeRequestQuery;
        private EntityQuery _addRequestQuery;

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<ArmyGroupConfig>();
            _removeRequestQuery = SystemAPI.QueryBuilder().WithAll<RemoveFromArmyGroupRequest>().Build();
            _addRequestQuery = SystemAPI.QueryBuilder().WithAll<AddToArmyGroupRequest>().Build();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var ecb = new EntityCommandBuffer(Allocator.Temp);


            if (!_removeRequestQuery.IsEmpty)
                DealRemoveFromArmyGroupRequests(ref state, ecb);
            if(!_addRequestQuery.IsEmpty)
                DealAddToArmyGroupRequests(ref state, ecb);

            ecb.Playback(state.EntityManager);
            ecb.Dispose();
        }

        private void DealRemoveFromArmyGroupRequests(ref SystemState state, EntityCommandBuffer ecb)
        {
            var entities = _removeRequestQuery.ToEntityArray(Allocator.Temp);
            var requests = _removeRequestQuery.ToComponentDataArray<RemoveFromArmyGroupRequest>(Allocator.Temp);

            for (var k = 0; k < entities.Length; k++)
            {
                var entity = entities[k];
                var request = requests[k];
                ecb.DestroyEntity(entity);

                var entityBuffer = SystemAPI.GetBuffer<ArmyGroupUnit>(request.ArmyGroup);
                var dataBuffer = SystemAPI.GetBuffer<ArmyGroupUnitTypeData>(request.ArmyGroup);
                var armyGroupStatData = SystemAPI.GetComponent<ArmyGroupStatData>(request.ArmyGroup);

                if (entityBuffer.Length == 0)
                {
                    continue;
                }

                switch (request.RemoveType)
                {
                    case RemoveFromArmyGroupType.MoveOutAll:
                        foreach (var armyGroupUnit in entityBuffer)
                        {
                            ecb.RemoveComponent<InArmyGroup>(armyGroupUnit.Unit);
                        }
                        // Clear count , buff, buffer, continue
                        entityBuffer.Clear();
                        dataBuffer.Clear();
                        SystemAPI.SetComponent(request.ArmyGroup, new ArmyGroupStatData());
                        continue;
                    case RemoveFromArmyGroupType.RemoveSpecifiedUnitWithoutRemovingInArmyGroup: 
                    case RemoveFromArmyGroupType.MoveOutAllSameId:
                    case RemoveFromArmyGroupType.RandomRemoveSingleSameId:
                        int i;
                        for (i = 0; i < dataBuffer.Length; i++)
                        {
                            var data = dataBuffer[i];
                            if (data.Id == request.MoveOutId)
                                break;
                        }

                        if (i == dataBuffer.Length)
                        {
                            continue;
                        }

                        var data2 = dataBuffer[i];
                        // Move out all same id
                        if (request.RemoveType == RemoveFromArmyGroupType.MoveOutAllSameId)
                        {
                            dataBuffer.RemoveAt(i);
                            for (var j = entityBuffer.Length - 1; j >= 0; j--)
                            {
                                if (entityBuffer[j].GlobalId != request.MoveOutId) continue;
                                var garrisonEntity = entityBuffer[j];
                                var statData = SystemAPI.GetComponent<StatData>(garrisonEntity.Unit);
                                armyGroupStatData.totalCurrentHp -= statData.curValue;
                                armyGroupStatData.totalMaxHp -= statData.maxValue;
                                
                                // Move out garrison units
                                ecb.RemoveComponent<InArmyGroup>(garrisonEntity.Unit);
                                entityBuffer.RemoveAt(j);
                            }
                        }
                        else if (request.RemoveType == RemoveFromArmyGroupType.RandomRemoveSingleSameId)
                        {
                            data2.Count--;
                            if (data2.Count == 0)
                                dataBuffer.RemoveAt(i);
                            else
                                dataBuffer[i] = data2;
                            for (var j = entityBuffer.Length - 1; j >= 0; j--)
                            {
                                var armyGroupUnit = entityBuffer[j];
                                if (armyGroupUnit.GlobalId != request.MoveOutId) continue;
                                // Move out garrison units
                                ecb.RemoveComponent<InArmyGroup>(armyGroupUnit.Unit);
                                entityBuffer.RemoveAt(j);
                                
                                // Calculate hp info
                                var statData = SystemAPI.GetComponent<StatData>(armyGroupUnit.Unit);
                                armyGroupStatData.totalCurrentHp -= statData.curValue;
                                armyGroupStatData.totalMaxHp -= statData.maxValue;
                                break;
                            }
                        }
                        else // Remove single specified unit
                        {
                            data2.Count--;
                            if (data2.Count == 0)
                                dataBuffer.RemoveAt(i);
                            else
                                dataBuffer[i] = data2;
                            for (var j = entityBuffer.Length - 1; j >= 0; j--)
                            {
                                var armyGroupUnit = entityBuffer[j];
                                if (armyGroupUnit.Unit == request.Unit)
                                {
                                    // Safety check, avoid unit dead remove 
                                    if (SystemAPI.HasComponent<StatData>(armyGroupUnit.Unit))
                                    {
                                        var statData = SystemAPI.GetComponent<StatData>(armyGroupUnit.Unit);
                                        armyGroupStatData.totalCurrentHp -= statData.curValue;
                                        armyGroupStatData.totalMaxHp -= statData.maxValue;
                                    }
                                    else
                                    {
                                        armyGroupStatData.totalMaxHp -= request.StatMaxValue;
                                    }
                                    entityBuffer.RemoveAt(j);
                                    break;
                                }
                            }
                        }
                        
                        SystemAPI.SetComponent(request.ArmyGroup, armyGroupStatData);

                        break;

                    default:
                        BurstSafe.UnexpectedEnum(request.RemoveType);
                        break;
                }

            }
        }

        private void DealAddToArmyGroupRequests(ref SystemState state, EntityCommandBuffer ecb)
        {
            var entities = _addRequestQuery.ToEntityArray(Allocator.Temp);
            var requests = _addRequestQuery.ToComponentDataArray<AddToArmyGroupRequest>(Allocator.Temp);
            for (int i = 0; i < entities.Length; i++)
            {
                var request = requests[i];
                ecb.DestroyEntity(entities[i]);
                var datas = SystemAPI.GetBuffer<ArmyGroupUnitTypeData>(request.ArmyGroup);
                var units = SystemAPI.GetBuffer<ArmyGroupUnit>(request.ArmyGroup);
                var armyGroupStatData = SystemAPI.GetComponent<ArmyGroupStatData>(request.ArmyGroup);

                switch (request.Type)
                {
                    case AddToArmyGroupType.AllSelectedExceptAlreadyIn:
                    case AddToArmyGroupType.AllSelectedOverrideAlreadyIn:
                        foreach (var (generalAttr, unitAttr,statData, unit) in SystemAPI
                                     .Query<RefRO<SubGameplayGeneralAttr>, RefRO<UnitAttr>,
                                     RefRO<StatData>>().WithAll<Selected>()
                                     .WithEntityAccess())
                        {
                            if (SystemAPI.HasComponent<InArmyGroup>(unit))
                            {
                                if (request.Type == AddToArmyGroupType.AllSelectedOverrideAlreadyIn)
                                {
                                    ref var inArmyGroup = ref SystemAPI.GetComponentRW<InArmyGroup>(unit).ValueRW;
                                    // The unit has army group already in the same army group, then do nothing
                                    if (inArmyGroup.BelongsTo == request.ArmyGroup) continue;
                                    var removeRequest = ecb.CreateEntity();
                                    ecb.AddComponent(removeRequest, new RemoveFromArmyGroupRequest
                                    {
                                        ArmyGroup = inArmyGroup.BelongsTo,
                                        Unit = unit,
                                        RemoveType = RemoveFromArmyGroupType
                                            .RemoveSpecifiedUnitWithoutRemovingInArmyGroup,
                                        MoveOutId = generalAttr.ValueRO.PrefabID
                                    });
                                    inArmyGroup.BelongsTo = request.ArmyGroup;
                                }
                                else
                                {
                                    continue;
                                }
                            }
                            else
                            {
                                ecb.AddComponent(unit, new InArmyGroup
                                {
                                    BelongsTo = request.ArmyGroup,
                                });
                            }
                            // Add to buffer
                            units.Add(new ArmyGroupUnit
                            {
                                Unit = unit,
                                GlobalId = generalAttr.ValueRO.PrefabID
                            });
                            armyGroupStatData.totalCurrentHp += statData.ValueRO.curValue;
                            armyGroupStatData.totalMaxHp += statData.ValueRO.maxValue;
                            FindAndAddOneTypeDatas(ref datas, generalAttr.ValueRO, unitAttr.ValueRO);
                        }
                        SystemAPI.SetComponent(request.ArmyGroup, armyGroupStatData);

                        break;
                    case AddToArmyGroupType.OnlySpecifiedUnit:
                        if(!SystemAPI.HasComponent<SubGameplayGeneralAttr>(request.Unit))continue;
                        var subGameplayGeneralAttr = SystemAPI.GetComponent<SubGameplayGeneralAttr>(request.Unit);
                        var uAttr = SystemAPI.GetComponent<UnitAttr>(request.Unit);
                        var uStatData = SystemAPI.GetComponent<StatData>(request.Unit);
                        
                        FindAndAddOneTypeDatas(ref datas, subGameplayGeneralAttr, uAttr);
                        units.Add(new ArmyGroupUnit
                        {
                            Unit = request.Unit,
                            GlobalId = subGameplayGeneralAttr.PrefabID
                        });
                        
                        armyGroupStatData.totalCurrentHp += uStatData.curValue;
                        armyGroupStatData.totalMaxHp += uStatData.maxValue;
                        SystemAPI.SetComponent(request.ArmyGroup, armyGroupStatData);
                        break;
                    default:
                        BurstSafe.UnexpectedEnum(request.Type);
                        break;
                }
            }
        }

        private static void FindAndAddOneTypeDatas(ref DynamicBuffer<ArmyGroupUnitTypeData> datas, SubGameplayGeneralAttr generalAttr,
            UnitAttr unitAttr)
        {
            int j;
            // Add unit type count if this unit type already exists
            for (j = 0; j < datas.Length; j++)
            {
                var data = datas[j];
                if (data.Id == generalAttr.PrefabID)
                {
                    break;
                }
            }
            // If not exists, add this unit type
            if (j == datas.Length)
            {
                datas.Add(new ArmyGroupUnitTypeData
                {
                    UnitType = unitAttr.Type,
                    Id = generalAttr.PrefabID,
                    Count = 1
                });
            }
            else
            {
                var data = datas[j];
                data.Count++;
                datas[j] = data;
            }
        }
    }
}