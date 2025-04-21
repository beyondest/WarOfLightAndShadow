using Unity.Entities;
using Unity.Transforms;
using Unity.Mathematics;
using SparFlame.GamePlaySystem.General;
using SparFlame.GamePlaySystem.Ooc;
using SparFlame.GamePlaySystem.Units;
using Unity.Burst;
using Unity.Collections;

namespace SparFlame.GamePlaySystem.Spawn
{
    [BurstCompile]
    [UpdateBefore(typeof(TransformSystemGroup))]
    public partial struct ConjureSystem : ISystem
    {
        private ComponentLookup<UnitAttr> _unitAttrLookup;
        private NativeHashSet<Entity> _alreadyTagged;
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<EndSimulationEntityCommandBufferSystem.Singleton>();
            state.RequireForUpdate<NotPauseTag>();
            state.RequireForUpdate<ConjureSystemConfig>();
            _unitAttrLookup = state.GetComponentLookup<UnitAttr>(true);
            _alreadyTagged = new NativeHashSet<Entity>(16,Allocator.Persistent);
        }

        [BurstCompile]
        public void OnDestroy(ref SystemState state)
        {
            if(_alreadyTagged.IsCreated)
                _alreadyTagged.Dispose();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var ecbSingleton = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>();
            var ecb = new EntityCommandBuffer(Allocator.Temp);
            var ecbP = ecbSingleton.CreateCommandBuffer(state.WorldUnmanaged).AsParallelWriter();
            // var config = SystemAPI.GetSingleton<ConjureSystemConfig>();
            
            CheckConjureUnitsRequest(ref state, ecb);
            ecb.Playback(state.EntityManager);
            ecb.Dispose();
            _unitAttrLookup.Update(ref state);
            new ConjureJob
            {
                ECB = ecbP,
                DeltaTime = SystemAPI.Time.DeltaTime,
                UnitAttrLookup = _unitAttrLookup,
            }.ScheduleParallel();
        }

        private void CheckConjureUnitsRequest(ref SystemState state, EntityCommandBuffer ecb)
        {
            _alreadyTagged.Clear();
            foreach (var (ro, entity) in SystemAPI.Query<RefRO<ConjureRequest>>().WithEntityAccess())
            {
                var request = ro.ValueRO;
                // Safety check
                if (!SystemAPI.HasBuffer<ConjuringData>(request.BuildingEntity))
                {
                    ecb.DestroyEntity(entity);
                    continue;
                }

                var buffer = SystemAPI.GetBuffer<ConjuringData>(request.BuildingEntity);
                if (_alreadyTagged.Add(request.BuildingEntity))
                    ecb.AddComponent<ConjuringTag>(request.BuildingEntity);
                // Add task to building buffer
                var timeCost = request.Count * SystemAPI.GetComponent<UnitAttr>(request.UnitPrefab)
                    .ConjureSpeedSecondPerUnit;
                int i;
                for (i = 0; i < buffer.Length; i++)
                {
                    var data = buffer[i];
                    if (data.ConjuringEntity == request.UnitPrefab)
                    {
                        break;
                    }
                }
                if (i == buffer.Length)
                {
                    buffer.Add(new ConjuringData
                    {
                        ConjuringEntity = request.UnitPrefab,
                        TargetAmount = request.Count,
                        ConjuredAmount = 0,
                        Counter = 0,
                        RemainingTimeSeconds = timeCost
                    });
                }
                else
                {
                    var data = buffer[i];
                    data.ConjuredAmount += request.Count;
                    data.RemainingTimeSeconds += timeCost;
                    buffer[i] = data;
                }

                ecb.DestroyEntity(entity);
            }
        }


        [BurstCompile]
        [WithNone(typeof(OocTag))]
        [WithAll(typeof(ConjuringTag))]
        private partial struct ConjureJob : IJobEntity
        {
            public EntityCommandBuffer.ParallelWriter ECB;
            public float DeltaTime;
            [ReadOnly] public ComponentLookup<UnitAttr> UnitAttrLookup;

            private void Execute([ChunkIndexInQuery] int index, in ConjureAttr conjureAttr,
                ref DynamicBuffer<ConjuringData> conjuringData,
                in LocalTransform transform, in GeneralAttr generalAttr,
                Entity entity)
            {
                if (conjuringData.Length == 0)
                {
                    ECB.RemoveComponent<ConjuringTag>(index, entity);
                    return;
                }
                var data = conjuringData[0];
                var speed = 1 / UnitAttrLookup[data.ConjuringEntity].ConjureSpeedSecondPerUnit;
                data.Counter += DeltaTime * speed;
                data.RemainingTimeSeconds -= DeltaTime;
                conjuringData[0] = data;
                // Conjure unit when time arrived
                if (data.Counter >= 1)
                {
                    var unit = ECB.Instantiate(index, data.ConjuringEntity);
                    var transformCopy = transform;
                    var pos = transformCopy.TransformPoint(conjureAttr.ConjurePositionBias);
                    // var pos = transform.Position + conjureAttr.ConjurePositionBias;
                    ECB.SetComponent(index, unit, new LocalTransform
                    {
                        Position = pos, // TODO : Make sure unit can never be stuck when it is conjured
                        Rotation = quaternion.identity,
                        Scale = 1f
                    });
                    data.Counter = 0;
                    data.ConjuredAmount++;
                    if (data.ConjuredAmount >= data.TargetAmount)
                    {
                        conjuringData.RemoveAt(0);
                    }
                    else
                    {
                        conjuringData[0] = data;
                    }
                }
                
            }
        }
    }
}