using SparFlame.Components.General;
using SparFlame.Components.MainGameplay;
using SparFlame.Components.SubGameplay;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
// ReSharper disable Unity.Entities.SingletonMustBeRequested

namespace SparFlame.Systems.MainGameplay.ArmyGroup
{
    public partial struct ArmyGroupManageSystem : ISystem
    {
        private EntityQuery _removeRequestQuery;

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<ArmyGroupManageConfig>();
            _removeRequestQuery = SystemAPI.QueryBuilder().WithAll<RemoveFromArmyGroupRequest>().Build();
            
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var ecb = new EntityCommandBuffer(Allocator.Temp);


            if (!_removeRequestQuery.IsEmpty)
            {
                DealRemoveFromArmyGroupRequests(ref state, ecb);
            }

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

                // Check if building is destroyed. The garrison state machine will move unit out if they are still in building
                if (!SystemAPI.HasBuffer<ArmyGroupUnitTypeData>(request.ArmyGroup))
                {
                    continue;
                }

                var entityBuffer = SystemAPI.GetBuffer<ArmyGroupUnit>(request.ArmyGroup);
                var dataBuffer = SystemAPI.GetBuffer<ArmyGroupUnitTypeData>(request.ArmyGroup);
                if (entityBuffer.Length == 0)
                {
                    continue;
                }

                switch (request.Type)
                {
                    case RemoveFromArmyGroupType.MoveOutAll:
                        foreach (var armyGroupUnit in entityBuffer)
                        {
                            ecb.RemoveComponent<InArmyGroup>(armyGroupUnit.Unit);
                        }

                        // Clear count , buff, buffer, continue
                        entityBuffer.Clear();
                        dataBuffer.Clear();
                        ecb.DestroyEntity(entity);
                        continue;
                    case RemoveFromArmyGroupType.RemoveSpecifiedUnit:
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
                        if (request.Type == RemoveFromArmyGroupType.MoveOutAllSameId)
                        {
                            dataBuffer.RemoveAt(i);
                            for (var j = entityBuffer.Length - 1; j >= 0; j--)
                            {
                                if (entityBuffer[j].Id != request.MoveOutId) continue;
                                var garrisonEntity = entityBuffer[j];
                                // Move out garrison units
                                ecb.RemoveComponent<InArmyGroup>(garrisonEntity.Unit);
                                entityBuffer.RemoveAt(j);
                            }
                        }
                        else if(request.Type == RemoveFromArmyGroupType.RandomRemoveSingleSameId)
                        {
                            data2.Count--;
                            if (data2.Count == 0)
                                dataBuffer.RemoveAt(i);
                            else
                                dataBuffer[i] = data2;
                            for (var j = entityBuffer.Length - 1; j >= 0; j--)
                            {
                                var armyGroupUnit = entityBuffer[j];
                                if (armyGroupUnit.Id != request.MoveOutId) continue;
                                // Move out garrison units
                                ecb.RemoveComponent<InArmyGroup>(armyGroupUnit.Unit);
                                entityBuffer.RemoveAt(j);
                                break;
                            }
                        }
                        else
                        {
                            data2.Count--;
                            if (data2.Count == 0)
                                dataBuffer.RemoveAt(i);
                            else
                                dataBuffer[i] = data2;
                            for (var j =entityBuffer.Length - 1; j >= 0; j--)
                            {
                                var armyGroupUnit = entityBuffer[j];
                                if (armyGroupUnit.Unit == request.Unit)
                                {
                                    entityBuffer.RemoveAt(j);
                                    break;
                                }
                            }
                        }
                        break;
                    
                }
            }
        }

    }
}